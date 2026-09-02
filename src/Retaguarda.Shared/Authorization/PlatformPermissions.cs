namespace Retaguarda.Shared.Authorization;

/// <summary>
/// Permissões da plataforma (o que a base entrega: Plantas, Usuários e Acessos). Um projeto
/// derivado NÃO edita esta classe: ele cria o próprio <see cref="IPermissionProvider"/> com as
/// permissões do domínio dele.
///
/// As constantes existem para serem usadas em <c>[Authorize(Policy = ...)]</c> — string solta no
/// atributo é erro de digitação esperando acontecer (o <c>PermissionConventionTests</c> quebra o
/// build se aparecer uma que não está no catálogo).
/// </summary>
public static class PlatformPermissions
{
    public static class Sites
    {
        public const string Resource = "sites";

        public const string View = "sites.view";
        public const string Create = "sites.create";
        public const string Edit = "sites.edit";
        public const string Delete = "sites.delete";
        public const string Export = "sites.export";
    }

    public static class Users
    {
        public const string Resource = "users";

        public const string View = "users.view";
        public const string Create = "users.create";
        public const string Edit = "users.edit";
        public const string Delete = "users.delete";
    }

    /// <summary>
    /// Vínculo usuário↔planta (<c>UserSitesController</c>). Recurso próprio, e não uma permissão
    /// solta dentro de Usuários: a tela tem exatamente Listar, Criar e Excluir, e "só listar" é um
    /// caso real (conferir em quais plantas alguém está, sem poder mexer). Não tem editar porque o
    /// vínculo não se edita — cria-se ou remove-se.
    /// </summary>
    public static class UserSites
    {
        public const string Resource = "usersites";

        public const string View = "usersites.view";
        public const string Create = "usersites.create";
        public const string Delete = "usersites.delete";
    }

    public static class Roles
    {
        public const string Resource = "roles";

        public const string View = "roles.view";
        public const string Create = "roles.create";
        public const string Edit = "roles.edit";
        public const string Delete = "roles.delete";
    }

    /// <summary>Provider das permissões da plataforma, registrado pela própria base.</summary>
    public sealed class Provider : IPermissionProvider
    {
        public IEnumerable<PermissionDefinition> GetPermissions() =>
        [
            new(Sites.View, Sites.Resource),
            new(Sites.Create, Sites.Resource),
            new(Sites.Edit, Sites.Resource),
            new(Sites.Delete, Sites.Resource),
            new(Sites.Export, Sites.Resource),

            new(Users.View, Users.Resource),
            new(Users.Create, Users.Resource),
            new(Users.Edit, Users.Resource),
            new(Users.Delete, Users.Resource),

            new(UserSites.View, UserSites.Resource),
            new(UserSites.Create, UserSites.Resource),
            new(UserSites.Delete, UserSites.Resource),

            new(Roles.View, Roles.Resource),
            new(Roles.Create, Roles.Resource),
            new(Roles.Edit, Roles.Resource),
            new(Roles.Delete, Roles.Resource),
        ];
    }
}
