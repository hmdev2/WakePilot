# PRC-004 — Instalar e configurar VPN

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Estabelecer tailnet sem armazenar chave de autenticação. |
| Evento inicial | Técnico inicia etapa VPN. |
| Evento final | Tailscale instalado, autenticado e testado ou pendente. |
| Atores | Técnico; Configurador; VPN |
| Raias | Técnico | Configurador | Tailscale |
| Pré-condições | Internet e conta do proprietário. |
| Pós-condições | Identidade/IP Tailscale referenciada sem token. |

## Fluxo principal

1. [Configurador] detecta cliente e versão.
2. [Técnico] consente instalação se ausente.
3. [Configurador] abre fonte/instalador oficial.
4. [Técnico] conclui instalação e autentica no navegador.
5. [Tailscale] registra dispositivo na tailnet.
6. [Configurador] lê estado local e endereço estável.
7. [Configurador] testa conectividade básica.
8. [Técnico] aprova o resultado.

## Gateways

G1 Instalado? Não→instalar. G2 Autenticado? Não→aguardar/retomar. G3 Conectividade? Não→diagnosticar VPN.

## Fluxos alternativos

Sem internet, login cancelado ou aprovação pendente preserva progresso.

## Exceções

Cliente adulterado/versão incompatível é bloqueado.

## Mensagens e eventos

vpn.detectada; vpn.autenticada; vpn.falhou

## Rastreabilidade

* **Regras:** RN008,RN015,RN016
* **Requisitos:** RF009
* **Dados manipulados:** VpnProfile,Device,AuditEvent

## Instrução para modelagem no Bizagi

Representar Tailscale como pool externo; autenticação como tarefa do usuário com mensagem de retorno.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
