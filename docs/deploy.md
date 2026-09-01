# Deploy (POC) — VPS Ubuntu + Docker + Cloudflare

Guia para subir a aplicação numa VPS Ubuntu, exposta em **`exemplo.com.br`** (portal web) e
**`api.exemplo.com.br`** (API), com **HTTPS via Cloudflare** e **Caddy** como proxy reverso.

> **Troque `exemplo.com.br` pelo domínio real do projeto** aqui, no `docker/Caddyfile` e no
> `docker/.env` (`WEB_ALLOWED_HOSTS` / `API_ALLOWED_HOSTS`).

```
Navegador / cliente da API ──HTTPS──▶ Cloudflare (Full strict) ──HTTPS──▶ Caddy :443 ──▶ web:8080 / api:8080
                                                                                      └── postgres (rede interna)
```

- **TLS:** a Cloudflare termina o TLS público; entre Cloudflare e a VPS usa-se um **certificado de origem
  da Cloudflare** (grátis, validade 15 anos) instalado no Caddy — modo **Full (strict)**.
- **Migrations:** aplicadas **automaticamente pelo Web** no startup (a Api não migra).
- **Segredos:** ficam em `docker/.env` (fora do git) e no `docker/certs/` (fora do git).

---

## 1. Pré-requisitos

- VPS Ubuntu com **Docker** e **Docker Compose v2** (`docker compose version`).
- Domínio `exemplo.com.br` gerenciado na **Cloudflare**.
- Acesso SSH à VPS e o IP público dela.

---

## 2. Cloudflare (painel)

1. **DNS:** crie dois registros **A** apontando para o IP da VPS, ambos **Proxied (nuvem laranja)**:
   - `exemplo.com.br` → `IP_DA_VPS`
   - `api.exemplo.com.br` → `IP_DA_VPS`
2. **SSL/TLS → Overview:** modo **Full (strict)**.
3. **SSL/TLS → Edge Certificates:** ative **Always Use HTTPS**.
4. **Certificado de origem** (SSL/TLS → **Origin Server** → **Create Certificate**):
   - Hostnames: `exemplo.com.br` e `*.exemplo.com.br`.
   - Baixe o **certificado** e a **chave privada** (você vai colocá-los na VPS no passo 3.4).
5. **⚠️ Se a API for consumida por um cliente que não é navegador** (app mobile, serviço), isente-a
   do challenge — caso contrário a Cloudflare o bloqueia:
   - Desative **Bot Fight Mode** (Security → Bots), **ou**
   - Crie uma **WAF Custom Rule** / **Configuration Rule**: se `Hostname = api.exemplo.com.br`
     → ação **Skip** (Managed Challenge / Bot Fight / Security).

---

## 3. VPS

### 3.1. Firewall
```bash
sudo ufw allow 22/tcp
sudo ufw allow 80/tcp
sudo ufw allow 443/tcp
sudo ufw enable
```

> **Só o Caddy pode ficar exposto.** No `docker-compose.prod.yml`, `web`, `api` e `postgres` não
> publicam porta no host de propósito — falam entre si pela rede interna do Docker. Não acrescente
> `ports:` a eles, e não libere as portas 8080/5432 no firewall.
>
> O motivo não é só "superfície de ataque": os dois apps confiam no cabeçalho `X-Forwarded-For`
> de qualquer origem (`KnownProxies` limpo, em ambos os `Program.cs`), porque assumem que só o
> Caddy os alcança. Se a porta do app for publicada, qualquer cliente forja o IP — os logs passam
> a mentir e o **rate limiting por IP dos endpoints de autenticação vira decoração**, já que basta
> trocar o cabeçalho a cada requisição para ter cota infinita. Se um dia precisar expor um app
> direto, preencha `KnownProxies`/`KnownIPNetworks` antes.

### 3.2. Liberar a porta 443
Se já houver outra aplicação publicando 80/443 nesta VPS, pare-a antes (na pasta dela):
```bash
docker compose stop
```
*(Os containers/volumes/dados ficam intactos.)*

### 3.3. Obter o código

**Se ainda NÃO clonou** (a partir da pasta-pai onde quer o projeto):
```bash
git clone <url-do-repo> <pasta-do-projeto>
cd <pasta-do-projeto>
```

**Se JÁ tem o repositório clonado** (ex.: em `/var/www/html/<pasta-do-projeto>`), pule o clone e vá para a raiz:
```bash
cd /var/www/html/<pasta-do-projeto>
git checkout main && git pull   # garante a versão mais recente
```

Em ambos os casos, você deve estar **na raiz do repo**. Confira que os arquivos de deploy existem e entre em `docker/`:
```bash
ls docker/docker-compose.prod.yml docker/Caddyfile docs/deploy.md
cd docker
```
> Se o `ls` acusar arquivo faltando, você está numa branch/estado sem o deploy: rode `git remote -v`,
> `git branch -a`, faça `git checkout main && git pull` e repita. A partir daqui, os comandos rodam de
> dentro de `docker/`.

### 3.4. Certificado de origem
Crie a pasta e salve os arquivos da Cloudflare **com estes nomes** (o Caddyfile os espera assim):
```bash
mkdir -p certs
# cole o conteudo do certificado em certs/origin.pem
# cole o conteudo da chave privada em certs/origin-key.pem
chmod 600 certs/origin-key.pem
```

### 3.5. Variáveis de ambiente
```bash
cp .env.example .env
# gere uma chave JWT forte:
openssl rand -base64 48
```
Edite o `docker/.env` preenchendo `POSTGRES_PASSWORD` e `JWT_SIGNING_KEY` (e revise os demais).

### 3.6. Subir
```bash
docker compose -f docker-compose.prod.yml up -d --build
```
O Web aplica as migrations no startup e cria o schema no Postgres automaticamente.

### 3.7. Verificar
```bash
docker compose -f docker-compose.prod.yml ps          # todos "running"/"healthy"
docker compose -f docker-compose.prod.yml logs -f web  # ver a aplicacao subir + migrations
```
No navegador: `https://exemplo.com.br` (portal) e `https://api.exemplo.com.br/health` (deve responder).

---

## 4. Primeiro acesso ao portal

O **admin inicial** é criado automaticamente no 1º boot (pelo Web), a partir do `docker/.env`:
- `SEED_ADMIN_EMAIL` (ex.: `admin@exemplo.com.br`)
- `SEED_ADMIN_PASSWORD` (defina uma senha forte)

Também é criada uma **planta inicial** (`Matriz`) para permitir o login. Depois de entrar, você pode
renomear/criar plantas, usuários e papéis.

O seeder é **idempotente**: se o usuário já existe, **não** redefine a senha (uma troca pelo portal não
é revertida em restart). **Recomendado: troque a senha logo após o primeiro login.** Se as duas variáveis
não estiverem no `.env`, nenhum admin é criado (o log avisa).

---

## 5. Operação

- **Logs:** `docker compose -f docker-compose.prod.yml logs -f web` (ou `api`, `caddy`, `postgres`).
- **Reiniciar:** `docker compose -f docker-compose.prod.yml restart`.
- **Atualizar versão:** `git pull && docker compose -f docker-compose.prod.yml up -d --build`.
- **Voltar a aplicação anterior:** pare esta (`docker compose -f docker-compose.prod.yml down`) e
  suba a outra. Para rodar **as duas juntas**, é preciso um **proxy reverso compartilhado** roteando
  por domínio (não coberto nesta POC).

---

## 6. Troubleshooting

| Sintoma | Causa provável / ação |
|---|---|
| **521/522** na Cloudflare | Origem inacessível: Caddy não está de pé, `ufw` bloqueando 443, ou DNS apontando errado. |
| **525/526** | Erro de TLS origem: cert de origem ausente/errado em `certs/`, ou modo não é Full (strict). |
| **Loop de redirect** | Esquema não chega como HTTPS ao app: confira o `ForwardedHeaders` (já configurado) e o modo **Full (strict)** (não use "Flexible"). |
| **Cliente não-navegador bloqueado (challenge)** | Falta a regra da Cloudflare isentando `api.exemplo.com.br` (passo 2.5). |
| **Login não fixa / cookie** | Cookie `Secure` exige HTTPS reconhecido — garantido pelo `ForwardedHeaders` + Full (strict). |
| **API sobe mas 500 no login/JWT** | `JWT_SIGNING_KEY` ausente/curto (< 32 bytes) no `.env`. |
