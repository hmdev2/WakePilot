# PRC-011 — Aguardar serviços

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Monitorar PC, Windows e serviço remoto em fases. |
| Evento inicial | Recibo wake ou PC online sem serviço. |
| Evento final | Pronto, timeout ou cancelamento. |
| Atores | Usuário comum; Launcher; ReadinessAgent; Windows; Aplicativo remoto |
| Raias | Usuário comum | Launcher | ReadinessAgent | Windows/PC | Aplicativo remoto |
| Pré-condições | Tentativa ativa. |
| Pós-condições | Fase e duração registradas. |

## Fluxo principal

1. [Launcher] inicia backoff e cronômetros.
2. [Launcher] sonda rede e abre HTTPS/1.1 com mTLS usando identidades fixadas.
3. [Windows/PC] torna-se acessível.
4. [ReadinessAgent] valida certificado, versão, timestamp, requestId e nonce.
5. [ReadinessAgent] consulta estado do Windows e do serviço remoto sem executar mutação.
6. [Launcher] valida a resposta correlacionada e confirma Windows readiness.
7. [Aplicativo remoto] é reportado pronto pelo agente.
8. [Launcher] encerra cronômetros e avança.
9. [Usuário] acompanha progresso/cancela.

## Gateways

G1 Cancelou?→fim cancelado. G2 Windows em 240 s? Não→PRC-015. G3 serviço em 120 s? Não→erro específico.

## Fluxos alternativos

PC pode ficar rede-online antes do agente; ICMP bloqueado não substitui a sonda HTTPS autenticada. O launcher mantém uma requisição em voo e backoff de 1, 2, 4, 8 e 10 segundos dentro dos limites da fase.

## Exceções

Falha de sonda não é automaticamente PC offline. Certificado divergente/expirado/revogado bloqueia com ERR010; versão ou payload inválido usa ERR021.

## Mensagens e eventos

fase.alterada; windows.pronto; app.pronto; timeout

## Rastreabilidade

* **Regras:** RN013,RN019,RN020
* **Requisitos:** RF019,RF020
* **Dados manipulados:** WakeAttempt,HealthProbe,HealthState,ReadinessEndpoint,DeviceCertificate

## Instrução para modelagem no Bizagi

Usar gateway baseado em eventos com timer e mensagem de cancelamento; subprocessos de sonda em loop.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
