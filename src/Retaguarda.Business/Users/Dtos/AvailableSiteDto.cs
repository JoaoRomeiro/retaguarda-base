namespace Retaguarda.Business.Users.Dtos;

// Planta disponível para associar a um usuário (opção do select de "Associar planta").
public sealed class AvailableSiteDto
{
    public int Id { get; set; }
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
}
