# Comandos úteis — Retaguarda Base

Referência prática dos comandos mais usados no dia a dia do projeto. Esta lista cresce conforme novos fluxos aparecem — ver a seção 6 ao final para o padrão de contribuição.

> **Convenção:** comandos em PowerShell (terminal padrão do projeto no Windows). Quando precisar variar, marcar com comentário `# bash:` no início da linha.

---

## 1. Git e GitHub

### 1.1. Estado, diff e histórico

```powershell
git status                              # arquivos modificados, staged, branch atual
git diff                                # diff dos arquivos NÃO staged
git diff --staged                       # diff dos arquivos staged (prontos pra commit)
git log --oneline -10                   # últimos 10 commits em uma linha cada
git log --all --oneline --graph -20     # grafo das últimas 20 entradas, todas branches
```

### 1.2. Fluxo de uma etapa (feature branch + PR)

Main local atualizada

```powershell
git checkout main
git pull
```

Branch da tarefa (prefixo/ticket/descrição — mantenha o mesmo prefixo de ticket do Jira)

```powershell
git checkout -b techdebt/ss-XXX/descricao-curta-em-ingles
```

Trabalhar; revisar antes de commitar

```powershell
git status            # confira o que vai entrar (evita o "add ." cego)
git add -A
git commit -m "Tech Debt: descrição da mudança"
```

Push da branch (backup remoto + tracking)

```powershell
git push --set-upstream origin techdebt/ss-XXX/descricao-curta-em-ingles
```

GATE antes de tocar a main

```powershell
dotnet build Retaguarda.sln -c Release && dotnet test Retaguarda.sln -c Release
```

Merge squash → 1 commit limpo na main

```powershell
git checkout main
git pull
git merge --squash techdebt/ss-XXX/descricao-curta-em-ingles
git commit -m "Tech Debt: descrição da mudança"
git push
```

# 7. Limpeza da branch (com --squash o -d acusa "not merged"; use -D)

```powershell
git branch -D techdebt/ss-XXX/descricao-curta-em-ingles
git push origin --delete techdebt/ss-XXX/descricao-curta-em-ingles
```

### 1.4. Operações comuns

```powershell
# Trocar de branch sem perder mudanças locais
git stash                               # guarda mudanças não commitadas
git checkout outra-branch
git stash pop                           # restaura aqui

# Atualizar SÓ a referência main local sem mudar de branch
git fetch origin main:main

# Descartar mudanças em UM arquivo (DESTRUTIVO)
git restore caminho/do/arquivo

# Descartar TODAS as mudanças não commitadas (DESTRUTIVO)
git restore .

# Apagar branch local (depois de mergeada)
git branch -d nome-da-branch            # protege se ainda não mergeada
git branch -D nome-da-branch            # força (cuidado)
```

### 1.5. GitHub Actions / CI (este projeto NÃO tem workflow — comandos úteis só se você adicionar um)

```powershell
gh run list --limit 10                  # últimos 10 runs do repo
gh run watch                            # acompanha o último em tempo real
gh run view --log                       # log completo do último
gh run view --log-failed                # só os passos que falharam
gh workflow list                        # workflows configurados no repo
```

### 1.6. Autenticação por SSH e GitHub CLI (configuração única)

> Pré-requisito: chave SSH já criada e adicionada à conta GitHub em **Settings → SSH and GPG keys**. O remote do repositório nasceu em HTTPS; os passos abaixo migram para SSH e autenticam o `gh`.

```powershell
# 1. Testar a conexão SSH com o GitHub (responde "Hi <user>! You've successfully authenticated..." se OK)
ssh -T git@github.com

# 2. Apontar o remote origin para a URL SSH (troca o HTTPS atual)
git remote set-url origin git@github.com:JoaoRomeiro/tibrasil-retaguarda.git

# 3. Conferir que o remote agora é SSH (deve mostrar git@github.com:...)
git remote -v
```

> Depois do passo 2, os comandos do dia a dia (`git pull`, `git push`, `git fetch` das seções 1.2–1.4) passam a usar a chave SSH automaticamente — sem mudança na sintaxe.

> **`gh` não usa a chave SSH para a API.** Comandos como `gh pr create`, `gh pr merge`, `gh run list` e `gh workflow list` (seções 1.3 e 1.5) falam com a API do GitHub via **token** — exigem `gh auth login`. A chave SSH só cobre as operações git (clone/pull/push).

```powershell
# Autenticar o gh uma vez (a chave SSH NÃO autentica a API — é preciso um token)
gh auth login
#   - What account do you want to log into?      GitHub.com
#   - Preferred protocol for Git operations?      SSH
#   - Upload your SSH public key / selecionar a chave existente
#   - How would you like to authenticate?         Login with a web browser (ou colar um token)

# Conferir login e protocolo configurado
gh auth status
```

> (Opcional) Se a chave tiver passphrase, carregue-a no ssh-agent do Windows para não digitar a cada push:

```powershell
Get-Service ssh-agent | Set-Service -StartupType Automatic   # habilita o serviço (uma vez, requer admin)
Start-Service ssh-agent
ssh-add $env:USERPROFILE\.ssh\id_ed25519                     # ajustar o nome se a chave for outra (ex.: id_rsa)
```

---

## 2. .NET / dotnet

### 2.1. Build e testes da solution

```powershell
dotnet restore Retaguarda.sln           # baixa pacotes NuGet
dotnet build Retaguarda.sln             # builda tudo (Debug por padrão)
dotnet build Retaguarda.sln -c Release  # builda em Release
dotnet test Retaguarda.sln              # roda todos os testes
dotnet clean Retaguarda.sln             # apaga bin/ e obj/ de todos os projetos
```

### 2.2. Rodar Web ou Api localmente (sem Docker)

```powershell
# Web em HTTPS (dev cert do .NET)
dotnet run --project src/Retaguarda.Web --launch-profile https
# Disponível em https://localhost:7202/

# Api em HTTPS
dotnet run --project src/Retaguarda.Api --launch-profile https
# Disponível em https://localhost:7286/ e /health

# Parar: Ctrl+C no terminal onde está rodando
```

### 2.3. EF Core — migrations

> Pré-requisito: ferramenta `dotnet-ef` global instalada uma vez:
> `dotnet tool install --global dotnet-ef`

```powershell
# Criar nova migration
dotnet ef migrations add NomeDaMigration `
  --project src/Retaguarda.Data `
  --startup-project src/Retaguarda.Web

# Aplicar migrations pendentes ao banco
dotnet ef database update `
  --project src/Retaguarda.Data `
  --startup-project src/Retaguarda.Web

# Reverter banco para uma migration anterior
dotnet ef database update NomeDaMigrationAnterior `
  --project src/Retaguarda.Data `
  --startup-project src/Retaguarda.Web

# Listar migrations existentes
dotnet ef migrations list `
  --project src/Retaguarda.Data `
  --startup-project src/Retaguarda.Web
```

### 2.4. User Secrets (connection strings e segredos em dev)

```powershell
# Listar secrets atuais do projeto
dotnet user-secrets list --project src/Retaguarda.Web

# Definir um secret (sobrescreve se já existir) — connection string do PostgreSQL (dev).
# Porta 15433 (não 5432/5433 — ver §4). Defina o MESMO secret nos projetos Web e Api.
dotnet user-secrets set "ConnectionStrings:DefaultConnection" "Host=localhost;Port=15433;Database=RetaguardaBase;Username=saci;Password=H4ck3r@978" `
  --project src/Retaguarda.Web

# Remover um secret
dotnet user-secrets remove "ConnectionStrings:DefaultConnection" `
  --project src/Retaguarda.Web
```

---

## 3. Docker

### 3.1. Subir / derrubar Web + Api via Compose

> **Nome do projeto Compose.** Os composes declaram `name: retaguarda-base` no topo. Sem isso o
> Compose deriva o nome da **pasta** (`docker`) — igual em todo projeto irmão — e um `up` aqui
> recriaria os containers do outro projeto por cima. Ao renomear o projeto, renomeie também esse
> `name` (o `rename.ps1` faz isso).

```powershell
# Subir (build + start em background)
docker compose -f docker/docker-compose.yml up --build -d

# Subir sem rebuild (se imagens já existem)
docker compose -f docker/docker-compose.yml up -d

# Status dos containers
docker compose -f docker/docker-compose.yml ps

# Derrubar (para + remove containers + remove network)
docker compose -f docker/docker-compose.yml down

# Derrubar e remover também as imagens locais (limpeza profunda)
docker compose -f docker/docker-compose.yml down --rmi local
```

### 3.2. Logs

```powershell
# Logs ao vivo (Ctrl+C sai)
docker compose -f docker/docker-compose.yml logs -f web
docker compose -f docker/docker-compose.yml logs -f api

# Últimas 50 linhas de todos os serviços
docker compose -f docker/docker-compose.yml logs --tail=50

# Logs do Serilog (File sink) gravados no host via volume bind
Get-Content logs/web/log-*.txt -Tail 30
Get-Content logs/api/log-*.txt -Tail 30
```

### 3.3. Entrar em um container / executar comando dentro

```powershell
# Shell interativo dentro do container do Web
docker exec -it retaguarda-base-web /bin/bash

# Comando único
docker exec retaguarda-base-web whoami              # confirma user 'app' (não-root)
docker exec retaguarda-base-web ls -la /app/logs    # estado do diretório de logs
docker exec retaguarda-base-web cat /app/appsettings.json
```

### 3.4. URLs e endpoints quando rodando via compose

```
Web:   http://localhost:8090/
Api:   http://localhost:8091/
       http://localhost:8091/health   (espera "Healthy")
```

### 3.5. Limpeza de espaço

```powershell
docker system df                        # quanto Docker está consumindo
docker image prune                      # apaga imagens dangling (sem tag)
docker system prune                     # apaga containers parados + imagens dangling + redes não usadas
docker system prune -a --volumes        # limpeza agressiva (DESTRUTIVO — apaga TUDO não em uso)
```

---

## 4. PostgreSQL (banco de dados de dev)

> O banco roda **em container** (serviço `postgres` do `docker-compose`, imagem `postgres:17`). Credenciais de dev: usuário `saci`, senha `H4ck3r@978`, banco `RetaguardaBase`. Em dev, a connection string vem de User Secrets (ver 2.4), nunca de `appsettings.json`.

> **Porta do host: `15433`** — e **não** 5432/5433. Na máquina de dev, o WSL tem um PostgreSQL nativo (e outro serviço) ocupando 5432 e 5433 no IPv4 do `localhost`; o container só conseguiria o IPv6 e clientes que tentam IPv4 primeiro cairiam no WSL. Por isso publicamos o container em **15433** (livre; a 15432 fica para outro projeto rodar em paralelo), onde app e DBeaver conectam limpo — inclusive o DBeaver rodando dentro do WSL.

> **Atenção:** identificadores são PascalCase e o Postgres dobra nomes não-citados para minúsculo — sempre use **aspas duplas** em SQL cru: `identity."Users"`, `"Sites"`, `"IsDeleted"`.

```powershell
# Subir só o banco (sem web/api)
docker compose -f docker/docker-compose.yml up -d postgres

# Conectar em modo interativo (dentro do container, via socket — não pede senha)
docker exec -it retaguarda-base-postgres psql -U saci -d RetaguardaBase

# Executar uma query direto e sair
docker exec retaguarda-base-postgres psql -U saci -d RetaguardaBase -c "SELECT `"Name`" FROM identity.`"Roles`";"

# Listar tabelas dos schemas
docker exec retaguarda-base-postgres psql -U saci -d RetaguardaBase -c "\dt identity.*"
docker exec retaguarda-base-postgres psql -U saci -d RetaguardaBase -c "\dt public.*"

# Listar todos os bancos do servidor
docker exec retaguarda-base-postgres psql -U saci -d postgres -c "\l"
```

**DBeaver (ou outro cliente no host):** New Database Connection → PostgreSQL → Host `localhost`, Port **`15433`**, Database `RetaguardaBase`, Username `saci`, Password `H4ck3r@978`.

---

## 5. Atalhos diversos

### 5.1. Limpar artefatos de build localmente

```powershell
# Apaga bin/ e obj/ de toda a solution (mais seguro que rm manual)
dotnet clean Retaguarda.sln

# Apaga logs gerados em desenvolvimento (não vai pro git, mas ocupa espaço)
Remove-Item -Recurse -Force logs/web/*, logs/api/*, src/Retaguarda.Web/logs/*, src/Retaguarda.Api/logs/* -ErrorAction SilentlyContinue
```

### 5.2. Conferir versões

```powershell
dotnet --version                        # SDK ativo
dotnet --list-sdks                      # todos os SDKs instalados
docker --version
docker compose version
gh --version
git --version
```

---

## 6. Snippets de Frontend

Pedaços de HTML/Razor curtos para colar temporariamente em alguma view e verificar uma convenção do projeto. Não fazem parte do produto.

### 6.1. Testar `forms.js` (convenção `data-disable-on-submit`)

> Ver §7 do `docs/padrao-ui.md`. Use este snippet para verificar manualmente o comportamento (botão desabilita + spinner ao submeter; validação inválida não trava o botão).

**Como usar:**
1. Cole o bloco abaixo dentro de qualquer view (ex.: `src/Retaguarda.Web/Views/Home/Index.cshtml`), logo após o conteúdo existente.
2. Adicione as 5 chaves correspondentes em `Resources/Views/<Controller>/<View>.pt-BR.resx`.
3. Suba a Web (`dotnet run --project src/Retaguarda.Web`) e abra a página.
4. **Remova o snippet e as chaves** ao terminar — não deixar demo poluindo a view.

```razor
@* DEMO TEMPORÁRIA — testar forms.js. Remover antes de commitar. *@
<section class="card mt-5" aria-labelledby="demo-title">
    <div class="card-body">
        <h2 id="demo-title" class="h5">@Localizer["form_demo_title"]</h2>
        <p class="text-muted small">@Localizer["form_demo_description"]</p>
        <form method="post" data-disable-on-submit class="row g-3">
            <div class="col-md-8">
                <label for="demo-name" class="form-label">@Localizer["form_demo_field_name"]</label>
                <input type="text" id="demo-name" name="name" class="form-control" required />
            </div>
            <div class="col-md-4 d-flex align-items-end">
                <button type="submit"
                        class="btn btn-primary"
                        data-loading-text="@Localizer["form_demo_submitting"]">
                    @Localizer["form_demo_submit"]
                </button>
            </div>
        </form>
    </div>
</section>
```

Chaves correspondentes para o `.resx` (pt-BR):

| Chave | Valor sugerido |
|---|---|
| `form_demo_title` | `Demonstração: forms.js + data-disable-on-submit` |
| `form_demo_description` | `Submeta o formulário para ver o botão desabilitar com spinner. Tente submeter vazio para verificar que a validação cancela o loading.` |
| `form_demo_field_name` | `Nome` |
| `form_demo_submit` | `Enviar` |
| `form_demo_submitting` | `Enviando...` |

**O que validar visualmente:**
- **Golden path:** preencher campo, clicar Enviar → botão exibe spinner + "Enviando...", fica `disabled`, página recarrega.
- **Validação cancela loading:** clicar Enviar com campo vazio → browser bloqueia (tooltip "Preencha este campo"), botão **não** desabilita.
- **bfcache (back/forward):** preencher, enviar, clicar Voltar do browser → botão volta ao estado normal (não preso em "Enviando...").

---

## 7. Como adicionar comandos neste arquivo

1. **Escolha a seção certa** (1. Git/GitHub, 2. .NET, 3. Docker, 4. SQL, 5. Atalhos diversos, 6. Snippets). Se nenhuma se encaixa, criar nova seção numerada antes desta.
2. **Cada comando vai num bloco** ` ```powershell ` (ou ` ```bash ` quando aplicável), com **comentário curto** ao lado ou acima explicando o que faz.
3. **Pré-requisitos** (instalar ferramenta global, autenticar, etc.) vão em **citação `>`** logo antes do bloco.
4. **Comandos destrutivos** levam marca **(DESTRUTIVO)** no comentário.
5. Manter os exemplos **realistas pro Retaguarda** — caminhos, nomes de banco, hostnames batendo com o que se usa de verdade.
