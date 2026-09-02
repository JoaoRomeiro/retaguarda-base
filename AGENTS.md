# Repository Guidelines

## Project Structure & Module Organization
`Retaguarda.sln` is a .NET 10 solution. Production code lives in `src/`: `Retaguarda.Web` is the MVC/Razor UI, `Retaguarda.Api` is the JWT-authenticated API for external clients, `Retaguarda.Business` contains services, contracts, validation, and mapping, `Retaguarda.Data` contains EF Core/Identity persistence plus the repository interfaces and implementations, and `Retaguarda.Shared` holds cross-project models and resources. `Retaguarda.AspNetCore` contains shared web infrastructure (current user, security headers, health checks); `Printing` and `Reporting` hold the PDF (QuestPDF) and Excel (ClosedXML) exporters that implement the `IPdfExporter`/`IExcelExporter` contracts declared in `Business`. Tests are under `tests/Retaguarda.UnitTests` and `tests/Retaguarda.IntegrationTests` (the latter is deliberately still the `dotnet new` template — there is no integration-test infrastructure). Docker assets are in `docker/`; project guidance and command references are in `docs/`. This repository is a **starter base**: it ships the platform (auth, multi-site, users/roles/sites, localization, auditing, export, Docker) and no business domain.

## Build, Test, and Development Commands
- `dotnet restore Retaguarda.sln`: restores NuGet packages.
- `dotnet build Retaguarda.sln -c Release`: builds all projects with warnings treated as errors.
- `dotnet test Retaguarda.sln -c Release --no-build`: runs the xUnit test suites after a Release build.
- `dotnet run --project src/Retaguarda.Web --launch-profile https`: runs the web app locally.
- `dotnet run --project src/Retaguarda.Api --launch-profile https`: runs the API locally.
- `docker compose -f docker/docker-compose.yml up -d postgres`: starts the PostgreSQL 17 container the apps expect on `localhost:15433`.
- `dotnet ef database update --project src/Retaguarda.Data --startup-project src/Retaguarda.Web`: applies pending migrations.
- `docker compose -f docker/docker-compose.yml up --build -d`: builds and starts the local container stack. There is no CI: `dotnet build -c Release`, `dotnet test` and `docker compose build` are the manual gate.

Dev secrets live in User Secrets, never in `appsettings.json`: `ConnectionStrings:DefaultConnection` in both `Retaguarda.Web` and `Retaguarda.Api`, plus `Jwt:SigningKey` (32+ chars) in `Retaguarda.Api`, which refuses to start without it. Stop the running Web/Api processes before a Debug build or an `dotnet ef` command — they lock the output files.

## Coding Style & Naming Conventions
Follow `.editorconfig`: 4-space indentation for C#, 2 spaces for JSON/XML/project files, CRLF endings, UTF-8, final newline, and trimmed trailing whitespace except Markdown. Use file-scoped namespaces, `System` usings first, usings outside namespaces, and braces for control flow. Code identifiers must be in English; reserve Portuguese for business documentation and localized UI text. Interfaces use the `IName` pattern. Keep visible user strings in localization resources where the existing feature does so. Form field help text follows one mandatory rule (`docs/padrao-ui.md` §8.2): a constraint that affects input (format, length, password rules) is **visible** below the field via the `_FieldHint` partial; an explanation of a consequence or of why a field is read-only goes in the `?` **tooltip** next to the label via the `_FieldHelp` partial, placed inside a `.form-label-row` wrapper **outside** the `<label>`. Both require `aria-describedby="<FieldId>-help"` on the field. Never handwrite `<span class="form-text">`. `FieldHelpConventionTests` fails the build on all three violations.

## Testing Guidelines
Tests use xUnit with `Microsoft.NET.Test.Sdk` and `coverlet.collector`. Name test classes `*Tests` and keep feature folders aligned with source areas, for example `tests/Retaguarda.UnitTests/Sites/SiteServiceTests.cs`. Use focused fakes such as `FakeSiteRepository` for unit tests. Add or update unit tests for business rules, validators, middleware, and regressions; use integration tests for behavior that depends on EF Core, Identity, or app wiring.

## Commit & Pull Request Guidelines
Use concise, scoped subjects with the Jira key, for example `Task: Exibir a planta ativa no topbar (RB-12)`. Branches follow `feature/<project-slug>/<kebab-case-summary>`. Keep commits scoped and descriptive. PRs should summarize behavior changes, link the issue, mention migrations/configuration changes, include screenshots for UI work, and state the verification commands run. Before changing CRUD behavior, consult `docs/padrao-crud.md`; for UI work, `docs/padrao-ui.md`; for the quality bar, `docs/baseline-microsoft.md`. Collaboration rules (small steps, plan before executing, report at the end) live in `CLAUDE.md` and apply to agents too.
