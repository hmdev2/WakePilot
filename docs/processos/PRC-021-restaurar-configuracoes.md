# PRC-021 — Restaurar configurações

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Reverter alterações gerenciadas com verificação. |
| Evento inicial | Técnico escolhe snapshot. |
| Evento final | Estado restaurado ou parcial explícito. |
| Atores | Técnico; Configurador; Serviço privilegiado; Windows |
| Raias | Técnico/Avançado | Configurador | Serviço privilegiado | Windows/PC |
| Pré-condições | Snapshot íntegro e consentimento. |
| Pós-condições | RestoreOperation auditada. |

## Fluxo principal

1. [Configurador] valida hash e compara estado atual.
2. [Técnico] revisa diferenças e consente.
3. [Serviço privilegiado] valida operações inversas.
4. [Windows/PC] recebe alterações em ordem reversa.
5. [Configurador] verifica cada valor.
6. [Configurador] repete de forma idempotente apenas pendências seguras.
7. [Técnico] recebe relatório item a item.

## Gateways

G1 Snapshot válido? Não→bloquear. G2 Mudança externa conflitante? Sim→pedir decisão. G3 Item verificado? Não→parcial.

## Fluxos alternativos

Usuário seleciona subconjunto; snapshot preservado até confirmação.

## Exceções

Não sobrescrever mudança externa silenciosamente.

## Mensagens e eventos

rollback.iniciado; rollback.item; rollback.concluído

## Rastreabilidade

* **Regras:** RN006,RN007,RN014
* **Requisitos:** RF008,RF029
* **Dados manipulados:** RestoreSnapshot,RestoreOperation,AuditEvent

## Instrução para modelagem no Bizagi

Transação compensatória com multi-instance sequencial por item e boundary error.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
