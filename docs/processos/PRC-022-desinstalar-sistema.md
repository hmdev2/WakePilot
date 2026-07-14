# PRC-022 — Desinstalar sistema

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Remover produto e acessos residuais com escolha de dados. |
| Evento inicial | Técnico/avançado inicia desinstalação. |
| Evento final | Componentes removidos e pendências informadas. |
| Atores | Técnico; Desinstalador; Windows; Celular |
| Raias | Técnico/Avançado | Desinstalador | Windows/PC | Android bridge |
| Pré-condições | Instalação detectada. |
| Pós-condições | InstallationRecord removido; resumo final. |

## Fluxo principal

1. [Desinstalador] inventaria componentes e snapshots.
2. [Usuário] escolhe restaurar Windows e preservar/apagar logs.
3. [Desinstalador] executa PRC-018 para credenciais.
4. [Desinstalador] executa PRC-021 se escolhido.
5. [Windows/PC] para/remove serviço, tarefas, app e dados.
6. [Android bridge] remove wrapper/chave quando alcançável.
7. [Desinstalador] verifica resíduos.
8. [Usuário] recebe resumo e instrução para pendência offline.

## Gateways

G1 Bridge online? Não→pendência manual. G2 Preservar logs? Sim→manter somente consentidos. G3 Resíduos? Sim→relatório.

## Fluxos alternativos

Reinstalação futura cria novas credenciais; falha parcial pode ser retomada.

## Exceções

Nunca apagar dados não pertencentes ao produto.

## Mensagens e eventos

uninstall.iniciado; acesso.revogado; uninstall.concluído

## Rastreabilidade

* **Regras:** RN007,RN011,RN014,RN016
* **Requisitos:** RF027,RF029,RF030
* **Dados manipulados:** InstallationRecord,AuthorizedDevice,RestoreOperation

## Instrução para modelagem no Bizagi

Chamar subprocessos; gateway de preservação; final “concluído com pendências” separado.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
