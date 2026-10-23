# Plan: <nome da funcionalidade>

## Referencia

- Spec: `specs/<funcionalidade>/spec.md`
- Requisitos cobertos: `REQ-001`, `REQ-002`

## Abordagem

<resumo da solucao e por que ela se encaixa no sistema atual>

## Componentes afetados

| Camada | Componente | Mudanca |
|---|---|---|
| Domain | <tipo/modulo> | <responsabilidade> |
| Application | <caso de uso/porta> | <responsabilidade> |
| Adapter | <HTTP/DB/UI/integracao> | <responsabilidade> |
| Bootstrap | <composicao> | <registro/configuracao> |

## Portas e adapters

- Porta de entrada: <caso de uso e contrato>
- Porta de saida: <necessidade da aplicacao>
- Adapter de entrada: <endpoint, job ou UI>
- Adapter de saida: <persistencia ou integracao>

## Modelo e regras

<entidades, value objects, invariantes e transicoes>

## API e contratos

<rotas, payloads, status codes, erros e compatibilidade>

## Interface web

<estrutura HTML, estados, acessibilidade, responsividade e comportamento JS>

## Persistencia e migracao

<schema, indices, migracao, compatibilidade e rollback>

## Seguranca e observabilidade

<autorizacao, validacao, auditoria, logs, metricas e dados sensiveis>

## Estrategia de testes

- Dominio: <regras e limites>
- Aplicacao: <casos de uso e portas simuladas>
- Integracao: <adapters e contratos>
- Ponta a ponta: <criterios de aceite essenciais>

## Sequencia de entrega

1. <incremento verificavel>
2. <incremento verificavel>

## Riscos e mitigacoes

| Risco | Impacto | Mitigacao |
|---|---|---|
| <risco> | <impacto> | <acao> |

## Decisoes e alternativas

- Decisao: <escolha>
- Alternativas: <opcoes descartadas e motivo>

