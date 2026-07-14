# PRC-007 — Parear dispositivos

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

| Campo | Especificação |
| --- | --- |
| Objetivo | Fixar identidades e vínculo entre launcher, bridge e PC. |
| Evento inicial | Técnico inicia pareamento presencial. |
| Evento final | SSH health e readiness HTTPS autenticados, com identidades fixadas. |
| Atores | Técnico; Configurador; Celular; Launcher; ReadinessAgent |
| Raias | Técnico | Configurador | Android bridge | Launcher | Windows/ReadinessAgent |
| Pré-condições | PRC-006; código de uso único. |
| Pós-condições | Pairing ativo e código consumido. |

## Fluxo principal

1. [Configurador] cria código/QR por 10 min.
2. [Técnico] apresenta código ao launcher/bridge.
3. [Android bridge] fornece ID e fingerprint da host key SSH.
4. [ReadinessAgent] fornece IP Tailscale atual, porta dinâmica e fingerprint do certificado de servidor.
5. [Launcher] gera certificado cliente não exportável e exibe os três fingerprints para conferência.
6. [Técnico] confirma coincidência nos dispositivos presentes.
7. [Launcher] fixa host key e certificado do agente; envia ao agente somente o certificado público do launcher.
8. [Android bridge] valida chave/alvo e responde ao comando health.
9. [ReadinessAgent] registra o certificado cliente e responde ao readiness somente após mTLS válido.
10. [Configurador] verifica os dois canais, consome o código e ativa o vínculo.

## Gateways

G1 Código válido? Não→novo código. G2 Todos os fingerprints coincidem? Não→cancelar. G3 SSH health passou? Não→incompleto. G4 mTLS readiness passou? Não→incompleto.

## Fluxos alternativos

Expiração ou interrupção apaga estado parcial.

## Exceções

Divergência SSH/mTLS gera ERR010; contrato/certificado inválido gera ERR021. Ambos exigem investigação e impedem aceite silencioso.

## Mensagens e eventos

pareamento.iniciado; pareamento.concluído; pareamento.divergente

## Rastreabilidade

* **Regras:** RN010,RN011,RN013
* **Requisitos:** RF012
* **Dados manipulados:** PairingSession,AuthorizedDevice,BridgeDevice,ReadinessEndpoint,DeviceCertificate

## Instrução para modelagem no Bizagi

Eventos de mensagem entre pools; temporizador limítrofe de 10 min; gateway exclusivo para conferência conjunta dos fingerprints SSH/mTLS.

Usar nomes de tarefas no infinitivo, eventos com resultado no particípio e documentar nos elementos BPMN os códigos de erro aplicáveis.
