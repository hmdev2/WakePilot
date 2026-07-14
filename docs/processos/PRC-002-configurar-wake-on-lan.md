# PRC-002 — Configurar Wake-on-LAN

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Aplicar configurações Windows autorizadas com snapshot e verificação. |
| Evento inicial | Técnico seleciona corrigir configuração. |
| Evento final | Configuração verificada ou rollback oferecido. |
| Atores | Técnico; Configurador; Windows/hardware |
| Raias | Técnico | Configurador | Serviço privilegiado | Windows/hardware |
| Pré-condições | PRC-001 concluído; ação suportada. |
| Pós-condições | Alterações verificadas e auditadas. |

## Fluxo principal

1. [Configurador] deriva plano de alterações.
2. [Técnico] revisa ação, risco e rollback.
3. [Técnico] seleciona ações e confirma.
4. [Configurador] captura snapshot.
5. [Serviço privilegiado] valida operação tipada e elevação.
6. [Windows/hardware] aplica configuração.
7. [Configurador] relê valor e compara.
8. [Técnico] recebe resultado.

## Gateways

G1 Consentiu? Não→encerrar incompleto. G2 Snapshot válido? Não→bloquear. G3 Verificação passou? Não→oferecer rollback.

## Fluxos alternativos

Ação já correta é ignorada; lote parcial interrompe no primeiro erro.

## Exceções

Operação fora da allowlist é ERR007 e não executa.

## Mensagens e eventos

configuração.proposta; configuração.aplicada; rollback.disponível

## Rastreabilidade

* **Regras:** RN006,RN007,RN016
* **Requisitos:** RF006-RF008
* **Dados manipulados:** ConfigurationChange,RestoreSnapshot,AuditEvent

## Instrução para modelagem no Bizagi

Modelar subprocesso transacional aplicar/verificar e evento de erro levando ao rollback.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
