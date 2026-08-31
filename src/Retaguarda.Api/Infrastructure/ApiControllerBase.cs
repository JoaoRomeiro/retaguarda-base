using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Retaguarda.Api.Contracts;

namespace Retaguarda.Api.Infrastructure;

/// <summary>
/// Base dos controllers da API. Padroniza o contrato de resposta: sucesso no envelope
/// <see cref="ApiResponse{T}"/> e erro em ProblemDetails (RFC 9457) com um <c>code</c>
/// estável (igual à chave do .resx) para o cliente ramificar sem ambiguidade.
/// </summary>
[ApiController]
public abstract class ApiControllerBase : ControllerBase
{
    // Mesmo traceId usado pelo ProblemDetails de erro: correlação ponta a ponta.
    protected string CurrentTraceId => Activity.Current?.Id ?? HttpContext.TraceIdentifier;

    // Resposta 2xx padronizada.
    protected OkObjectResult OkData<T>(T data)
        => Ok(new ApiResponse<T>(data, new ApiResponseMeta(CurrentTraceId)));

    // Resposta de erro padronizada: ProblemDetails + extensão "code" (estável, = chave .resx).
    // A mensagem (title) já vem localizada do controller.
    protected IActionResult ProblemWithCode(int statusCode, string code, string title)
    {
        var problem = ProblemDetailsFactory.CreateProblemDetails(
            HttpContext, statusCode: statusCode, title: title);
        problem.Extensions["code"] = code;

        return new ObjectResult(problem)
        {
            StatusCode = statusCode,
            ContentTypes = { "application/problem+json" },
        };
    }
}
