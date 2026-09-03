# Baseline de Boas Práticas Microsoft — Retaguarda Base

> **Versão:** 1.1 · **Data:** 2026-08-31 (reset para o projeto-base) · **Alvo:** .NET 10 / ASP.NET Core (`view=aspnetcore-10.0`)
> **Régua arquitetural:** mantém a arquitetura em camadas descrita no `CLAUDE.md`, validada contra os princípios de Clean Architecture da Microsoft.

---

## 1. Como usar este documento

Este é o **checklist de conformidade** do projeto com as práticas documentadas pela Microsoft no [Microsoft Learn](https://learn.microsoft.com/en-us/aspnet/core/?view=aspnetcore-10.0). Cada linha aponta uma prática oficial, sua fonte, o estado atual no projeto e a ação/etapa em que será endereçada.

**Use assim:**
- **Antes de cada etapa nova**, consulte as seções afetadas para saber o que a Microsoft recomenda.
- **Ao concluir uma etapa**, atualize o status das linhas tocadas.
- Não tratar tudo de uma vez: itens ⏳ são deliberadamente adiados para a etapa apropriada — isso respeita a regra de "etapas pequenas" do `CLAUDE.md`.

**Legenda de status:**

| Símbolo | Significado |
|---|---|
| ✅ | Conforme — implementado segundo a recomendação |
| ⚠️ | Parcial / requer atenção — existe, mas com lacuna ou inconsistência |
| ❌ | Ausente — aplicável e ainda não feito |
| ⏳ | Futuro — pertinente a uma etapa posterior; ainda não cabia implementar |
| N/A | Não se aplica ao escopo do Retaguarda (ver §19 do doc de orientação) |

> **Importante:** este documento é uma *régua*, não um plano de execução. Nenhuma correção listada aqui deve ser implementada sem autorização explícita, conforme as regras de colaboração do `CLAUDE.md`.

---

## 2. Arquitetura da aplicação

Fonte: [Architect modern web applications with ASP.NET Core and Azure](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/) · [Common web application architectures](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures) · [Architectural principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)

| Prática recomendada | Estado | Onde está / ação |
|---|---|---|
| Separação em camadas com dependências apontando para dentro (Dependency Inversion) | ✅ | `Web`/`Api` → `Business` → `Data`/`Printing`/`Reporting` → `Shared`; `Business` define interfaces |
| O núcleo (domínio) não depende de infraestrutura nem de framework web | ✅ | Resolvido (2026-05-24): o `SecurityHeadersMiddleware` foi movido para o novo projeto `Retaguarda.AspNetCore` (infra web). `Retaguarda.Shared` voltou a ser agnóstico de framework — sem `FrameworkReference`. `Web` → `Shared` + `AspNetCore`; `Api` → `AspNetCore` |
| Testabilidade do núcleo garantida pela inversão de dependências | ✅ | Confirmado (2026-08-12): serviços e regras puras (`MovementDetection`, `DeviceHealthRules`, `FleetStatuses`, `ExportPeriodRules`) são testados sem banco, com fakes escritos à mão sobre as interfaces de repositório (ver §11) |
| Evitar dependências circulares | ✅ | Nenhuma detectada |

---

## 3. Fundamentos: DI, Configuração, Middleware

Fonte: [ASP.NET Core fundamentals](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/?view=aspnetcore-10.0) · [Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0) · [Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0)

| Prática recomendada | Estado | Onde está / ação |
|---|---|---|
| Serviços registrados via DI no `Program.cs` | ✅ | Web e Api |
| Configuração em camadas (appsettings + ambiente + secrets) | ✅ | `appsettings.json` + `appsettings.Development.json` + User Secrets/env |
| Ordem de middleware correta (segurança cedo; auth antes de authz) | ✅ | `SecurityHeaders` é o primeiro; `UseAuthentication` antes de `UseAuthorization` |
| Middleware customizado seguindo a convenção (classe + extensão `Use…`) | ✅ | `SecurityHeadersMiddleware` + `UseSecurityHeaders` |

---

## 4. Segurança

Fonte raiz: [ASP.NET Core security topics](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-10.0)

### 4.1 Headers de segurança e cabeçalhos HTTP

> A Microsoft não mantém uma página única de "security headers" (a referência de mercado é o OWASP); a base é a seção de segurança acima.

| Prática | Estado | Onde está / ação |
|---|---|---|
| Content-Security-Policy restritiva | ✅ | `SecurityHeadersMiddleware`; Web com `script-src 'self'`, Api `default-src 'none'` |
| `X-Content-Type-Options: nosniff` | ✅ | Middleware |
| Anti-clickjacking (`X-Frame-Options` + `frame-ancestors`) | ✅ | Middleware |
| `Referrer-Policy` | ✅ | Middleware (`no-referrer`) |
| `Permissions-Policy` | ✅ | Middleware (2026-09-01, item 8 da avaliação técnica): `camera=(), microphone=(), geolocation=()`. Configurável por host, como a CSP |
| `Cache-Control: no-store` nas respostas autenticadas | ✅ | Web (2026-09-01): filtro global `ResponseCacheAttribute` no MVC — fica no filtro, e **não** no middleware de headers, para não matar o cache dos arquivos estáticos |
| Não vazar tecnologia do servidor (`Server` header) | ✅ | `Kestrel.AddServerHeader = false` em Web e Api |

### 4.2 HTTPS e HSTS

Fonte: [Enforce HTTPS in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-10.0)

| Prática | Estado | Onde está / ação |
|---|---|---|
| `UseHttpsRedirection` | ✅ | Web mantém (browser). Resolvido na API (2026-05-24): `UseHttpsRedirection` **removido** — TLS é terminado no proxy reverso/host (on-premise); redirect não é obedecido por clientes não-browser e era no-op. Some o warning `Failed to determine the https port` |
| `UseHsts` fora de Development | ✅ | Web (`if (!IsDevelopment())`). API não usa HSTS — correto: HSTS é instrução de browser |

### 4.3 Antiforgery (CSRF)

Fonte: [Prevent CSRF attacks](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0) · [CA5391](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca5391)

| Prática | Estado | Onde está / ação |
|---|---|---|
| Token antiforgery em forms (gerado pelo Form Tag Helper) | ✅ | Iniciado (2026-05-24): forms de login e logout geram o token automaticamente |
| `[ValidateAntiForgeryToken]` / `[AutoValidateAntiforgeryToken]` em ações que modificam estado | ✅ | Iniciado (2026-05-24): `Login` (POST) e `Logout` decorados. Manter o padrão nos CRUDs (CA5391 flagra) |

### 4.4 Segredos e dados sensíveis

Fonte: [Safe storage of app secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)

| Prática | Estado | Onde está / ação |
|---|---|---|
| Nunca commitar segredos | ✅ | `appsettings.Production.json` e `*.Local.json` no `.gitignore` |
| User Secrets em desenvolvimento | ✅ | `UserSecretsId` no `Retaguarda.Web.csproj` |
| Variáveis de ambiente / cofre em produção | ✅ | `ConnectionStrings__DefaultConnection` por env (doc §9.3); Azure Key Vault fora de escopo (on-premise) |

### 4.5 Data Protection

Fonte: [Configure ASP.NET Core Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0) · [Key storage providers](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0)

| Prática | Estado | Onde está / ação |
|---|---|---|
| Persistir as chaves fora do container (volume ou provedor externo) | ✅ | Resolvido (2026-05-24): Web usa `PersistKeysToFileSystem` quando `DataProtection:KeysPath` está definido; no compose aponta para o volume nomeado `retaguarda-web-dataprotection-keys`. Em dev local, mantém o store default do SO |

### 4.6 Autenticação e autorização

Fonte: [Authentication overview](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0) · [Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0)

| Prática | Estado | Onde está / ação |
|---|---|---|
| Identity configurado com store EF Core | ✅ | `AddIdentity<ApplicationUser, ApplicationRole>` + `AddEntityFrameworkStores` + `AddSignInManager<ApplicationSignInManager>` (bloqueia inativos). `ApplicationUser`/`ApplicationRole` estendem o Identity com auditoria + soft delete |
| Política de senha forte | ✅ | 8+ caracteres, dígito, maiúscula, minúscula, não-alfanumérico; e-mail único. Teto de 128 caracteres (2026-09-01) nos dois hosts, aplicado ANTES da verificação de hash — senha gigante só queima CPU no PBKDF2 |
| Lockout de conta declarado explicitamente | ✅ | 2026-09-01: 5 tentativas / 5 min nos dois `Program.cs`. São os mesmos valores do default do Identity, escritos de propósito — configuração de segurança implícita é configuração que ninguém revisa. Protege UMA conta; a proteção volumétrica por IP é o rate limiting (§10) |
| Desativar a conta encerra as sessões já abertas | ✅ | Corrigido em 2026-08-31 (item 1 da avaliação técnica). Bloquear o próximo login não bastava: o cookie e o refresh token emitidos continuavam válidos por horas, porque `UserManager.UpdateAsync` não altera o security stamp. `UserService.UpdateAsync` detecta a transição ativo → inativo e chama `IUserRepository.RegenerateSecurityStampAsync` (o `SecurityStampValidator` passa a rejeitar o cookie, em até 30 min — `ValidationInterval` default) + `IRefreshTokenRepository.RevokeAllForUserAsync` (derruba a sessão da Api). A **exclusão** já estava coberta pelo soft delete: o global query filter faz o usuário sumir e o principal é rejeitado pelo mesmo caminho |
| Pipeline com `UseAuthentication`/`UseAuthorization` na ordem correta | ✅ | Web e Api (Api: `UseAuthentication` + `UseAuthorization`, etapa 3.1.a/2026-06-05) |
| Autorização baseada em **política**, não em papel fixo | ✅ | 2026-09-03: os quatro cadastros da Web (Plantas, Usuários, Plantas do usuário, Acessos) saíram de `[Authorize(Roles = Admin)]` para `[Authorize(Policy = PlatformPermissions.…)]` por action. As políticas são criadas sob demanda pelo `PermissionPolicyProvider` a partir do catálogo (`IPermissionProvider`), e a permissão chega ao principal como claim de papel (`identity."RoleClaims"`, copiada pelo `UserClaimsPrincipalFactory`). Menu, botões e ações de linha usam `User.HasPermission(...)`. `ControllerPermissionConventionTests` quebra o build se uma action de cadastro ficar sem política. **A Api ainda não emite permissões no JWT** — lá a autorização continua por papel |
| Negar por padrão (`AuthorizationOptions.FallbackPolicy`) | ❌ | **Aberto.** Sem `FallbackPolicy`, um controller novo sem `[Authorize]` nasce público. Ao fechar, dois pontos precisam de atenção: `Home/Error` não tem nem `[Authorize]` nem `[AllowAnonymous]`, e `/health` + `/health/ready` passariam a exigir login |
| Telas de login / fluxo de autenticação | ⚠️ | Parcial (etapa 2.1a, 2026-05-24): login + logout por cookie funcionando. Recuperação de senha = etapa 2.1b |
| JWT na API | ✅ | Fluxo em 2 etapas (login → select-site) emitindo access + refresh token; `AddJwtBearer` + `IdentityCore` na Api; refresh token persistido (hash) em `[identity].[RefreshTokens]`; `refresh` com rotação (revoga o usado, reemite o par, revalida usuário/planta) e `logout` (revogação, restrito ao dono do token) |
| Detecção de reuso de refresh token | ✅ | 2026-09-01 (item 5 da avaliação técnica): reapresentar um token **revogado** revoga toda a sessão do usuário e loga `LogWarning` com `UserId` (nunca o token nem o hash) — não dá para saber se quem rotacionou foi o dono ou o ladrão, então derruba os dois e força login novo. Token apenas **expirado** não conta como reuso: é validade vencida de cliente offline, e derrubar a sessão deslogaria gente legítima |

---

## 5. Tratamento de erros

Fonte: [Handle errors in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0) · [Handle errors in ASP.NET Core APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0)

| Prática | Estado | Onde está / ação |
|---|---|---|
| Developer Exception Page só em Development | ✅ | Comportamento default do template (Web) |
| `UseExceptionHandler` em produção (Web) | ✅ | `UseExceptionHandler("/Home/Error")` quando `!IsDevelopment()` |
| `AddProblemDetails` + tratamento global na API (RFC 9457/7807) | ✅ | Resolvido (2026-05-24): `AddProblemDetails()` + `UseExceptionHandler()` (produção) + `UseStatusCodePages()` na API. Respostas de erro saem em `problem+json` |
| Não vazar detalhes de exceção em produção | ✅ | Garantido pelo gate de ambiente |
| Exit code de falha de startup propagado (containers) | ✅ | Resolvido (2026-05-24): `catch ... when (ex is not HostAbortedException)` + `return 1` em Web e Api. Falha de startup propaga exit ≠ 0; `dotnet ef` não loga mais "terminated unexpectedly" |

---

## 6. Logging

Fonte: [Logging in .NET and ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-10.0)

| Prática | Estado | Onde está / ação |
|---|---|---|
| Logging estruturado via abstração `ILogger` | ✅ | Serilog plugado no host (`UseSerilog`), compatível com `ILogger<T>` |
| Sinks/destinos configuráveis | ✅ | File (prod) + Console (dev); rolling diário, retenção 30 dias |
| Captura de erros de startup antes do host | ✅ | `CreateBootstrapLogger` + `try/catch/finally` |
| Log de requisições HTTP | ✅ | `UseSerilogRequestLogging` após `UseRouting` |
| Não logar dados sensíveis (senhas, tokens, connection strings) | ✅ | Nenhum log de segredo identificado |

---

## 7. Configuração de host e filtragem

Fonte: [Host filtering com Kestrel](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/host-filtering?view=aspnetcore-10.0)

| Prática | Estado | Onde está / ação |
|---|---|---|
| `AllowedHosts` restrito quando exposto publicamente | ⏳ | Decisão (2026-05-24): mantido `"*"` na base (dev). **Ação de deploy:** produção define os hosts reais via `appsettings.Production.json` (gitignored) ou env `AllowedHosts`. Sem mudança de código agora |

---

## 8. Localização e globalização

Fonte: [Globalization and localization in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0)

| Prática | Estado | Onde está / ação |
|---|---|---|
| `AddLocalization` + `RequestLocalizationOptions` | ✅ | Web e Api |
| `IStringLocalizer` / `IViewLocalizer` (sem strings hardcoded) | ✅ | `SharedResources`, `MessagesController`, views |
| Estratégia de seleção de cultura por request | ✅ | Cookie (Web) / `Accept-Language` (Api); default pt-BR |
| Estrutura aceita novos idiomas sem mudar código | ✅ | Basta adicionar `.resx` (doc §12) |

---

## 9. Health checks

Fonte: [Health checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0)

| Prática | Estado | Onde está / ação |
|---|---|---|
| Endpoint de health para orquestrador/load balancer | ✅ | Resolvido (2026-05-24): Web e Api expõem `/health` |
| Checks de dependências (DB, etc.) | ✅ | Resolvido (2026-07-29): `DatabaseHealthCheck` (`CanConnectAsync`, em `Retaguarda.AspNetCore`) exposto em **`/health/ready`** (readiness) em Web e Api; `/health` segue **liveness** (sem depender do banco — um blip do DB não reinicia o processo). Validado: DB fora → `/health` 200, `/health/ready` 503 |
| Orquestrador consumindo o endpoint | ✅ | Resolvido (2026-05-24): `HEALTHCHECK` nos dois Dockerfiles consulta `/health` via `curl` (image-level — vale para `docker run` e `docker compose ps`) |

---

## 10. Performance e best practices gerais

Fonte: [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0) · [Rate limiting middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)

| Prática | Estado | Onde está / ação |
|---|---|---|
| Acesso a dados assíncrono, sem bloqueio de threads | ✅ | Resolvido (2026-08-12, verificação): todos os repositórios são `async`/`await` de ponta a ponta, com `CancellationToken` propagado; nenhum `.Result`/`.Wait()` no código |
| Não buscar mais dados que o necessário (paginação/projeção) | ✅ | As três listagens (`Site`, `User`, `Role`) paginam e projetam no SQL (`AsNoTracking` + DTOs). **Manter o padrão** em cada listagem nova — filtrar/paginar em memória é o erro clássico |
| SQL cru parametrizado (sem concatenação de entrada do usuário) | ✅ | A base não tem SQL cru. Se precisar, use `SqlQueryRaw` com `NpgsqlParameter` — nunca interpolação de entrada do usuário |
| `IHttpClientFactory` em vez de `new HttpClient()` | ⏳ | Quando houver chamada HTTP de saída |
| Cache de dados quentes (`IMemoryCache`/distribuído) | ⏳ | Avaliar quando houver consulta cara e repetida |
| Rate limiting na API pública | ✅ | Implementado em 2026-08-31 (item 2 da avaliação técnica). `AddRateLimiter` + `UseRateLimiter` nos DOIS hosts, com política de janela fixa **por IP** em `Retaguarda.AspNetCore/Security/AuthRateLimiting.cs`: `auth-credentials` (20/min — login, seleção de planta, esqueci/redefinir senha) e `auth-refresh` (60/min). Aplicado endpoint a endpoint via `[EnableRateLimiting]`, nunca global, então `/health` e as telas autenticadas ficam de fora por construção. Recusa devolve **429 + `Retry-After`**: ProblemDetails com `code=too_many_requests` na Api, página localizada na Web. **Atenção ao publicar endpoint de escrita:** ele não é coberto automaticamente — precisa do atributo. Efeito colateral do IP compartilhado registrado como gotcha no CLAUDE.md |

---

## 11. Testes

Fonte: [Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0) · [Test ASP.NET Core MVC apps](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/test-asp-net-core-mvc-apps)

| Prática | Estado | Onde está / ação |
|---|---|---|
| Separar testes de unidade e de integração em projetos distintos | ✅ | `Retaguarda.UnitTests` e `Retaguarda.IntegrationTests` existem |
| Testes de unidade cobrindo lógica isolada | ✅ | **75 testes** em `Retaguarda.UnitTests`, com pastas espelhando as áreas (`Sites/`, `Users/`, `Roles/`, `Exporting/`, `Security/`, `Data/`, `Api/`). Cobrem serviços, validadores e regras puras; dependências substituídas por **fakes escritos à mão** (não há lib de mocking). **Ação:** manter o padrão a cada fatia nova |
| Integração com `WebApplicationFactory<T>` / `TestServer` | ❌ | **Lacuna consciente do base.** `Retaguarda.IntegrationTests` é o template do `dotnet new` (`UnitTest1.cs`) — não há infraestrutura ali. Decisão de 2026-08-31: não montar agora (custo de manutenção alto para dev solo); a validação ponta a ponta é manual. Reavaliar quando um projeto derivado tiver domínio próprio |

---

## 12. Docker e deploy

Fonte: [Data Protection em containers](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0) · [Health checks](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0)

| Prática | Estado | Onde está / ação |
|---|---|---|
| Build multi-stage | ✅ | `Dockerfile.web` / `Dockerfile.api` |
| `Directory.Build.props` aplicado no build de container | ✅ | Copiado nos dois Dockerfiles (2026-05-24). Sem ele, a imagem ignora os padrões centralizados (ImplicitUsings, analyzers, warnings-as-errors) |
| CI exercita o build de imagem Docker | ❌ | **Não há CI** (decisão de 2026-08-31: dev solo). O `docker compose build` é manual, antes do deploy — regressão de container não é pega por nenhum gate automático |
| Volume para chaves de Data Protection | ✅ | Volume `retaguarda-base-web-dataprotection-keys` montado em `/app/keys` |
| `healthcheck` da imagem/compose | ✅ | Resolvido (2026-05-24): `HEALTHCHECK` nos Dockerfiles (ver §9) |
| Política de `restart` | ✅ | `restart: unless-stopped` em `web` e `api`; `postgres` de dev é `no` (sobe só quando pedido) |
| Nome explícito do projeto Compose | ✅ | `name: retaguarda-base` nos dois composes — sem isso o Compose usa o nome da pasta (`docker`) e projetos irmãos recriam os containers uns dos outros |
| Ambiente não fixado em Development para produção | ✅ | `docker-compose.yml` é o de dev (`Development`); `docker-compose.prod.yml` usa `Production` com segredos vindos do `.env` |

---

## 13. Frontend e acessibilidade

> A Microsoft não prescreve um stack de frontend obrigatório; aqui a régua é o `docs/padrao-ui.md` + WCAG. Listado para completude, **fora do escopo das práticas Microsoft**.

| Prática | Estado | Onde está / ação |
|---|---|---|
| Tema centralizado em tokens (CSS custom properties) | ✅ | `wwwroot/css/theme.css` |
| Acessibilidade (skip-link, foco visível, contraste) | ✅ | Casca + tokens; regra de contraste alto documentada em `docs/padrao-ui.md` §6/§9 |
| Texto de ajuda de campo padronizado e ligado por `aria-describedby` | ✅ | 2026-09-02: dois formatos com regra de escolha (`docs/padrao-ui.md` §8.2) — `_FieldHint` (visível, para restrição de preenchimento) e `_FieldHelp` (tooltip, para explicação secundária); ambos expõem o texto ao leitor de tela pelo `aria-describedby` do campo |
| Sem framework JS / sem etapa de build | ✅ | JS vanilla (`forms.js`); libs versionadas em `wwwroot/lib` |

---

## 14. Resumo das lacunas priorizadas

Itens que hoje estão ❌/⚠️ e são aplicáveis ao estado atual (não ⏳):

1. ~~**Arquitetura (§2):** middleware web dentro de `Shared` vaza ASP.NET Core para o domínio.~~ ✅ **Resolvido em 2026-05-24** (projeto `Retaguarda.AspNetCore`).
2. ~~**API — ProblemDetails (§5):** sem tratamento de erro estruturado.~~ ✅ **Resolvido em 2026-05-24** (`AddProblemDetails` + `UseExceptionHandler` + `UseStatusCodePages`).
3. ~~**Data Protection (§4.5 / §12):** chaves efêmeras em container.~~ ✅ **Resolvido em 2026-05-24** (volume de chaves).
4. ~~**Health checks (§9):** Web sem endpoint; compose sem `healthcheck`.~~ ✅ **Resolvido em 2026-05-24** (`/health` na Web + `HEALTHCHECK` nas imagens).
5. ~~**Testes (§11):** zero cobertura da lógica já escrita (middleware).~~ ✅ **Resolvido em 2026-05-24** (`SecurityHeadersMiddlewareTests`, 6 testes). Cobertura segue crescendo com o código.
6. ~~**Startup exit code (§5):** falha de startup mascarada com exit 0.~~ ✅ **Resolvido em 2026-05-24** (`when (ex is not HostAbortedException)` + `return 1`).
7. ~~**HTTPS na API (§4.2):** redirect inoperante.~~ ✅ **Resolvido em 2026-05-24** (removido; TLS no proxy). **`AllowedHosts` (§7):** ⏳ ação de deploy (documentada).
8. **CI (§12):** ❌ removido em 2026-08-31 (dev solo). Não há gate automático nenhum: build, testes e `docker compose build` dependem de disciplina manual antes do deploy.

9. **Testes de integração (§11):** ❌ **aberto, por decisão.** Login, seleção de planta, os três CRUDs e a exportação são validados **à mão** com a app no ar; nada disso é reexecutado automaticamente. Uma regressão de rota, DI, autorização ou tradução de consulta EF só apareceria em uso. É a maior lacuna de qualidade da base — assumida conscientemente em 2026-08-31 (custo × benefício para dev solo).
10. ~~**Rate limiting (§10):** ❌ a Api hoje só expõe autenticação.~~ ✅ **Resolvido em 2026-08-31:** endpoints anônimos de autenticação limitados por IP nos dois hosts (§10). Endpoint novo de escrita continua **não** coberto por padrão — precisa do `[EnableRateLimiting]`.

> A ordem acima é sugestão de prioridade (impacto × custo), **não** uma autorização de execução. Cada item vira uma etapa quando você decidir.

---

## 15. Fontes (Microsoft Learn)

- [ASP.NET Core Best Practices](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/best-practices?view=aspnetcore-10.0)
- [ASP.NET Core fundamentals](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/?view=aspnetcore-10.0)
- [Middleware](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/middleware/?view=aspnetcore-10.0)
- [Configuration](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/configuration/?view=aspnetcore-10.0)
- [Architect modern web applications with ASP.NET Core and Azure](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/)
- [Common web application architectures](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/common-web-application-architectures)
- [Architectural principles](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/architectural-principles)
- [ASP.NET Core security topics](https://learn.microsoft.com/en-us/aspnet/core/security/?view=aspnetcore-10.0)
- [Enforce HTTPS in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/enforcing-ssl?view=aspnetcore-10.0)
- [Prevent Cross-Site Request Forgery (XSRF/CSRF)](https://learn.microsoft.com/en-us/aspnet/core/security/anti-request-forgery?view=aspnetcore-10.0)
- [CA5391 — Use antiforgery tokens](https://learn.microsoft.com/en-us/dotnet/fundamentals/code-analysis/quality-rules/ca5391)
- [Safe storage of app secrets in development](https://learn.microsoft.com/en-us/aspnet/core/security/app-secrets?view=aspnetcore-10.0)
- [Configure ASP.NET Core Data Protection](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/configuration/overview?view=aspnetcore-10.0)
- [Key storage providers](https://learn.microsoft.com/en-us/aspnet/core/security/data-protection/implementation/key-storage-providers?view=aspnetcore-10.0)
- [Authentication overview](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/?view=aspnetcore-10.0)
- [Introduction to Identity](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/identity?view=aspnetcore-10.0)
- [Handle errors in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling?view=aspnetcore-10.0)
- [Handle errors in ASP.NET Core APIs](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/error-handling-api?view=aspnetcore-10.0)
- [Logging in .NET and ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/logging/?view=aspnetcore-10.0)
- [Host filtering com Kestrel](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/host-filtering?view=aspnetcore-10.0)
- [Globalization and localization](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/localization?view=aspnetcore-10.0)
- [Health checks in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/host-and-deploy/health-checks?view=aspnetcore-10.0)
- [Rate limiting middleware](https://learn.microsoft.com/en-us/aspnet/core/performance/rate-limit?view=aspnetcore-10.0)
- [Integration tests in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/test/integration-tests?view=aspnetcore-10.0)
- [Test ASP.NET Core MVC apps](https://learn.microsoft.com/en-us/dotnet/architecture/modern-web-apps-azure/test-asp-net-core-mvc-apps)
