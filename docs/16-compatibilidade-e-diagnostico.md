# Compatibilidade e diagnóstico

## Controle

Versão 1.1.0 — Estado: Aprovado com matriz inicial — Data: 14/07/2026.

## Princípio

* **Detecção:** dado diretamente observado por API/ferramenta.
* **Inferência:** conclusão provável a partir de evidências.
* **Validação real:** teste ponta a ponta registrado no hardware e estado.
* **Limitação:** comportamento documentado ou observado que restringe o cenário.
* **Decisão:** regra do produto, não fato do fornecedor.

## Matriz Windows/hardware

| Dimensão | Suporte MVP | Diagnóstico | Resultado |
| --- | --- | --- | --- |
| Windows 11 x64 | Sim, builds suportadas | build/edição/arquitetura | homologado após testes |
| Windows 10 22H2 x64 | Condicional a atualizações/ESU/LTSC | ciclo e build | aviso/bloqueio de produção |
| ARM/x86 32-bit | Não | arquitetura | incompatível no MVP |
| Ethernet físico | Sim | interface/driver/link/MAC | candidato |
| Wi-Fi/WoWLAN | Não | classificar Wi-Fi | fora do escopo |
| Adaptador virtual/VPN | Não como alvo | PnP/interface type | ignorado |
| S3 | Quando disponível | powercfg + teste | por equipamento |
| S4 hibernação | Quando suportado | powercfg + teste | por equipamento |
| S4 Fast Startup | Risco conhecido | registro/energia | não assumir wake |
| S5 | Sem garantia | firmware/driver/teste | apenas se teste real passar |
| Modern Standby | Diagnóstico específico | powercfg /a | inconclusivo até teste |
| Desktop/notebook | Ambos, Ethernet energizada | modelo/energia | por equipamento |

A documentação Microsoft afirma suporte de WoL nos estados documentados e ressalta limitações de Fast Startup/S5; comportamento de fabricante pode diferir, por isso a regra é teste real.

## BIOS/UEFI

Base de guias por fabricante deve armazenar sinônimos como Wake on LAN, Power On By PCI-E/PCI, Resume by LAN, PME Event Wake Up e Deep Sleep/ErP, sempre marcados como exemplos. Fonte oficial do modelo prevalece. ErP/Deep Sleep pode cortar energia da NIC.

## Android

| Dimensão | MVP | Testes |
| --- | --- | --- |
| Versão | Android 10+ | boot, tela bloqueada, Doze, 24 h |
| Google Pixel | Prioritário | Android de referência |
| Samsung | Prioritário | bateria/Auto optimization |
| Motorola | Prioritário | bateria/background |
| Outros OEMs | Não homologado inicialmente | diagnóstico inconclusivo até matriz |
| Root | Não requerido/não suportado | bridge sandbox |
| Termux/Boot | Mesma origem de assinatura | primeira abertura, boot script |
| Alimentação | Contínua recomendada | desconexão/reconexão |

## VPN e aplicações

| Item | Estado |
| --- | --- |
| Tailscale | MVP; versões suportadas testadas a cada release |
| ReadinessAgent | MVP; HTTPS/1.1 com mTLS conforme ADR-010; contrato, pinning, certificados e firewall testados |
| RustDesk | Preset MVP de detecção/abertura; credenciais fora do produto |
| Executável personalizado | MVP com path/hash/args tipados |
| AnyDesk | Evolução |
| Moonlight/Sunshine | Evolução |
| RDP | Evolução; exige análise de edição/política Windows |

## Árvore de classificação

1. Impedimento confirmado do escopo → incompatível.
2. Configuração obrigatória não concluída → configuração incompleta.
3. Evidência insuficiente/contraditória → inconclusivo.
4. Evidências favoráveis sem teste → provavelmente compatível.
5. Teste completo no estado selecionado → compatível e validado por teste.

Validação é específica a PC, adaptador, driver, firmware, estado de energia, bridge, ReadinessAgent, certificados e versões. Mudança relevante torna o resultado “revalidação necessária”.

## Referências oficiais

* [Microsoft: comportamento Wake-on-LAN](https://learn.microsoft.com/en-us/troubleshoot/windows-client/setup-upgrade-and-drivers/wake-on-lan-feature)
* [Microsoft: estados de energia](https://learn.microsoft.com/en-us/windows/win32/power/system-power-states)
* [Microsoft: powercfg](https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options)
* [Android: otimização em segundo plano](https://developer.android.com/topic/performance/background-optimization)
* [Termux:Boot](https://github.com/termux/termux-boot/blob/master/README.md)
