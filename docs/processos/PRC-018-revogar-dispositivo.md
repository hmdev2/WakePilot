# PRC-018 — Revogar dispositivo

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Invalidar acesso de notebook/bridge removido. |
| Evento inicial | Técnico/avançado seleciona revogar. |
| Evento final | Credencial rejeitada ou pendência explícita. |
| Atores | Técnico; Configurador; Celular; Launcher |
| Raias | Técnico/Avançado | Configurador | Android bridge | Launcher |
| Pré-condições | Dispositivo autorizado. |
| Pós-condições | AuthorizedDevice revogado. |

## Fluxo principal

1. [Usuário] seleciona dispositivo e motivo.
2. [Configurador] mostra impactos e confirma.
3. [Android bridge] remove chave pública se alcançável.
4. [Launcher] apaga privada/referência se local.
5. [Configurador] marca revogado e testa rejeição.
6. [Configurador] registra evento.
7. [Usuário] recebe conclusão/pendência.

## Gateways

G1 Bridge online? Não→revogação pendente e alerta. G2 Teste rejeitou? Não→falha crítica.

## Fluxos alternativos

Revogar bridge invalida perfil operacional; outra chave não é afetada.

## Exceções

Falha em remover autorização exige instrução presencial.

## Mensagens e eventos

dispositivo.revogado; revogação.pendente

## Rastreabilidade

* **Regras:** RN011,RN013,RN014
* **Requisitos:** RF027
* **Dados manipulados:** AuthorizedDevice,AuditEvent

## Instrução para modelagem no Bizagi

Evento de mensagem para bridge; gateway online; final pendente distinto de concluído.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
