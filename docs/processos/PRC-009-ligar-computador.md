# PRC-009 — Ligar computador remotamente

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Orquestrar a jornada comum de wake autenticado. |
| Evento inicial | Usuário aciona Ligar e conectar. |
| Evento final | PC/serviço pronto e cliente aberto ou erro acionável. |
| Atores | Usuário comum; Launcher; VPN; Celular; Windows; Aplicativo remoto |
| Raias | Usuário comum | Launcher | Tailscale | Android bridge | Windows/PC | Aplicativo remoto |
| Pré-condições | Perfil validado. |
| Pós-condições | Tentativa concluída e auditada. |

## Fluxo principal

1. [Usuário] aciona botão.
2. [Launcher] verifica PC, VPN e bridge.
3. [Launcher] gera request ID/nonce/timestamp.
4. [Android bridge] autentica, autoriza e limita frequência.
5. [Android bridge] envia burst WoL e recibo.
6. [Launcher] executa PRC-011.
7. [Launcher] executa PRC-012.
8. [Usuário] visualiza sucesso.

## Gateways

G1 PC pronto? Sim→PRC-010. G2 Bridge saudável? Não→PRC-013. G3 Pedido aceito? Não→erro. G4 Serviços prontos? Não→PRC-014/015.

## Fluxos alternativos

Cancelamento interrompe sondas e retry; um retry controlado de burst.

## Exceções

Replay, host divergente ou alvo não autorizado bloqueiam.

## Mensagens e eventos

wake.solicitado; wake.recibo; operação.concluída

## Rastreabilidade

* **Regras:** RN001,RN008-RN013,RN019,RN020
* **Requisitos:** RF015-RF023
* **Dados manipulados:** WakeRequest,WakeAttempt,HealthState

## Instrução para modelagem no Bizagi

Processo principal chama subprocessos; usar event-based gateway para pronto/timeout/cancelar.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
