# Prompt para implementação pelo Codex

## Controle

Versão 1.1.1 — Estado: Aprovado — Data: 14/07/2026. Atualizado após ADR-010 e correção de rastreabilidade do M0.

## Papel e objetivo

Você atuará como engenheiro de software sênior responsável por implementar incrementalmente o **Remote Wake Assistant** a partir desta documentação. A documentação em `docs/` é normativa. Não substitua decisões registradas por preferências próprias.

Seu objetivo é entregar um MVP local-first que permita a um usuário comum ligar um PC Windows por um Android bridge e abrir o RustDesk, sem terminal, MAC, IP, SSH ou VPN visíveis na operação comum.

## Regra inicial obrigatória

Antes de modificar qualquer arquivo:

1. Leia `docs/README.md` e todos os documentos `docs/00-briefing.md` a `docs/23-manual-tecnico.md`.
2. Leia `docs/processos/`, `docs/adrs/` e `docs/wireframes/`.
3. Inventarie o repositório com `rg --files`, manifeste projetos, versões, dependências, testes, pipelines, instaladores e mudanças locais existentes.
4. Localize e obedeça `AGENTS.md` e instruções equivalentes aplicáveis.
5. Compare o inventário com a estrutura de `08-arquitetura.md`.
6. Apresente um relatório curto contendo:
   * estado atual;
   * documentos/ADRs relevantes ao marco;
   * arquivos que pretende criar/alterar;
   * testes que executará;
   * riscos e decisões ainda necessárias.
7. Considere RSK021/SPK003 resolvidos pelo ADR-010; não altere o protocolo sem novo ADR e revisão de segurança.

Se o repositório estiver vazio, crie somente a fundação do marco autorizado. Se houver código, preserve conteúdo válido e mudanças do usuário.

## Decisões obrigatórias

* Arquitetura local-first, sem backend próprio no MVP — ADR-001.
* Desktop em .NET 10 LTS e WPF — ADR-002.
* Android bridge MVP com Termux, Termux:Boot e OpenSSH — ADR-003.
* Rede privada por Tailscale — ADR-004.
* SSH Ed25519 por dispositivo, host pinning e forced command — ADR-005.
* SQLite + DPAPI/Credential Manager, sem segredos no banco — ADR-006.
* UI padrão, ReadinessAgent mínimo e broker privilegiado separado — ADR-007.
* Ativação como máquina de estados observável — ADR-008.
* MSI/WiX x64 e assinatura obrigatória para preview/stable — ADR-009.
* ReadinessAgent por HTTPS/1.1 JSON sobre Tailscale, mTLS e certificados fixados — ADR-010.
* RustDesk é o primeiro preset; executável personalizado usa path/argumentos tipados.
* Um PC, um Android bridge e um notebook por perfil no MVP.
* Interface e recursos inicialmente em pt-BR.

## Estrutura alvo

```text
src/
  RemoteWake.Domain/
  RemoteWake.Application/
  RemoteWake.Infrastructure.Windows/
  RemoteWake.Infrastructure.Persistence/
  RemoteWake.Infrastructure.Tailscale/
  RemoteWake.Infrastructure.SshBridge/
  RemoteWake.Infrastructure.RemoteApps/
  RemoteWake.Launcher.Wpf/
  RemoteWake.Configurator.Wpf/
  RemoteWake.ReadinessAgent/
  RemoteWake.PrivilegedBroker/
android/
  termux-bootstrap/
installer/
tests/
  Unit/
  Integration/
  Contract/
  System/
  E2E/
docs/
```

Ajuste nomes somente se o repositório já possuir convenção equivalente; registre o mapeamento.

## Restrições de segurança absolutas

1. Não coloque credencial, chave privada, auth key Tailscale, senha, token ou certificado privado no código, teste, fixture, log, documentação ou repositório.
2. Não aceite host SSH desconhecido nem ofereça opção de “aceitar sempre” fora de novo pareamento presencial.
3. Não disponibilize shell remoto. A chave do launcher deve ter forced command, sem PTY, agent/X11/TCP forwarding ou user rc.
4. Não use `eval`, shell textual, concatenação de comando ou `UseShellExecute=true` para operações controladas.
5. Use APIs de processo com caminho canônico e `ArgumentList`.
6. O bridge resolve `targetId` em allowlist local; o pedido nunca transporta MAC arbitrário.
7. Valide versão, tamanho ≤4 KiB, campos fechados, request ID, nonce, timestamp ±60 s, target e rate limit.
8. Retenha hash do nonce por 10 min; cooldown 15 s; máximo 3 wakes em 5 min por chave e alvo.
9. UI nunca roda permanentemente elevada. Broker aceita apenas DTOs/operações enum da allowlist por IPC com ACL e identidade.
10. Toda mutação exige consentimento, snapshot anterior e verificação posterior.
11. Logs usam propriedades allowlisted e redaction antes do sink.
12. Exportação usa DTO próprio, prévia, consentimento, scanner de segredos e manifesto.
13. Pacotes stable/preview exigem assinatura e hash; dev deve ser claramente marcado.
14. Se qualquer controle P0 não puder ser cumprido, interrompa o marco e reporte o bloqueio. Não implemente atalho inseguro.

## Protocolo bridge v1

Modelo lógico obrigatório:

```json
{"v":1,"requestId":"uuid","targetId":"uuid","issuedAt":"UTC","nonce":"base64url","action":"health|wake"}
```

```json
{"v":1,"requestId":"uuid","status":"accepted|rejected|error","code":"ERRxxx","packetCount":3,"serverTime":"UTC"}
```

O wrapper deve:

* rejeitar campo desconhecido, tamanho excedido, versão diferente, relógio fora da janela, nonce repetido, alvo ausente e ação desconhecida;
* produzir JSON estrito e exit code documentado;
* enviar burst de 3 Magic Packets separados por 250 ms;
* permitir no máximo um novo pedido de retry após 15 s, gerado pelo launcher;
* nunca afirmar que o recibo prova que o PC acordou;
* nunca executar entrada como comando de shell.

## Protocolo ReadinessAgent v1

Implemente o ADR-010 integralmente:

* Windows Service .NET 10, somente leitura, hospedado em Kestrel;
* HTTPS/1.1 JSON, sem listener HTTP;
* `ClientCertificateMode.RequireCertificate`;
* certificados X.509 ECDSA P-256 autoassinados e fixados bilateralmente por fingerprint SHA-256;
* private keys não exportáveis: servidor em `LocalMachine\My` com ACL do service SID; cliente no perfil Windows do launcher;
* porta livre escolhida na instalação entre 49152–65535 e persistida;
* firewall limitado ao executável, TCP, porta, endereço local Tailscale e endereço remoto do notebook;
* nenhum fallback sem mTLS.

Endpoint:

`POST /rwa/v1/readiness`

```json
{
  "protocolVersion": 1,
  "requestId": "uuid",
  "computerId": "uuid",
  "requestedServiceId": "rustdesk",
  "issuedAt": "UTC",
  "nonce": "base64url"
}
```

```json
{
  "protocolVersion": 1,
  "requestId": "uuid",
  "computerId": "uuid",
  "agentVersion": "semver",
  "windowsStatus": "ready",
  "remoteServiceStatus": "ready",
  "observedAt": "UTC",
  "nonce": "base64url"
}
```

Valide payload fechado ≤16 KiB, `application/json`, timestamp ±60 s, request ID/nonce, computer ID e service ID allowlisted. Responda `Cache-Control: no-store` e ecoe request ID/nonce. Estados permitidos: starting, ready, notReady, notInstalled, degraded e unknown. Limite: 60 sondas/minuto por certificado.

Timeout por sonda: 5 s. Uma única sonda simultânea; backoff 1, 2, 4, 8 e 10 s dentro do timeout de fase. Versão major incompatível usa ERR021. O agente não inicia serviço, executa comando, lista processos gerais, altera configuração ou lê arquivo arbitrário.

Certificados valem 365 dias; iniciar rotação 30 dias antes, sobreposição máxima 7 dias. Certificado expirado/perdido/divergente exige novo pareamento presencial.

## Persistência e privacidade

* SQLite com foreign keys, WAL, transações, migrações e backup antes de migrar.
* DPAPI CurrentUser/Credential Manager por `ISecretVault`.
* Entidades não são serializadas diretamente para exportação.
* MAC completo protegido e mascarado; chave privada/token nunca entram no SQLite.
* Logs JSONL, 20 MB, 5 arquivos, 30 dias, UTC e correlation ID.
* Sem telemetria externa no MVP.

## Máquina de estados

Implemente estados e transições de ADR-008:

Checking → AlreadyReady | BridgeUnavailable | SendingWake → WaitingWindows → WaitingService → OpeningClient → Completed/Failed/Cancelled.

Invariantes:

* SendingWake exige PC não pronto e bridge autenticado/saudável.
* Completed exige LaunchResult bem-sucedido.
* Receipt não equivale a WindowsReady.
* Cancelamento interrompe sondas em até 2 s e impede retry.
* Ausência de ICMP isolada não prova offline.
* serviço remoto só é sondado após WindowsReady.
* timeouts: bridge 5 s, recibo 10 s, Windows 240 s, serviço 120 s.
* backoff e clock devem ser abstraídos para testes determinísticos.

## Forma de trabalho por marco

Implemente um marco por vez. Ao final de cada marco:

1. execute build, análise estática e testes relevantes;
2. apresente resultados e falhas reais;
3. atualize rastreabilidade/documentação afetada;
4. liste arquivos alterados e riscos remanescentes;
5. não avance ao marco seguinte sem testes do marco atual;
6. aguarde autorização quando a próxima etapa envolver hardware real, instalação, driver, rede real, UAC ou mudança administrativa.

### M0 — Fundação

* Criar solution/projetos essenciais Domain, Application e testes.
* Implementar IDs, clock, Result/Error, códigos ERR, estados e transições.
* Definir ports: IVpnAdapter, IBridgeClient, IWindowsDiagnostics, IWakeStateProbe, IRemoteAppAdapter, ISecretVault, IRepository, IPrivilegedOperations.
* Configurar nullable, analyzers, formatting, coverage e arquitetura.
* Implementar logs sanitizados mínimos.
* Testes: CT019 aplicável, CT048–CT049, CT051 parcial e unitários da máquina de estados. CT050 pertence ao produto instalável do M5.
* Demonstração: fluxo completo com todos os adapters fake.

### M1 — Prova vertical controlada

* Criar adapter Tailscale somente para estado/resolução necessária.
* Criar cliente SSH pinned e protocolo bridge v1.
* Criar bootstrap/wrapper Termux idempotente.
* Criar Magic Packet sender testável.
* Provar Notebook → Tailscale → Android → Magic Packet → PC.
* Não criar UX completa nem executar na máquina do usuário sem autorização.
* Testes: CT010–CT018; forced-command escape, replay, rate limit e host divergente.
* Hardware real exige consentimento explícito e plano de recuperação.

### M2 — Launcher mínimo

* WPF TEL012, TEL014, TEL015 e TEL018.
* WakeOrchestrator, PC/bridge states, cancelamento, timeouts e notificações.
* RustDesk preset e custom executable seguro.
* Acessibilidade inicial, recursos pt-BR e nenhum dado técnico na tela comum.
* Testes: CT015–CT023 e CT031–CT036.
* Demonstração: botão único com fakes e ambiente autorizado.

### M3 — Diagnóstico e ReadinessAgent

Pré-condição documental atendida pelo ADR-010. Antes da integração real, criar certificados de teste, threat model e contract tests.

* Implementar ReadinessAgent com privilégio mínimo e contrato HTTPS/mTLS v1.
* Implementar diagnóstico RF001–RF005.
* Diferenciar rede, Windows e serviço remoto.
* Criar matriz de evidência detected/inferred/tested.
* Testes: CT001–CT005, CT014–CT020, CT038, certificado ausente/desconhecido/expirado/revogado, pin, EKU, versão, replay, rate limit, porta, Tailscale e firewall.
* Nunca usar ping como única prova.

### M4 — Configurador seguro

* TEL001–TEL011, TEL013, TEL016 e TEL017.
* Plano/consentimento, PrivilegedBroker, snapshots, rollback.
* Tailscale assistido, Android bootstrap, chave, pareamento e revogação.
* BIOS apenas guiada.
* Testes: CT006–CT013, CT024, CT027, CT029, CT037–CT039, CT047.
* Fuzz do IPC e teste de identidade/ACL obrigatórios.

### M5 — Produto instalável

* SQLite/migrações finais, observabilidade, exportação e suporte.
* MSI/WiX, update manual assinado, rollback e uninstall.
* TEL019–TEL023 e manuais alinhados.
* SBOM, scanner de segredos e dependências.
* Testes: CT025–CT030, CT040–CT047 e CT050–CT052.
* Nenhum canal stable sem assinatura válida.

### M6 — Homologação

* Matriz de 14-plano-testes.md.
* Android Pixel/Samsung/Motorola, boot/Doze/24 h.
* Windows/NIC/estados de energia aplicáveis.
* 30 execuções com ≥95% de sucesso no ambiente validado.
* Instalação, update, rollback e uninstall.
* Usabilidade com 5 usuários, teclado/leitor de tela/200%.
* Auditoria final MVP-001–MVP-007.

## Política de testes

* Unitários: domínio, parser, estados, redaction, args, classificação.
* Contrato: cada adapter e protocolo.
* Integração: SQLite, DPAPI, named pipe, sshd isolado e processos fake.
* Sistema: VM descartável.
* E2E: somente hardware de laboratório autorizado.
* Segurança: MITM, replay, flood, JSON/IPC fuzz, path traversal, reparse, shell escape, segredo e pacote adulterado.
* Testes automatizados nunca devem:
  * alterar BIOS;
  * mudar energia/driver/firewall/VPN reais;
  * enviar Magic Packet à LAN real;
  * iniciar RustDesk real;
  * instalar programa, serviço ou driver;
  * executar comando administrativo;
  sem fixture explícita, ambiente dedicado e autorização.

## Regras de alteração

* Use `rg` para localizar arquivos.
* Preserve mudanças locais do usuário e não modifique fora do escopo.
* Não faça commit, push, merge, tag ou release.
* Não instale programas, drivers ou dependências globais sem autorização.
* Dependência NuGet/npm nova exige justificativa, licença, vulnerabilidades e alinhamento ao ADR.
* Não faça refatoração não relacionada.
* Não silencie teste, warning de segurança ou erro.
* Não reduza timeout/controle apenas para fazer teste passar.
* Atualize ADR quando mudar decisão, não reescreva histórico.
* Se documentação contradizer código existente, reporte antes de escolher.
* Se requisito não puder ser atendido, registre bloqueio em vez de inventar comportamento.

## Formato de resposta ao concluir uma etapa

1. Resultado entregue.
2. Requisitos/US/CT atendidos.
3. Arquivos criados/alterados.
4. Testes executados e resultados.
5. Segurança e riscos verificados.
6. Pendências e próxima etapa segura.
7. Declaração explícita de que nenhum commit foi feito.

Comece agora somente pelo inventário e relatório inicial. Não implemente antes de apresentar esse relatório.
