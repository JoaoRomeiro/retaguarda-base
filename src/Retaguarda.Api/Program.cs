using System.Globalization;
using System.Security.Claims;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.Extensions.Localization;
using Retaguarda.Api.Authentication;
using Retaguarda.Api.Controllers;
using Retaguarda.Api.Infrastructure;
using Retaguarda.AspNetCore.Health;
using Retaguarda.AspNetCore.Identity;
using Retaguarda.AspNetCore.Security;
using Retaguarda.Business.Authentication;
using Retaguarda.Business.Users;
using Retaguarda.Data.Identity;
using Retaguarda.Data.Interceptors;
using Retaguarda.Data.Repositories;
using Retaguarda.Shared.Authentication;
using Retaguarda.Shared.Contracts;
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
    Log.Information("Api application starting");

    var builder = WebApplication.CreateBuilder(args);

    // Oculta o header "Server: Kestrel" das respostas — não vazar tecnologia do backend.
    builder.WebHost.ConfigureKestrel(options => options.AddServerHeader = false);

    // Serilog: configuração lida de appsettings.json (seção Serilog) + enrichers programáticos.
    builder.Host.UseSerilog((context, services, configuration) => configuration
        .ReadFrom.Configuration(context.Configuration)
        .ReadFrom.Services(services)
        .Enrich.FromLogContext());

    // Localização. pt-BR é a única cultura suportada nesta versão.
    // Na API, a cultura vem do header Accept-Language; sem ele, fallback pra pt-BR.
    builder.Services.AddLocalization(options => options.ResourcesPath = "Resources");

    builder.Services.Configure<RequestLocalizationOptions>(options =>
    {
        var supported = new[] { new CultureInfo("pt-BR") };
        options.DefaultRequestCulture = new RequestCulture("pt-BR");
        options.SupportedCultures = supported;
        options.SupportedUICultures = supported;

        // Provedor único: Accept-Language. Clients da API (mobile, etc.) enviam o header.
        options.RequestCultureProviders.Clear();
        options.RequestCultureProviders.Add(new AcceptLanguageHeaderRequestCultureProvider());
    });

    // ----- Autenticação (JWT) + acesso a dados -----

    // Usuário corrente a partir das claims do JWT — MESMA implementação do Web (cookie):
    // expõe UserId e a planta ativa (SiteId) para auditoria e Global Query Filter multi-site.
    builder.Services.AddHttpContextAccessor();
    builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
    builder.Services.AddScoped<AuditableEntityInterceptor>();

    // EF Core: mesmo ApplicationDbContext do Web (connection string de User Secrets em dev,
    // de ConnectionStrings__DefaultConnection em produção).
    builder.Services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        options
            .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
            .AddInterceptors(serviceProvider.GetRequiredService<AuditableEntityInterceptor>()));

    // Identity "core": UserManager/RoleManager + stores EF, SEM cookies (a API autentica via JWT).
    builder.Services.AddIdentityCore<ApplicationUser>(options =>
    {
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = true;
        options.User.RequireUniqueEmail = true;

        // Lockout por conta: mesmos valores do Web e do default do Identity, explícitos para
        // ficarem revisáveis. O AuthenticationService chama AccessFailedAsync a cada senha errada.
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;
    })
    .AddRoles<ApplicationRole>()
    .AddEntityFrameworkStores<ApplicationDbContext>();

    // Opções do JWT. A chave de assinatura NÃO é commitada: vem de User Secrets (dev)
    // ou da variável de ambiente JWT__SigningKey (produção).
    var jwtSection = builder.Configuration.GetSection(JwtOptions.SectionName);
    builder.Services.Configure<JwtOptions>(jwtSection);
    var jwtOptions = jwtSection.Get<JwtOptions>() ?? new JwtOptions();

    // Falha rápida de startup: HS256 exige uma chave de pelo menos 32 bytes.
    if (Encoding.UTF8.GetByteCount(jwtOptions.SigningKey) < 32)
    {
        throw new InvalidOperationException(
            "Jwt:SigningKey is missing or shorter than 32 bytes. Configure it via JWT__SigningKey.");
    }

    var signingKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtOptions.SigningKey));

    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            // Sem remapeamento: os tipos de claim ficam como emitidos (iguais aos do cookie do Web),
            // para o CurrentUserService compartilhado ler UserId/SiteId sem alteração.
            options.MapInboundClaims = false;
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidIssuer = jwtOptions.Issuer,
                // Audience do ACCESS token: o token de pré-autenticação usa outra audience e,
                // portanto, é recusado aqui (não acessa endpoints protegidos).
                ValidateAudience = true,
                ValidAudience = jwtOptions.Audience,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = signingKey,
                ClockSkew = TimeSpan.FromSeconds(30),
                NameClaimType = ClaimTypes.NameIdentifier,
                RoleClaimType = ClaimTypes.Role,
            };
        });

    // Fluxo de autenticação (Business) + dependências de dados.
    builder.Services.AddScoped<IUserRepository, UserRepository>();
    builder.Services.AddScoped<ISiteSelectionService, SiteSelectionService>();
    builder.Services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
    builder.Services.AddSingleton<ITokenService, JwtTokenService>();
    builder.Services.AddScoped<IAuthenticationService, AuthenticationService>();

    // Add services to the container. Filtro automático de ModelState desligado: os erros de
    // validação seguem o mesmo contrato dos demais (ProblemDetails com "code"). O
    // ValidationExceptionFilter traduz ValidationException (Business) → 400 ProblemDetails no
    // mesmo contrato, evitando 500 nos endpoints de escrita.
    builder.Services.AddControllers(options => options.Filters.Add<ValidationExceptionFilter>())
        .ConfigureApiBehaviorOptions(options => options.SuppressModelStateInvalidFilter = true);

    // Rate limiting dos endpoints anônimos de autenticação (política por IP em AuthRateLimiting).
    // A recusa segue o MESMO contrato dos demais erros: ProblemDetails + "code" = chave do .resx.
    builder.Services.AddAuthRateLimiter(async (context, cancellationToken) =>
    {
        AuthRateLimiting.ApplyRetryAfter(context);

        var httpContext = context.HttpContext;
        var localizer = httpContext.RequestServices.GetRequiredService<IStringLocalizer<AuthController>>();
        var factory = httpContext.RequestServices.GetRequiredService<ProblemDetailsFactory>();

        var problem = factory.CreateProblemDetails(
            httpContext,
            statusCode: StatusCodes.Status429TooManyRequests,
            title: localizer["too_many_requests"]);
        problem.Extensions["code"] = "too_many_requests";

        httpContext.Response.ContentType = "application/problem+json";
        await httpContext.Response.WriteAsJsonAsync(
            problem, options: null, contentType: "application/problem+json", cancellationToken);
    });

    // ProblemDetails (RFC 9457): respostas de erro estruturadas e previsíveis para os clientes.
    // Habilita corpo problem+json em exceções não tratadas e em status de erro sem corpo.
    builder.Services.AddProblemDetails();

    // Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
    builder.Services.AddOpenApi();

    // Health checks: /health (liveness) e /health/ready (readiness, inclui o Postgres).
    builder.Services.AddHealthChecks()
        .AddCheck<DatabaseHealthCheck>("database", tags: [DatabaseHealthCheck.ReadyTag]);

    var app = builder.Build();

    // Atrás do proxy reverso (Caddy) e da Cloudflare: aplica esquema/IP reais (X-Forwarded-*) ANTES
    // de tudo — esquema HTTPS e IP do cliente corretos nos logs. A API só é acessível via Caddy na
    // rede interna do Docker; por isso confiamos nos proxies (listas limpas).
    //
    // ATENÇÃO: listas limpas significam "confio no X-Forwarded-For venha de onde vier". Isso só é
    // seguro enquanto a porta da Api NÃO estiver publicada na internet. Se publicar, qualquer
    // cliente forja o IP: os logs passam a mentir e o rate limiting por IP (AuthRateLimiting)
    // vira decoração — basta trocar o cabeçalho a cada requisição para ter cota infinita.
    // Ver docs/deploy.md.
    var forwardedOptions = new ForwardedHeadersOptions
    {
        ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    };
    forwardedOptions.KnownIPNetworks.Clear();
    forwardedOptions.KnownProxies.Clear();
    app.UseForwardedHeaders(forwardedOptions);

    // PRIMEIRO middleware: headers de segurança aplicam a TODAS as respostas.
    // API responde JSON; nenhum recurso executável — CSP é mínima (default-src 'none').
    app.UseSecurityHeaders(options =>
    {
        options.ContentSecurityPolicy = "default-src 'none'; frame-ancestors 'none'; base-uri 'none'";
    });

    // RequestLocalization precisa vir cedo no pipeline.
    app.UseRequestLocalization(app.Services.GetRequiredService<IOptions<RequestLocalizationOptions>>().Value);

    // Tratamento global de erros → ProblemDetails. Em produção, UseExceptionHandler captura
    // exceções não tratadas e devolve problem+json sem vazar stack trace. Em Development, o
    // developer exception page (default do host) já retorna ProblemDetails p/ requisições não-HTML.
    if (!app.Environment.IsDevelopment())
    {
        app.UseExceptionHandler();
    }

    // Gera ProblemDetails para respostas de erro sem corpo (ex.: 404, 405).
    app.UseStatusCodePages();

    // Configure the HTTP request pipeline.
    if (app.Environment.IsDevelopment())
    {
        app.MapOpenApi();
    }

    // Sem UseHttpsRedirection na API: a terminação TLS é feita no proxy reverso / host
    // (on-premise). Redirect HTTP→HTTPS não é obedecido por clientes não-browser e seria
    // no-op aqui (a API escuta só HTTP).

    // Log estruturado de cada requisição HTTP (latência, status, rota).
    app.UseSerilogRequestLogging();

    // Autenticação (JWT) antes da autorização.
    app.UseAuthentication();
    app.UseAuthorization();

    // Rate limiting: depois do roteamento (precisa do endpoint para achar a política do
    // [EnableRateLimiting]) e antes das actions. Só os endpoints anotados são limitados —
    // /health e o resto passam livres.
    app.UseRateLimiter();

    app.MapControllers();
    // Liveness: o processo está de pé (sem checar o banco). Readiness (/health/ready) inclui o Postgres.
    app.MapHealthChecks("/health", new HealthCheckOptions { Predicate = _ => false });
    app.MapHealthChecks("/health/ready", new HealthCheckOptions
    {
        Predicate = check => check.Tags.Contains(DatabaseHealthCheck.ReadyTag),
    });

    app.Run();

    return 0;
}
// HostAbortedException é disparada pelo `dotnet ef` (design-time) e não é falha real —
// não capturar evita logar "terminated unexpectedly" indevidamente. Para falhas reais de
// startup, retornar 1 propaga exit code ≠ 0 (o orquestrador detecta em vez de achar que subiu).
catch (Exception ex) when (ex is not HostAbortedException)
{
    Log.Fatal(ex, "Api application terminated unexpectedly");
    return 1;
}
finally
{
    Log.CloseAndFlush();
}
