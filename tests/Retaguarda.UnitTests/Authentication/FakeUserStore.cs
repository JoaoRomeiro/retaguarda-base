using Microsoft.AspNetCore.Identity;
using Retaguarda.Data.Identity;

namespace Retaguarda.UnitTests.Authentication;

/// <summary>
/// Store mínimo para construir um <see cref="UserManager{TUser}"/> real nos testes do
/// <c>AuthenticationService</c> — o UserManager é classe concreta e não dá para substituir por
/// interface (e o projeto não usa biblioteca de mocking).
/// </summary>
/// <remarks>
/// ESCOPO DELIBERADAMENTE PEQUENO: implementa só o que os fluxos testados hoje usam
/// (<c>FindByIdAsync</c> e <c>GetRolesAsync</c>). Todo o resto lança
/// <see cref="NotSupportedException"/> de propósito — quem for testar login, lockout ou troca de
/// senha vai bater na exceção e saberá exatamente o que precisa implementar, em vez de receber
/// um null silencioso e um teste que passa por engano.
/// </remarks>
internal sealed class FakeUserStore : IUserStore<ApplicationUser>, IUserRoleStore<ApplicationUser>
{
    private readonly Dictionary<string, ApplicationUser> _users = [];
    private readonly Dictionary<string, List<string>> _rolesByUser = [];

    public ApplicationUser Add(string id, bool isActive = true, params string[] roles)
    {
        var user = new ApplicationUser
        {
            Id = id,
            UserName = $"{id}@teste.local",
            Email = $"{id}@teste.local",
            FullName = "Usuário de teste",
            IsActive = isActive,
        };

        _users[id] = user;
        _rolesByUser[id] = [.. roles];
        return user;
    }

    // --- IUserStore: só o que é usado ---

    public Task<ApplicationUser?> FindByIdAsync(string userId, CancellationToken cancellationToken)
        => Task.FromResult(_users.GetValueOrDefault(userId));

    public Task<string> GetUserIdAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.Id);

    public Task<string?> GetUserNameAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult(user.UserName);

    // --- IUserRoleStore: só o que é usado ---

    public Task<IList<string>> GetRolesAsync(ApplicationUser user, CancellationToken cancellationToken)
        => Task.FromResult<IList<string>>(_rolesByUser.GetValueOrDefault(user.Id) ?? []);

    public void Dispose()
    {
        // Nada a liberar: tudo em memória.
    }

    // --- Não usado pelos fluxos cobertos hoje. Implementar sob demanda. ---

    private static NotSupportedException NotCovered([System.Runtime.CompilerServices.CallerMemberName] string member = "")
        => new($"FakeUserStore não implementa {member}. Implemente ao cobrir o fluxo que precisa dele.");

    public Task<IdentityResult> CreateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotCovered();
    public Task<IdentityResult> UpdateAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotCovered();
    public Task<IdentityResult> DeleteAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotCovered();
    public Task<ApplicationUser?> FindByNameAsync(string normalizedUserName, CancellationToken cancellationToken) => throw NotCovered();
    public Task<string?> GetNormalizedUserNameAsync(ApplicationUser user, CancellationToken cancellationToken) => throw NotCovered();
    public Task SetNormalizedUserNameAsync(ApplicationUser user, string? normalizedName, CancellationToken cancellationToken) => throw NotCovered();
    public Task SetUserNameAsync(ApplicationUser user, string? userName, CancellationToken cancellationToken) => throw NotCovered();
    public Task AddToRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) => throw NotCovered();
    public Task RemoveFromRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) => throw NotCovered();
    public Task<IList<ApplicationUser>> GetUsersInRoleAsync(string roleName, CancellationToken cancellationToken) => throw NotCovered();
    public Task<bool> IsInRoleAsync(ApplicationUser user, string roleName, CancellationToken cancellationToken) => throw NotCovered();
}
