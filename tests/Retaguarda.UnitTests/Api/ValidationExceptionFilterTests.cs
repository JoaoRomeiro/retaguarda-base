using System.Globalization;
using FluentValidation;
using FluentValidation.Results;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Localization;
using Retaguarda.Api.Infrastructure;
using Retaguarda.Shared;

namespace Retaguarda.UnitTests.Api;

public sealed class ValidationExceptionFilterTests
{
    [Fact]
    public void OnException_maps_validation_exception_to_400_problem_details()
    {
        var filter = CreateFilter();
        var exception = new ValidationException(
        [
            new ValidationFailure("Code", "site_code_required"),
            new ValidationFailure("Name", "site_name_required"),
        ]);
        var context = CreateExceptionContext(exception);

        filter.OnException(context);

        Assert.True(context.ExceptionHandled);
        var result = Assert.IsType<ObjectResult>(context.Result);
        Assert.Equal(StatusCodes.Status400BadRequest, result.StatusCode);

        var problem = Assert.IsType<ValidationProblemDetails>(result.Value);
        Assert.Equal("validation_error", Assert.Contains("code", problem.Extensions));
        // Título e mensagens localizados (o fake prefixa "loc:" para evidenciar a tradução).
        Assert.Equal("loc:validation_error", problem.Title);
        Assert.Equal(["loc:site_code_required"], problem.Errors["Code"]);
        Assert.Equal(["loc:site_name_required"], problem.Errors["Name"]);
    }

    [Fact]
    public void OnException_localizes_messages_against_the_real_resx_in_pt_BR()
    {
        // Resolve um IStringLocalizer<SharedResources> REAL (resx embarcado no assembly Shared),
        // o mesmo mecanismo da Web/Api. Em runtime, a cultura vem do Accept-Language (default
        // pt-BR); aqui fixamos a cultura da thread para exercitar as chaves contra o .resx real.
        var previousCulture = CultureInfo.CurrentUICulture;
        CultureInfo.CurrentUICulture = new CultureInfo("pt-BR");
        try
        {
            using var provider = new ServiceCollection()
                .AddLogging()  // ResourceManagerStringLocalizerFactory depende de ILoggerFactory.
                .AddLocalization(options => options.ResourcesPath = "Resources")
                .BuildServiceProvider();
            var localizer = provider.GetRequiredService<IStringLocalizer<SharedResources>>();

            var filter = new ValidationExceptionFilter(new FakeProblemDetailsFactory(), localizer);
            var exception = new ValidationException(
            [
                new ValidationFailure("Code", "site_code_required"),
            ]);
            var context = CreateExceptionContext(exception);

            filter.OnException(context);

            var result = Assert.IsType<ObjectResult>(context.Result);
            var problem = Assert.IsType<ValidationProblemDetails>(result.Value);
            // Título e mensagem por campo vêm do .resx real (pt-BR), não das chaves cruas.
            Assert.Equal("Há erros de validação.", problem.Title);
            Assert.Equal(["Informe o código da planta."], problem.Errors["Code"]);
        }
        finally
        {
            CultureInfo.CurrentUICulture = previousCulture;
        }
    }

    [Fact]
    public void OnException_ignores_non_validation_exceptions()
    {
        var filter = CreateFilter();
        var context = CreateExceptionContext(new InvalidOperationException("boom"));

        filter.OnException(context);

        Assert.False(context.ExceptionHandled);
        Assert.Null(context.Result);
    }

    private static ValidationExceptionFilter CreateFilter()
        => new(new FakeProblemDetailsFactory(), new FakeLocalizer());

    private static ExceptionContext CreateExceptionContext(Exception exception)
    {
        var actionContext = new ActionContext(
            new DefaultHttpContext(),
            new RouteData(),
            new ActionDescriptor());

        return new ExceptionContext(actionContext, [])
        {
            Exception = exception,
        };
    }

    // Localizer falso: devolve a própria chave prefixada por "loc:" para confirmar nos testes
    // que a tradução foi aplicada (sem depender dos arquivos .resx reais).
    private sealed class FakeLocalizer : IStringLocalizer<SharedResources>
    {
        public LocalizedString this[string name] => new(name, $"loc:{name}", resourceNotFound: false);

        public LocalizedString this[string name, params object[] arguments]
            => new(name, $"loc:{name}", resourceNotFound: false);

        public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures) => [];
    }

    // Fábrica mínima: o filtro só precisa de CreateValidationProblemDetails; a outra sobrecarga
    // não é exercitada por este filtro.
    private sealed class FakeProblemDetailsFactory : ProblemDetailsFactory
    {
        public override ProblemDetails CreateProblemDetails(
            HttpContext httpContext,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new() { Status = statusCode, Title = title };

        public override ValidationProblemDetails CreateValidationProblemDetails(
            HttpContext httpContext,
            ModelStateDictionary modelStateDictionary,
            int? statusCode = null,
            string? title = null,
            string? type = null,
            string? detail = null,
            string? instance = null)
            => new(modelStateDictionary) { Status = statusCode, Title = title };
    }
}
