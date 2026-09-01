using System.Globalization;
using System.Security.Claims;
using FluentValidation;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Localization;
using Retaguarda.Business.Exporting;
using Retaguarda.Business.Sites;
using Retaguarda.Business.Sites.Dtos;
using Retaguarda.Shared;
using Retaguarda.Web.Infrastructure;
using Retaguarda.Web.Models.Sites;

namespace Retaguarda.Web.Controllers;

// Cadastro de plantas (Site). Restrito a Admin — configuração de infraestrutura do tenant.
// O estado da listagem (busca + página) é propagado por todo o fluxo para que o usuário
// volte sempre ao mesmo ponto após criar/editar/excluir/cancelar.
[Authorize(Roles = RetaguardaRoles.Admin)]
public sealed class SitesController : Controller
{
    private const int PageSize = 10;

    // Teto de linhas da exportação: a lista de plantas é curta por natureza, mas a action é GET e
    // dá para forjar a URL — o limite evita varrer a tabela inteira por acidente.
    private const int ExportMaxRows = 5000;

    private const string ExcelContentType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
    private const string PdfContentType = "application/pdf";

    private readonly ISiteService _siteService;
    private readonly IExcelExporter _excel;
    private readonly IPdfExporter _pdf;
    private readonly IStringLocalizer<SharedResources> _localizer;
    private readonly SiteTimeZone _siteTime;

    public SitesController(
        ISiteService siteService,
        IExcelExporter excel,
        IPdfExporter pdf,
        IStringLocalizer<SharedResources> localizer,
        SiteTimeZone siteTime)
    {
        _siteService = siteService;
        _excel = excel;
        _pdf = pdf;
        _localizer = localizer;
        _siteTime = siteTime;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var result = await _siteService.ListAsync(search, page, PageSize, cancellationToken);
        return View(new SiteIndexViewModel { Sites = result, Search = search });
    }

    // Exporta a listagem (com o mesmo termo de busca da tela) em Excel ou PDF. Referência do padrão
    // de exportação: monte uma ExportTable com cabeçalhos localizados e células já formatadas, e
    // deixe o exportador cuidar do formato.
    [HttpGet]
    public async Task<IActionResult> Export(
        string? format, string? search, CancellationToken cancellationToken = default)
    {
        var result = await _siteService.ListAsync(search, 1, ExportMaxRows, cancellationToken);

        var meta = new List<string> { SiteMeta() };
        if (!string.IsNullOrWhiteSpace(search))
        {
            meta.Add(Fmt("export_meta_search", search.Trim()));
        }

        meta.Add(GeneratedMeta());

        var columns = new[]
        {
            new ExportColumn(_localizer["site_field_code"].Value),
            new ExportColumn(_localizer["site_field_name"].Value),
            new ExportColumn(_localizer["site_field_active"].Value),
        };

        var yes = _localizer["active_yes"].Value;
        var no = _localizer["active_no"].Value;
        var rows = result.Items
            .Select(s => (IReadOnlyList<string>)[s.Code, s.Name, s.IsActive ? yes : no])
            .ToList();

        var table = new ExportTable(_localizer["sites_title"].Value, meta, columns, rows);
        return ExportFile(table, format, "plantas");
    }

    [HttpGet]
    public IActionResult Create(string? search, int page = 1)
    {
        SetListState(search, page);
        return View(new CreateSiteRequest());
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create(
        CreateSiteRequest request, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        try
        {
            await _siteService.CreateAsync(request, cancellationToken);
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            SetListState(search, page);
            return View(request);
        }

        TempData["StatusMessage"] = _localizer["site_created"].Value;
        return RedirectToAction(nameof(Index), new { search, page });
    }

    [HttpGet]
    public async Task<IActionResult> Edit(int id, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var site = await _siteService.GetByIdAsync(id, cancellationToken);
        if (site is null)
        {
            return NotFound();
        }

        SetListState(search, page);
        return View(new UpdateSiteRequest
        {
            Id = site.Id,
            Name = site.Name,
            Code = site.Code,
            TimeZone = site.TimeZone,
            IsActive = site.IsActive,
        });
    }

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(
        UpdateSiteRequest request, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        bool updated;
        try
        {
            updated = await _siteService.UpdateAsync(request, cancellationToken);
        }
        catch (ValidationException ex)
        {
            AddValidationErrors(ex);
            SetListState(search, page);
            return View(request);
        }

        if (!updated)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = _localizer["site_updated"].Value;
        return RedirectToAction(nameof(Index), new { search, page });
    }

    [HttpGet]
    public async Task<IActionResult> Delete(int id, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var site = await _siteService.GetByIdAsync(id, cancellationToken);
        if (site is null)
        {
            return NotFound();
        }

        SetListState(search, page);
        return View(site);
    }

    [HttpPost]
    [ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(
        int id, string? search, int page = 1, CancellationToken cancellationToken = default)
    {
        var deleted = await _siteService.DeleteAsync(id, cancellationToken);
        if (!deleted)
        {
            return NotFound();
        }

        TempData["StatusMessage"] = _localizer["site_deleted"].Value;
        return RedirectToAction(nameof(Index), new { search, page });
    }

    // Disponibiliza o estado da listagem para a view (campos ocultos + link Cancelar).
    private void SetListState(string? search, int page)
    {
        ViewData["ListSearch"] = search;
        ViewData["ListPage"] = page;
    }

    // Escolhe o exportador pelo format e devolve o arquivo com nome + timestamp local.
    private FileContentResult ExportFile(ExportTable table, string? format, string baseName)
    {
        var stamp = _siteTime.ToLocal(DateTime.UtcNow).ToString("yyyyMMdd_HHmmss", CultureInfo.InvariantCulture);
        var isPdf = string.Equals(format, "pdf", StringComparison.OrdinalIgnoreCase);

        return isPdf
            ? File(_pdf.Export(table), PdfContentType, $"{baseName}_{stamp}.pdf")
            : File(_excel.Export(table), ExcelContentType, $"{baseName}_{stamp}.xlsx");
    }

    // Linha de contexto "Planta: {nome}" a partir da claim da sessão.
    private string SiteMeta()
        => Fmt("export_meta_site", User.FindFirstValue(RetaguardaClaims.SiteName) ?? "—");

    // Linha "Gerado em: {data/hora local}".
    private string GeneratedMeta()
        => Fmt("export_meta_generated",
            _siteTime.ToLocal(DateTime.UtcNow).ToString("dd/MM/yyyy HH:mm:ss", CultureInfo.InvariantCulture));

    // Atalho para uma string localizada formatada.
    private string Fmt(string key, params object[] args)
        => string.Format(CultureInfo.CurrentCulture, _localizer[key].Value, args);

    // Traduz as falhas do validator (cujas mensagens são chaves de recurso) para o ModelState.
    private void AddValidationErrors(ValidationException exception)
    {
        foreach (var error in exception.Errors)
        {
            ModelState.AddModelError(error.PropertyName, _localizer[error.ErrorMessage].Value);
        }
    }
}
