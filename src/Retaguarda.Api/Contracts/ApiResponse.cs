namespace Retaguarda.Api.Contracts;

/// <summary>
/// Envelope padrão de SUCESSO da API (Path 2 do contrato): toda resposta 2xx tem a mesma
/// casca <c>{ "data": ..., "meta": { "traceId": ... } }</c>. Os erros usam ProblemDetails
/// (RFC 9457) com um <c>code</c> estável — ver ApiControllerBase.
/// </summary>
public sealed record ApiResponse<T>(T Data, ApiResponseMeta Meta);

// Metadados transversais a toda resposta de sucesso (correlação com logs/traces).
public sealed record ApiResponseMeta(string TraceId);
