# Padrão de UI (frontend do `Retaguarda.Web`)

> **Fonte da verdade da interface.** Herdado do projeto original e mantido nesta base: casca única,
> tema por tokens, JavaScript vanilla sem passo de build e acessibilidade com rigor extra.
> Padrões de CRUD (camadas, telas, validação) estão em `docs/padrao-crud.md`.

## 1. Princípios (5)

1. **Casca centralizada** — layout vive em poucos arquivos (`_Layout.cshtml` + partials); telas de conteúdo só preenchem o corpo via `@RenderBody()`.
2. **Nada visual se repete sem virar componente** — padrões compostos repetidos viram Partial, ViewComponent ou Tag Helper (ver §4).
3. **Tema via variáveis CSS — um único arquivo de tema** — `wwwroot/css/theme.css` com CSS custom properties (ver §5). Rebrand = editar um arquivo.
4. **Mínimo de dependências de frontend** — toda biblioteca nova é decisão consciente; lista permitida em §11.
5. **Server-side first** — rendering é Razor; JavaScript é vanilla, escopo limitado a UX local (validação, máscaras, comportamentos pequenos). Sem framework JS, sem SPA, sem hidratação.

## 2. Decisão técnica

- **ASP.NET Core MVC + Razor + Bootstrap 5.**
- **Sem passo de build de frontend** (sem Node/npm, sem SASS, sem PostCSS, sem Webpack/Vite). Reduz superfície de supply-chain risk (npm) e elimina Node como dependência de dev/CI.
- **Sem framework JS** (sem HTMX, Alpine, Stimulus, jQuery além do que o Bootstrap traz). Vanilla JS pontual em arquivos pequenos.

## 3. Estratégia de layout trocável

- `_Layout.cshtml` + partials (`_Navbar`, `_Sidebar`, `_Footer`) concentram a casca.
- Views de CRUD não repetem estrutura de layout, só preenchem `@RenderBody()` (e `@section Scripts` / `@section Styles` quando aplicável).
- **Classes utilitárias do Bootstrap em casos pontuais** (`mb-3`, `d-flex`, `text-muted`) são esperadas e não exigem extração. **Padrões compostos repetidos** (cabeçalho de página + ações, card de estatística, badge de status, formulário CRUD padrão) viram componentes.
- **Sem `style="..."` inline nas Views.** Exceção explícita: **CSS custom properties data-driven** (ex.: `<div style="--progress: 73%">` para drive de dados). Todo o resto vai para classes em `theme.css` ou `wwwroot/css/pages/<area>.css`.

## 4. Componentização

| Mecanismo | Quando usar | Exemplos |
|---|---|---|
| **Partial View** | Markup repetido **sem lógica** | Cabeçalho de página, badge de status, botões de ação padrão |
| **ViewComponent** | Bloco com **lógica/dados** próprios | Tabela paginada genérica, seletor de Site, card de estatística |
| **Tag Helper** | Elemento HTML customizado reutilizável (atributo declarativo) | `<status-badge value="@order.Status" />`, `<page-title>...</page-title>` |

**Regra prática:** ao construir a 2ª tela, extrair a repetição **antes** de seguir para a 3ª.

**Localização:** componentes que exibem texto ao usuário também usam `IStringLocalizer<T>` ou `@Localizer["..."]` (ver a regra de localização no `CLAUDE.md`).

## 5. Sistema de tema (design tokens)

- Arquivo único: `wwwroot/css/theme.css` com CSS custom properties em `:root { ... }`.
- Categorias mínimas:
  - **Cores:** primária, superfície, fundo, texto, borda, status (success, info, warning, danger).
  - **Tipografia:** família, escala (sm/base/lg/xl), pesos, line-height.
  - **Raio de borda, sombras, espaçamento, transições.**
- **Paleta contida:** 1 cor primária + neutros + cores de status. Evitar o arco-íris padrão do Bootstrap.
- **Sobrescrever as variáveis CSS do próprio Bootstrap 5** quando aplicável (ex.: `--bs-primary`). Bootstrap 5.3+ já usa custom properties internamente.
- **Rebrand = editar só esse arquivo.**

**Componente "card de conteúdo":** o conteúdo das telas de CRUD vive dentro de um `.card` (Bootstrap) remapeado para os tokens do tema (superfície branca, borda, raio, sombra suave). A variante `.card-accent` adiciona uma linha de destaque no topo (`--color-primary`). Padrão das telas: `page-header` (título + ações) **acima** do card; corpo no `.card-body`; ações de formulário/paginação no `.card-footer`. Reutilizável em todos os cadastros.

**Dark mode:** **fora desta versão**. Mas a infraestrutura permite adicionar futuramente sem refactor de Views — basta criar um segundo bloco `[data-bs-theme="dark"] { ... }` no mesmo `theme.css`.

## 6. Diretrizes de elegância sem designer

- **Fonte:** **Inter** (`.woff2` local), com fallback de stack de sistema:

  ```css
  font-family: 'Inter', system-ui, -apple-system, 'Segoe UI', Roboto, sans-serif;
  ```

  Se a fonte custom falhar em carregar, a interface não fica horrível.
- **Espaçamento generoso e consistente.** Definir escala no tema (ex.: `--space-1: 4px`, `--space-2: 8px`, ..., `--space-6: 48px`).
- **Hierarquia tipográfica clara.** Poucos tamanhos, contrastes nítidos.
- **Sombras suaves, raios discretos** — via variáveis do tema.
- **Componentes Bootstrap nativos** (cards, tabelas hover, badges, modais, toasts) em vez de inventar do zero.
- **Tratar estados:** hover, foco, **disabled**, **loading** (ver §7).
- **Contraste alto — padrão herdado, regra inegociável até que o projeto decida outra coisa.** O tema nasceu para **operadores de chão de fábrica/armazém: idade média alta, iluminação irregular, telas baratas, reflexos**. Estética sutil ("cinzas claros elegantes", outlines finos, texto muted excessivo) inutiliza a interface para esse público. **Quando a escolha for entre uma cor/borda/sombra mais clara/elegante e uma mais escura/óbvia, escolher a mais escura.** Mesma lógica para tamanho de fonte (preferir maior), tap targets (mínimo 44×44px) e estados disabled (cor sólida distinta, nunca opacidade). Limiares concretos em §9.

  > **Projeto com outro público?** Esta é a única diretriz do documento que depende de quem usa o sistema. Se o seu projeto atende um público diferente, **decida explicitamente** (e registre a decisão aqui) antes de afrouxar contraste — não afrouxe por gosto pessoal.

> **Decisão revista (2026-05-25, dono do produto):** a **fonte base** do tema foi fixada em **14px** (`--font-size-base: 0.875rem`), densidade estilo AdminLTE — sobrepondo o "preferir maior" acima. A escala segue em `rem`, então toda a interface encolhe proporcionalmente e o zoom/preferência de fonte do navegador continua respeitado. **Os limiares de contraste de §9 permanecem inalterados** (a revisão foi só do tamanho, não do contraste). Risco assumido: legibilidade menor para o perfil de §6. **Condição de reavaliação:** validar com usuários reais; se a leitura em campo for insuficiente, voltar a subir a base.

## 7. Loading states

Padrão único e mínimo. Sem libs, sem framework, sem skeletons.

| Cenário | Tratamento |
|---|---|
| **Submit de formulário** | Botão fica `disabled` + texto muda para "Aguarde…" (localizado via `.resx`) no `onsubmit`. 1 helper vanilla JS (~15 linhas) em `wwwroot/js/forms.js`. Aplicado via convenção: form com atributo `data-disable-on-submit`. **Mesmo padrão para todas as ~30 telas de CRUD.** |
| **Navegação entre páginas** | **Nada.** O browser já mostra spinner nativo na favicon/URL bar. |
| **Operações longas** (ex.: relatório pesado) | **Não tratar nesta versão.** Quando aparecerem, decidir caso a caso (página intermediária com auto-refresh ou background job + polling — virará discussão arquitetural separada). |

Sem skeleton screens, sem spinner global, sem `aria-busy` espalhado.

## 8. JavaScript (vanilla, escopo limitado)

- **Vanilla JS.** Sem framework, sem TypeScript, sem build.
- Cada `wwwroot/js/<nome>.js` resolve um comportamento pontual (ex.: `forms.js` para disable-on-submit, `masks.js` para máscaras). **Sem `main.js` inchado.**
- **JS usa atributos `data-*` como seletores** (ex.: `document.querySelectorAll('[data-controller="search-form"]')`), nunca `id`/`class` (estes pertencem ao CSS/Bootstrap). Quando o markup virar componente, mudar `class` não quebra o JS.
- Importação no `_Layout` ou via `@section Scripts` no fim do `<body>`.
- **Validação client-side de formulários** segue o mesmo princípio: **vanilla**, dentro de `forms.js`. **Não usar `jquery.validate.unobtrusive`** (jQuery e suas libs não fazem parte do projeto). Estratégia: ler atributos `data-val-*` que o ASP.NET MVC emite via Data Annotations, interceptar `submit`, renderizar erros com classes Bootstrap. Quando o primeiro formulário precisar de UX inline, abrir uma etapa dedicada. **Até lá, validação roda só server-side** — Data Annotations sempre re-validam no controller, então não há regressão de correção, apenas de UX (round-trip por submit em vez de validação inline).

### 8.1. Truncamento de `<option>` em `<select>` (padrão obrigatório)

A lista aberta de um `<select>` nativo é desenhada pelo navegador/SO — **não há como limitar sua largura ou aplicar reticências por CSS de forma confiável entre navegadores**. Por isso o projeto trunca o **texto** das options, de forma centralizada e automática:

- **Mecanismo:** `wwwroot/js/select-truncate.js` (carregado em `_Layout` e `_LayoutAuth`) normaliza **todo `<select>` da página**: corta o texto da option em `data-label-max` caracteres (padrão **60**) com reticências e guarda o texto completo no `title` (hover). Um `MutationObserver` cobre também selects/options inseridos depois (AJAX, modais, formulários dinâmicos).
- **Regra para novas telas:** **não implementar truncamento manual** de options (sem `substring` em view, sem CSS de largura, sem helper próprio). Toda nova tela com `<select>` já é coberta automaticamente — **nada a fazer**.
- **Único ajuste disponível:** se um select específico precisar de outro limite, defina `data-label-max="N"` no elemento `<select>`.
- **Não** depender de `RtSelect.applyTo(select)` em código novo (o observer já cobre); a API pública existe apenas como reforço pontual em fluxos JS legados.

### 8.2. Texto de ajuda em campos de formulário (padrão obrigatório)

Existem **dois** formatos, e a escolha entre eles **não é de gosto**: depende do que o texto diz.

| Se o texto… | Formato | Partial |
|---|---|---|
| **é restrição que afeta o preenchimento** — formato, tamanho, máscara, regra de senha, intervalo aceito | **visível abaixo do campo** (`form-text`) | `_FieldHint` |
| **explica consequência ou semântica secundária** — o que "inativo" causa, o que o fuso afeta, por que o campo é somente-leitura | **tooltip no ícone `?` ao lado do rótulo** | `_FieldHelp` |

O ícone do `_FieldHelp` fica **fora do `<label>`**, dentro de um wrapper `.form-label-row`. Não é
detalhe de layout: o nome acessível do campo é montado a partir do conteúdo do `<label>`, então um
texto de ajuda lá dentro passaria a ser anunciado como parte do rótulo em toda navegação.

**Por que não tooltip para tudo:** texto que o usuário precisa ler *antes* de digitar não pode
depender de hover. Escondê-lo troca um erro evitado por um erro de validação depois do submit — e
hover não existe em touch. O tooltip serve para o que é bom saber, não para o que é preciso saber.

**Ambos usam o mesmo model** (`Models/Shared/FieldHelp.cs`), que deriva um id estável a partir do id
do campo (`"Campo"` → `"Campo-help"`). **O campo deve referenciar esse id no `aria-describedby`** —
é o que faz o leitor de tela anunciar a ajuda ao focar o campo, em vez de exigir que o usuário
tabule até o ícone.

```cshtml
@* Restrição de preenchimento: visível *@
<label asp-for="Password" class="form-label"><strong>@L["user_field_password"]</strong></label>
<input asp-for="Password" class="form-control" type="password" aria-describedby="Password-help" />
<partial name="_FieldHint" model='new FieldHelp("Password", L["user_field_password_hint"].Value)' />

@* Explicação secundária: tooltip *@
<div class="form-label-row">
    <label asp-for="IsActive" class="form-label"><strong>@L["site_field_active"]</strong></label>
    <partial name="_FieldHelp" model='new FieldHelp("IsActive", L["site_field_active_hint"].Value)' />
</div>
<select name="IsActive" id="IsActive" class="form-select" aria-describedby="IsActive-help"> ... </select>
```

- **Não** escrever `<span class="form-text">` solto em view nova — use o `_FieldHint`, senão o
  `aria-describedby` fica de fora.
- **Não** usar `title` nativo para ajuda de campo (não é acessível por teclado e some rápido demais).
- O texto vive no `.resx` como qualquer string de UI, com a chave terminando em `_hint`.
- O tooltip é inicializado globalmente por `wwwroot/js/site.js` — nada a fazer por tela.

## 9. Acessibilidade básica (mínimo)

- **HTML semântico:** `<button>` (nunca `<div onclick>`), `<header>`, `<main>`, `<nav>`, `<h1>`–`<h6>` em ordem.
- **`<label>` em todo campo** de formulário (ou `aria-label` quando não houver label visível).
- **`aria-describedby` em todo campo com texto de ajuda** — visível ou em tooltip (§8.2). Sem isso a ajuda só existe para quem enxerga a tela.
- **Contraste adequado — público-alvo exige rigor extra.** Decorre da regra de contraste do §6. Limiares concretos:
  - **Texto:** mínimo WCAG AA (4.5:1). Preferir AAA (7:1) quando possível.
  - **Bordas de componentes** (input, button, card, badge): mínimo WCAG 1.4.11 (3:1). Em dúvida entre 3:1 e 5:1, **escolher 5:1**.
  - **Foco visível:** outline grosso (2-3px) com contraste alto contra background **e** contra o elemento focado.
  - **Estados de status** (success/warning/danger): cor sempre acompanhada de **ícone + texto** (daltonismo + monitor barato no campo). Nunca informação só por cor.
  - **Disabled:** cor sólida distinta, **nunca apenas opacidade**.
  - DevTools de qualquer navegador tem checker de contraste embutido — usar.
- **Foco visível** — não remover o outline default; estilizar com o tema se quiser.
- **Navegação por teclado** — todos os elementos interativos acessíveis via Tab. HTML semântico já entrega isso de graça; testar.
- **Skip-to-main-content link** no início do `_Layout.cshtml` (1 link + 1 regra CSS).

Lembrete: conformidade WCAG **completa** (auditoria formal) continua fora de escopo; o que está acima é o mínimo obrigatório.

## 10. Segurança de bibliotecas de frontend

- **Tudo local, sem CDN.**
- **Versões fixadas (pin específico):** `bootstrap@5.3.x` com `x` fixo, nunca `^` nem `latest`.
- **Procedência verificada:** só fontes oficiais (`getbootstrap.com`, releases oficiais no GitHub do projeto). Cuidado com **typosquat** (ex.: `bootstr@p`, `boot-strap`).
- **Conferência de integridade:** verificar SHA-256 do arquivo ao baixar pela primeira vez. Documentar em **`wwwroot/lib/CHECKSUMS.md`** (um único arquivo cobre todas as libs — auditoria centralizada; registra hash do arquivo final + hash do ZIP oficial do release pra re-verificação contra upstream).
- **Content Security Policy (CSP)** restritiva, baseline (configurada via middleware em `Retaguarda.AspNetCore`, compartilhado por `Web` e `Api`):

  ```
  default-src 'self';
  script-src 'self';
  style-src 'self';
  font-src 'self';
  img-src 'self' data:;
  connect-src 'self';
  frame-ancestors 'none';
  base-uri 'self';
  form-action 'self';
  ```

  Evitar `unsafe-inline` em `script-src` e `style-src`.
- **Outros headers de segurança** (via middleware custom — ~30 linhas, sem pacote novo):
  - `X-Content-Type-Options: nosniff`
  - `X-Frame-Options: DENY`
  - `Referrer-Policy: same-origin`
  - `Strict-Transport-Security: max-age=31536000; includeSubDomains` (HTTPS é obrigatório)
  - `Permissions-Policy: camera=(), microphone=(), geolocation=()`
- **Política de atualização:** alguém da equipe acompanha CVEs das libs de frontend (releases oficiais, GitHub security advisories da lib).
- **Minimização:** cada biblioteca nova é decisão consciente, com justificativa documentada em PR e atualização da lista do §11.

## 11. Bibliotecas de frontend permitidas

| Biblioteca | Versão atual (pin) | Origem | Uso |
|---|---|---|---|
| **Bootstrap** | **5.3.3** (release oficial — o template MVC do .NET traz uma variante divergente; não usar a dele) | `github.com/twbs/bootstrap/releases/tag/v5.3.3` | Sistema de UI (grid, componentes, utilitários) |
| **Inter** (fonte) | **4.1** — variable (`InterVariable.woff2`) | `github.com/rsms/inter/releases/tag/v4.1` | Tipografia (cobre todos os pesos 100–900 via CSS `font-weight`) |
| **Bootstrap Icons** | **1.13.1** — usado como web font | `github.com/twbs/icons/releases/tag/v1.13.1` | Iconografia, alinhada visualmente com Bootstrap |

**Toda adição nova exige:** justificativa no PR + análise de segurança (procedência, CVEs, manutenção do projeto upstream) + atualização desta tabela + entry no `wwwroot/lib/CHECKSUMS.md`.

**Fora do projeto de propósito** (justificativas em `wwwroot/lib/CHECKSUMS.md`): **jQuery**, **jQuery Validation** e **jQuery Validation Unobtrusive**. Bootstrap 5 não depende de jQuery; validação client-side, quando necessária, é vanilla (ver §8).

## 12. HTMX e outros frameworks JS — fora de escopo nesta versão

HTMX, Alpine.js, Stimulus, jQuery (além do que o Bootstrap traz) e qualquer outro framework/biblioteca JS estão **fora de escopo nesta versão**.

A decisão é **"fora de escopo por falta de necessidade demonstrada"**, não "proibido por princípio". Pode ser **reavaliado em futura release** com:

- Justificativa concreta de caso de uso específico documentado (ex.: um painel que precise de atualização ao vivo).
- Análise de segurança (15.10).
- Atualização desta seção + 15.11.

---
