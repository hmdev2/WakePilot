# PRC-010 — Tratar computador já ligado

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Evitar WoL e concluir acesso quando PC já está pronto. |
| Evento inicial | Sonda inicial confirma PC/serviço. |
| Evento final | Aplicativo aberto ou erro específico. |
| Atores | Usuário comum; Launcher; Aplicativo remoto |
| Raias | Usuário comum | Launcher | Aplicativo remoto |
| Pré-condições | PRC-009 em verificação inicial. |
| Pós-condições | Nenhuma tentativa WoL criada. |

## Fluxo principal

1. [Launcher] confirma Windows pronto.
2. [Launcher] consulta adapter remoto.
3. [Aplicativo remoto] informa readiness.
4. [Launcher] registra decisão de não enviar WoL.
5. [Launcher] abre cliente validado.
6. [Usuário] recebe confirmação.

## Gateways

G1 Serviço pronto? Não→PRC-011 apenas para serviço. G2 Executável válido? Não→TEL018.

## Fluxos alternativos

Serviço ainda iniciando é aguardado; PC online mas readiness desconhecido não usa este atalho.

## Exceções

Sonda conflitante retorna desconhecido.

## Mensagens e eventos

pc.já_pronto; wake.ignorado; app.aberto

## Rastreabilidade

* **Regras:** RN001,RN018,RN019
* **Requisitos:** RF015,RF020,RF021
* **Dados manipulados:** HealthState,RemoteApplicationProfile

## Instrução para modelagem no Bizagi

Gateway inicial de PRC-009 direciona a subprocesso; não modelar envio WoL.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
