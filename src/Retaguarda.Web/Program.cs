using System.Globalization;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Localization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using FluentValidation;
using Microsoft.Extensions.Options;
using Retaguarda.AspNetCore.Health;
using Retaguarda.AspNetCore.Identity;
using Retaguarda.AspNetCore.Security;
using Retaguarda.Business.Exporting;
using Retaguarda.Business.Notifications;
using Retaguarda.Business.Roles;
using Retaguarda.Business.Users;
using Retaguarda.Business.Sites;
using Retaguarda.Business.Sites.Validators;
using Retaguarda.Data.Identity;
using Retaguarda.Data.Interceptors;
using Retaguarda.Data.Repositories;
using Retaguarda.Printing;
using Retaguarda.Reporting;
using Retaguarda.Shared;
using Retaguarda.Shared.Contracts;
using Retaguarda.Web.Infrastructure;
using Serilog;

// Bootstrap logger: captura erros de startup antes do host estar pronto.
// Será substituído pelo logger completo via builder.Host.UseSerilog logo abaixo.
#pragma warning disable CA1305 // WriteTo.Console() é config de sink — não formata string com locale; IFormatProvider é descartado pelo Serilog.
Log.Logger = new LoggerConfiguration()
    .WriteTo.Console()
    .CreateBootstrapLogger();
#pragma warning restore CA1305

try
{
    Log.Information("Web application starting");

    var builder = WebApplication.CreateBuilder(args);

    // Oculta o header "Server: Kestrel" das respostas — não vazar tecnologia do backend.
    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

    // Serilog: configuração lida de appsettings.json (seção Serilog) + enrichers programáticos.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Localização. pt-BR é a única cultura suportada nesta versão;
    // a infraestrutura aceita novos idiomas sem alterar código (basta adicionar .resx).
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

    builder.Services.AddControllersWithViews(options =>
        {
            // Desliga o [Required] inferido para tipos de referência não-anuláveis: ele geraria
            // mensagens padrão EM INGLÊS antes da nossa validação. A validação fica a cargo do
            // FluentValidation (localizado via .resx), fonte única e multi-idioma.
            options.SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true;

            // Resolve o fuso da planta ativa antes da action: as views convertem datas de forma
            // síncrona e não podem esperar por uma consulta.
            options.Filters.Add<SiteTimeZoneFilter>();
        })
        .AddViewLocalization()
        .AddDataAnnotationsLocalization(options =>
            // Mensagens de validação (DataAnnotations) resolvidas a partir de SharedResources.
            options.DataAnnotationLocalizerProvider = (type, factory) =>
                factory.Create(typeof(SharedResources)));

    // Mensagens do model binder em pt-BR (ex.: campo numérico limpo → "O valor '' é inválido.").
    // O Suppress...NonNullableReferenceTypes acima só cobre tipos de referência; tipos de valor
    // (int, DateTime) caem no model binder, cujo padrão é em inglês.
    builder.Services.AddSingleton<IConfigureOptions<MvcOptions>, LocalizedModelBindingMessages>();

    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        var supported = new[] { new CultureInfo("pt-BR") };
        options.DefaultRequestCulture = new RequestCulture("pt-BR");
        options.SupportedCultures = supported;
        options.SupportedUICultures = supported;

        // Provedor único: cookie. Em web admin, troca de idioma (quando vier) será via cookie.
        options.RequestCultureProviders.Clear();
        options.RequestCultureProviders.Add(new CookieRequestCultureProvider());
    });

    // Usuário corrente (auditoria e filtro multi-site). Precisa do HttpContext, por isso a
    // implementação vive em Retaguarda.AspNetCore.
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();

    // Interceptor que carimba auditoria e aplica soft delete a cada SaveChanges.
    builder.Services.AddScoped<AuditableEntityInterceptor>();

    // EF Core: ApplicationDbContext apontando para o PostgreSQL (connection string vem de User Secrets em dev
    // e de variável de ambiente ConnectionStrings__DefaultConnection em produção).
    builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        options
            .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
            .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

    // ASP.NET Core Identity com política de senha mínima de 8 caracteres + complexidade.
    builder.Services.AddIdentity<ApplicationUser, ApplicationRole>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;

        options.User.RequireUniqueEmail = true;
    })
    .AddEntityFrameworkStores<ApplicationDbContext>()
    .AddSignInManager<ApplicationSignInManager>()  // bloqueia login de usuários inativos
    .AddDefaultTokenProviders();

    // Cookie de autenticação: caminhos das telas e expiração da sessão.
    builder.Services.ConfigureApplicationCookie(options =>
    {
        options.LoginPath = "/Account/Login";
        options.LogoutPath = "/Account/Logout";
        options.AccessDeniedPath = "/Account/AccessDenied";
        options.ExpireTimeSpan = TimeSpan.FromHours(8); // ~uma jornada de trabalho
        options.SlidingExpiration = true;
    });

    builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    {
        options.OnRefreshingPrincipal = context =>
        {
            ActiveSiteClaims.PreserveFromCurrentPrincipal(context.CurrentPrincipal, context.NewPrincipal);
            return Task.CompletedTask;
        };
    });

    // Data Protection: em container, persistir as chaves num volume evita invalidar cookies de
    // autenticação e tokens antiforgery a cada restart. O caminho vem de configuração
    // (DataProtection:KeysPath); em dev local sem essa config, usa o store default do SO.
    var dataProtectionKeysPath = builder.Configuration["DataProtection:KeysPath"];
    if (!string.IsNullOrWhiteSpace(dataProtectionKeysPath))
    {
        builder.Services.AddDataProtection()
            .PersistKeysToFileSystem(new DirectoryInfo(dataProtectionKeysPath))
            .SetApplicationName("Retaguarda.Web");
    }

    // Envio de e-mail (recuperação de senha). Em Development, sender que registra o link no log
    // (sem SMTP). Fora de Development, placeholder que falha de forma explícita até um sender
    // SMTP real ser implementado.
    if (builder.Environment.IsDevelopment())
    {
        builder.Services.AddScoped<IEmailSender, DevelopmentEmailSender>();
    }
    else
    {
        builder.Services.AddScoped<IEmailSender, NotConfiguredEmailSender>();
    }

    // CRUD de Site (planta): repositório, serviço e validadores (FluentValidation).
    builder.Services.AddScoped<ISiteRepository, SiteRepository>();
    builder.Services.AddScoped<ISiteService, SiteService>();
    builder.Services.AddValidatorsFromAssemblyContaining<CreateSiteRequestValidator>();

    // CRUD de Role: repositório e serviço. Os validadores já são varridos pelo
    // AddValidatorsFromAssemblyContaining acima (mesma assembly Retaguarda.Business).
    builder.Services.AddScoped<IRoleRepository, RoleRepository>();
    builder.Services.AddScoped<IRoleService, RoleService>();

    // Configurações da planta ativa (hoje, o fuso de exibição). Scoped porque o serviço memoriza
    // o valor durante a requisição — várias camadas o consultam no mesmo request.
    builder.Services.AddScoped<ISiteSettingsRepository, SiteSettingsRepository>();
    builder.Services.AddScoped<ISiteSettingsService, SiteSettingsService>();

    // Fuso de exibição da planta ativa: resolvido uma vez por requisição pelo SiteTimeZoneFilter e
    // consumido por controllers e views (injetado no _ViewImports).
    builder.Services.AddScoped<SiteTimeZone>();
    builder.Services.AddScoped<SiteTimeZoneFilter>();

    // Exportação (Excel/PDF): exportadores genéricos e stateless. Excel no Reporting, PDF no
    // Printing. Referência de uso: SitesController.Export.
    builder.Services.AddSingleton<IExcelExporter, ClosedXmlExcelExporter>();
    builder.Services.AddSingleton<IPdfExporter, QuestPdfExporter>();

    // CRUD de User: repositório e serviço. Inclui o sub-CRUD de plantas do usuário.
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<IUserService, UserService>();
    builder.Services.AddScoped<IUserSiteService, UserSiteService>();
    builder.Services.AddScoped<ISiteSelectionService, SiteSelectionService>();

    // Health checks: /health (liveness) e /health/ready (readiness, inclui o Postgres).
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database", tags: [DatabaseHealthCheck.ReadyTag]);

    var app = builder.Build();

    // Atrás do proxy reverso (Caddy) e da Cloudflare: aplica esquema/IP reais (X-Forwarded-*) ANTES
    // de tudo, para que HTTPS, cookies Secure, redirect e logs fiquem corretos. Os apps só são
    // acessíveis via Caddy na rede interna do Docker; por isso confiamos nos proxies (listas limpas).
    // Ver docs/deploy.md.
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    };
    forwardedOptions.KnownIPNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedOptions);

    // PRIMEIRO middleware: headers de segurança aplicam a TODAS as respostas
    // (inclusive arquivos estáticos, redirects, páginas de erro).
    // CSP — style-src precisa de 'unsafe-inline' porque Bootstrap 5 seta atributos style="..."
    // em runtime para Collapse/Modal/Dropdown/Tooltip; script-src permanece estrito.
    app.UseSecurityHeaders(options =>
    {
        options.ContentSecurityPolicy =
            "default-src 'self'; " +
            "script-src 'self'; " +
            "style-src 'self' 'unsafe-inline'; " +
            "font-src 'self'; " +
            "img-src 'self' data:; " +
            "connect-src 'self'; " +
            "frame-ancestors 'none'; " +
            "base-uri 'self'; " +
            "form-action 'self'; " +
            "object-src 'none'";
    });

    // RequestLocalization precisa vir cedo no pipeline, antes de qualquer middleware
    // que possa precisar da cultura corrente (incluindo MVC e logging de request).
    app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

    // Configure the HTTP request pipeline.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler("/Home/Error");
        // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
        app.UseHsts();
    }

    app.UseHttpsRedirection();
    app.UseRouting();

    // Log estruturado de cada requisição HTTP (latência, status, rota). Depois de UseRouting
    // para que a rota matched já apareça no log.
    app.UseSerilogRequestLogging();

    // Autenticação precisa vir antes de autorização.
    app.UseAuthentication();
    app.UseAuthorization();

    // Usuário autenticado sem planta ativa é levado à tela de seleção.
    app.UseMiddleware<ActiveSiteSelectionMiddleware>();

    app.MapStaticAssets();

    app.MapControllerRoute(
        name: "default",
        pattern: "{controller=Home}/{action=Index}/{id?}")
        .WithStaticAssets();

    // Liveness: o processo está de pé (sem checar dependências — não reinicia por blip do DB).
    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
    // Readiness: apto a servir tráfego (inclui o Postgres). Consumido por probes de readiness.
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains(DatabaseHealthCheck.ReadyTag),
    });

    // Seed de desenvolvimento: garante planta e usuário iniciais para testar a autenticação.
    if (app.Environment.IsDevelopment())
    {
        using var scope = app.Services.CreateScope();
        await DevelopmentDataSeeder.SeedAsync(scope.ServiceProvider);
    }
    else
    {
        // Produção: aplica as migrations pendentes no startup. Só o Web migra (a Api não), evitando
        // corrida entre os dois processos. Depois garante o admin inicial (do .env). Ver docs/deploy.md.
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
        await db.Database.MigrateAsync();
        await ProductionDataSeeder.SeedAsync(scope.ServiceProvider);
    }

    app.Run();

    return 0;
}
// HostAbortedException é disparada pelo `dotnet ef` (design-time) e não é falha real —
// não capturar evita logar "terminated unexpectedly" indevidamente. Para falhas reais de
// startup, retornar 1 propaga exit code ≠ 0 (o orquestrador detecta em vez de achar que subiu).
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Web application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
