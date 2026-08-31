using FluentValidation;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Localization;
using Retaguarda.Shared;

namespace Retaguarda.Api.Infrastructure;

/// <summary>
/// Converte <see cref="ValidationException"/> (FluentValidation, lançada pela camada Business via
/// <c>ValidateAndThrowAsync</c>) no MESMO contrato de erro da API: ProblemDetails (RFC 9457) com
/// status 400, extensão estável <c>code = "validation_error"</c> e os erros por campo em
/// <c>errors</c>. As mensagens dos validators são CHAVES de recurso, localizadas aqui via
/// <see cref="IStringLocalizer{T}"/> — o mesmo mecanismo que a Web usa. Sem este filtro, o
/// <c>UseExceptionHandler</c> transformaria a exceção em 500, quebrando o contrato nos endpoints
/// de escrita (ver parecer técnico, ponto C).
/// </summary>
public sealed class ValidationExceptionFilter : IExceptionFilter
{
    // code estável (= chave do .resx do título); o cliente ramifica por ele.
    private const string ValidationErrorCode = "validation_error";

    private readonly ProblemDetailsFactory _problemDetailsFactory;
    private readonly IStringLocalizer<SharedResources> _localizer;

    public ValidationExceptionFilter(
        ProblemDetailsFactory problemDetailsFactory,
        IStringLocalizer<SharedResources> localizer)
    {
        _problemDetailsFactory = problemDetailsFactory;
        _localizer = localizer;
    }

    public void OnException(ExceptionContext context)
    {
        if (context.Exception is not ValidationException exception)
        {
            return;
        }

        // Agrupa os erros por campo, localizando cada mensagem (ErrorMessage é a chave do .resx).
        var modelState = new ModelStateDictionary();
        foreach (var error in exception.Errors)
        {
            modelState.AddModelError(error.PropertyName, _localizer[error.ErrorMessage].Value);
        }

        var problem = _problemDetailsFactory.CreateValidationProblemDetails(
            context.HttpContext,
            modelState,
            statusCode: StatusCodes.Status400BadRequest,
            title: _localizer[ValidationErrorCode].Value);
        problem.Extensions["code"] = ValidationErrorCode;

        context.Result = new ObjectResult(problem)
        {
            StatusCode = StatusCodes.Status400BadRequest,
            ContentTypes = { "application/problem+json" },
        };
        context.ExceptionHandled = true;
    }
}
