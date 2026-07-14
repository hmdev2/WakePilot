# Escopo do MVP

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

## Objetivo do MVP

Validar e disponibilizar, com segurança mínima de produção, a jornada de um usuário que liga um PC Windows por meio de um celular Android na mesma LAN e abre o RustDesk quando o PC e o serviço estiverem prontos.

## Dentro do MVP

| Área | Inclusão aprovada |
| --- | --- |
| Topologia | 1 PC principal, 1 Android bridge e 1 notebook launcher por perfil |
| PC principal | Windows 11 x64; Windows 10 22H2 x64 somente se coberto por atualizações de segurança/ESU/LTSC |
| Notebook | Windows 11 x64; Windows 10 nas mesmas condições |
| Rede do PC | Ethernet IPv4 em LAN doméstica/pequeno escritório |
| Android | Android 10 ou superior; teste inicial em Google, Samsung e Motorola |
| Rede privada | Tailscale, instalado e autenticado visualmente |
| Bridge | Termux + Termux:Boot + OpenSSH com chave individual e forced command |
| Desktop | .NET 10 LTS + WPF, dois executáveis visuais e bibliotecas compartilhadas |
| Persistência | SQLite para estado; DPAPI/Credential Manager e Windows Certificate Store para material secreto; JSON apenas para exportação sanitizada |
| Aplicativo remoto | Preset RustDesk e perfil de executável personalizado; abertura do cliente sem automatizar credenciais do RustDesk |
| Diagnóstico | Windows, adaptador Ethernet, MAC, powercfg, driver, Fast Startup, fabricante/modelo e checklist BIOS |
| Operação | detectar estados, solicitar wake, acompanhar, cancelar, abrir app, registrar erros |
| Segurança | host/certificate pinning, SSH e mTLS por dispositivo, forced command, nonce/timestamp, rate limit, revogação e logs sanitizados |
| Administração | consentimento por alteração, elevação pontual, snapshot e rollback |
| Suporte | relatório ZIP/JSON sanitizado e códigos ERR001–ERR021 |
| Distribuição | instalador x64 para teste; releases públicas exigem assinatura |

## Fora do MVP

* Wake-on-WLAN, IPv6-only, macOS, Linux, iOS e Windows ARM.
* Mais de um PC, bridge ou notebook por perfil.
* Configuração automática de BIOS/UEFI.
* Backend próprio, conta Remote Wake Assistant ou sincronização em nuvem.
* App Android nativo e publicação em Play Store.
* Integrações profundas com AnyDesk, Moonlight/Sunshine e RDP.
* Armazenar ou preencher senha de aplicações remotas.
* Atualização silenciosa automática.
* Ambientes de domínio/MDM com políticas corporativas não homologadas.
* NAT traversal próprio e redirecionamento de portas no roteador.
* Garantia de S5; o suporte real é resultado do teste por equipamento.

## Evolução posterior

1. Bridge Android nativa com foreground service e protocolo próprio.
2. Múltiplos PCs e notebooks autorizados.
3. Presets AnyDesk, Moonlight/Sunshine e RDP.
4. Windows ARM e outros sistemas operacionais.
5. Canal de atualização assinado e automatizado.
6. Internacionalização.
7. Políticas corporativas opcionais, somente com arquitetura própria.

## Restrições

* O técnico terá acesso físico na configuração inicial.
* O Android ficará alimentado e conectado ao Wi-Fi da LAN.
* A VPN deverá ser autenticada pelo proprietário; chaves de Tailscale não serão incluídas no produto.
* Toda compatibilidade “validada” exigirá teste de wake a partir do estado de energia selecionado.
* O usuário comum não utilizará terminal.
* Alterações administrativas exigirão UAC e consentimento específico.

## Critérios de conclusão

| ID | Critério |
| --- | --- |
| MVP-001 | Fluxo ponta a ponta passa 30 vezes em ambiente homologado com ≥95% de sucesso. |
| MVP-002 | RF001–RF030 prioritários P0/P1 estão implementados ou explicitamente adiados sem quebrar a jornada. |
| MVP-003 | Todos os CT críticos passam; nenhuma vulnerabilidade crítica/alta conhecida permanece sem decisão. |
| MVP-004 | Instalação, rollback e desinstalação são testados em Windows limpo. |
| MVP-005 | Nenhum segredo aparece em repositório, banco, logs ou exportação. |
| MVP-006 | Usuário comum conclui teste moderado sem terminal ou identificação de MAC. |
| MVP-007 | Documentação técnica, manual do usuário e relatório de limitações estão entregues. |

## Premissas convertidas em decisão

PP-001, PP-002, PP-003, PP-004, PP-005, PP-006, PP-007, PP-008, PP-009 e PP-010 foram resolvidas conforme este escopo e os ADRs. Permanecem riscos, não dúvidas de escopo.

## Mudança de escopo

Qualquer inclusão que adicione plataforma, topologia múltipla, backend ou shell remoto requer revisão de arquitetura, segurança, testes e prazo antes de aprovação.
