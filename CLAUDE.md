# Instruções para o Claude Code

Este repositório é o **Retaguarda Base** — o **ponto de partida** de novos projetos, não um produto.
Ele entrega a plataforma pronta e reaproveitável: autenticação, multi-site (plantas), CRUDs de
**Papéis, Usuários e Plantas**, localização, auditoria com soft delete, cabeçalhos de segurança,
exportação Excel/PDF, logging estruturado, PostgreSQL em Docker e deploy. **Domínio de negócio,
nenhum** — é isso que cada projeto derivado constrói por cima.

Nasceu como fork reduzido do **Retaguarda Argos** (rastreamento por RFID), do qual todo o domínio
foi removido; o que sobrou é a fundação que já estava testada em produção.

> Os **namespaces do código são `Retaguarda.*`**. Ao iniciar um projeto real, rode
> `tools/rename.sh` (ver §"Como iniciar um projeto a partir desta base").

### Documentos do repositório (fontes da verdade)

| Documento | O que é | Quando consultar |
|---|---|---|
| `docs/padrao-crud.md` | **Template oficial de CRUD** (cadastros): camadas, validação, localização e layout das telas — `Site` é a implementação de referência | **Obrigatório** antes de criar ou alterar qualquer cadastro |
| `docs/padrao-ui.md` | **Padrão de interface:** casca única, tema por tokens, componentização, loading states, JS vanilla, acessibilidade e libs permitidas | Antes de mexer em qualquer tela, CSS ou JS |
| `docs/baseline-microsoft.md` | **Régua de qualidade:** práticas oficiais da Microsoft (.NET 10 / ASP.NET Core) × estado atual do código | Antes de implementar uma etapa e ao validá-la no fim (regra 10) |
| `docs/comandos-uteis.md` | Comandos do dia a dia (git, dotnet, docker, psql) com as particularidades desta máquina de dev | Quando precisar de um comando que não está resumido aqui |
| `docs/padrao-tarefas-jira.md` | Formato das tarefas no Jira | Ao abrir ou descrever uma tarefa |
| `docs/deploy.md` | Deploy (POC): VPS Ubuntu + Docker + Caddy + Cloudflare | Só em assuntos de deploy |

> **Fonte da verdade vive no repositório, não em memória pessoal.** Tudo que *qualquer* LLM ou
> desenvolvedor deve seguir está versionado no repo (`CLAUDE.md` + `docs/`). Memória local de um
> agente (ex.: `~/.claude/`) vale só para aquela máquina/sessão — nunca dependa dela para regras do
> projeto.

---

## Regras de Colaboração (inegociáveis)

### 1. Trabalho por etapas pequenas
- Avance **apenas até o limite do que foi pedido** na mensagem atual.
- Não execute "próximos passos óbvios" sem permissão explícita.
- Ao concluir uma etapa, **pare e aguarde** a próxima instrução.

### 2. Plano antes de execução
- Antes de criar ou modificar arquivos, **apresente um plano curto**: o que vai criar/alterar, onde, e por quê.
- Aguarde o "OK" antes de executar.
- Exceção: mudanças triviais (typo, rename de variável local) podem ir direto.

### 3. Idiomas do código
- **Código (classes, métodos, propriedades, variáveis):** inglês.
- **Tabelas e colunas do banco:** inglês, PascalCase.
- **Comentários no código:** português.
- **Logs estruturados:** inglês.
- **Mensagens visíveis ao usuário (UI, erros da API, e-mails):** português via `IStringLocalizer` + `.resx`. **Nunca hardcoded.**
- **Chaves dos `.resx`:** inglês ou snake_case (ex: `site_not_found`).
- **Nomes de branches:** inglês, kebab-case, com o ticket do Jira (ex: `feature/rb/user-profile-photo`).

### 4. Glossário é fonte da verdade
- Esta base **não tem glossário de domínio** — ela não tem domínio.
- **O primeiro passo de um projeto derivado é criar `docs/dominio-<projeto>.md`** com o glossário
  PT↔EN, o modelo de dados e as decisões travadas, e registrá-lo na tabela de documentos acima.
- Enquanto ele não existir: se aparecer um termo novo de negócio, **pare, pergunte qual deve ser o
  nome em inglês** e registre-o antes de codificar.

### 5. Localização desde o dia 1
- `AddLocalization`, `AddViewLocalization` e `RequestLocalizationOptions` já estão configurados (`Program.cs` da Web e da Api). pt-BR é a única cultura suportada hoje.
- Toda string visível ao usuário usa `IStringLocalizer<T>` ou `@Localizer["chave"]`.
- **Arquivo único de recursos:** `src/Retaguarda.Shared/Resources/SharedResources.pt-BR.resx` (chaves de UI, validação e feedback). As views recebem `IViewLocalizer` pelo `_ViewImports.cshtml`; a Api tem `Resources/Controllers/AuthController.pt-BR.resx` para as mensagens dela. Não crie um `.resx` por tela.
- Adicionar idioma = adicionar `.resx`, sem mudança de código. Nada pode depender de idioma fixo.
- **O rótulo "Planta" é só texto:** a entidade continua `Site` no código e no banco. Para chamá-la
  de Filial, Loja, Obra ou Unidade, edite as chaves `site_*`/`nav_sites` no `.resx` — nunca renomeie
  a entidade.

### 6. Respeitar decisões e limites
- Decisões de CRUD/UI já fechadas: `docs/padrao-crud.md` §9 e `docs/padrao-ui.md`.
- Se algo do documento parecer errado ou desatualizado, **avise e pergunte** — não decida sozinho.

### 7. Testabilidade
- Cada etapa concluída deve ser **testável isoladamente**.
- Ao final da etapa, indicar exatamente como testar: comando, URL, query SQL, requisição HTTP.
- Se a etapa pedida não for testável isoladamente, **avisar antes de começar**.

### 8. Perguntar em vez de adivinhar
- Em caso de ambiguidade ou falta de informação, **perguntar antes de codar**.
- Prefira uma pergunta a entregar trabalho que precisará ser refeito.

### 9. Não instalar nada sem aviso
- Antes de adicionar pacote NuGet, instalar ferramenta global, ou alterar variáveis de ambiente, **pedir permissão**.
- Mesmo para pacotes "óbvios" da stack.

### 10. Relatório ao fim de cada etapa
Ao concluir cada etapa, entregar resumo curto com:
- **O que foi feito** (lista objetiva).
- **Arquivos criados ou modificados** (caminhos).
- **Como testar** (comandos, URLs).
- **O que NÃO foi feito** (limites da etapa, para evitar confusão sobre escopo).

**Definition of Done (checklist obrigatório antes de declarar a etapa pronta):**
- [ ] A etapa foi validada contra as seções afetadas de `docs/baseline-microsoft.md`.
- [ ] Nenhuma nova lacuna foi introduzida (não regredir itens já ✅).
- [ ] Se a etapa fechou/abriu uma lacuna do baseline, o status (✅/⚠️/❌/⏳) foi atualizado lá.
- [ ] `dotnet build` (Release, warnings-as-errors) e `dotnet test` passam — ou foi explicado por que não se aplica.

### 11. Fonte da verdade e anti-alucinação
- **Citar fonte ou verificar:** nenhuma afirmação sobre prática da Microsoft sem link de `docs/baseline-microsoft.md` ou verificação na documentação oficial (`learn.microsoft.com`). Não afirmar de memória.
- **Ler antes de afirmar:** o estado de um arquivo, configuração ou dependência se confirma **lendo o arquivo**, nunca por suposição ou pelo que "deveria" estar lá.
- **Nunca inventar:** não criar diretriz, API, pacote, opção de configuração ou nome de domínio que não exista. Na dúvida, aplicar a regra 8 (perguntar).

---

## Como iniciar um projeto a partir desta base

```bash
# 1. Clone com outro nome e desconecte do repositório da base
git clone https://github.com/JoaoRomeiro/retaguarda-base.git meu-projeto
cd meu-projeto && rm -rf .git && git init

# 2. Renomeie tudo (namespaces, csproj, solution, banco, containers, portas)
./tools/rename.sh --name MeuProjeto --dry-run   # simula
./tools/rename.sh --name MeuProjeto             # aplica

# 3. Siga os "próximos passos" que o script imprime (segredos, banco, migration, gate)
```

Opções úteis: `--site-label Filial --site-label-plural Filiais` troca o rótulo "Planta" no
repositório inteiro (a entidade continua `Site`), e `--port-offset 20` desloca todas as portas
para o projeto novo rodar ao lado da base. Detalhes no cabeçalho do próprio `tools/rename.sh`.

**Depois do rename, o primeiro passo do projeto novo é criar o `docs/dominio-<projeto>.md`** (regra 4).

---

## Stack do Projeto (resumo)

- **.NET 10 (LTS)** + **C# 14**
- **ASP.NET Core MVC** (Web admin) + **ASP.NET Core Web API** (clientes externos)
- **Identity** com cookie (web) + **JWT** com refresh token (API)
- **EF Core 10** Code-First com Migrations, sobre **PostgreSQL 17 em container** (Npgsql)
- **Serilog** (logging estruturado, sinks Console + File)
- **FluentValidation**, **Mapster**, **xUnit**
- **ClosedXML** (Excel, em `Reporting`) e **QuestPDF** licença Community (PDF, em `Printing`)
- **Docker** (multi-stage, apenas a aplicação); deploy POC em **VPS Ubuntu** atrás de Caddy + Cloudflare
- **Bootstrap 5** + `theme.css` no frontend — sem build step, sem framework JS

> Disciplina de build vem do `Directory.Build.props` na raiz: `Nullable`, `ImplicitUsings`, **`TreatWarningsAsErrors`**, `EnforceCodeStyleInBuild` e analyzers CA/IDE em modo `Recommended`. Supressões globais (CA1716, CA1848, CA1873) estão justificadas lá — não adicione outra sem justificar no mesmo formato.

## Arquitetura (resumo)

Projetos em camadas dentro de uma única solution (`Retaguarda.sln`):

```
Hosts:       Retaguarda.Web        Retaguarda.Api
                   │                     │
                   ├─────────┬───────────┤
                   ▼         ▼           ▼
             Business   AspNetCore   (Web também: Printing, Reporting)
                   │         │
                   └────► Data ────► Shared
```

- Os **hosts** (`Web`, `Api`) consomem `Business` para regra de negócio. Também referenciam `Data` — mas **só para wiring**: registrar o `ApplicationDbContext`, os repositórios no DI e rodar as migrations. Regra de negócio em controller ou em host é desvio do padrão.
- `Retaguarda.AspNetCore` guarda a infraestrutura web compartilhada pelos dois hosts: `CurrentUserService`, `SecurityHeadersMiddleware`, `DatabaseHealthCheck`.
- `Business` define as interfaces de **serviço** (`ISiteService`, `IUserService`) e as de **exportação** (`IExcelExporter`/`IPdfExporter`) — estas últimas implementadas em `Reporting`/`Printing`, que por isso apontam **para** `Business` (só a `Web` os referencia, para o DI).
- **Atenção:** as interfaces de **repositório** (`ISiteRepository`, `IUserRepository`, …) moram em `Retaguarda.Data/Repositories/`, junto da implementação — não em `Business`. Siga o que já existe.
- Tudo pode usar `Shared`. Nunca dependência circular.

**Fatia vertical típica** (ver `docs/padrao-crud.md`): `Data/Entities/X.cs` + `Data/Repositories/XRepository.cs` → `Business/X/XService.cs` + `Dtos/` + `Validators/` → `Web/Controllers/XController.cs` + `Models/X/XIndexViewModel.cs` + `Views/X/*.cshtml`.

## Conceitos-Chave da Plataforma (resumo)

- **Multi-Site:** instalação única atende várias plantas; isolamento lógico por `SiteId` + Global Query Filter no `ApplicationDbContext`, alimentado por `ICurrentUserService` (planta ativa vive em claim, trocada em `SiteSelectionController`). **A base ainda não tem nenhuma entidade isolada por planta** — a primeira do seu domínio deve replicar o filtro (`ApplicationDbContext.CurrentUser` está exposto para isso).
- **Entidades:** `Site`, `ApplicationUser`, `ApplicationRole`, `UserSite` (N:N usuário↔planta), `RefreshToken`. Nada além disso.
- **Autenticação:** cookie (web admin, login em duas etapas com seleção de planta) + JWT/refresh token na API.
- **Roles (seed):** só `Admin` (`ProductionDataSeeder`; em Development o `DevelopmentDataSeeder` cria também a planta `DEV` e o usuário admin `admin@retaguarda.local` / `Admin@123`).
- **Autorização por permissão:** um papel é um pacote de permissões (`recurso.acao`), marcadas por checkbox no cadastro de Acessos e guardadas como claim em `identity."RoleClaims"`. O catálogo vive **em código** (`IPermissionProvider`; a base traz `PlatformPermissions`, e um projeto derivado registra o seu — não edita o da base). Nos controllers: `[Authorize]` na classe + `[Authorize(Policy = PlatformPermissions.Sites.Edit)]` por action, sempre pela constante. Nas views: `User.HasPermission(...)` para esconder menu, botão e ação de linha. `ControllerPermissionConventionTests` quebra o build se uma action de cadastro ficar sem política. O papel `Admin` é `IsSystem`: suas permissões são reconciliadas pelo `RolePermissionSeeder` a cada boot e ignoradas no POST. **A Api ainda não recebe permissões no JWT.**
- **Auditoria + soft delete:** `AuditableEntity` (`Created/Updated/Deleted` `By`/`At` + `IsDeleted`), carimbado pelo `AuditableEntityInterceptor` no `SaveChanges`; excluídos somem via Global Query Filter. Índices únicos usam `HasFilter("\"IsDeleted\" = false")` para permitir reutilizar códigos de registros excluídos.
- **Ajuda em campo de formulário:** dois formatos com regra fechada (`docs/padrao-ui.md` §8.2) — `_FieldHint` (visível, para restrição de preenchimento) e `_FieldHelp` (tooltip `?`, para explicação secundária), ambos com `aria-describedby="<Campo>-help"` no campo. `FieldHelpConventionTests` quebra o build no desvio.
- **Exportação:** `ExportTable` (neutra) → `IExcelExporter`/`IPdfExporter`. Referência viva: `SitesController.Export` + o dropdown "Exportar" na listagem de Plantas.

---

## Comandos do dia a dia

Terminal padrão: **PowerShell**. Lista completa em `docs/comandos-uteis.md`.

```powershell
# Build e testes (o gate do projeto: Release + warnings-as-errors)
dotnet build Retaguarda.sln -c Release
dotnet test Retaguarda.sln -c Release

# Só os testes unitários / um teste específico
dotnet test tests/Retaguarda.UnitTests
dotnet test tests/Retaguarda.UnitTests --filter "FullyQualifiedName~SiteServiceTests"

# Subir só o banco (Docker precisa estar rodando) — host localhost:15433
docker compose -f docker/docker-compose.yml up -d postgres

# Rodar Web (https://localhost:7202) e Api (https://localhost:7286, health em /health)
dotnet watch run --project src/Retaguarda.Web --launch-profile https
dotnet run --project src/Retaguarda.Api --launch-profile https

# Migrations (Data é o projeto, Web é o startup)
dotnet ef migrations add NomeDaMigration --project src/Retaguarda.Data --startup-project src/Retaguarda.Web
dotnet ef database update --project src/Retaguarda.Data --startup-project src/Retaguarda.Web

# Query direta no banco (identificadores PascalCase exigem aspas duplas)
docker exec retaguarda-base-postgres psql -U saci -d RetaguardaBase -c "\dt public.*"
```

**Segredos de dev** (User Secrets, nunca `appsettings.json`): `ConnectionStrings:DefaultConnection` nos **dois** projetos (Web e Api) e `Jwt:SigningKey` **só na Api** (mín. 32 caracteres — a Api não sobe sem ela). Passo a passo no `README.md`.

**Sem CI.** Não há GitHub Actions neste projeto (decisão de 2026-08-31: dev solo, o custo não se paga). O gate é local e manual: `dotnet build -c Release` + `dotnet test`. **Antes de um deploy**, rode também `docker compose -f docker/docker-compose.yml build` — é a única forma de pegar regressão de container, que o build local não enxerga.

**Testes:** xUnit em `tests/Retaguarda.UnitTests`, com pastas espelhando as áreas do código (`Sites/`, `Users/`, `Roles/`…). Dependências são substituídas por **fakes escritos à mão** (`FakeSiteRepository`, `FakeUserRepository`) — não há biblioteca de mocking na solution; siga o padrão. `tests/Retaguarda.IntegrationTests` é só o template do template (`UnitTest1.cs`): não há infraestrutura de teste de integração montada, então não prometa cobertura ali sem construí-la antes.

## Gotchas de ambiente (não óbvios no código)

- **Postgres em `localhost:15433`**, não 5432/5433: nesta máquina o WSL tem Postgres nativo ocupando as portas padrão no IPv4. A 15432 é do Retaguarda Argos.
- **Os composes declaram `name: retaguarda-base`.** Sem isso o Compose usaria o nome da pasta (`docker`), igual em todo projeto irmão, e um `up` recriaria os containers do outro projeto por cima. Ao renomear o projeto, renomeie também esse `name`.
- **Login pode devolver 429 em horário de pico.** Os endpoints anônimos de autenticação (login, seleção de planta, esqueci/redefinir senha e o `refresh` da Api) têm limite de requisições **por IP**, não por usuário. Atrás da Cloudflare + Caddy, um escritório inteiro chega com o **mesmo IP público** e divide a mesma cota. Sintoma: usuários legítimos recebem 429 no login quando várias pessoas entram no mesmo minuto — e a mensagem ("Muitas tentativas") não deixa óbvio que a causa é o IP compartilhado. Ajuste: `CredentialsPermitLimit` (hoje 20/min) e `RefreshPermitLimit` (60/min) em `src/Retaguarda.AspNetCore/Security/AuthRateLimiting.cs` — valores de partida, escolhidos sem dado real de uso. Isso é proteção **volumétrica**; a proteção por conta continua sendo o lockout do Identity.
- **`dotnet build` (Debug) e `dotnet ef` travam** se a Web ou a Api estiverem rodando (arquivo em uso). Mate os processos antes: `Get-Process Retaguarda.Web, Retaguarda.Api -ErrorAction SilentlyContinue | Stop-Process -Force`. Release usa pasta separada e não conflita.
- **`dotnet watch` esbarra no limite de inotify do WSL** (`fs.inotify.max_user_instances`, padrão 128) quando há vários projetos abertos. Sintoma: `The configured user limit on the number of inotify instances has been reached`. Solução: `sudo sysctl -w fs.inotify.max_user_instances=512` (persistir em `/etc/sysctl.d/`), ou usar `dotnet run`.
- **Consulta EF nova precisa ser validada contra o Postgres real** antes de dar por pronta — a tradução falha só em runtime (projetar para record e filtrar depois não traduz; filtre as entidades antes).
- **SQL cru precisa de aspas duplas**: identificadores são PascalCase e o Postgres dobra nomes não-citados para minúsculo — `identity."Users"`, `"Sites"`, `"IsDeleted"`.

---

## Como Pedir uma Nova Etapa (formato esperado do usuário)

O usuário usará o formato:

```
ETAPA: [nome curto]
OBJETIVO: [o que ter ao final]
CRITÉRIO DE PRONTO: [como sabemos que terminou]
FORA DO ESCOPO DESTA ETAPA: [o que NÃO fazer agora]
```

Resposta esperada do Claude Code:
1. Confirmar entendimento.
2. Apresentar plano curto.
3. Aguardar OK.
4. Executar.
5. Entregar relatório final no formato da regra 10.
