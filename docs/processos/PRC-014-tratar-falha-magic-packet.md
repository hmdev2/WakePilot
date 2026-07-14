# PRC-014 — Tratar falha no Magic Packet

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Tratar recibo ausente/rejeitado ou PC não acordado. |
| Evento inicial | PRC-009 não confirma wake. |
| Evento final | Falha classificada, retry limitado ou orientação. |
| Atores | Usuário comum; Launcher; Celular; Windows |
| Raias | Usuário comum | Launcher | Android bridge | Windows/PC |
| Pré-condições | Pedido iniciado. |
| Pós-condições | Tentativa encerrada com evidência. |

## Fluxo principal

1. [Launcher] verifica recibo do bridge.
2. [Android bridge] retorna aceito/rejeitado/erro de envio.
3. [Launcher] valida se retry é permitido.
4. [Android bridge] opcionalmente envia segundo burst após 15 s.
5. [Launcher] sonda PC até limite.
6. [Launcher] classifica envio falho ou wake não confirmado.
7. [Usuário] recebe checklist energia/BIOS/rede.

## Gateways

G1 Recibo? Não→ERR012. G2 Rejeitado?→código auth/rate. G3 Retry disponível? Sim→uma vez. G4 PC acordou? Não→ERR013.

## Fluxos alternativos

Cancelamento elimina retry; já acordado avança.

## Exceções

Nunca repetir indefinidamente nem afirmar pacote chegou à NIC.

## Mensagens e eventos

wake.rejeitado; wake.retry; wake.não_confirmado

## Rastreabilidade

* **Regras:** RN004,RN009,RN012,RN020
* **Requisitos:** RF017-RF019,RF022
* **Dados manipulados:** WakeAttempt,ErrorEvent,HealthProbe

## Instrução para modelagem no Bizagi

Boundary timer de recibo; loop máximo 1 explicitamente anotado.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
