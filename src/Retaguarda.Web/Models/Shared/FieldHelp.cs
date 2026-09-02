namespace Retaguarda.Web.Models.Shared;

/// <summary>
/// Texto de ajuda de um campo de formulário, usado pelos partials <c>_FieldHelp</c> (tooltip no
/// rótulo) e <c>_FieldHint</c> (texto visível abaixo do campo). Ver `docs/padrao-ui.md` §8.2 para
/// a regra de qual dos dois usar.
/// </summary>
/// <param name="FieldId">
/// O <c>id</c> do campo descrito (o mesmo que o <c>asp-for</c> gera). Serve só para derivar um id
/// estável para o texto — o campo referencia esse id no <c>aria-describedby</c>.
/// </param>
/// <param name="Text">O texto de ajuda, já localizado.</param>
public sealed record FieldHelp(string FieldId, string Text)
{
    /// <summary>Id do elemento que carrega o texto; vai no <c>aria-describedby</c> do campo.</summary>
    public string DescriptionId => $"{FieldId}-help";
}
