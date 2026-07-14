# Requisitos não funcionais

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

## Catálogo mensurável

| ID | Nome | Requisito/métrica | Verificação | Prioridade |
| --- | --- | --- | --- | --- |
| RNF001 | Experiência sem terminal | 100% das tarefas de PER001 concluídas sem terminal, comandos, MAC, IP ou edição de arquivo. | Teste moderado de usabilidade com 5 participantes; zero ocorrência. | P0 |
| RNF002 | Tempo de estado inicial | Dashboard apresenta estado inicial ou “verificando” em ≤1 s e decisão em p95 ≤5 s em rede saudável. | Telemetria local em 100 execuções. | P0 |
| RNF003 | Responsividade | A UI mantém resposta a entrada em ≤100 ms e cancelamento efetivo em ≤2 s. | Teste automatizado de UI e carga de sondas. | P0 |
| RNF004 | Tempo de abertura | Após serviço remoto pronto, cliente inicia em p95 ≤3 s. | Medição E2E em ambiente homologado. | P0 |
| RNF005 | Confiabilidade | Em ambiente validado, a cadeia obtém ≥95% de sucesso em 30 execuções, desconsiderada indisponibilidade externa comprovada. | Teste de estabilidade em hardware real. | P0 |
| RNF006 | Timeouts limitados | Bridge: 5 s; recibo wake: 10 s; Windows: 240 s; serviço remoto: 120 s; nenhuma espera infinita. | Testes com dependências simuladas. | P0 |
| RNF007 | Segredos protegidos | Nenhuma chave privada/token em texto puro, SQLite, logs, exportação ou repositório; privada do launcher protegida por DPAPI. | Scanner de segredos, inspeção e testes de acesso por outro usuário. | P0 |
| RNF008 | Criptografia e identidade | Bridge usa SSH Ed25519 e host key pinning; ReadinessAgent usa HTTPS/mTLS com certificados ECDSA P-256 fixados por dispositivo; TLS anterior a 1.2 e identidade desconhecida são rejeitados. | Testes MITM, certificado ausente/desconhecido/expirado/revogado e downgrade TLS. | P0 |
| RNF009 | Menor privilégio | UI roda sem elevação; broker aceita somente operações tipadas da allowlist; UAC apenas por lote consentido. | Teste negativo de IPC e análise de token. | P0 |
| RNF010 | Proteção contra replay | Pedidos aceitos apenas em janela de ±60 s, nonce único retido por 10 min, cooldown 15 s e limite 3/5 min. | Testes de repetição, relógio e volume. | P0 |
| RNF011 | Privacidade | Coleta limitada a dados operacionais; exportação exige pré-visualização/consentimento; MAC mascarado por padrão. | Revisão do esquema e teste de exportação. | P0 |
| RNF012 | Observabilidade | 100% das operações críticas têm correlation ID, início, fim, componente, duração e código; logs rotacionam a 20 MB e expiram em 30 dias. | Teste de eventos e retenção. | P0 |
| RNF013 | Acessibilidade | Navegação por teclado, foco visível, nomes acessíveis, leitor de tela, contraste ≥4,5:1 e não depender apenas de cor. | Accessibility Insights e checklist WCAG 2.2 AA aplicável. | P1 |
| RNF014 | Compatibilidade Windows | Windows 11 x64 suportado; Windows 10 22H2 x64 somente com atualização de segurança aplicável; builds não homologadas recebem aviso. | Matriz de VMs e hardware. | P0 |
| RNF015 | Compatibilidade Android | Android 10+; Pixel, Samsung e Motorola na matriz; fabricante não testado recebe classificação não homologada. | Testes após boot, Doze e 24 h ocioso. | P0 |
| RNF016 | Integridade de configuração | SQLite com transações, migrações versionadas e backup antes de migração; arquivos com ACL e hash quando exportados. | Testes de corrupção e migração. | P0 |
| RNF017 | Recuperação | Rollback de ações gerenciadas é idempotente e recupera 100% dos valores capturados nos ambientes de teste. | Teste instalação→alteração→rollback. | P0 |
| RNF018 | Manutenibilidade | Cobertura de linhas ≥80% no domínio e ≥70% global; complexidade ciclomática por método ≤10 salvo justificativa. | CI com cobertura e análise estática. | P1 |
| RNF019 | Testabilidade | Todo adapter externo possui interface e fake; testes automatizados nunca alteram BIOS, energia, driver, VPN real ou disparam executável real por padrão. | Revisão de arquitetura e execução em sandbox. | P0 |
| RNF020 | Atualização segura | Canal estável aceita apenas pacote e manifesto assinados; hash SHA-256; health check e rollback após falha. | Teste de pacote adulterado e rollback. | P1 |
| RNF021 | Portabilidade arquitetural | Domínio e contratos não dependem de WPF, SSH, Tailscale ou RustDesk; adapters podem ser substituídos. | Teste de arquitetura de dependências. | P1 |
| RNF022 | Internacionalização futura | Textos de UI e erro em recursos pt-BR, sem literals na camada de apresentação; datas/tempos culturalmente corretos. | Análise estática e pseudo-localização. | P1 |

## Critérios gerais

* Métricas p95 usam no mínimo 30 amostras por ambiente.
* “Rede saudável” significa perda <1% e RTT da tailnet <150 ms.
* Exceção a uma meta exige registro de risco, evidência e aprovação; não pode ser silenciosa.
* Requisitos de segurança P0 bloqueiam o marco quando não atendidos.

## Rastreabilidade

RNF001–RNF022 relacionam-se a OBJ001–OBJ007, CMP001–CMP015 e ao plano de testes, especialmente CT031–CT052.
