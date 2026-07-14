# PRC-017 — Adicionar computador

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Criar perfil completo do único PC do MVP. |
| Evento inicial | Técnico escolhe adicionar. |
| Evento final | Perfil operacional ou rascunho. |
| Atores | Técnico; Configurador; Windows; Celular |
| Raias | Técnico | Configurador | Windows/PC | Android bridge |
| Pré-condições | Nenhum perfil ativo no MVP. |
| Pós-condições | ComputerProfile criado. |

## Fluxo principal

1. [Configurador] verifica limite do MVP.
2. [Técnico] informa apenas nome amigável.
3. [Configurador] executa PRC-001.
4. [Configurador] associa adaptador/MAC/broadcast.
5. [Configurador] associa bridge pareado.
6. [Técnico] configura app remoto.
7. [Configurador] valida completude.
8. [Técnico] executa PRC-008.

## Gateways

G1 Limite atingido? Sim→bloquear/substituir. G2 Completo? Não→rascunho. G3 Teste passou?→classificação.

## Fluxos alternativos

Perfil anterior pode ser removido após revogação/backup.

## Exceções

Duplicidade de MAC/ID gera erro de integridade.

## Mensagens e eventos

pc.rascunho; pc.ativo; pc.validado

## Rastreabilidade

* **Regras:** RN002,RN003,RN004,RN015
* **Requisitos:** RF013,RF024
* **Dados manipulados:** ComputerProfile,NetworkAdapter,WakeTest

## Instrução para modelagem no Bizagi

Chamar processos reutilizáveis; gateway de limite no início.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
