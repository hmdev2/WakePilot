# PRC-016 — Alterar aplicativo remoto

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Trocar adapter/perfil mantendo execução segura. |
| Evento inicial | Avançado/técnico abre configurações. |
| Evento final | Novo perfil validado ou alteração descartada. |
| Atores | Técnico; Usuário avançado; Configurador; Aplicativo remoto |
| Raias | Técnico/Avançado | Configurador | Aplicativo remoto |
| Pré-condições | Perfil de PC existente. |
| Pós-condições | RemoteApplicationProfile versionado. |

## Fluxo principal

1. [Usuário] seleciona preset RustDesk ou personalizado.
2. [Configurador] localiza executável.
3. [Usuário] escolhe health probe permitida.
4. [Configurador] valida caminho, hash e argumentos.
5. [Aplicativo remoto] é aberto em teste controlado.
6. [Usuário] confirma resultado.
7. [Configurador] salva nova versão e mantém anterior.

## Gateways

G1 Executável válido? Não→bloquear. G2 Teste passou? Não→manter anterior.

## Fluxos alternativos

Cancelamento descarta rascunho; instância existente pode ser fechada pelo usuário.

## Exceções

Texto livre de shell é rejeitado.

## Mensagens e eventos

app.perfil_alterado; app.teste

## Rastreabilidade

* **Regras:** RN016,RN018,RN019
* **Requisitos:** RF014
* **Dados manipulados:** RemoteApplicationProfile,LaunchResult

## Instrução para modelagem no Bizagi

Subprocesso de validação; gateway de teste; data store de perfil versionado.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
