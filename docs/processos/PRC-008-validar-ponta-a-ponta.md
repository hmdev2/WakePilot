# PRC-008 — Validar ativação de ponta a ponta

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Comprovar wake, Windows, serviço e abertura no estado escolhido. |
| Evento inicial | Técnico inicia teste real. |
| Evento final | Classificação registrada com evidências. |
| Atores | Técnico; Configurador; Windows/hardware; Celular; VPN; Aplicativo remoto |
| Raias | Técnico | Configurador | Android bridge | Windows/PC | Aplicativo remoto |
| Pré-condições | Perfil completo; acesso físico de contingência. |
| Pós-condições | Teste imutável e classificação atualizada. |

## Fluxo principal

1. [Técnico] escolhe estado S3/S4/S5 aplicável e consente.
2. [Configurador] registra baseline e orienta colocar PC no estado.
3. [Android bridge] recebe pedido e envia Magic Packet.
4. [Configurador] mede tempo até rede/Windows.
5. [Windows/PC] inicia e sinaliza readiness.
6. [Configurador] aguarda serviço remoto.
7. [Aplicativo remoto] é aberto em modo de teste.
8. [Técnico] confirma resultado e observações.
9. [Configurador] classifica compatibilidade.

## Gateways

G1 Pedido aceito? G2 PC acordou? G3 Windows pronto? G4 serviço pronto? Todos Sim→validado; qualquer Não→classe inferior.

## Fluxos alternativos

Teste repetido por estado; abertura pode ser simulada até último passo.

## Exceções

Perda de acesso físico encerra teste com segurança.

## Mensagens e eventos

teste.iniciado; wake.aceito; windows.pronto; teste.concluído

## Rastreabilidade

* **Regras:** RN004,RN005,RN006,RN019,RN020
* **Requisitos:** RF024
* **Dados manipulados:** WakeTest,WakeAttempt,DiagnosticResult

## Instrução para modelagem no Bizagi

Subprocessos reutilizáveis PRC-009,011,012; gateways por camada e evento temporizador.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
