# Padrao de criacao de tarefas no Jira

Este documento define o formato recomendado para abrir tarefas no Jira a partir da analise do
codigo-fonte do projeto. O objetivo e que a tarefa seja clara para desenvolvimento, revisao,
QA e rastreabilidade tecnica.

> Use este guia sempre que uma tarefa nascer de alteracoes locais, analise de codigo, revisao de
> comportamento ou descoberta tecnica no repositorio.

---

## 1. Antes de criar a tarefa

Sempre que possivel, levante o contexto tecnico antes de escrever a tarefa:

- Arquivos alterados recentemente (`git status --short`, `git diff --stat`).
- Diff dos arquivos relevantes (`git diff -- <arquivos>`).
- Commits recentes, quando ajudarem a entender a etapa do produto (`git log --name-status --oneline -5`).
- Documentos de referencia do projeto, principalmente:
  - `docs/padrao-ui.md`
  - `docs/padrao-crud.md`, quando envolver CRUD.
- Resultado de validacao tecnica, se aplicavel:
  - `dotnet build Retaguarda.sln -c Release --no-restore`
  - `dotnet test Retaguarda.sln -c Release --no-build`

Nao abra uma tarefa vaga como "ajustar tela" ou "corrigir bug" se o codigo ja permite identificar
o comportamento esperado, arquivos afetados e criterios de aceite.

---

## 2. Campos obrigatorios

### Tipo

Use o tipo mais especifico disponivel no Jira:

- `Task`: melhoria, ajuste tecnico ou implementacao pequena sem historia de negocio propria.
- `Story`: entrega funcional com valor direto para usuario.
- `Bug`: comportamento incorreto ja existente ou regressao.
- `Spike`: investigacao tecnica sem compromisso imediato de implementacao.

### Titulo

O titulo deve descrever o resultado esperado, nao apenas a area alterada.

Bom:

- `Exibir planta ativa no topbar apos selecao de site`
- `Bloquear criacao de cliente sem planta ativa`
- `Adicionar paginacao ao cadastro de centro de custo`

Evite:

- `Ajuste topbar`
- `Bug site`
- `Melhoria tela`

### Descricao

A descricao deve explicar o problema ou melhoria, o comportamento esperado e o contexto tecnico
minimo. Quando a tarefa vier de uma analise de codigo, mencione o que foi observado.

### Arquivos analisados ou afetados

Liste os arquivos relevantes. Isso acelera revisao e reduz retrabalho.

### Criterios de aceite

Use criterios verificaveis. Cada criterio deve poder ser confirmado por teste automatizado,
teste manual ou revisao objetiva.

### Validacao tecnica

Informe os comandos executados e o resultado. Se nao foram executados, registre explicitamente.

### Nome da branch

O nome da branch deve aparecer no final da tarefa, como ultimo campo.

Padrao recomendado:

```text
feature/ra-<resumo-curto-em-kebab-case>
bugfix/ra-<resumo-curto-em-kebab-case>
spike/ra-<resumo-curto-em-kebab-case>
```

Quando a chave do Jira ja existir, prefira incluir o numero:

```text
feature/ra-123-active-site-topbar
bugfix/ra-124-site-selection-claim
```

---

## 3. Template copiavel

```markdown
Tipo: Task

Titulo: <resultado esperado em uma frase>

Descricao:
<Explique o problema ou melhoria, o comportamento esperado e o contexto tecnico observado.>

Arquivos analisados/afetados:
- `<caminho/do/arquivo.cs>`
- `<caminho/da/view.cshtml>`

Criterios de aceite:
- <Criterio verificavel 1>
- <Criterio verificavel 2>
- <Criterio verificavel 3>

Validacao tecnica:
- `<comando executado>`: <resultado>
- `<comando executado>`: <resultado>

Observacoes tecnicas:
<Registre lacunas, riscos, dependencias, decisoes ou pontos que precisam de atencao.>

Nome da branch:
`feature/<sigla-do-projeto>/<resumo-curto-em-kebab-case>`
```

---

## 4. Exemplo

```markdown
Tipo: Task

Titulo: Exibir planta ativa no topbar apos selecao de site

Descricao:
Implementar a identificacao visual da planta ativa no shell da aplicacao. Apos o usuario selecionar
uma planta valida, o sistema deve persistir `SiteId` e `SiteName` no cookie de autenticacao e exibir
o nome da planta no topbar, evitando consulta desnecessaria ao banco em cada renderizacao quando o
nome ja estiver disponivel na sessao.

Arquivos analisados/afetados:
- `src/Retaguarda.Shared/RetaguardaClaims.cs`
- `src/Retaguarda.Web/Controllers/HomeController.cs`
- `src/Retaguarda.Web/Controllers/SiteSelectionController.cs`
- `src/Retaguarda.Web/Views/Shared/_Topbar.cshtml`
- `src/Retaguarda.Web/wwwroot/css/theme.css`

Criterios de aceite:
- Ao selecionar uma planta ativa e associada ao usuario, o cookie deve conter `SiteId` e `SiteName`.
- A topbar deve exibir o nome da planta ativa.
- Sessoes antigas com `SiteId` mas sem `SiteName` devem continuar exibindo a planta via fallback por `SiteId`.
- Selecao invalida, inativa ou nao associada deve retornar erro localizado sem trocar a planta ativa.
- A Home deve exigir autenticacao.
- O nome da planta deve truncar corretamente em desktop e mobile.
- Build Release e testes automatizados devem passar.

Validacao tecnica:
- `dotnet build Retaguarda.sln -c Release --no-restore`: passou com 0 warnings e 0 erros.
- `dotnet test Retaguarda.sln -c Release --no-build`: passou com 81 testes.

Observacoes tecnicas:
A implementacao atual compila e os testes passam, mas ainda nao ha cobertura especifica para o
controller emitindo o claim `SiteName` nem para a renderizacao da topbar. Essa cobertura pode ser
incluida nesta tarefa ou em subtarefa tecnica.

Nome da branch:
`feature/rb/active-site-topbar`
```

---

## 5. Checklist final

Antes de salvar a tarefa no Jira, confirme:

- O titulo expressa o resultado esperado.
- A descricao explica o motivo da tarefa.
- Os arquivos relevantes foram listados.
- Os criterios de aceite sao objetivos.
- A validacao tecnica foi registrada ou marcada como nao executada.
- Riscos, lacunas ou dependencias aparecem em observacoes tecnicas.
- O ultimo campo da tarefa e `Nome da branch`.
