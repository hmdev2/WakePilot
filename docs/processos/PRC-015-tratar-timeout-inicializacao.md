# PRC-015 — Tratar timeout da inicialização

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Encerrar espera e diferenciar ausência de wake de Windows não pronto. |
| Evento inicial | Cronômetro de fase expira. |
| Evento final | Erro específico e opções exibidos. |
| Atores | Usuário comum; Launcher; Windows |
| Raias | Usuário comum | Launcher | Windows/PC |
| Pré-condições | PRC-011 ativo. |
| Pós-condições | Timeout e última evidência registrados. |

## Fluxo principal

1. [Launcher] captura última sonda e linha do tempo.
2. [Launcher] verifica se houve qualquer evidência de rede/agent.
3. [Windows/PC] pode estar ligado sem readiness.
4. [Launcher] classifica ERR013 ou ERR014.
5. [Launcher] encerra sondas e libera recursos.
6. [Usuário] escolhe repetir verificação, abrir ajuda ou encerrar.

## Gateways

G1 Alguma evidência do PC? Não→wake não confirmado. Sim, agent ausente→Windows não pronto. Serviço apenas ausente→ERR015.

## Fluxos alternativos

Repetir somente sondas não reenvia WoL; novo wake exige nova operação.

## Exceções

Dados insuficientes usam desconhecido, não diagnóstico inventado.

## Mensagens e eventos

timeout.windows; timeout.wake; operação.encerrada

## Rastreabilidade

* **Regras:** RN020
* **Requisitos:** RF019,RF022
* **Dados manipulados:** WakeAttempt,HealthState,ErrorEvent

## Instrução para modelagem no Bizagi

Evento temporizador limítrofe no subprocesso de espera; gateway por evidência.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
