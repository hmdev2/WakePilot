# Plano de testes

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

## Estratégia

Pirâmide: domínio unitário; adapters por contrato; integração em sandbox; sistema em VM; E2E em hardware; segurança adversarial; usabilidade moderada. Operação destrutiva é fake por padrão.

## Casos rastreáveis

| ID | Requisito | Tipo/tema | Resultado esperado | Automação |
| --- | --- | --- | --- | --- |
| CT001 | RF001 | Unitário/integração/sistema conforme adapter | Inventário corresponde ao SO e falha parcial é explícita | Automatizado; hardware quando indicado |
| CT002 | RF002 | Unitário/integração/sistema conforme adapter | Ethernet físico/MAC são detectados; virtual não selecionado | Automatizado; hardware quando indicado |
| CT003 | RF003 | Unitário/integração/sistema conforme adapter | Evidência não produz validado sem teste | Automatizado; hardware quando indicado |
| CT004 | RF004 | Unitário/integração/sistema conforme adapter | S3/S4/S5/Fast Startup são distinguidos | Automatizado; hardware quando indicado |
| CT005 | RF005 | Unitário/integração/sistema conforme adapter | Guia BIOS não alega automação | Automatizado; hardware quando indicado |
| CT006 | RF006 | Unitário/integração/sistema conforme adapter | Nenhuma mutação sem consentimento | Automatizado; hardware quando indicado |
| CT007 | RF007 | Unitário/integração/sistema conforme adapter | Broker rejeita operação fora da allowlist | Automatizado; hardware quando indicado |
| CT008 | RF008 | Unitário/integração/sistema conforme adapter | Snapshot íntegro antecede mutação | Automatizado; hardware quando indicado |
| CT009 | RF009 | Unitário/integração/sistema conforme adapter | Tailscale valida sem token persistido | Automatizado; hardware quando indicado |
| CT010 | RF010 | Unitário/integração/sistema conforme adapter | Bridge volta saudável após reboot | Automatizado; hardware quando indicado |
| CT011 | RF011 | Unitário/integração/sistema conforme adapter | Privada fica no DPAPI e pública restrita | Automatizado; hardware quando indicado |
| CT012 | RF012 | Unitário/integração/sistema conforme adapter | Código expira e host divergente bloqueia | Automatizado; hardware quando indicado |
| CT013 | RF013 | Unitário/integração/sistema conforme adapter | Perfil comum não exige MAC | Automatizado; hardware quando indicado |
| CT014 | RF014 | Unitário/integração/sistema conforme adapter | Processo usa path/args seguros | Automatizado; hardware quando indicado |
| CT015 | RF015 | Unitário/integração/sistema conforme adapter | Estado pronto exige agent/serviço | Automatizado; hardware quando indicado |
| CT016 | RF016 | Unitário/integração/sistema conforme adapter | VPN/bridge/host têm códigos distintos | Automatizado; hardware quando indicado |
| CT017 | RF017 | Unitário/integração/sistema conforme adapter | Pedido autorizado envia burst limitado | Automatizado; hardware quando indicado |
| CT018 | RF018 | Unitário/integração/sistema conforme adapter | Replay/rate limit rejeitam duplicata | Automatizado; hardware quando indicado |
| CT019 | RF019 | Unitário/integração/sistema conforme adapter | Progresso, timeout e cancelamento funcionam | Automatizado; hardware quando indicado |
| CT020 | RF020 | Unitário/integração/sistema conforme adapter | Serviço é sondado após Windows | Automatizado; hardware quando indicado |
| CT021 | RF021 | Unitário/integração/sistema conforme adapter | Cliente abre sem shell em ≤3 s | Automatizado; hardware quando indicado |
| CT022 | RF022 | Unitário/integração/sistema conforme adapter | Erro possui ação/correlação e sem stack | Automatizado; hardware quando indicado |
| CT023 | RF023 | Unitário/integração/sistema conforme adapter | Notificação não vaza dado e tem fallback | Automatizado; hardware quando indicado |
| CT024 | RF024 | Unitário/integração/sistema conforme adapter | Somente E2E integral marca validado | Automatizado; hardware quando indicado |
| CT025 | RF025 | Unitário/integração/sistema conforme adapter | Logs correlacionados, sanitizados e rotacionados | Automatizado; hardware quando indicado |
| CT026 | RF026 | Unitário/integração/sistema conforme adapter | ZIP possui manifesto e zero segredo | Automatizado; hardware quando indicado |
| CT027 | RF027 | Unitário/integração/sistema conforme adapter | Chave revogada rejeita novo pedido | Automatizado; hardware quando indicado |
| CT028 | RF028 | Unitário/integração/sistema conforme adapter | Pacote inválido rejeita e falha reverte | Automatizado; hardware quando indicado |
| CT029 | RF029 | Unitário/integração/sistema conforme adapter | Rollback idempotente verifica itens | Automatizado; hardware quando indicado |
| CT030 | RF030 | Unitário/integração/sistema conforme adapter | Desinstalação remove acessos e relata pendência | Automatizado; hardware quando indicado |
| CT031 | RNF001 | Sem terminal | Métrica de RNF001 atendida no ambiente definido | Automatizado ou medição homologada |
| CT032 | RNF002 | Estado inicial p95 | Métrica de RNF002 atendida no ambiente definido | Automatizado ou medição homologada |
| CT033 | RNF003 | Responsividade | Métrica de RNF003 atendida no ambiente definido | Automatizado ou medição homologada |
| CT034 | RNF004 | Abertura p95 | Métrica de RNF004 atendida no ambiente definido | Automatizado ou medição homologada |
| CT035 | RNF005 | 30 execuções | Métrica de RNF005 atendida no ambiente definido | Automatizado ou medição homologada |
| CT036 | RNF006 | Timeouts | Métrica de RNF006 atendida no ambiente definido | Automatizado ou medição homologada |
| CT037 | RNF007 | Proteção de segredos | Métrica de RNF007 atendida no ambiente definido | Automatizado ou medição homologada |
| CT038 | RNF008 | Criptografia/identidade | SSH pinned e ReadinessAgent mTLS rejeitam identidade ausente, desconhecida, expirada e revogada; TLS <1.2 é rejeitado | Automatizado + integração em VMs |
| CT039 | RNF009 | Menor privilégio | Métrica de RNF009 atendida no ambiente definido | Automatizado ou medição homologada |
| CT040 | RNF010 | Replay/rate | Métrica de RNF010 atendida no ambiente definido | Automatizado ou medição homologada |
| CT041 | RNF011 | Privacidade | Métrica de RNF011 atendida no ambiente definido | Automatizado ou medição homologada |
| CT042 | RNF012 | Logs/retention | Métrica de RNF012 atendida no ambiente definido | Automatizado ou medição homologada |
| CT043 | RNF013 | Acessibilidade | Métrica de RNF013 atendida no ambiente definido | Automatizado ou medição homologada |
| CT044 | RNF014 | Windows matrix | Métrica de RNF014 atendida no ambiente definido | Automatizado ou medição homologada |
| CT045 | RNF015 | Android matrix | Métrica de RNF015 atendida no ambiente definido | Automatizado ou medição homologada |
| CT046 | RNF016 | Integridade SQLite | Métrica de RNF016 atendida no ambiente definido | Automatizado ou medição homologada |
| CT047 | RNF017 | Rollback | Métrica de RNF017 atendida no ambiente definido | Automatizado ou medição homologada |
| CT048 | RNF018 | Cobertura/complexidade | Métrica de RNF018 atendida no ambiente definido | Automatizado ou medição homologada |
| CT049 | RNF019 | Fakes/sem destruição | Métrica de RNF019 atendida no ambiente definido | Automatizado ou medição homologada |
| CT050 | RNF020 | Update assinado | Métrica de RNF020 atendida no ambiente definido | Automatizado ou medição homologada |
| CT051 | RNF021 | Arquitetura de ports | Métrica de RNF021 atendida no ambiente definido | Automatizado ou medição homologada |
| CT052 | RNF022 | Recursos pt-BR | Métrica de RNF022 atendida no ambiente definido | Automatizado ou medição homologada |

## Cenários E2E obrigatórios

PC já ligado; bridge offline; VPN offline; host key alterada; certificado mTLS inválido/expirado/revogado; versão de protocolo incompatível; porta ocupada; Tailscale atrasado; pacote rejeitado; PC não acorda; PC acorda sem Windows; serviço remoto atrasado/ausente; cliente ausente; cancelamento em cada fase; S3/S4/S5 conforme hardware; Fast Startup; Android após reboot/Doze/24 h; firewall/antivírus; update/rollback/uninstall.

## Matriz

| Eixo | Valores mínimos |
| --- | --- |
| Windows | 11 atual; 10 22H2 coberto; VM limpa e PC físico |
| NIC | Intel, Realtek e USB Ethernet quando suportado |
| Energia | S3, S4 hibernação, Fast Startup, S5 observado |
| Firmware | desktop montado, OEM desktop, notebook Ethernet |
| Android | Pixel, Samsung, Motorola; Android 10 e versão atual |
| Rede | LAN saudável, perda, alta latência, troca Wi-Fi, internet ausente |
| Aplicativo | RustDesk instalado/ausente/atrasado/alterado |
| Segurança | MITM SSH/mTLS, replay, flood, downgrade TLS, certificado revogado, args maliciosos, chave revogada, pacote adulterado |

## Segurança

Fuzz do JSON/IPC; path traversal; symlink/reparse; forced-command escape; PTY/forwarding; host/certificado desconhecido; mTLS obrigatório; pin bilateral; EKU/validade; rotação com sobreposição; porta/firewall restritos; protocolo v1, nonce, clock e rate limit; segredo em logs/dumps/export; permissões entre usuários; update adulterado; dependências/SBOM.

## Instalação e recuperação

Instalação limpa, upgrade N-1, reparo, interrupção, migração, disco cheio, banco corrompido, rollback, desinstalação com bridge online/offline e reinstalação com novas chaves.

## Usabilidade/acessibilidade

Cinco usuários comuns executam ligar/conectar sem ajuda técnica; teclado completo; leitor de tela; 200% zoom; contraste; mensagens por camada. Zero solicitação de MAC/terminal.

## Dados e evidência

Cada execução registra ambiente, versão, hardware, estado, correlation ID, duração e resultado. Evidências não contêm segredo. Flaky test é corrigido ou isolado com risco; nunca aceito silenciosamente.

## Gate

P0: 100%; P1: ≥95% com nenhum erro de segurança; zero vulnerabilidade crítica/alta; 30 E2E com ≥95%; rollback/uninstall 100% nos ambientes suportados.
