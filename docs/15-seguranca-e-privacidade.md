# Segurança e privacidade

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026. Metodologia: STRIDE + minimização de dados.

## Ativos

Chave privada SSH do notebook; certificados e chaves privadas mTLS; pins/fingerprints autorizados; perfil do PC/MAC; snapshots; instaladores; configuração do broker; logs e diagnósticos; disponibilidade do PC/bridge; confiança de pareamento.

## Atores e adversários

Usuário autorizado, técnico, suporte, malware no notebook, dispositivo perdido, agente na LAN, nó indevido na tailnet, MITM, pacote de atualização adulterado e processo local de baixa integridade.

## Limites de confiança

Os sete limites definidos em 08-arquitetura.md são normativos. VPN é apenas canal. O bridge não confia em target, timestamp ou argumentos recebidos antes de validar.

## STRIDE

| Ameaça | Cenário | Controle | Verificação |
| --- | --- | --- | --- |
| Spoofing | Falso bridge, agente ou launcher | SSH host pinning, Ed25519 individual, mTLS pinned e pareamento presencial | MITM, chave e certificado desconhecidos |
| Tampering | Configuração/pacote alterado | ACL, hash, assinatura, SQLite transacional | alteração de arquivo/pacote |
| Repudiation | Usuário nega wake/revogação | correlation ID, fingerprint curta, eventos UTC | trilha de auditoria |
| Information disclosure | Segredo em log/export | DPAPI, DTO, redaction, scanner | secret scanning |
| Denial of service | Wake repetido/sondas | rate limit, cooldown, backoff, circuit breaker | carga e replay |
| Elevation of privilege | IPC/broker abusado | allowlist, pipe ACL, identidade, UAC pontual | fuzzing IPC e token |

## Chaves e pareamento

* Ed25519 exclusiva por notebook; privada gerada no dispositivo e nunca transferida.
* Privada protegida por DPAPI CurrentUser; banco guarda SecretReference.
* Host key coletada e comparada no pareamento presencial; mudança bloqueia.
* Código de pareamento aleatório ≥128 bits, uso único, TTL 10 min, armazenado em hash.
* authorized_keys usa forced command e restrições no nível suportado: sem PTY, agent/X11/TCP forwarding e user rc.
* Wrapper aceita somente action health/wake, versão 1, campos fechados, ≤4 KiB e target allowlisted.
* Revogação remove chave pública, apaga privada local e mantém somente evidência de auditoria.
* Rotação cria nova chave, valida, troca autorização e somente então revoga anterior.

## Replay e frequência

Janela ±60 s; nonce criptograficamente aleatório; hash retido 10 min; request ID único; cooldown 15 s; máximo 3 wakes em 5 min por chave+alvo. Relógio divergente não relaxa regra.

## Identidade do ReadinessAgent

ADR-010 é normativo. O agente usa exclusivamente HTTPS/1.1 com mTLS em Kestrel e não possui listener HTTP. `ClientCertificateMode.RequireCertificate` é obrigatório. Certificados X.509 ECDSA P-256 autoassinados são aceitos somente por fingerprint SHA-256 previamente pareado, finalidade correta, período válido e estado não revogado.

A chave do servidor é não exportável em `LocalMachine\My` e acessível somente pelo service SID; a chave cliente é não exportável no perfil do usuário. A regra de firewall limita programa, TCP, porta persistida, endereço local Tailscale e endereço remoto do notebook. Falha de pin, certificado, Tailscale, bind ou firewall encerra a sonda sem fallback inseguro.

O endpoint é somente leitura, consulta apenas service IDs allowlisted, limita payload a 16 KiB e 60 sondas/minuto, não lista processos e não inicia serviço. Certificados duram 365 dias, giram 30 dias antes e coexistem por no máximo 7 dias; perda/expiração exige pareamento presencial.

## Privilégios

UI padrão; agente LocalService/conta virtual; broker efêmero elevado; instalador administrativo. Nenhum PowerShell livre. Operações: enums e DTOs versionados; tamanho máximo; caminho canônico; ArgumentList, UseShellExecute=false.

## Segredos e dados

Proibidos em repositório, .env versionado, SQLite, logs e exportação: privadas, auth keys Tailscale, senha RustDesk/AnyDesk, tokens e código de pareamento vigente. MAC completo é dado técnico protegido e mascarado; IP Tailscale pode ser exportado apenas em modo técnico consentido.

## Logs

Campos allowlisted; redaction antes do sink; valores de chaves nunca são formatados; correlation ID não contém dado pessoal; retenção 30 dias/20 MB; exportação por cópia sanitizada, nunca arquivo bruto.

## Atualizações e dependências

SBOM por release; dependências fixadas por lockfile; análise de vulnerabilidade; pacote/manifesto assinados; chave de assinatura fora do repositório e pipeline; TLS; rollback. Vulnerabilidade crítica bloqueia release.

## Hardening Android

Sem root; instalação de mesma origem de assinatura; HOME/configuração com permissões mínimas; sshd apenas na interface necessária da tailnet quando viável; senha desabilitada; uma chave forced-command; wrapper sem eval/shell dinâmico; allowlist local imutável pelo pedido.

## Hardening Windows

Named pipe com ACL explícita; serviço sem perfil interativo; Kestrel somente no IPv4 Tailscale; certificado não exportável; regra de firewall exata; diretórios Program Files/ProgramData com ACL; DLL search path seguro; caminhos absolutos; assinatura em produção; proteção contra symlink/reparse point em arquivos sensíveis.

## Resposta a incidente

1. Preservar correlation IDs e desconectar perfil comprometido.
2. Revogar chave/dispositivo e bloquear host alterado.
3. Rotacionar material afetado.
4. Verificar logs sanitizados, integridade e versão.
5. Corrigir, testar e distribuir pacote assinado.
6. Informar impacto e instruções; apagar diagnóstico compartilhado quando aplicável.

## Privacidade

Base local e finalidade operacional; sem telemetria externa no MVP. Compartilhamento com suporte é manual, pré-visualizado e consentido. Usuário pode apagar histórico e logs, exceto retenção mínima explicitamente escolhida durante investigação local.

## Riscos residuais

Malware sob a conta do usuário pode operar a chave enquanto desbloqueada; Tailscale e apps remotos possuem seus próprios modelos; Termux depende de política OEM; hardware pode acordar inesperadamente. RSK004, RSK009 e RSK019 permanecem residuais.

## Gate de segurança

Falha em host pinning, mTLS/pin bilateral, forced command, proteção de privada, firewall restrito, broker allowlist, sanitização ou assinatura estável interrompe o marco; não existe waiver implícito.
