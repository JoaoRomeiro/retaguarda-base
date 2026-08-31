# Retaguarda Base

**Ponto de partida para novos projetos.** Não é um produto: é a plataforma pronta, sem domínio de
negócio nenhum — você clona, renomeia e começa a construir o que o projeto precisa.

Derivado do **Retaguarda Argos** (rastreamento por RFID) removendo todo o domínio e mantendo a
fundação que já estava testada em produção.

## O que vem pronto

- **CRUDs de Papéis, Usuários e Plantas**, com o sub-CRUD de plantas por usuário
- **Multi-site**: uma instalação atende várias plantas, com isolamento por Global Query Filter
- **Autenticação**: cookie no admin web (login em duas etapas com seleção de planta) + JWT com refresh token na API
- **Localização** pt-BR via `IStringLocalizer` + `.resx` (nenhuma string visível hardcoded)
- **Auditoria + soft delete** transversais, carimbados por interceptor no `SaveChanges`
- **Exportação Excel/PDF** genérica, com a listagem de Plantas como referência viva
- **Casca de UI** completa: sidebar, topbar, tema por tokens (`theme.css`), Bootstrap 5 sem build step
- **Segurança**: headers + CSP, antiforgery, Data Protection persistida, health checks
- **Serilog** (console + arquivo), **PostgreSQL 17** em container, **Docker** e deploy com Caddy + Cloudflare

## O que NÃO vem

Domínio de negócio. As entidades são só `Site`, `ApplicationUser`, `ApplicationRole`, `UserSite` e
`RefreshToken`. A infraestrutura de isolamento por planta está pronta e testada, mas **a primeira
entidade isolada por planta é a do seu projeto** — ela vira a implementação de referência.

Também não há infraestrutura de testes de integração: `tests/Retaguarda.IntegrationTests` é o
template do `dotnet new`, mantido de propósito (ver `docs/baseline-microsoft.md` §11).

## Stack

.NET 10 / C# 14 · ASP.NET Core MVC (web) + Web API · EF Core 10 · **PostgreSQL 17** (container) ·
Serilog · FluentValidation · Mapster · xUnit · ClosedXML + QuestPDF · Bootstrap 5 · Docker

---

## Iniciar um projeto a partir desta base

```bash
git clone https://github.com/JoaoRomeiro/retaguarda-base.git meu-projeto
cd meu-projeto && rm -rf .git && git init

./tools/rename.sh --name MeuProjeto --dry-run   # simula: mostra tudo o que mudaria
./tools/rename.sh --name MeuProjeto             # aplica
```

O script renomeia namespaces, pastas, `.csproj`, a solution, o banco, os containers, a rede, os
volumes e o nome do projeto Compose, gera `UserSecretsId` novos e imprime os próximos passos.
Opções úteis: `--site-label Filial --site-label-plural Filiais` (troca o rótulo "Planta" no
repositório inteiro — a entidade continua `Site`) e `--port-offset 20` (desloca as portas, para o
projeto novo rodar ao lado da base). Rode sempre com `--dry-run` primeiro.

---

## Como executar localmente

### Pré-requisitos

- **.NET 10 SDK** — `dotnet --version` deve retornar 10.x
- **Docker** — para o container do PostgreSQL
- **`dotnet-ef`**, instalado uma vez: `dotnet tool install --global dotnet-ef`

### 1. Subir o banco

```bash
docker compose -f docker/docker-compose.yml up -d postgres
```

> Publica no host em **`localhost:15433`** (não 5432 — o porquê está em `docs/comandos-uteis.md §4`).
> Credenciais de dev: usuário `saci`, senha `H4ck3r@978`, banco `RetaguardaBase`.

### 2. Configurar os segredos de dev (só na primeira vez)

Connection string — nos **dois** projetos:

```bash
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=15433;Database=RetaguardaBase;Username=saci;Password=H4ck3r@978" --project src/Retaguarda.Web
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=15433;Database=RetaguardaBase;Username=saci;Password=H4ck3r@978" --project src/Retaguarda.Api
```

Chave de assinatura do JWT — **só na API** (mín. 32 caracteres; a API não sobe sem ela):

```bash
dotnet user-secrets set "Jwt:SigningKey" "$(openssl rand -base64 48)" --project src/Retaguarda.Api
```

### 3. Aplicar as migrations

```bash
dotnet ef database update --project src/Retaguarda.Data --startup-project src/Retaguarda.Web
```

### 4. Rodar

```bash
dotnet watch run --project src/Retaguarda.Web --launch-profile https   # https://localhost:7202
dotnet run --project src/Retaguarda.Api --launch-profile https         # https://localhost:7286
```

No primeiro start em Development o seed cria o papel `Admin`, a planta `DEV` e o usuário admin.

### 5. Acessar

Login em **https://localhost:7202/** com **`admin@retaguarda.local`** / **`Admin@123`**
(credenciais de dev, criadas só em Development).

### Parar

- **Ctrl+C** nos terminais da Web/API.
- Banco: `docker compose -f docker/docker-compose.yml down` (os dados ficam no volume; `down -v` apaga também).

> Subir Web + API em container também funciona (`docker compose -f docker/docker-compose.yml up --build -d`
> → Web em `:8090`, API em `:8091`), mas o serviço `api` do compose de dev não define `Jwt__SigningKey`
> — para rodar a API em container use o `docker-compose.prod.yml` com o `.env`. Ver `docs/deploy.md`.

## Testes

```bash
dotnet build Retaguarda.sln -c Release   # warnings-as-errors: o gate do projeto
dotnet test Retaguarda.sln -c Release
```

## Documentação

| Documento | O que é |
|---|---|
| `CLAUDE.md` | Regras de colaboração, arquitetura e comandos — leitura obrigatória para quem (ou o que) escreve código aqui |
| `docs/padrao-crud.md` | Template oficial de CRUD; `Site` é a referência |
| `docs/padrao-ui.md` | Padrão de interface: tema, componentes, JS, acessibilidade |
| `docs/baseline-microsoft.md` | Régua de qualidade × estado atual do código |
| `docs/comandos-uteis.md` | Comandos do dia a dia |
| `docs/deploy.md` | Deploy em VPS Ubuntu com Docker + Caddy + Cloudflare |
| `docs/padrao-tarefas-jira.md` | Formato das tarefas no Jira |
