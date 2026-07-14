# AGENTS.md — WakePilot / Remote Wake Assistant

Este arquivo orienta agentes de programação que trabalham neste repositório. Ele se aplica à raiz e a todas as subpastas, salvo quando um `AGENTS.md` mais específico existir em uma subárvore.

## Objetivo do produto

Implementar incrementalmente um MVP local-first que permita a uma pessoa usuária ligar um PC Windows por meio de um Android bridge e abrir o RustDesk quando Windows e serviço remoto estiverem prontos. A experiência comum não deve expor terminal, MAC, IP, SSH, certificados ou detalhes da VPN.

## Fonte normativa e precedência

1. Pedido atual e explícito da pessoa usuária.
2. Este `AGENTS.md` e arquivos `AGENTS.md` mais específicos.
3. ADRs aceitos em `docs/adrs/`.
4. Requisitos, regras, interfaces, segurança e testes em `docs/`.
5. Convenções já estabelecidas pelo código e pelas ferramentas do repositório.

Em caso de conflito entre documentos, não escolha silenciosamente. Registre o conflito, cite os arquivos envolvidos e solicite decisão quando ela alterar segurança, escopo, compatibilidade ou arquitetura.

Comece por `docs/README.md`. Para qualquer implementação, leia ao menos os documentos diretamente relacionados ao marco e aos componentes alterados. `docs/24-prompt-codex.md` consolida o fluxo de implementação, mas não substitui os documentos normativos.

## Decisões que não podem ser alteradas sem novo ADR

* MVP local-first, sem backend próprio.
* .NET 10 LTS e WPF para os aplicativos Windows.
* Tailscale como rede privada do MVP.
* Android bridge com Termux, Termux:Boot e OpenSSH.
* SSH Ed25519 individual, host pinning e forced command.
* SQLite para estado; DPAPI, Credential Manager e Windows Certificate Store para material secreto.
* UI comum sem elevação permanente; broker privilegiado separado e mínimo.
* Ativação modelada como máquina de estados observável.
* MSI/WiX x64; canais preview/stable somente com assinatura.
* ReadinessAgent conforme `docs/adrs/ADR-010.md`: HTTPS/1.1 JSON sobre Tailscale, Kestrel, mTLS e certificados fixados bilateralmente.

## Estado atual e marcos

O protocolo do ReadinessAgent não é uma pendência: o ADR-010 está aceito e libera o M3, sujeito aos testes de contrato e segurança. Trabalhe em um marco por vez, seguindo `docs/13-plano-desenvolvimento.md`:

* M0 — fundação e máquina de estados;
* M1 — prova Tailscale → SSH → Android → Wake-on-LAN;
* M2 — launcher WPF mínimo;
* M3 — diagnóstico e ReadinessAgent;
* M4 — configurador, broker, pareamento e rollback;
* M5 — persistência final, instalador, atualização e suporte;
* M6 — homologação.

Não avance ao marco seguinte sem concluir os testes e o gate do marco atual. Hardware real, UAC, instalação de serviço, alteração de firewall, energia, driver, rede ou máquina do desenvolvedor exigem autorização explícita e plano de recuperação.

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

Se o repositório já usar nomes equivalentes, preserve-os e documente o mapeamento. Não faça reorganização ampla junto de uma mudança funcional sem necessidade comprovada.

## Fluxo obrigatório antes de editar

1. Inventarie arquivos, projetos, dependências, testes, pipelines e mudanças locais.
2. Identifique o marco, requisitos, regras, ADRs, riscos e casos de teste relacionados.
3. Declare brevemente os arquivos que pretende alterar e como verificará o resultado.
4. Preserve mudanças existentes da pessoa usuária e evite arquivos fora do escopo.
5. Se o repositório estiver vazio, crie somente a fundação do marco autorizado.

## Regras de implementação

* Mantenha `Domain` e `Application` independentes de WPF, SQLite, DPAPI, Tailscale, SSH e APIs concretas do Windows.
* Integrações devem implementar ports e retornar resultados tipados com código, causa sanitizada e indicação explícita de retry.
* Use nullable reference types, analyzers e warnings tratados como erros nos projetos de produção.
* Faça I/O assíncrono com `CancellationToken`; cancelamento deve interromper sondas em até 2 segundos e impedir novos retries.
* Abstraia relógio, backoff, geração de nonce, processos e rede para testes determinísticos.
* Use UTC internamente e recursos pt-BR na interface. Não espalhe texto de UI em código de domínio.
* Use caminho canônico e `ProcessStartInfo.ArgumentList`; não concatene comandos e não use shell textual.
* Migrações SQLite são versionadas, transacionais e precedidas por backup verificável.
* Logs são estruturados, correlacionados e sanitizados antes do sink. Não registre payload bruto por conveniência.
* Nunca serialize entidades de persistência diretamente para UI, logs ou exportação.

## Segurança: proibições absolutas

* Nunca grave no repositório chave privada, senha, token, auth key Tailscale, certificado privado ou fixture com segredo real.
* Nunca aceite host SSH ou certificado mTLS desconhecido, divergente, expirado ou revogado.
* Nunca crie fallback HTTP, mTLS opcional ou opção de ignorar validação de certificado.
* Nunca disponibilize shell remoto, PTY, port forwarding, agent forwarding, X11 forwarding ou comando arbitrário.
* Nunca transporte MAC arbitrário no pedido de wake; o bridge resolve `targetId` em allowlist local.
* Nunca execute mutação administrativa sem consentimento, snapshot, operação tipada e verificação posterior.
* Nunca amplie o PrivilegedBroker para comando genérico. Named pipe deve validar ACL, identidade e DTO fechado.
* Nunca desative firewall, antivírus, validação TLS ou controle de acesso para fazer um teste passar.
* Nunca afirme que recibo do bridge prova que o PC acordou, ou que ping isolado prova prontidão/falha.
* Um controle P0 de segurança impossível de cumprir bloqueia o marco; reporte o bloqueio em vez de criar atalho.

## Contrato bridge v1

O contrato normativo está em `docs/09-interfaces-e-integracoes.md`. Preserve estes invariantes:

* JSON UTF-8 fechado com no máximo 4 KiB;
* versão, request ID, nonce, timestamp UTC ±60 s, target e ação obrigatórios;
* nonce retido em hash por 10 minutos;
* cooldown de 15 segundos e máximo de 3 wakes em 5 minutos por chave/alvo;
* burst de 3 Magic Packets com intervalo de 250 ms;
* campo desconhecido, replay, alvo ausente ou ação desconhecida são rejeitados;
* entrada nunca é interpretada como comando de shell.

## Contrato ReadinessAgent v1

Implemente literalmente `docs/adrs/ADR-010.md` e `docs/09-interfaces-e-integracoes.md`:

* Windows Service .NET 10, somente leitura, hospedado em Kestrel;
* endpoint único `POST /rwa/v1/readiness`;
* HTTPS/1.1 sem listener HTTP e `ClientCertificateMode.RequireCertificate`;
* X.509 ECDSA P-256 autoassinado, pin SHA-256 bilateral e chave privada não exportável;
* certificado servidor em `LocalMachine\\My`, com ACL exclusiva do service SID;
* certificado cliente no Certificate Store do perfil do launcher;
* porta persistida entre 49152 e 65535;
* firewall limitado a executável, TCP, porta, IP local Tailscale e IP remoto pareado;
* JSON fechado de até 16 KiB, `application/json` e `Cache-Control: no-store`;
* timestamp ±60 s, request ID e nonce novos e ecoados na resposta;
* máximo de 60 sondas por minuto por certificado;
* timeout de 5 segundos, uma sonda em voo e backoff 1/2/4/8/10 segundos;
* certificado válido por 365 dias; rotação 30 dias antes; sobreposição máxima de 7 dias;
* versão major incompatível retorna ERR021.

O agente pode consultar somente estados allowlisted do Windows e do serviço remoto. Ele não inicia serviço/processo, não altera configuração, não lista processos gerais, não lê arquivo arbitrário e não expõe endpoint administrativo.

## Máquina de estados

Preserve as transições de ADR-008:

`Checking → AlreadyReady | BridgeUnavailable | SendingWake → WaitingWindows → WaitingService → OpeningClient → Completed | Failed | Cancelled`

Invariantes:

* `SendingWake` exige PC não pronto e bridge autenticado/saudável;
* recibo do wake não equivale a `WindowsReady`;
* serviço remoto só é sondado depois de Windows pronto;
* `Completed` exige abertura bem-sucedida do cliente;
* cancelamento interrompe sondas e impede retries;
* timeouts: bridge 5 s, recibo 10 s, Windows 240 s e serviço 120 s.

## Testes e verificação

Toda mudança funcional deve incluir ou atualizar testes no nível mais baixo capaz de provar o comportamento. Use fakes por padrão; testes destrutivos ou dependentes de hardware nunca devem rodar automaticamente.

Antes de concluir, execute os comandos existentes equivalentes a:

```powershell
dotnet restore
dotnet build --no-restore
dotnet test --no-build
dotnet format --verify-no-changes
```

Se algum projeto/comando ainda não existir, informe isso; não simule sucesso. Para mudanças de arquitetura, segurança, protocolo ou persistência, execute também os testes de contrato, integração e segurança aplicáveis de `docs/14-plano-testes.md`.

Testes mínimos de segurança incluem identidade SSH/mTLS ausente, desconhecida, divergente, expirada e revogada; downgrade TLS; replay; flood/rate limit; payload excedido/desconhecido; versão incompatível; porta ocupada; firewall restrito; cancelamento e sanitização de logs/exportação.

## Documentação e rastreabilidade

Ao alterar comportamento, atualize no mesmo trabalho os requisitos, regras, interface, risco, teste, manual e matriz de rastreabilidade afetados. Novo erro público deve entrar no catálogo `ERR`; nova decisão arquitetural exige ADR.

Não marque compatibilidade como validada sem teste ponta a ponta no hardware e estado de energia específicos. Diferencie sempre detecção, inferência e validação real.

## Entrega do trabalho

O relatório final deve informar:

* resultado entregue;
* arquivos criados ou alterados;
* testes realmente executados e seus resultados;
* testes não executados e motivo;
* riscos, limitações e decisões pendentes;
* próxima ação segura, quando aplicável.

Não esconda warnings, testes falhos, comportamento não verificado ou alteração manual necessária.
