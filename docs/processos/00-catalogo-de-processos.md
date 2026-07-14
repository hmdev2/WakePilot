# Catálogo de processos

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

| ID | Processo | Arquivo | Ator primário | Requisitos centrais |
| --- | --- | --- | --- | --- |
| PRC-001 | Executar diagnóstico de compatibilidade | [PRC-001-diagnostico-compatibilidade.md](PRC-001-diagnostico-compatibilidade.md) | Técnico | RF001-RF004 |
| PRC-002 | Configurar Wake-on-LAN | [PRC-002-configurar-wake-on-lan.md](PRC-002-configurar-wake-on-lan.md) | Técnico | RF006-RF008 |
| PRC-003 | Orientar configuração da BIOS | [PRC-003-orientar-bios.md](PRC-003-orientar-bios.md) | Técnico | RF005 |
| PRC-004 | Instalar e configurar VPN | [PRC-004-instalar-configurar-vpn.md](PRC-004-instalar-configurar-vpn.md) | Técnico | RF009 |
| PRC-005 | Preparar celular intermediário | [PRC-005-preparar-celular.md](PRC-005-preparar-celular.md) | Técnico | RF010 |
| PRC-006 | Configurar autenticação | [PRC-006-configurar-autenticacao.md](PRC-006-configurar-autenticacao.md) | Técnico | RF011 |
| PRC-007 | Parear dispositivos | [PRC-007-parear-dispositivos.md](PRC-007-parear-dispositivos.md) | Técnico | RF012 |
| PRC-008 | Validar ativação de ponta a ponta | [PRC-008-validar-ponta-a-ponta.md](PRC-008-validar-ponta-a-ponta.md) | Técnico | RF024 |
| PRC-009 | Ligar computador remotamente | [PRC-009-ligar-computador.md](PRC-009-ligar-computador.md) | Usuário comum | RF015-RF023 |
| PRC-010 | Tratar computador já ligado | [PRC-010-computador-ja-ligado.md](PRC-010-computador-ja-ligado.md) | Usuário comum | RF015,RF020,RF021 |
| PRC-011 | Aguardar serviços | [PRC-011-aguardar-servicos.md](PRC-011-aguardar-servicos.md) | Usuário comum | RF019,RF020 |
| PRC-012 | Abrir acesso remoto | [PRC-012-abrir-acesso-remoto.md](PRC-012-abrir-acesso-remoto.md) | Usuário comum | RF014,RF021 |
| PRC-013 | Tratar celular offline | [PRC-013-tratar-celular-offline.md](PRC-013-tratar-celular-offline.md) | Usuário comum | RF016,RF022 |
| PRC-014 | Tratar falha no Magic Packet | [PRC-014-tratar-falha-magic-packet.md](PRC-014-tratar-falha-magic-packet.md) | Usuário comum | RF017-RF019,RF022 |
| PRC-015 | Tratar timeout da inicialização | [PRC-015-tratar-timeout-inicializacao.md](PRC-015-tratar-timeout-inicializacao.md) | Usuário comum | RF019,RF022 |
| PRC-016 | Alterar aplicativo remoto | [PRC-016-alterar-aplicativo-remoto.md](PRC-016-alterar-aplicativo-remoto.md) | Técnico | RF014 |
| PRC-017 | Adicionar computador | [PRC-017-adicionar-computador.md](PRC-017-adicionar-computador.md) | Técnico | RF013,RF024 |
| PRC-018 | Revogar dispositivo | [PRC-018-revogar-dispositivo.md](PRC-018-revogar-dispositivo.md) | Técnico | RF027 |
| PRC-019 | Atualizar sistema | [PRC-019-atualizar-sistema.md](PRC-019-atualizar-sistema.md) | Técnico | RF028 |
| PRC-020 | Exportar diagnóstico | [PRC-020-exportar-diagnostico.md](PRC-020-exportar-diagnostico.md) | Usuário | RF025,RF026 |
| PRC-021 | Restaurar configurações | [PRC-021-restaurar-configuracoes.md](PRC-021-restaurar-configuracoes.md) | Técnico | RF008,RF029 |
| PRC-022 | Desinstalar sistema | [PRC-022-desinstalar-sistema.md](PRC-022-desinstalar-sistema.md) | Técnico | RF027,RF029,RF030 |

## Convenções BPMN

* Um pool representa o Remote Wake Assistant; Tailscale, Android, Windows/PC e aplicativo remoto podem ser pools externos quando a troca de mensagem for relevante.
* Raias representam responsabilidade, não tela.
* Gateways exclusivos possuem perguntas e saídas nomeadas.
* Esperas usam eventos temporizadores; cancelamento usa evento de mensagem.
* Operações reversíveis usam compensação ou fluxo explícito de rollback.
* Nenhum processo do usuário comum contém tarefa de terminal.

## Cobertura

PRC-001–008 cobrem instalação/validação; PRC-009–015 cobrem operação e falhas; PRC-016–022 cobrem manutenção e ciclo de vida.
