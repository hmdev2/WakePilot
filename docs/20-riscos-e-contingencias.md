# Riscos e contingências

## Controle

Versão 1.1.0 — Estado: Aprovado com ajustes pontuais — Data: 14/07/2026.

## Escala

Probabilidade: Baixa, Média, Alta. Impacto: Baixo, Médio, Alto, Crítico. Risco Crítico aberto bloqueia release.

| ID | Descrição | Causa | Prob. | Impacto | Prevenção | Detecção | Contingência | Responsável | Estado |
| --- | --- | --- | --- | --- | --- | --- | --- | --- | --- |
| RSK001 | PC não acorda do estado escolhido | Firmware/driver/energia | Alta | Alto | Diagnóstico e teste por estado | Timeout sem rede/agent | Orientar BIOS/Fast Startup; usar S3/S4 validado; hardware alternativo | Técnico | Aberto |
| RSK002 | Fast Startup impede WoL esperado | Desligamento híbrido | Alta | Alto | Detectar e explicar; não alterar sem consentimento | Estado habilitado + falha | Testar hibernação/S3; alteração reversível autorizada | Técnico | Mitigado |
| RSK003 | Driver omite ou mente capacidade | Implementação do fabricante | Média | Alto | Separar evidência de validação | Dados contraditórios/teste falho | Classificar inconclusivo e testar hardware | Produto | Mitigado |
| RSK004 | Android encerra bridge | Doze/otimização OEM | Alta | Alto | Guia de bateria, wake lock justificado, boot/24h test | Health perdido | Orientação OEM; reinício; migrar app nativo | Produto | Aberto |
| RSK005 | Celular sem energia/Wi-Fi | Operação física | Média | Alto | Requisito de alimentação e status | Node offline | Usuário local religa/reconecta; futura redundância | Usuário | Aceito |
| RSK006 | Tailscale indisponível/desconectado | Cliente/conta/rede externa | Média | Alto | Health por camada e reconexão guiada | ERR008 | Reautenticar; usar acesso físico; reavaliar adapter | Usuário/Técnico | Aceito |
| RSK007 | Identidade SSH/mTLS alterada | Reinstalação, expiração inesperada ou MITM | Baixa | Crítico | Pinning, rotação controlada e novo pareamento presencial | ERR010/ERR021 | Bloquear; investigar; revogar e reparar | Técnico | Mitigado |
| RSK008 | Forced command permite escape | Erro de configuração/wrapper | Baixa | Crítico | restrict/no-pty/no-forwarding; parser fechado; testes | Teste de shell/argument fuzz | Bloquear release; remover chave; corrigir bootstrap | Segurança | Mitigado |
| RSK009 | Chave privada SSH ou mTLS exfiltrada | Malware/perfil Windows | Baixa | Crítico | DPAPI, Certificate Store, chaves não exportáveis, ACL, identidade individual e revogação | Acesso anômalo/rate limit | Revogar notebook/certificado, gerar nova identidade e revisar logs | Usuário/Técnico | Residual |
| RSK010 | Replay/abuso de wake | Pedido capturado ou comprometimento | Baixa | Alto | Nonce, tempo, cache, cooldown, rate limit | ERR011/ERR012 | Revogar chave; sincronizar relógio; bloquear origem | Segurança | Mitigado |
| RSK011 | Executável remoto substituído | Malware/atualização | Média | Alto | Caminho canônico, hash/assinatura, sem shell | Hash diverge/ERR016 | Revalidar atualização legítima ou bloquear | Produto | Mitigado |
| RSK012 | Log/exportação vaza segredo | Serialização indevida | Baixa | Crítico | DTO, redaction, scanner, testes | Scanner/secret test | Bloquear exportação; apagar artefato; rotacionar credencial | Segurança | Mitigado |
| RSK013 | Broker privilegiado abusado | IPC fraco/operador genérico | Baixa | Crítico | ACL, identidade, allowlist, tipagem, tamanho | Operação desconhecida/telemetria | Parar broker; bloquear release; auditoria | Segurança | Mitigado |
| RSK014 | Rollback incompleto | Mudança externa/snapshot corrompido | Média | Alto | Hash, diff, verificação e idempotência | Resultado parcial | Orientação manual e relatório por item | Técnico | Aberto |
| RSK015 | PC liga mas Windows não inicia | Falha de boot/update | Média | Alto | Estados separados e timeout | Rede/firmware sem agent | Acesso físico; recuperação Windows fora do produto | Usuário | Aceito |
| RSK016 | Serviço remoto demora/falha | Startup/app externa | Média | Médio | Probe própria e timeout 120 s | ERR015 | Repetir sonda; iniciar/reconfigurar localmente | Usuário/Técnico | Mitigado |
| RSK017 | Windows 10 sem suporte seguro | Fim de ciclo de vida | Alta | Alto | Exigir atualização de segurança/ESU/LTSC e aviso | Build sem suporte | Bloquear produção ou migrar Windows 11 | Produto/Usuário | Aberto |
| RSK018 | Instalador/update adulterado | Cadeia de distribuição | Baixa | Crítico | Assinatura, SHA-256, TLS, manifesto | Assinatura inválida | Rejeitar, revogar certificado, publicar incidente | Release | Mitigado |
| RSK019 | Dependência externa muda contrato | Tailscale/Termux/RustDesk | Média | Médio | Adapters, versão mínima/máxima e contract tests | Teste de compatibilidade | Desabilitar adapter; fixar versão; atualizar | Produto | Aceito |
| RSK020 | Escopo cresce além de equipe pequena | Múltiplas plataformas/apps | Alta | Alto | Gate de escopo e marcos verticais | Backlog sem conclusão | Adiar evolução; manter um perfil/app | Produto | Mitigado |
| RSK021 | Implementação do ReadinessAgent diverge do contrato seguro | Erro em mTLS, pinning, certificado, porta ou firewall | Baixa | Crítico | ADR-010, contract tests, certificados não exportáveis e firewall restrito | CT038, ERR010/ERR021 e teste de instalação | Bloquear M3, revogar certificados, remover regra e reinstalar/parear | Arquitetura/Segurança | Mitigado |
| RSK022 | Antivírus/firewall bloqueia componentes | Políticas locais | Média | Médio | Assinatura, portas mínimas, diagnóstico | ERR014/015; evento AV | Orientar allowlist específica; não desativar proteção | Técnico | Aceito |

## Premissas do briefing

PP-001–PP-010 foram convertidas em decisões de escopo/ADRs. Seus riscos correspondentes permanecem principalmente em RSK001, RSK004–006, RSK017, RSK019–RSK021.

## Revisão

Revisar em cada marco, mudança de fornecedor, build Windows/Android, incidente e release. RSK004, RSK009, RSK014, RSK017 e RSK021 exigem evidência explícita no gate correspondente.
