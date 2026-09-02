# Padrão de CRUD (template das telas de cadastro)

Este documento é o **template oficial** para construir qualquer cadastro (CRUD) neste projeto.
A implementação de **referência viva** é o CRUD de **`Site`** — quando este texto e o código divergirem,
o código do `Site` (que está testado e revisado) é a fonte; atualize este documento para refletir.

> **Consulta obrigatória antes de criar ou alterar um cadastro.** O objetivo é que todos os CRUDs
> (`Site`, `Role`, `User`, …) tenham a mesma arquitetura, validação, localização e layout.

Pré-requisitos de leitura: `CLAUDE.md` (arquitetura, localização, regras de colaboração),
`docs/padrao-ui.md` (tema, componentes, acessibilidade) e `docs/baseline-microsoft.md`.

---

## 1. Arquitetura em camadas (onde cada artefato mora)

| Artefato | Projeto | Exemplo (`Site`) |
|---|---|---|
| Entidade + base de auditoria | `Retaguarda.Data/Entities` | `Site : AuditableEntity` |
| Interface + implementação de repositório | `Retaguarda.Data/Repositories` | `ISiteRepository`, `SiteRepository` |
| DTOs (leitura, lista, create, update) | `Retaguarda.Business/<Modulo>/Dtos` | `SiteDto`, `SiteListItemDto`, `CreateSiteRequest`, `UpdateSiteRequest` |
| Validadores (FluentValidation) | `Retaguarda.Business/<Modulo>/Validators` | `CreateSiteRequestValidator`, `UpdateSiteRequestValidator` |
| Interface + serviço (casos de uso) | `Retaguarda.Business/<Modulo>` | `ISiteService`, `SiteService` |
| Controller + Views | `Retaguarda.Web/Controllers`, `Views/<Modulo>` | `SitesController`, `Views/Sites/*` |
| ViewModel de tela (quando necessário) | `Retaguarda.Web/Models/<Modulo>` | `SiteIndexViewModel` |
| Contratos neutros / tipos compartilhados | `Retaguarda.Shared` | `ICurrentUserService`, `PagedResult<T>` |

**Regra de onde fica a interface** (§4.2): a **interface de repositório** fica no `Data` (é tipada na entidade,
e `Data` não pode referenciar `Business`); a **interface de serviço** fica no `Business` (consumida por Web/Api);
contratos sem tipos do `Data` (ex.: `ICurrentUserService`) ficam no `Shared`.

---

## 2. Dados (camada `Data`)

- **PK inteira:** `int` para cadastros de baixo volume; `bigint` para alto volume (§6.2). Identity mantém chave `string`/GUID padrão.
- **Herdar `AuditableEntity`** → ganha auditoria (`CreatedAt/CreatedById/UpdatedAt/UpdatedById`) e soft delete (`IsDeleted/DeletedAt/DeletedById`). Esses campos são carimbados automaticamente pelo `AuditableEntityInterceptor` no `SaveChanges` — **não** preencher na mão.
- **Soft delete em todos os cadastros:** nunca exclusão física. O `ApplicationDbContext` aplica `HasQueryFilter(e => !e.IsDeleted)`.
- **Índice único filtrado** para campos únicos: `HasIndex(x => x.Code).IsUnique().HasFilter("\"IsDeleted\" = false")` — assim o valor é único só entre ativos e pode ser reutilizado após exclusão lógica.
- **Repositório** encapsula EF e persiste por operação. Métodos típicos: `GetByIdAsync`, `ListAsync(search, page, pageSize)` (busca + paginação), `CodeExistsAsync(code, excludeId)` (checa só ativos), `AddAsync`, `UpdateAsync`, `DeleteAsync` (chama `Remove`; o interceptor converte em soft delete).
- **Busca da listagem: `EF.Functions.ILike` + `SearchPattern.Contains`** — nunca `EF.Functions.Like`, nunca concatenar `%termo%` à mão:

  ```csharp
  var term = SearchPattern.Contains(search);
  query = query.Where(x =>
      EF.Functions.ILike(x.Name, term, SearchPattern.EscapeCharacter)
      || EF.Functions.ILike(x.Code, term, SearchPattern.EscapeCharacter));
  ```

  Dois motivos, os dois já custaram bug nesta base: (1) a collation padrão do PostgreSQL é **case-sensitive**, então com `Like` a busca por `matriz` não encontra `Matriz` (`ILIKE` resolve); (2) `%` e `_` digitados pelo usuário são **curingas** — sem o escape do `SearchPattern`, buscar `%` devolve a tabela inteira. **Isso não é pego por teste unitário:** os fakes usam LINQ em memória, que é case-insensitive e trata os curingas como texto. Valide contra o Postgres real.
- **Datas sempre em UTC.**
- **Toda nova entidade gera migration** (`dotnet ef migrations add <Nome> --project src/Retaguarda.Data --startup-project src/Retaguarda.Web`).

Referência: `src/Retaguarda.Data/Entities/{AuditableEntity,Site}.cs`, `Repositories/{ISiteRepository,SiteRepository}.cs`, `Interceptors/{AuditStamper,AuditableEntityInterceptor}.cs`, `Identity/ApplicationDbContext.cs`.

---

## 3. Negócio (camada `Business`)

- **DTOs separados por intenção:** `…Dto` (detalhe), `…ListItemDto` (linha de lista, enxuto), `Create…Request`, `Update…Request`.
- **Validação com FluentValidation**, um validador por request. As **mensagens são chaves de recurso** (ex.: `"site_code_required"`), nunca texto literal — a camada Web/Api as localiza.
  - Use `Cascade(CascadeMode.Stop)` em regras encadeadas (ex.: não bater no banco se o campo está vazio).
  - Unicidade via `MustAsync` chamando `…Repository.CodeExistsAsync` (excluindo o próprio Id no update).
  - Listas fechadas (ex.: fuso) validam por pertencimento a uma lista curada em código.
- **Serviço** (`I…Service`): `ListAsync` (retorna `PagedResult<T>` do `Shared`), `GetByIdAsync`, `CreateAsync`, `UpdateAsync`, `DeleteAsync`.
  - Create/Update chamam `validator.ValidateAndThrowAsync(...)` → lançam `FluentValidation.ValidationException` em caso inválido. **A validação vive no serviço** para valer igual no Web e na futura API.
  - Update/Delete retornam `false` quando o registro não existe.
  - Mapeamento DTO↔entidade com **Mapster** (`request.Adapt<Entity>()`, `entity.Adapt<Dto>()`, `request.Adapt(entity)` no update).
- **Pacotes:** `FluentValidation` + `FluentValidation.DependencyInjectionExtensions` + `Mapster`.

Referência: `src/Retaguarda.Business/Sites/*`, `src/Retaguarda.Shared/Models/PagedResult.cs`.

---

## 4. Web (camada `Web`)

### Controller
- `[Authorize(Roles = "…")]` na classe (ex.: `Site` é `Admin`; defina o papel por cadastro).
- Actions: `Index(search, page)`, `Create` (GET/POST), `Edit(id, …)` (GET/POST), `Delete(id, …)` (GET) + `DeleteConfirmed` (POST, `[ActionName("Delete")]`).
- **Todos os POST** têm `[ValidateAntiForgeryToken]`.
- **Propagação do estado da listagem:** todas as actions recebem e devolvem `search` + `page`; os redirects voltam para `Index` com eles (`RedirectToAction(nameof(Index), new { search, page })`). Nas GET de Create/Edit/Delete, guarde via `SetListState` em `ViewData["ListSearch"]`/`ViewData["ListPage"]`.
- **Tradução de validação:** capture `ValidationException` e mapeie para o `ModelState` localizando a chave:
  ```csharp
  foreach (var error in ex.Errors)
      ModelState.AddModelError(error.PropertyName, _localizer[error.ErrorMessage].Value);
  ```
- **Feedback de sucesso:** `TempData["StatusMessage"] = _localizer["…"].Value;` e exiba no Index.

### Program.cs (uma vez por projeto, já feito)
- `SuppressImplicitRequiredAttributeForNonNullableReferenceTypes = true` — evita mensagens `[Required]` inferidas **em inglês**; a validação fica 100% no FluentValidation (localizado).
- Registrar no DI: `I…Repository`/impl, `I…Service`/impl, `AddValidatorsFromAssemblyContaining<…Validator>()`.

Referência: `src/Retaguarda.Web/Controllers/SitesController.cs`, `Program.cs`.

---

## 5. Localização

- **Nenhuma string visível ao usuário hardcoded.** Tudo via `IStringLocalizer<SharedResources>` (`@inject … L` nas views) + `.resx` (`src/Retaguarda.Shared/Resources/SharedResources.pt-BR.resx`).
- **Chaves em inglês/snake_case**: rótulos (`site_field_code`), mensagens de validação (`site_code_required`), ações genéricas reutilizáveis (`new`, `edit`, `delete`, `save`, `cancel`, `search`, `clear_search`, `actions`, `active_yes`, `active_no`, `pagination_*`), feedback (`site_created`, `site_updated`, `site_deleted`).
- Idioma atual hoje é só **pt-BR**, mas a estrutura aceita novos idiomas só adicionando `.resx` — não introduza nada que dependa de idioma fixo.

---

## 6. Layout / UI (o padrão visual)

Sem `style=""` inline (`padrao-ui.md` §3). Só classes Bootstrap 5 + tokens do `theme.css`. Loading no submit com `data-disable-on-submit` + `data-loading-text`.

**Estrutura comum de toda tela:**
1. `<div class="page-header"><h1>@L["…_title"]</h1></div>` — só o título.
2. Alerta de sucesso: `@if (TempData["StatusMessage"] is string status) { <div class="alert alert-success" role="alert">@status</div> }`.
3. Conteúdo dentro de **`<div class="card card-accent mb-4">`** (card branco com linha azul no topo).

**Index (listagem):**
- No `.card-body`: botão **Novo** à direita (`<div class="d-flex justify-content-end mb-3">` + `btn btn-sm btn-primary`), depois **busca** (`<form method="get">` com `input-group`: input `name="search"`, botão buscar, e link **Limpar** condicional quando há busca), depois **tabela** (`table table-striped table-hover align-middle mb-0`) ou empty-state (`<p class="text-muted mb-0">`).
- **Tabela sempre dentro de `<div class="table-responsive">`** → scroll horizontal quando transborda (muitas/largas colunas), inclusive no mobile. As células **não quebram linha** (`white-space: nowrap`, definido no tema). Colunas de **texto longo** recebem `class="cell-truncate"` + `title="@valor"` → truncam com reticências e expõem o valor completo no hover/toque longo. **Não** truncar por contagem de caracteres no servidor (a abordagem é por largura, responsiva).
- **Linhas zebradas:** o `<table>` usa `table-striped` (cores padrão do Bootstrap) combinado com `table-hover`. O **cabeçalho (`thead`)** segue o mesmo estilo dos labels de formulário (negrito, tamanho base, **sem** maiúsculas) — definido no tema, sem classe extra na view.
- Coluna de ações à direita (`text-end`) com `Editar` (`btn-sm btn-primary`) e `Excluir` (`btn-sm btn-danger`), ambos levando `asp-route-search`/`asp-route-page`.
- **Paginação no `.card-footer`** (só quando `TotalPages > 1`): "Página X de Y" + Anterior/Próxima, preservando `search`.

**Create / Edit (formulário):**
- `<form>` envolve o card. Campos ocultos `search`/`page` (e `Id` no Edit) no topo do form.
- Resumo de erros de modelo: `@if (!ViewData.ModelState.IsValid) { <div asp-validation-summary="ModelOnly" …> }`.
- Campos em **grid responsivo**: `<div class="row g-3">` com `col-md-*` para ficarem lado a lado quando há espaço.
- **Labels com `<strong>`** (`<label asp-for="…" class="form-label"><strong>@L["…"]</strong></label>`).
- **`<select>` para booleanos** (Sim/Não) e para listas fechadas/curadas. Para bool, use `name`/`id` explícitos + `selected="@(cond ? "selected" : null)"` (evita ambiguidade do `asp-for`).
- **`<select>` obrigatório ligado a `int` não anulável** (ex.: `CustomerId`, `SiteId`, `DefaultSiteId`): a opção placeholder deve usar `value="0"` e o validator deve ter `GreaterThan(0).WithMessage("..._required")`. **Não use `value=""` nesses casos**, porque o model binder tenta converter string vazia para `int` e exibe a mensagem padrão em inglês `"The value '' is invalid."` antes da validação localizada. Para `int?` opcional (ex.: `CostCenterId`) e para `string` obrigatória (ex.: `RoleName`, `TimeZone`), `value=""` continua correto, desde que o FluentValidation trate `NotEmpty`/`When` com chave `.resx`.
- **Campos com fundo branco e borda forte:** `.form-control`/`.form-select`/`.form-check-input` recebem fundo branco (`--color-surface`) e borda `--color-border` (slate-400) pelo tema — sem classe extra. Campos **desabilitados** mantêm o cinza sólido do Bootstrap (acessibilidade — `padrao-ui.md` §9: disabled distinto, nunca só opacidade).
- **Texto das options trunca sozinho:** options com rótulo longo (nome de cliente, produto, planta) são truncadas automaticamente pelo `select-truncate.js` (`padrao-ui.md` §8.1), com o texto completo no `title`. **Não** truncar na view (sem `substring`, sem CSS de largura, sem helper próprio). Se precisar de outro limite que não o padrão (60), use `data-label-max="N"` no `<select>`.
- **Texto de ajuda de campo tem dois formatos, e a escolha é regrada (`padrao-ui.md` §8.2):** restrição que afeta o preenchimento (formato, tamanho, regra de senha) vai **visível** abaixo do campo via `<partial name="_FieldHint" …>`; explicação de consequência ou de por que o campo é somente-leitura vai no **ícone `?` ao lado do rótulo** via `<partial name="_FieldHelp" …>`, dentro do wrapper `<div class="form-label-row">` (o ícone fica **fora** do `<label>`). Os dois recebem `new FieldHelp("IdDoCampo", L["chave_hint"].Value)` e **exigem `aria-describedby="IdDoCampo-help"` no campo**. Não escrever `<span class="form-text">` solto.
- Botões no **`.card-footer`**: `Salvar` (`btn-primary`, com `data-loading-text`) + `Cancelar` (`btn-secondary`/`btn-outline-secondary`, voltando ao Index com `search`/`page`). O `.card-footer` tem fundo levemente acinzentado (`#00000008`), funcionando como "barra de ações" (estilo AdminLTE).

**Delete (confirmação):**
- `<form>` envolve o card; `id`/`search`/`page` ocultos. Mensagem no `.card-body`; botões `Excluir` (`btn-danger`) + `Cancelar` no `.card-footer`.

**Menu lateral (`_Sidebar`):** adicionar o item na seção apropriada, **gated pelo papel** (`@if (User.IsInRole("…"))`).

Componentes de tema relevantes (em `theme.css`): `.card` (remapeado para tokens, borda suave + sombra em 2 camadas, respiro interno), `.card-accent` (linha azul no topo), `.card-footer` (fundo `#00000008`, barra de ações), `.table { --bs-table-bg: var(--color-surface) }` (fundo branco) + `table-striped` (zebra com cores padrão do Bootstrap) + células `white-space: nowrap`, `.cell-truncate` (truncamento por largura com reticências), `thead th` em negrito (igual aos labels), `.form-control`/`.form-select`/`.form-check-input` com fundo branco, `.page-header`. Tema refinado com inspiração no AdminLTE (paleta/sombras/densidade); fonte base **14px** — ver `docs/padrao-ui.md` §5/§6.

Referência: `src/Retaguarda.Web/Views/Sites/{Index,Create,Edit,Delete}.cshtml`, `Views/Shared/_Sidebar.cshtml`, `wwwroot/css/theme.css`.

---

## 7. Componentização — "regra da 2ª tela" (`padrao-ui.md` §4)

No CRUD de `Site` (o 1º), a marcação repetível (card+header, barra de busca, tabela paginada, formulário-no-card)
está **inline** de propósito. **Ao construir o 2º CRUD** (Role/User), antes de copiar/colar, **extraia o que se repete**
para Partials/ViewComponents reutilizáveis (ex.: ViewComponent de tabela paginada com busca, Partial de barra de ações,
Partial de formulário-no-card). Não propague duplicação: o 2º uso é o gatilho da extração.

---

## 8. Definition of Done de um CRUD novo

- [ ] Entidade + migration; soft delete e índice único filtrado (se houver campo único) configurados.
- [ ] Repositório (interface no `Data`) com busca (`ILike` + `SearchPattern`, validada contra o Postgres real) + paginação + checagem de unicidade.
- [ ] DTOs + validadores (mensagens como chaves) + serviço (valida e lança `ValidationException`, usa Mapster, retorna `PagedResult<T>`).
- [ ] Controller `[Authorize(Roles=…)]`, antiforgery nos POST, propagação de `search`/`page`, tradução de validação para `ModelState` localizado.
- [ ] Views no padrão do §6 (card, grid, selects, paginação no footer, estado da listagem, loading), sem `style=""` inline; selects obrigatórios `int` usam placeholder `value="0"` + mensagem localizada via validator.
- [ ] Item no `_Sidebar` gated por papel.
- [ ] Todas as strings em `.resx` (nada hardcoded); termos de domínio no glossário (§2 do orientacao).
- [ ] Testes (serviço com repositório fake; lógica pura quando aplicável).
- [ ] A partir do 2º CRUD: componentes repetidos extraídos (§7).
- [ ] `dotnet build` (Release, warnings-as-errors) e `dotnet test` verdes.
- [ ] Baseline (`docs/baseline-microsoft.md`) revisado; sem regressão.

---

## 9. Decisões já tomadas (não reabrir sem necessidade)

- PK inteira para entidades de negócio; Identity com chave `string`/GUID padrão.
- Soft delete + índice único filtrado (`WHERE IsDeleted = 0`).
- Validação centralizada no serviço (FluentValidation), traduzida para `ModelState`/`.resx` na Web.
- `select` simples (sem lib de busca tipo Tom Select) — revisitar só quando houver listas realmente grandes.
- Sem framework JS, sem build de frontend, conjunto fechado de libs (`padrao-ui.md` §11).

## 10. Implementações de referência adicionais (Role, User)

O CRUD de `Site` é o template puro. `Role` e `User` seguem o mesmo padrão de camadas/UI,
mas têm **divergências** por serem entidades gerenciadas pelo Identity — documentadas aqui
para não se repetir o estudo a cada cadastro de segurança.

### 10.1. Role (Acessos) — `ApplicationRole : IdentityRole`
- **Entidade do Identity estendida** (PK `string`/GUID): permanece em `[identity].[Roles]`,
  sem tabela nova; ganha `Description`, `IsSystem`, auditoria e soft delete.
- **Persistência:** create/update via `RoleManager` (mantém `NormalizedName`/`ConcurrencyStamp`);
  listagem/consulta/soft delete via `DbContext`. Delete = `Remove` (interceptor → soft delete).
- **Papéis internos (`IsSystem`):** nome **imutável** e **exclusão proibida** (o código e
  `[Authorize(Roles=...)]` dependem dos nomes). Só a descrição é editável.
- **Delete com motivo:** serviço retorna um **enum** (`RoleDeletionResult`) em vez de `bool`,
  para a Web exibir a mensagem certa (interno / com usuários vinculados).

### 10.2. User (Usuários) — `ApplicationUser : IdentityUser`
- Estende o Identity com `FullName`, `IsActive`, `PreferredLanguage`, `DefaultSiteId`,
  auditoria e soft delete. Persistência via `UserManager`.
- **Uma role por usuário:** imposto no serviço (ao salvar, troca o vínculo).
- **Inativo não loga:** `ApplicationSignInManager.CanSignInAsync` recusa `IsActive == false`.
- **Senha:** definida pelo admin no Create (política do Identity); no Edit **não** muda
  (fluxo de recuperação). **E-mail** (login) é **imutável** no Edit (exibido somente-leitura).
- **Self-delete bloqueado:** `UserDeletionResult.SelfDelete` (e o botão some na própria linha).

### 10.3. Sub-CRUD aninhado (Plantas do usuário) — R/C/D
Padrão para gerenciar uma **associação N:N** de uma entidade-pai (ex.: `UserSite`):
- Acessado por um **botão na linha** do index pai (`Plantas`), levando o `id` do pai.
- Index/Create/Delete no padrão CRUD, **sem Update** (associação não tem o que editar).
- **Dois contextos de estado** propagados: o do index **pai** (`userSearch`/`userPage`,
  usado pelo **Voltar**, que retorna ao index pai como o Cancelar de um Edit) e o **próprio**
  (`search`/`page`, busca/paginação da associação, usado por Novo/Cancelar/Salvar).
- O `select` de Create lista só os itens **ainda não associados** (e ativos).
- **Guarda:** a planta **padrão** não pode ser desassociada (trocar a padrão no Edit do usuário
  antes) — garante ≥1 associação. O vínculo é tabela de associação: **hard delete** do link.

### 10.4. Cadastro multi-site (pertence a uma planta)
Padrão para entidades de negócio **isoladas por planta**. Vale para qualquer cadastro com `SiteId`.

> **Nota:** esta base **não tem** nenhuma entidade isolada por planta — `Site`, `User` e `Role` são cadastros de plataforma. A **infraestrutura** de isolamento está pronta e testada (`ISiteScoped`, `AuditStamper.StampSite`, Global Query Filter por planta, `ICurrentUserService`, `ApplicationDbContext.CurrentUser`); os exemplos abaixo (`CostCenter`, `Customer`) são ilustrativos. **A primeira entidade de domínio do seu projeto vira a implementação de referência viva — atualize esta seção quando ela existir.**
- **Entidade implementa `ISiteScoped`** (`int SiteId`) além de `AuditableEntity`. O `Site` é a raiz
  e **não** implementa. Inclui nav `Site?` + FK `Restrict` (sites são excluídos logicamente).
- **`SiteId` nunca vem do request/DTO nem do formulário:** é a **planta ativa**. Carimbado no
  `AuditableEntityInterceptor` (via `AuditStamper.StampSite`) na inclusão — mesma mecânica da auditoria.
- **Global Query Filter** combina soft delete + planta: `!IsDeleted && SiteId == currentUser.SiteId`.
  O `ApplicationDbContext` recebe `ICurrentUserService` no construtor; o EF re-avalia o filtro por query.
  Resultado: repositório e validadores **não** filtram `SiteId` à mão — já vem isolado.
- **Índice único composto e filtrado:** `(SiteId, Code)` com `HasFilter("\"IsDeleted\" = false")` — `Code`
  único **por planta**, reutilizável após exclusão lógica.
- **Teste:** o fake repo representa os registros já visíveis da planta ativa (o filtro é da camada Data);
  cobrir o `StampSite` (carimba quando vazio / não sobrescreve explícito / sem planta deixa 0).
- **FK entre dois cadastros multi-site** (ex.: `Customer.CostCenterId` → `CostCenter`, opcional): a entidade
  ganha `int? <Outra>Id` + nav; FK `Restrict` (a outra ponta é soft-deleted, a linha permanece). O repositório
  faz `.Include(c => c.<Outra>)` para a lista/detalhe exibirem o nome (flatten via Mapster: `OutraName`).
  O **select** do formulário é populado no controller (helper que chama o `Service.ListAsync` da outra
  entidade e filtra `IsActive`), passado por `ViewData` como um `record Option(Id, Name, Code)` — mesmo
  padrão do select de planta do `User`. Em **edição**, se o registro aponta para uma opção que ficou inativa,
  inclua-a mesmo assim na lista (senão o valor some silenciosamente ao salvar). Validação: campo opcional via
  `When(x => x.<Outra>Id.HasValue, …)` + `MustAsync` checando existência na planta ativa (`RuleFor(x => x.<Outra>Id)`
  para o `PropertyName` casar com o campo do form).
