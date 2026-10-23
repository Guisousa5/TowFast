# Time de agentes

Este diretorio contem um modelo de colaboracao para desenvolvimento orientado a
especificacao. O fluxo recomendado e:

```text
Solicitacao
    |
    v
Arquiteto ------> spec.md ------> plan.md
                                      |
                                      v
Engenheiro Senior ----------------> tasks.md
                                      |
                                      v
                                  Implementacao
                                      |
                                      v
Code Reviewer -------------------> parecer final
```

## Como usar

1. Crie `specs/<funcionalidade>/`.
2. Peca ao Arquiteto para preencher `spec.md` e `plan.md` usando os templates.
3. Peca ao Engenheiro Senior para produzir `tasks.md` e implementar as tarefas.
4. Peca ao Code Reviewer para revisar o diff e validar os criterios de aceite.
5. Atualize os checkboxes de `tasks.md` somente depois das verificacoes passarem.

O `orchestrator.md` serve como prompt coordenador quando um unico agente precisa
conduzir o fluxo completo.

