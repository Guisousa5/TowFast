# Plan: precos de atendimento

## Referencia

- Spec: `specs/precos-atendimento/spec.md`
- Requisitos cobertos: `REQ-001` a `REQ-006`.

## Abordagem

Adicionar tarifas ao catalogo e snapshot de preco a solicitacao. Um servico de aplicacao reutiliza a mesma regra de cotacao na consulta e na abertura; o dominio valida valores. A interface apresenta a cotacao antes de confirmar.

## Componentes afetados

| Camada | Componente | Mudanca |
|---|---|---|
| Domain | `ServiceType`, `ServiceRequest` | Tarifas e snapshot imutavel de preco. |
| Application | `PricingService`, `ServiceRequestService`, contratos | Calculo, comparacao e respostas. |
| Adapter HTTP | Controllers | Cotacao e configuracao de tarifas. |
| Adapter DB | EF Core/migration | Colunas decimais nullable e checks. |
| UI | Angular requests/admin | Cotacao, confirmacao e exibicao. |

## Portas e adapters

- Entrada: casos de uso de cotar, configurar tarifa e criar chamado.
- Saida: `ITowFastStore` existente.
- HTTP: endpoints REST autenticados.
- Persistencia: PostgreSQL via EF Core.

## Modelo e regras

- Tarifas nao negativas; valor por km obrigatorio somente para reboque.
- Preco final calculado em decimal e arredondado a centavos; distancia aproximada pelo Haversine entre origem e destino.
- O snapshot do chamado e definido no construtor e nao muda nas transicoes de status.
- A abertura confere `expectedPrice` com o calculo atual.

## API e contratos

- Cotacao recebe a mesma selecao de servico e locais da criacao.
- Retorno de cotacao informa preco, moeda e distancia estimada opcional.
- Configuracao de tarifa retorna o catalogo atualizado.
- `409` para preco alterado desde a cotacao; erro de validacao para tarifa/coordenada ausente.

## Interface web

- Modal de novo chamado: consultar preco, mostrar total e distancia estimada; confirmar com o preco exibido.
- Mudanca em campos relevantes invalida a cotacao anterior.
- Fila, historico, detalhe e tela de admin: valor formatado em BRL.
- Fila e painel consultam novos chamados a cada 15 segundos enquanto a pagina esta visivel.
- Aba Servicos da administracao: editar tarifas com rotulos explicitos.

## Persistencia e migracao

- Adicionar colunas nullable a `service_types` e `service_requests` para compatibilidade com registros existentes.
- Gerar migration EF Core e atualizar snapshot.
- Uma migration configura as tarifas iniciais informadas pelo solicitante nos tres servicos; alteracoes posteriores sao feitas pelo administrador.

## Seguranca e observabilidade

- Endpoint de tarifa: Admin/Operator.
- Cotacao: perfis autorizados a criar chamado; API valida propriedade dos locais/veiculo na abertura.
- Preco nao e aceito como autoridade do cliente; divergencia e registrada como conflito funcional.

## Estrategia de testes

- Dominio: tarifa e imutabilidade do snapshot.
- Aplicacao: calculo fixo e por distancia, ausencia de tarifa, divergencia de cotacao.
- Integracao: migration e serializacao dos novos contratos.
- UI: build e fluxo de consulta/confirmacao.

## Sequencia de entrega

1. Modelar tarifas, calculo e contratos.
2. Persistir e expor endpoints.
3. Atualizar contratos Angular e telas.
4. Executar testes, build e revisao do diff.

## Riscos e mitigacoes

| Risco | Impacto | Mitigacao |
|---|---|---|
| Distancia reta difere da rota | Preco pode divergir do deslocamento real | Rotular como distancia aproximada e manter tarifa transparente. |
| Tarifa muda entre cotacao e confirmacao | Cliente aceita valor diferente | `expectedPrice` validado na API. |
| Historico sem preco | UI confusa | Mostrar valor nao registrado para chamados antigos. |

## Decisoes e alternativas

- Calculo aproximado Haversine: escolha expressa do solicitante.
- Administracao define valores: evita precos ficticios.
