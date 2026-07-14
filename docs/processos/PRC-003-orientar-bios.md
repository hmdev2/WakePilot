# PRC-003 — Orientar configuração da BIOS

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Guiar configuração de firmware sem alegar automação. |
| Evento inicial | Diagnóstico indica BIOS necessária ou incerta. |
| Evento final | Confirmação humana/pendência registrada. |
| Atores | Técnico; Configurador; BIOS/UEFI |
| Raias | Técnico | Configurador | BIOS/UEFI |
| Pré-condições | Fabricante/modelo conhecidos ou guia genérico. |
| Pós-condições | Checklist e resultado salvos. |

## Fluxo principal

1. [Configurador] identifica fabricante/modelo.
2. [Configurador] seleciona guia oficial ou genérico.
3. [Técnico] lê riscos e prepara acesso local.
4. [Técnico] reinicia e entra no firmware.
5. [BIOS/UEFI] apresenta opções disponíveis.
6. [Técnico] localiza opção equivalente, altera e salva.
7. [Técnico] retorna ao configurador e declara resultado.
8. [Configurador] marca concluído, não localizado ou adiado.

## Gateways

G1 Guia específico? Não→genérico. G2 Opção localizada? Não→pendência/inconclusivo. G3 Alteração confirmada? Não→não concluir.

## Fluxos alternativos

Usuário adia; fabricante exige procedimento próprio.

## Exceções

Falha de boot exige contingência do fabricante e não ação automática do produto.

## Mensagens e eventos

bios.orientação_exibida; bios.resultado_registrado

## Rastreabilidade

* **Regras:** RN005,RN006
* **Requisitos:** RF005
* **Dados manipulados:** FirmwareGuide,DiagnosticResult

## Instrução para modelagem no Bizagi

Usar tarefa manual no técnico e subprocesso colapsado externo BIOS/UEFI; não usar service task para alterar BIOS.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
