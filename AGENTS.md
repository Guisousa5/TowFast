# Modelo de agentes do projeto

Este projeto usa um fluxo orientado a especificacao para mudancas de software:

1. O Arquiteto cria ou valida `spec.md`.
2. O Arquiteto transforma a especificacao aprovada em `plan.md`.
3. O Engenheiro de Software Senior detalha o plano em `tasks.md`.
4. O Engenheiro de Software Senior implementa uma tarefa por vez.
5. O Code Reviewer revisa cada incremento antes da conclusao.

Os papeis e seus prompts estao em `.agents/`:

- `.agents/architect.md`
- `.agents/senior-software-engineer.md`
- `.agents/code-reviewer.md`
- `.agents/orchestrator.md`

## Principios globais

- Stack principal: .NET, C#, ASP.NET Core, HTML, CSS e JavaScript.
- Arquitetura preferencial: hexagonal (Ports and Adapters).
- O dominio nao depende de banco de dados, framework web, UI ou integracoes externas.
- Casos de uso dependem de portas; adapters implementam essas portas.
- Controllers, endpoints e componentes de UI devem ser finos.
- Regras de negocio devem ser testaveis sem infraestrutura.
- Mudancas devem ser pequenas, rastreaveis e associadas a criterios de aceite.
- Nao introduzir abstracoes sem necessidade concreta.
- Seguir os padroes existentes no repositorio antes de criar novos.

## Artefatos por funcionalidade

Cada funcionalidade deve ficar em `specs/<nome-da-funcionalidade>/`:

```text
specs/<nome-da-funcionalidade>/
|-- spec.md
|-- plan.md
`-- tasks.md
```

Use os modelos em `.agents/templates/`. Nao iniciar implementacao enquanto houver
questoes funcionais relevantes em aberto na especificacao.

