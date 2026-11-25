# Spec: precos de atendimento

## Status

Aprovada para implementacao conforme decisoes do solicitante em 12/11/2026.

## Contexto e problema

Os chamados atuais nao registram um preco. Cliente, motorista e operacao precisam visualizar o mesmo valor desde a abertura da solicitacao.

## Objetivo

Exibir o preco ao cliente antes da confirmacao, persistir o valor confirmado e apresenta-lo ao motorista e a administracao durante todo o atendimento.

## Fora de escopo

- Cobranca, pagamento, repasse e nota fiscal.
- Calculo por trajeto rodoviario ou API externa de rotas.
- Alteracao retroativa de precos de chamados anteriores.

## Atores

- Administrador/operador: configura tarifas e consulta precos dos chamados.
- Cliente: consulta o preco, confirma a solicitacao e visualiza o preco registrado.
- Motorista: visualiza o preco na fila e no detalhe do atendimento.

## Fluxo principal

1. Administrador configura a tarifa base e, para reboque, a tarifa por quilometro no catalogo de servicos.
2. Cliente preenche tipo, veiculo e origem; no reboque informa tambem destino.
3. Interface consulta a cotacao na API e mostra valor em BRL. Para reboque, mostra tambem distancia aproximada.
4. Cliente confirma o pedido com o preco esperado. API recalcula, confere o valor e grava um snapshot no chamado.
5. Cliente, motorista e operacao veem o preco registrado nas listas, fila e detalhe.

## Fluxos alternativos e erros

- Tarifa nao configurada: cotacao e abertura retornam erro explicito; a administracao configura o servico.
- Coordenadas ausentes no reboque: cotacao e abertura sao recusadas.
- Tarifa ou locais mudaram apos cotacao: confirmacao e recusada com conflito; cliente consulta novamente.
- Chamado antigo sem preco: interfaces mostram indicacao discreta de valor nao registrado.

## Requisitos funcionais

- `REQ-001`: administracao configura tarifa fixa para bateria/pneu e tarifa base mais valor por km para reboque; tarifas iniciais: reboque R$ 100,00 + R$ 3,00/km, bateria R$ 150,00 e pneu R$ 80,00.
- `REQ-002`: API calcula reboque por distancia aproximada entre coordenadas de origem e destino usando Haversine, arredonda distancia e valor a duas casas e retorna cotacao em BRL.
- `REQ-003`: cliente ve a cotacao antes de confirmar a abertura.
- `REQ-004`: API recalcula na abertura, compara com o preco esperado e persiste o preco imutavel do chamado.
- `REQ-005`: cliente, motorista e administracao visualizam o preco persistido nas superficies de atendimento.
- `REQ-005A`: fila e painel abertos atualizam os chamados periodicamente, incluindo o preco de novas solicitacoes.
- `REQ-006`: alteracoes posteriores na tarifa nao modificam chamados ja abertos.

## Requisitos nao funcionais

- Seguranca: somente Admin/Operator configuram tarifas; preco e calculado no servidor.
- Precisao: decimal em BRL com duas casas; tarifa por km e distancia com precisao adequada no banco.
- Acessibilidade: valor identificado por texto, inclusive em tela pequena.
- Compatibilidade: chamadas antigas continuam legiveis com preco nulo.

## Criterios de aceite

### AC-001 - Cotacao fixa

- Dado um servico de bateria ou pneu com tarifa configurada
- Quando o cliente consulta o preco
- Entao recebe o valor fixo em BRL.

### AC-002 - Cotacao de reboque

- Dado origem e destino com coordenadas e tarifa base mais valor por km
- Quando o cliente consulta o preco
- Entao recebe a distancia aproximada e o total calculado pela API.

### AC-003 - Confirmacao e snapshot

- Dada uma cotacao exibida ao cliente
- Quando ele confirma a solicitacao com o mesmo preco esperado
- Entao o chamado e criado com esse preco, que permanece estavel apos mudanca de tarifa.

### AC-004 - Visibilidade

- Dado um chamado com preco
- Quando cliente, motorista ou operacao o consultam conforme suas permissoes
- Entao o mesmo valor aparece na lista/fila e no detalhe.

### AC-005 - Novo chamado em tela aberta

- Dado um motorista ou administrador com a fila ou o painel abertos
- Quando um cliente cria um chamado
- Entao a lista e atualizada periodicamente e apresenta o preco registrado.

## Dados e contratos

- `ServiceType`: `BasePrice` (BRL) e `PricePerKm` (BRL/km, apenas reboque).
- `ServiceRequest`: `QuotedPrice` (BRL) e `EstimatedDistanceKm` (apenas reboque), nulos para dados anteriores.
- `POST /api/v1/service-requests/quote`: entrada com servico e locais; resposta `price`, `currency=BRL`, `estimatedDistanceKm`.
- `POST /api/v1/service-requests`: acrescenta `expectedPrice` obrigatorio para a confirmacao.
- `PATCH /api/v1/service-types/{id}/pricing`: atualiza tarifas, restrito a Admin/Operator.

## Suposicoes

- Distancia aproximada em linha reta, conforme escolha do solicitante; nao representa distancia percorrida pelas ruas.
- Reboque: `basePrice + estimatedDistanceKm * pricePerKm`; demais: `basePrice`.
- As tarifas iniciais foram informadas pelo solicitante e ficam no seed versionado; a administracao pode altera-las no painel.
- "Funcionario" inclui o motorista que executa o chamado.

## Questoes abertas

Nenhuma questao funcional bloqueante.
