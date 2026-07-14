# PRC-013 — Tratar celular offline

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Distinguir VPN, alcance e serviço bridge sem enviar WoL. |
| Evento inicial | Launcher não obtém health. |
| Evento final | Causa provável e ação exibidas. |
| Atores | Usuário comum; Launcher; VPN; Celular |
| Raias | Usuário comum | Launcher | Tailscale | Android bridge |
| Pré-condições | PC offline; bridge não saudável. |
| Pós-condições | Nenhum wake executado. |

## Fluxo principal

1. [Launcher] consulta estado Tailscale local.
2. [Tailscale] retorna conectado/desconectado.
3. [Launcher] testa alcance do node.
4. [Android bridge] responde ou não ao health.
5. [Launcher] compara host key quando alcançado.
6. [Launcher] classifica ERR008/009/010.
7. [Usuário] recebe ação e pode repetir.

## Gateways

G1 VPN local conectada? G2 node alcançável? G3 wrapper saudável? G4 host coincide?

## Fluxos alternativos

Usuário abre ajuda; tentativa posterior reutiliza perfil, não credencial temporária.

## Exceções

Host divergente nunca oferece aceitar automaticamente.

## Mensagens e eventos

bridge.offline; vpn.offline; host.divergente

## Rastreabilidade

* **Regras:** RN008,RN009,RN013,RN020
* **Requisitos:** RF016,RF022
* **Dados manipulados:** BridgeHealth,VpnProfile,ErrorEvent

## Instrução para modelagem no Bizagi

Cadeia de gateways exclusivos com finais distintos; nenhum fluxo para envio WoL.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
