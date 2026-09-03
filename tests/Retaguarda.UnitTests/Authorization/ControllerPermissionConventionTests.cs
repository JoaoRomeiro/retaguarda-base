using System.Reflection;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Retaguarda.Shared.Authorization;
using Retaguarda.Web.Controllers;

namespace Retaguarda.UnitTests.Authorization;

/// <summary>
/// Garante que toda ação de cadastro exige permissão. Sem isto, uma action nova nasce liberada
/// para qualquer pessoa autenticada — não dá erro, não aparece no log, e só se descobre quando
/// alguém acessa o que não devia.
///
/// A regra é por exclusão: controller novo entra na conta automaticamente. Quem não tem permissão
/// (login, home, troca de planta) precisa estar na lista de isentos, com motivo.
/// </summary>
public sealed class ControllerPermissionConventionTests
{
    private static readonly PermissionCatalog Catalog = new([new PlatformPermissions.Provider()]);

    // Isentos, com o motivo. Nada aqui é cadastro: são as telas que a pessoa usa antes de ter
    // qualquer permissão, ou que valem para todo mundo que está logado.
    private static readonly Dictionary<Type, string> Exempt = new()
    {
        [typeof(AccountController)] = "login, perfil e recuperação de senha: anteriores a qualquer permissão",
        [typeof(HomeController)] = "página inicial e erro: valem para todo usuário autenticado",
        [typeof(SiteSelectionController)] = "troca da planta ativa: parte do próprio fluxo de login",
    };

    [Fact]
    public void Every_action_of_a_registration_controller_requires_a_permission()
    {
        var offenders = new List<string>();

        foreach (var controller in GuardedControllers())
        {
            foreach (var action in Actions(controller))
            {
                var policies = action
                    .GetCustomAttributes<AuthorizeAttribute>(inherit: true)
                    .Select(attribute => attribute.Policy)
                    .Where(policy => !string.IsNullOrWhiteSpace(policy))
                    .ToList();

                if (policies.Count == 0)
                {
                    offenders.Add($"{controller.Name}.{action.Name}: sem [Authorize(Policy = ...)]");
                    continue;
                }

                offenders.AddRange(policies
                    .Where(policy => !Catalog.Contains(policy!))
                    .Select(policy => $"{controller.Name}.{action.Name}: política \"{policy}\" não existe no catálogo"));
            }
        }

        Assert.True(
            offenders.Count == 0,
            "Toda ação de cadastro precisa de [Authorize(Policy = PlatformPermissions....)]. " +
            "Se o controller não é cadastro, declare-o em Exempt com o motivo.\n" + string.Join('\n', offenders));
    }

    [Fact]
    public void Registration_controllers_are_actually_found()
    {
        // Rede de segurança: sem isto, um namespace renomeado faria o teste acima passar vazio.
        Assert.NotEmpty(GuardedControllers());
        Assert.All(GuardedControllers(), controller => Assert.NotEmpty(Actions(controller)));
    }

    [Fact]
    public void Exempt_list_only_names_controllers_that_still_exist()
    {
        // Isenção órfã esconde o motivo real: se o controller sumiu, a linha tem de sumir junto.
        var missing = Exempt.Keys.Except(AllControllers()).Select(type => type.Name).ToList();
        Assert.True(missing.Count == 0, string.Join('\n', missing));
    }

    private static List<Type> AllControllers() =>
        [.. typeof(SitesController).Assembly
            .GetTypes()
            .Where(type => type.IsPublic && !type.IsAbstract && typeof(Controller).IsAssignableFrom(type))];

    private static List<Type> GuardedControllers() =>
        [.. AllControllers().Where(type => !Exempt.ContainsKey(type)).OrderBy(type => type.Name, StringComparer.Ordinal)];

    private static List<MethodInfo> Actions(Type controller) =>
        [.. controller
            .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
            .Where(method => !method.IsSpecialName)
            .Where(method => method.GetCustomAttribute<NonActionAttribute>() is null)];
}
