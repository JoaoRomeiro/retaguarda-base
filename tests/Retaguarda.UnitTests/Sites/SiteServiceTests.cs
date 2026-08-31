using FluentValidation;
using Retaguarda.Business.Sites;
using Retaguarda.Business.Sites.Dtos;
using Retaguarda.Business.Sites.Validators;

namespace Retaguarda.UnitTests.Sites;

public sealed class SiteServiceTests
{
    private static SiteService BuildService(FakeSiteRepository repository) =>
        new(repository, new CreateSiteRequestValidator(repository), new UpdateSiteRequestValidator(repository));

    private static CreateSiteRequest ValidCreate(string code = "SP01") => new()
    {
        Name = "São Paulo",
        Code = code,
        TimeZone = "America/Sao_Paulo",
        IsActive = true,
    };

    [Fact]
    public async Task CreateAsync_persists_and_returns_dto()
    {
        var repository = new FakeSiteRepository();
        var service = BuildService(repository);

        var dto = await service.CreateAsync(ValidCreate());

        Assert.True(dto.Id > 0);
        Assert.Equal("SP01", dto.Code);
        Assert.Single(repository.Store);
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_code()
    {
        var repository = new FakeSiteRepository();
        var service = BuildService(repository);
        await service.CreateAsync(ValidCreate("SP01"));

        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(ValidCreate("SP01")));
    }

    [Fact]
    public async Task CreateAsync_rejects_duplicate_name()
    {
        var repository = new FakeSiteRepository();
        var service = BuildService(repository);
        await service.CreateAsync(ValidCreate("SP01"));  // Name "São Paulo"

        // Mesmo nome, código diferente: deve falhar na unicidade do nome.
        await Assert.ThrowsAsync<ValidationException>(
            () => service.CreateAsync(ValidCreate("SP02")));
    }

    [Fact]
    public async Task UpdateAsync_keeps_same_name_for_same_site()
    {
        var repository = new FakeSiteRepository();
        var service = BuildService(repository);
        var created = await service.CreateAsync(ValidCreate("SP01"));

        var ok = await service.UpdateAsync(new UpdateSiteRequest
        {
            Id = created.Id,
            Name = "São Paulo",  // mesmo nome do próprio registro não deve falhar
            Code = "SP01",
            TimeZone = "America/Sao_Paulo",
            IsActive = true,
        });

        Assert.True(ok);
    }

    [Fact]
    public async Task CreateAsync_rejects_invalid_timezone()
    {
        var repository = new FakeSiteRepository();
        var service = BuildService(repository);

        var request = ValidCreate("AB1");
        request.TimeZone = "Not/AZone";

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task CreateAsync_rejects_non_brazilian_timezone()
    {
        var repository = new FakeSiteRepository();
        var service = BuildService(repository);

        var request = ValidCreate("AB1");
        request.TimeZone = "Europe/London";  // IANA válido, mas fora da lista curada

        await Assert.ThrowsAsync<ValidationException>(() => service.CreateAsync(request));
    }

    [Fact]
    public async Task UpdateAsync_returns_false_when_site_missing()
    {
        var repository = new FakeSiteRepository();
        var service = BuildService(repository);

        var ok = await service.UpdateAsync(new UpdateSiteRequest
        {
            Id = 999,
            Name = "X",
            Code = "ZZ9",
            TimeZone = "America/Sao_Paulo",
            IsActive = true,
        });

        Assert.False(ok);
    }

    [Fact]
    public async Task UpdateAsync_keeps_same_code_for_same_site()
    {
        var repository = new FakeSiteRepository();
        var service = BuildService(repository);
        var created = await service.CreateAsync(ValidCreate("SP01"));

        var ok = await service.UpdateAsync(new UpdateSiteRequest
        {
            Id = created.Id,
            Name = "São Paulo Capital",
            Code = "SP01",  // mesmo code do próprio site não deve falhar
            TimeZone = "America/Sao_Paulo",
            IsActive = true,
        });

        Assert.True(ok);
    }

    [Fact]
    public async Task DeleteAsync_soft_deletes_existing_site()
    {
        var repository = new FakeSiteRepository();
        var service = BuildService(repository);
        var created = await service.CreateAsync(ValidCreate("SP01"));

        var ok = await service.DeleteAsync(created.Id);

        Assert.True(ok);
        Assert.True(repository.Store[0].IsDeleted);
    }

    [Fact]
    public async Task DeleteAsync_returns_false_when_site_missing()
    {
        var repository = new FakeSiteRepository();
        var service = BuildService(repository);

        Assert.False(await service.DeleteAsync(999));
    }

    [Fact]
    public async Task CreateAsync_allows_reusing_code_of_deleted_site()
    {
        var repository = new FakeSiteRepository();
        var service = BuildService(repository);
        var created = await service.CreateAsync(ValidCreate("SP02"));
        await service.DeleteAsync(created.Id);

        // Recadastrar o mesmo código após exclusão lógica deve funcionar.
        var recreated = await service.CreateAsync(ValidCreate("SP02"));

        Assert.True(recreated.Id > 0);
        Assert.NotEqual(created.Id, recreated.Id);
    }
}
