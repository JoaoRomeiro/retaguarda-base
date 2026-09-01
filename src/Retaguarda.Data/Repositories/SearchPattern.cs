namespace Retaguarda.Data.Repositories;

/// <summary>
/// Monta o padrão de busca "contém" usado pelas listagens dos cadastros.
/// Ponto único: os repositórios não devem concatenar <c>%termo%</c> à mão.
/// </summary>
/// <remarks>
/// O termo vem do usuário e por isso precisa ser tratado como TEXTO LITERAL. Sem escapar,
/// os curingas do LIKE/ILIKE vazam para a consulta: <c>%</c> casa com qualquer coisa (buscar
/// "%" traria a tabela inteira), <c>_</c> casa com um caractere qualquer, e um <c>\</c> solto
/// forma sequência de escape inválida. Não é risco de injeção — o valor continua sendo
/// parâmetro —, é resultado errado e varredura desnecessária no banco.
/// </remarks>
public static class SearchPattern
{
    /// <summary>
    /// Caractere de escape do padrão. É o default do PostgreSQL, mas vai explícito na consulta
    /// (<c>EF.Functions.ILike(campo, padrão, EscapeCharacter)</c>) para não depender do default.
    /// </summary>
    public const string EscapeCharacter = "\\";

    /// <summary>
    /// Converte o termo digitado no padrão <c>%termo%</c>, com os curingas escapados.
    /// A barra é escapada primeiro, senão escaparia as barras adicionadas depois.
    /// </summary>
    public static string Contains(string term)
    {
        ArgumentNullException.ThrowIfNull(term);

        var escaped = term
            .Trim()
            .Replace("\\", "\\\\", StringComparison.Ordinal)
            .Replace("%", "\\%", StringComparison.Ordinal)
            .Replace("_", "\\_", StringComparison.Ordinal);

        return $"%{escaped}%";
    }
}
