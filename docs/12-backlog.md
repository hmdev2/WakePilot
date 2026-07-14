# Backlog

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

## Épicos

Fundação; Domínio; Diagnóstico Windows; Configuração Wake-on-LAN; Android; VPN; Autenticação; Pareamento; Launcher; Monitoramento; Acesso remoto; Segurança; Observabilidade/Suporte; Instaladores; Atualização; Testes; Documentação.

## Histórias priorizadas

| ID | Épico | História | Critério de aceite | Rastreabilidade |
| --- | --- | --- | --- | --- |
| US001 | Fundação | Como mantenedor, quero solução modular e CI para evoluir com segurança. | Build limpo; testes unitários; análise estática; arquitetura validada. | OBJ007;RNF018;CMP004;CT048 |
| US002 | Domínio | Como desenvolvedor, quero máquina de estados tipada para distinguir cada fase. | Transições inválidas rejeitadas; cancelamento e timeout determinísticos. | OBJ004;RF019;CMP003;CT019 |
| US003 | Diagnóstico Windows | Como técnico, quero inventariar o Windows para conhecer o ambiente. | RF001 aceito. | OBJ002;RF001;PRC-001;CT001 |
| US004 | Diagnóstico Windows | Como técnico, quero identificar o Ethernet e MAC automaticamente. | RF002 aceito. | OBJ002;RF002;PRC-001;CT002 |
| US005 | Diagnóstico Windows | Como técnico, quero evidências de WoL/energia sem falso positivo. | RF003 e RF004 aceitos. | OBJ003;RF003,RF004;CT003,CT004 |
| US006 | Wake-on-LAN | Como técnico, quero orientação BIOS específica ou genérica. | RF005 aceito. | OBJ003;RF005;PRC-003;CT005 |
| US007 | Wake-on-LAN | Como técnico, quero consentir cada mudança. | RF006 aceito. | OBJ006;RF006;PRC-002;CT006 |
| US008 | Wake-on-LAN | Como técnico, quero aplicar configuração por operações seguras. | RF007 aceito. | OBJ002;RF007;CMP005;CT007 |
| US009 | Wake-on-LAN | Como técnico, quero snapshot antes de alterar. | RF008 aceito. | OBJ006;RF008;CMP009;CT008 |
| US010 | VPN | Como técnico, quero instalar/autenticar Tailscale sem guardar token. | RF009 aceito. | OBJ005;RF009;PRC-004;CT009 |
| US011 | Android | Como técnico, quero preparar bridge e validar reinício. | RF010 aceito. | OBJ004;RF010;PRC-005;CT010 |
| US012 | Autenticação | Como técnico, quero chave exclusiva protegida. | RF011 aceito. | OBJ005;RF011;PRC-006;CT011 |
| US013 | Pareamento | Como técnico, quero confirmar fingerprints presencialmente. | RF012 aceito. | OBJ005;RF012;PRC-007;CT012 |
| US014 | Cadastro | Como técnico, quero cadastrar o PC sem digitar MAC. | RF013 aceito. | OBJ001;RF013;PRC-017;CT013 |
| US015 | Acesso remoto | Como avançado, quero configurar RustDesk com abertura segura. | RF014 aceito. | OBJ001;RF014;PRC-016;CT014 |
| US016 | Launcher | Como usuário, quero saber se o PC está pronto. | RF015 aceito. | OBJ004;RF015;CT015 |
| US017 | Launcher | Como usuário, quero distinguir VPN e bridge offline. | RF016 aceito. | OBJ004;RF016;PRC-013;CT016 |
| US018 | Launcher | Como usuário, quero ligar o PC por um botão com proteção antiabuso. | RF017 e RF018 aceitos. | OBJ001,OBJ005;RF017,RF018;CT017,CT018 |
| US019 | Monitoramento | Como usuário, quero acompanhar a inicialização e cancelar. | RF019 aceito. | OBJ001;RF019;PRC-011;CT019 |
| US020 | Monitoramento | Como usuário, quero aguardar o serviço remoto real. | RF020 aceito. | OBJ004;RF020;CT020 |
| US021 | Acesso remoto | Como usuário, quero que RustDesk abra quando estiver pronto. | RF021 aceito. | OBJ001;RF021;PRC-012;CT021 |
| US022 | Suporte | Como usuário, quero erros compreensíveis e acionáveis. | RF022 aceito. | OBJ004;RF022;TEL018;CT022 |
| US023 | UX | Como usuário, quero notificação local do resultado. | RF023 aceito. | OBJ001;RF023;CT023 |
| US024 | Validação | Como técnico, quero teste real que classifique compatibilidade. | RF024 aceito. | OBJ003;RF024;PRC-008;CT024 |
| US025 | Observabilidade | Como suporte, quero linha do tempo sanitizada. | RF025 aceito. | OBJ004,OBJ005;RF025;CT025 |
| US026 | Suporte | Como usuário, quero exportar diagnóstico seguro. | RF026 aceito. | OBJ004;RF026;PRC-020;CT026 |
| US027 | Autenticação | Como responsável, quero revogar dispositivo perdido. | RF027 aceito. | OBJ005;RF027;PRC-018;CT027 |
| US028 | Atualização | Como responsável, quero atualizar somente pacote confiável. | RF028 aceito. | OBJ005;RF028;PRC-019;CT028 |
| US029 | Recuperação | Como técnico, quero restaurar configurações. | RF029 aceito. | OBJ006;RF029;PRC-021;CT029 |
| US030 | Desinstalação | Como responsável, quero remover produto e acessos. | RF030 aceito. | OBJ005,OBJ006;RF030;PRC-022;CT030 |

## Tarefas técnicas transversais

* Criar solution/projects conforme CMP001–CMP015.
* Implementar Result/Error, clock, ID/nonce e cancellation abstraídos.
* Criar fakes para Windows, VPN, SSH, WoL, processo, cofre e tempo.
* Configurar SQLite/migrações, redaction, logs e secret scanning.
* Criar bootstrap Android idempotente e contract tests.
* Empacotar MSI, SBOM e pipeline de assinatura sem inserir segredo.
* Manter documentação/ADRs e matriz de rastreabilidade em cada PR.

## Definition of Ready

Requisito, regra, UX, dependências, ameaça e teste conhecidos; nenhuma decisão crítica pendente para a história.

## Definition of Done

Código revisado; testes do nível aplicável; logs/erros; segurança; acessibilidade; documentação; rastreabilidade; nenhum segredo; demonstração do critério. Nenhum commit é requisito automático do agente Codex.
