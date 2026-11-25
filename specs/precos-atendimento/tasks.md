# Tasks: precos de atendimento

## Referencias

- Spec: `specs/precos-atendimento/spec.md`
- Plan: `specs/precos-atendimento/plan.md`

## Tarefas

- [x] `TASK-001` - Modelar tarifas, snapshot e regra de calculo.
  - Requisitos: `REQ-001`, `REQ-002`, `REQ-004`, `REQ-006`
  - Arquivos provaveis: Domain, Application.
  - Verificacao: testes de dominio/aplicacao.
  - Dependencias: nenhuma.

- [x] `TASK-002` - Persistir dados e criar migration.
  - Requisitos: `REQ-001`, `REQ-004`, `REQ-006`
  - Arquivos provaveis: DbContext, Migrations.
  - Verificacao: build e testes de persistencia.
  - Dependencias: `TASK-001`.

- [x] `TASK-003` - Expor cotacao, configuracao e preco nos contratos HTTP.
  - Requisitos: `REQ-001` a `REQ-006`
  - Arquivos provaveis: Controllers, Contracts, Services.
  - Verificacao: testes de API e OpenAPI.
  - Dependencias: `TASK-002`.

- [x] `TASK-004` - Atualizar interfaces de cliente, motorista e administracao.
  - Requisitos: `REQ-003`, `REQ-005`, `REQ-005A`
  - Arquivos provaveis: Angular features, API client, generated models.
  - Verificacao: build Angular e fluxo visual.
  - Dependencias: `TASK-003`.

## Validacao final

- [x] Criterios de aceite verificados por testes de calculo, API e revisao dos contratos.
- [x] Testes .NET e Angular e build Angular passando.
- [x] Revisao do diff sem achados bloqueantes.
- [x] Riscos residuais registrados: a distancia do reboque e aproximada em linha reta; chamados anteriores exibem preco nao registrado.
