# PRC-012 — Abrir acesso remoto

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Iniciar cliente validado com argumentos seguros. |
| Evento inicial | Serviço remoto pronto. |
| Evento final | Processo iniciado ou ERR de abertura. |
| Atores | Usuário comum; Launcher; Aplicativo remoto |
| Raias | Usuário comum | Launcher | Aplicativo remoto |
| Pré-condições | RF020 pronto. |
| Pós-condições | LaunchResult registrado. |

## Fluxo principal

1. [Launcher] resolve caminho canônico.
2. [Launcher] revalida arquivo/assinatura conforme política.
3. [Launcher] constrói ArgumentList tipada.
4. [Launcher] inicia processo sem shell.
5. [Aplicativo remoto] confirma criação de processo.
6. [Launcher] registra duração/resultado.
7. [Usuário] recebe foco ou orientação.

## Gateways

G1 Executável válido? Não→reconfigurar. G2 Processo iniciou? Não→ERR016.

## Fluxos alternativos

Instância já aberta pode receber foco conforme adapter.

## Exceções

Alteração de hash/caminho após configuração bloqueia.

## Mensagens e eventos

app.validação; app.aberto; app.falhou

## Rastreabilidade

* **Regras:** RN016,RN018,RN019
* **Requisitos:** RF014,RF021
* **Dados manipulados:** RemoteApplicationProfile,LaunchResult

## Instrução para modelagem no Bizagi

Tarefa de serviço Launcher; gateway exclusivo; aplicativo remoto como pool externo.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
