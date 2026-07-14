# PRC-019 — Atualizar sistema

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Aplicar pacote compatível e assinado com migração e rollback. |
| Evento inicial | Usuário autorizado verifica atualização. |
| Evento final | Nova versão saudável ou anterior restaurada. |
| Atores | Técnico; Atualizador; Componentes |
| Raias | Técnico/Avançado | Atualizador | Launcher/Serviço |
| Pré-condições | Manifesto acessível; versão instalada. |
| Pós-condições | InstallationRecord e schema consistentes. |

## Fluxo principal

1. [Atualizador] baixa manifesto via TLS.
2. [Atualizador] valida assinatura e compatibilidade.
3. [Usuário] revisa versão e consente.
4. [Atualizador] cria backup/snapshot.
5. [Atualizador] valida hash e instala.
6. [Componentes] executam migração e health check.
7. [Atualizador] confirma ou reverte.
8. [Usuário] recebe relatório.

## Gateways

G1 Assinatura válida? Não→rejeitar. G2 Compatível? Não→bloquear. G3 Health passou? Não→rollback.

## Fluxos alternativos

MVP permite download/instalação manual assistida; offline adia.

## Exceções

Resultado de finalização incerto não é repetido automaticamente.

## Mensagens e eventos

update.disponível; update.aplicado; update.rollback

## Rastreabilidade

* **Regras:** RN007,RN016
* **Requisitos:** RF028
* **Dados manipulados:** SoftwareVersion,InstallationRecord,RestoreSnapshot

## Instrução para modelagem no Bizagi

Subprocesso transacional com compensation/rollback; gateways de assinatura e health.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
