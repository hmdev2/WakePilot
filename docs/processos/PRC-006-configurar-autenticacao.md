# PRC-006 — Configurar autenticação

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Criar credencial exclusiva e instalar autorização mínima. |
| Evento inicial | Técnico solicita credencial. |
| Evento final | Chave pública restrita e privada protegida. |
| Atores | Técnico; Configurador; Celular |
| Raias | Técnico | Launcher/Configurador | Android bridge |
| Pré-condições | PRC-005 concluído. |
| Pós-condições | CredentialReference ativa e auditada. |

## Fluxo principal

1. [Launcher] gera Ed25519 e fingerprint.
2. [Launcher] protege privada com DPAPI.
3. [Técnico] confere identidade do notebook.
4. [Configurador] prepara entrada authorized_keys com forced command.
5. [Android bridge] instala somente chave pública e restrições.
6. [Launcher] testa autenticação sem PTY.
7. [Android bridge] executa apenas health.
8. [Configurador] registra referência/fingerprint.

## Gateways

G1 DPAPI protegeu? Não→destruir e abortar. G2 Restrições válidas? Não→bloquear. G3 Teste health? Não→reverter autorização.

## Fluxos alternativos

Regeneração revoga material anterior após confirmação.

## Exceções

Qualquer shell/forwarding possível é falha crítica.

## Mensagens e eventos

auth.chave_gerada; auth.instalada; auth.rejeitada

## Rastreabilidade

* **Regras:** RN010,RN011,RN012,RN014
* **Requisitos:** RF011
* **Dados manipulados:** AuthorizedDevice,SecretReference,AuditEvent

## Instrução para modelagem no Bizagi

Subprocesso transacional; evento de erro conduz à destruição de material temporário.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
