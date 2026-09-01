using FluentValidation;
using Retaguarda.Business.Users;
using Retaguarda.Business.Users.Dtos;
using Retaguarda.Business.Users.Validators;

namespace Retaguarda.UnitTests.Users;

public sealed class UserServiceTests
{
    private static FakeUserRepository BuildRepository()
    {
        var repo = new FakeUserRepository();
        repo.SeedRole("Picker");
        repo.SeedRole("Manager");
        repo.SeedSite(1);
        repo.SeedSite(2);
        return repo;
    }

    private static UserService BuildService(
        FakeUserRepository repository, FakeRefreshTokenRepository? refreshTokens = null) =>
        new(
            repository,
            refreshTokens ?? new FakeRefreshTokenRepository(),
            new CreateUserRequestValidator(repository),
            new UpdateUserRequestValidator(repository));

    private static CreateUserRequest ValidCreate(string email = "op@tibrasil.local") => new()
    {
        FullName = "Operador",
        Email = email,
        Password = "Abcd@123",
        RoleName = "Picker",
        DefaultSiteId = 1,
        IsActive = true,
    };

    [Fact]
    public async Task CreateAsync_persists_and_returns_dto()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var dto = await service.CreateAsync(ValidCreate());

        Assert.False(string.IsNullOrEmpty(dto.Id));
        Assert.Equal("op@tibrasil.local", dto.Email);
        Assert.Equal("Picker", dto.RoleName);
        Assert.Single(repo.Users);
    }

    [Fact]
    public async Task CreateAsync_associates_chosen_site_as_default()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var dto = await service.CreateAsync(ValidCreate());

        // A planta escolhida é a padrão e a única associação inicial.
        Assert.Equal(1, dto.DefaultSiteId);
        Assert.Equal([1], await repo.GetSiteIdsAsync(dto.Id));
        Assert.Equal("Picker", await repo.GetRoleNameAsync(dto.Id));
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_email()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        await service.CreateAsync(ValidCreate("dup@tibrasil.local"));

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(ValidCreate("dup@tibrasil.local")));
    }

    [Fact]
    public async Task CreateAsync_rejects_invalid_email()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var request = ValidCreate();
        request.Email = "not-an-email";

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_rejects_weak_password()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var request = ValidCreate();
        request.Password = "abc";

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_rejects_unknown_role()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var request = ValidCreate();
        request.RoleName = "Ghost";

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_rejects_unknown_site()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var request = ValidCreate();
        request.DefaultSiteId = 99;

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_returns_NotFound_when_user_missing()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        var result = await service.UpdateAsync(
            new UpdateUserRequest
            {
                Id = "missing",
                FullName = "X",
                RoleName = "Picker",
                DefaultSiteId = 1,
            },
            currentUserId: "admin");

        Assert.Equal(UserUpdateResult.NotFound, result);
    }

    [Fact]
    public async Task UpdateAsync_changes_role_and_default_among_linked_sites()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var created = await service.CreateAsync(ValidCreate());
        repo.LinkSite(created.Id, 2);  // planta 2 passa a estar associada

        var result = await service.UpdateAsync(
            new UpdateUserRequest
            {
                Id = created.Id,
                FullName = "Operador Sr.",
                RoleName = "Manager",
                DefaultSiteId = 2,
                IsActive = true,
            },
            currentUserId: "admin");

        Assert.Equal(UserUpdateResult.Updated, result);
        Assert.Equal("Manager", await repo.GetRoleNameAsync(created.Id));
        Assert.Equal(2, repo.Users[0].DefaultSiteId);
    }

    [Fact]
    public async Task UpdateAsync_rejects_default_site_not_linked()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var created = await service.CreateAsync(ValidCreate());  // associado só à planta 1

        await Assert.ThrowsAsync<ValidationException>(() => service.UpdateAsync(
            new UpdateUserRequest
            {
                Id = created.Id,
                FullName = "Operador",
                RoleName = "Picker",
                DefaultSiteId = 2,  // não associada
                IsActive = true,
            },
            currentUserId: "admin"));
    }

    // --- Desativação encerra as sessões abertas (item 1 da avaliação técnica) ---

    [Fact]
    public async Task UpdateAsync_deactivating_user_regenerates_stamp_and_revokes_refresh_tokens()
    {
        var repo = BuildRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var service = BuildService(repo, refreshTokens);
        var created = await service.CreateAsync(ValidCreate());
        refreshTokens.SeedActive(created.Id, "hash-sessao-api");
        var stampBefore = repo.Users[0].SecurityStamp;

        var result = await service.UpdateAsync(
            new UpdateUserRequest
            {
                Id = created.Id,
                FullName = "Operador",
                RoleName = "Picker",
                DefaultSiteId = 1,
                IsActive = false,
            },
            currentUserId: "admin");

        Assert.Equal(UserUpdateResult.Updated, result);
        Assert.False(repo.Users[0].IsActive);
        // Cookie da Web: o stamp mudou, então o SecurityStampValidator rejeita o principal.
        Assert.Equal(1, repo.SecurityStampRegenerations);
        Assert.NotEqual(stampBefore, repo.Users[0].SecurityStamp);
        // Sessão da Api: o refresh token deixa de renovar.
        Assert.NotNull(refreshTokens.Tokens[0].RevokedAt);
    }

    [Fact]
    public async Task UpdateAsync_keeping_user_active_preserves_sessions()
    {
        var repo = BuildRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var service = BuildService(repo, refreshTokens);
        var created = await service.CreateAsync(ValidCreate());
        refreshTokens.SeedActive(created.Id, "hash-sessao-api");

        await service.UpdateAsync(
            new UpdateUserRequest
            {
                Id = created.Id,
                FullName = "Operador Sr.",
                RoleName = "Manager",
                DefaultSiteId = 1,
                IsActive = true,
            },
            currentUserId: "admin");

        // Editar o perfil não pode derrubar a sessão de quem continua ativo.
        Assert.Equal(0, repo.SecurityStampRegenerations);
        Assert.Equal(0, refreshTokens.RevokeAllCalls);
        Assert.Null(refreshTokens.Tokens[0].RevokedAt);
    }

    [Fact]
    public async Task UpdateAsync_user_already_inactive_does_not_revoke_again()
    {
        var repo = BuildRepository();
        var refreshTokens = new FakeRefreshTokenRepository();
        var service = BuildService(repo, refreshTokens);
        var created = await service.CreateAsync(ValidCreate());

        var update = new UpdateUserRequest
        {
            Id = created.Id,
            FullName = "Operador",
            RoleName = "Picker",
            DefaultSiteId = 1,
            IsActive = false,
        };

        await service.UpdateAsync(update, currentUserId: "admin");   // ativo → inativo: revoga
        await service.UpdateAsync(update, currentUserId: "admin");   // já inativo: nada a fazer

        // Só a TRANSIÇÃO derruba as sessões; reeditar um inativo não repete o trabalho.
        Assert.Equal(1, repo.SecurityStampRegenerations);
        Assert.Equal(1, refreshTokens.RevokeAllCalls);
    }

    // --- Autoedição destrutiva do administrador (item 4 da avaliação técnica) ---

    // Cria um admin e devolve o id. Precisa do papel "Admin" semeado no fake.
    private static async Task<string> CreateAdminAsync(
        FakeUserRepository repo, UserService service, string email)
    {
        repo.SeedRole("Admin");
        var request = ValidCreate(email);
        request.RoleName = "Admin";
        var dto = await service.CreateAsync(request);
        return dto.Id;
    }

    private static UpdateUserRequest EditOf(string id, string roleName, bool isActive) => new()
    {
        Id = id,
        FullName = "Administrador",
        RoleName = roleName,
        DefaultSiteId = 1,
        IsActive = isActive,
    };

    [Fact]
    public async Task UpdateAsync_blocks_deactivating_yourself()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var meId = await CreateAdminAsync(repo, service, "admin@base.local");

        var result = await service.UpdateAsync(EditOf(meId, "Admin", isActive: false), currentUserId: meId);

        Assert.Equal(UserUpdateResult.SelfDeactivate, result);
        Assert.True(repo.Users[0].IsActive);  // nada foi persistido
    }

    [Fact]
    public async Task UpdateAsync_blocks_changing_your_own_role()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var meId = await CreateAdminAsync(repo, service, "admin@base.local");

        var result = await service.UpdateAsync(EditOf(meId, "Picker", isActive: true), currentUserId: meId);

        Assert.Equal(UserUpdateResult.SelfRoleChange, result);
        Assert.Equal("Admin", await repo.GetRoleNameAsync(meId));  // papel intacto
    }

    [Fact]
    public async Task UpdateAsync_lets_you_edit_your_own_name()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var meId = await CreateAdminAsync(repo, service, "admin@base.local");

        // As guardas miram só desativação e troca de papel: editar o próprio nome continua livre.
        var result = await service.UpdateAsync(EditOf(meId, "Admin", isActive: true), currentUserId: meId);

        Assert.Equal(UserUpdateResult.Updated, result);
    }

    [Fact]
    public async Task UpdateAsync_blocks_deactivating_the_last_active_admin()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var adminId = await CreateAdminAsync(repo, service, "unico@base.local");

        // Editado por OUTRA pessoa (as guardas de autoedição não se aplicam), mas ele é o único
        // admin ativo: desativá-lo deixaria o sistema sem quem gerencie usuários.
        var result = await service.UpdateAsync(
            EditOf(adminId, "Admin", isActive: false), currentUserId: "outro-usuario");

        Assert.Equal(UserUpdateResult.LastAdmin, result);
        Assert.True(repo.Users[0].IsActive);
    }

    [Fact]
    public async Task UpdateAsync_blocks_removing_the_role_of_the_last_active_admin()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var adminId = await CreateAdminAsync(repo, service, "unico@base.local");

        var result = await service.UpdateAsync(
            EditOf(adminId, "Picker", isActive: true), currentUserId: "outro-usuario");

        Assert.Equal(UserUpdateResult.LastAdmin, result);
        Assert.Equal("Admin", await repo.GetRoleNameAsync(adminId));
    }

    [Fact]
    public async Task UpdateAsync_allows_demoting_an_admin_when_another_one_remains()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var firstId = await CreateAdminAsync(repo, service, "admin1@base.local");
        await CreateAdminAsync(repo, service, "admin2@base.local");

        // Com dois admins ativos, rebaixar um é operação legítima — a guarda não pode atrapalhar.
        var result = await service.UpdateAsync(
            EditOf(firstId, "Picker", isActive: true), currentUserId: "outro-usuario");

        Assert.Equal(UserUpdateResult.Updated, result);
        Assert.Equal("Picker", await repo.GetRoleNameAsync(firstId));
    }

    [Fact]
    public async Task UpdateAsync_ignores_the_last_admin_rule_for_an_already_inactive_admin()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var adminId = await CreateAdminAsync(repo, service, "inativo@base.local");
        repo.Users[0].IsActive = false;  // já estava inativo: não é o "último admin ATIVO"

        var result = await service.UpdateAsync(
            EditOf(adminId, "Picker", isActive: false), currentUserId: "outro-usuario");

        Assert.Equal(UserUpdateResult.Updated, result);
    }

    [Fact]
    public async Task DeleteAsync_blocks_self_delete()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var created = await service.CreateAsync(ValidCreate());

        var result = await service.DeleteAsync(created.Id, currentUserId: created.Id);

        Assert.Equal(UserDeletionResult.SelfDelete, result);
        Assert.False(repo.Users[0].IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_returns_NotFound_when_missing()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);

        Assert.Equal(
            UserDeletionResult.NotFound,
            await service.DeleteAsync("missing", currentUserId: "admin"));
    }

    [Fact]
    public async Task DeleteAsync_soft_deletes_other_user()
    {
        var repo = BuildRepository();
        var service = BuildService(repo);
        var created = await service.CreateAsync(ValidCreate());

        var result = await service.DeleteAsync(created.Id, currentUserId: "admin");

        Assert.Equal(UserDeletionResult.Deleted, result);
        Assert.True(repo.Users[0].IsDeleted);
    }
}
