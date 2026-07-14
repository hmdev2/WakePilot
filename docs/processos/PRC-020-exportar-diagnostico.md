# PRC-020 — Exportar diagnóstico

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Gerar pacote sanitizado e verificável para suporte. |
| Evento inicial | Usuário solicita exportação. |
| Evento final | ZIP e manifesto salvos ou nada persistido. |
| Atores | Usuário; Configurador; Suporte |
| Raias | Usuário/Técnico | Configurador | Suporte |
| Pré-condições | Dados de diagnóstico disponíveis. |
| Pós-condições | ExportArtifact criado e auditado. |

## Fluxo principal

1. [Usuário] escolhe período/categorias.
2. [Configurador] mostra prévia e dados mascarados.
3. [Usuário] consente e escolhe destino.
4. [Configurador] coleta inventário, testes e logs.
5. [Configurador] sanitiza e executa scanner de segredos.
6. [Configurador] gera manifesto/hash e ZIP.
7. [Usuário] compartilha externamente por sua decisão.
8. [Suporte] lê correlation IDs e orienta.

## Gateways

G1 Scanner limpo? Não→bloquear/remover item. G2 Gravação passou? Não→apagar parcial.

## Fluxos alternativos

Usuário exclui categorias; detalhes técnicos completos exigem aviso.

## Exceções

Nunca transmitir automaticamente nem incluir privada/token.

## Mensagens e eventos

diagnóstico.exportado; exportação.bloqueada

## Rastreabilidade

* **Regras:** RN014,RN015
* **Requisitos:** RF025,RF026
* **Dados manipulados:** DiagnosticResult,LogEvent,ExportArtifact

## Instrução para modelagem no Bizagi

Suporte em pool externo; transferência é tarefa do usuário; gateway scanner.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
