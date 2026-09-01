namespace Retaguarda.Shared;

/// <summary>
/// Papéis internos da plataforma. Só existe um: a base não tem domínio, e portanto
/// não tem papéis de negócio — cada projeto derivado cria os seus.
/// </summary>
public static class RetaguardaRoles
{
    // Acesso amplo: cadastros, usuários e papéis. Criado pelo seeder com IsSystem = true,
    // o que impede renomear ou excluir pelo cadastro de papéis (o código depende do nome).
    public const string Admin = "Admin";
}
