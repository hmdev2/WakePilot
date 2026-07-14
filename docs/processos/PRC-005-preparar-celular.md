# PRC-005 — Preparar celular intermediário

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Instalar bridge restrito e resiliente no Android. |
| Evento inicial | Técnico inicia preparação com celular em mãos. |
| Evento final | Health e reinício validados. |
| Atores | Técnico; Configurador; Celular; VPN |
| Raias | Técnico | Configurador | Android/Termux | Tailscale |
| Pré-condições | PRC-004; Android 10+; acesso físico. |
| Pós-condições | Bridge cadastrado como pronto ou risco explícito. |

## Fluxo principal

1. [Configurador] gera pacote/QR de bootstrap assinado.
2. [Técnico] instala Termux e Termux:Boot da mesma origem.
3. [Android/Termux] instala OpenSSH e wrapper.
4. [Técnico] abre Termux:Boot uma vez e ajusta bateria.
5. [Tailscale] autentica o Android.
6. [Android/Termux] instala boot script e configuração restrita.
7. [Técnico] reinicia o celular.
8. [Configurador] verifica Tailscale, host e health.

## Gateways

G1 Origem compatível? Não→corrigir instalação. G2 Background permitido? Não→orientar. G3 Reinício passou? Não→incompleto.

## Fluxos alternativos

Fabricante exige menu específico; usuário pode retomar após ajuste.

## Exceções

Pacote sem integridade ou SSH irrestrito bloqueia conclusão.

## Mensagens e eventos

bridge.bootstrap; bridge.health; bridge.boot_falhou

## Rastreabilidade

* **Regras:** RN009,RN010,RN016
* **Requisitos:** RF010
* **Dados manipulados:** BridgeDevice,BridgeHealth,InstallationRecord

## Instrução para modelagem no Bizagi

Modelar tarefas manuais Android e evento temporizador para reinício/health.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
