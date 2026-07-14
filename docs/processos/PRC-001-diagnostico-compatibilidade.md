# PRC-001 — Executar diagnóstico de compatibilidade

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Produzir evidências e classificação preliminar sem alterar o sistema. |
| Evento inicial | Técnico inicia diagnóstico. |
| Evento final | Resultado salvo e plano de próximos passos exibido. |
| Atores | Técnico; Configurador; Windows/hardware |
| Raias | Técnico | Configurador | Windows/hardware |
| Pré-condições | Aplicativo local instalado; acesso ao PC. |
| Pós-condições | Relatório preliminar persistido. |

## Fluxo principal

1. [Técnico] Inicia e autoriza coleta não destrutiva.
2. [Configurador] Cria correlation ID e inventaria Windows.
3. [Windows/hardware] Retorna sistema, fabricante e energia.
4. [Configurador] enumera e classifica adaptadores.
5. [Técnico] confirma Ethernet candidato quando ambíguo.
6. [Configurador] coleta propriedades WoL, powercfg e Fast Startup.
7. [Configurador] cruza evidências e atribui confiança.
8. [Técnico] revisa resultado e pendências.

## Gateways

G1 Dados mínimos obtidos? Não→inconclusivo. G2 Ethernet elegível? Não→incompatível no MVP. G3 teste real existe? Sim→usar resultado; Não→provável/incompleto.

## Fluxos alternativos

Consulta parcial gera item inconclusivo; adaptadores ambíguos exigem seleção técnica.

## Exceções

Permissão negada ou API indisponível é registrada sem inventar dado.

## Mensagens e eventos

diagnóstico.iniciado; diagnóstico.concluído

## Rastreabilidade

* **Regras:** RN001,RN002,RN003,RN004,RN017
* **Requisitos:** RF001-RF004
* **Dados manipulados:** SystemInfo,NetworkAdapter,DiagnosticResult

## Instrução para modelagem no Bizagi

Pool único; raias na ordem indicada; subprocessos para inventário e classificação; gateways exclusivos com rótulos Sim/Não.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
