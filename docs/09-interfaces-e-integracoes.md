# Interfaces e integrações

## Controle

Versão 1.1.1 — Estado: Aprovado — Data: 14/07/2026. Código de sucesso do bridge explicitado.

## Protocolo bridge v1

Pedido lógico:

```json
{"v":1,"requestId":"uuid","targetId":"uuid","issuedAt":"UTC","nonce":"base64url","action":"health|wake"}
```

Resposta lógica:

```json
{"v":1,"requestId":"uuid","status":"accepted|rejected|error","code":"OK|ERRxxx","packetCount":3,"serverTime":"UTC"}
```

Limites: UTF-8, ≤4 KiB, campos adicionais rejeitados no v1, request/nonce únicos, relógio ±60 s. `OK` é permitido somente com `accepted`; rejeição/erro usa `ERRxxx`. MAC nunca cruza no pedido: targetId resolve allowlist local.

## Protocolo ReadinessAgent v1

Canal: HTTPS/1.1 JSON via Kestrel, escutando somente no IPv4 Tailscale e em porta livre persistida do intervalo 49152–65535. Kestrel exige certificado cliente.

Endpoint:

`POST /rwa/v1/readiness`

Pedido:

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

Resposta:

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

Regras: payload fechado ≤16 KiB; `application/json`; `Cache-Control: no-store`; timestamp ±60 s; request ID e nonce novos; response ecoa ambos; 60 sondas/minuto por certificado; uma sonda simultânea por launcher; versão major incompatível retorna ERR021. Estados: `starting`, `ready`, `notReady`, `notInstalled`, `degraded`, `unknown`.

Autenticação: X.509 ECDSA P-256 autoassinado por dispositivo, pin SHA-256 bilateral, private keys não exportáveis no Windows Certificate Store, validade 365 dias, rotação 30 dias antes e sobreposição máxima 7 dias. Nenhum fallback sem mTLS.

## Catálogo

| ID | Integração | MVP |
| --- | --- | --- |
| INT001 | Windows Power Management | Sim |
| INT002 | Adaptadores de rede Windows | Sim |
| INT003 | PowerShell/APIs nativas | Sim |
| INT004 | Tailscale | Sim |
| INT005 | OpenSSH | Sim |
| INT006 | Android | Sim |
| INT007 | Termux + Termux:Boot | Sim |
| INT008 | Wake-on-LAN | Sim |
| INT009 | RustDesk | Sim |
| INT010 | AnyDesk | Não; contrato futuro |
| INT011 | Moonlight | Não; contrato futuro |
| INT012 | Sunshine | Não; contrato futuro |
| INT013 | Executável personalizado | Sim |
| INT014 | Armazenamento seguro Windows | Sim |
| INT015 | SQLite | Sim |
| INT016 | Notificações Windows | Sim |
| INT017 | Readiness Agent | Sim |

## Contratos

### INT001 — Windows Power Management

| Campo | Contrato |
| --- | --- |
| Direção | Configurador→Windows |
| Objetivo | Consultar estados, wake armed/programável, Fast Startup; aplicar apenas operação tipada autorizada. |
| Entrada | GUID do adaptador/operação enum |
| Saída | evidências/resultado verificado |
| Autenticação/autorização | Token Windows; broker para mutação |
| Erros | ERR001,ERR004,ERR007 |
| Timeout | leitura 10 s; mutação 30 s |
| Retry | sem retry de mutação; uma releitura |
| Fallback | orientação manual/rollback |
| Logs | comando, duração e código; saída sanitizada |
| Riscos | mudanças entre builds/idiomas |
| Estratégia de teste | fake + VM + hardware real |
| Abstração | IWindowsPowerManager |

### INT002 — Adaptadores de rede Windows

| Campo | Contrato |
| --- | --- |
| Direção | Configurador→Windows |
| Objetivo | Enumerar e classificar interfaces; obter MAC/driver/propriedades. |
| Entrada | nenhuma/interface GUID |
| Saída | NetworkAdapterSnapshot |
| Autenticação/autorização | sessão local |
| Erros | ERR002,ERR003 |
| Timeout | 10 s |
| Retry | uma repetição de leitura |
| Fallback | seleção técnica |
| Logs | contagem/tipo sem MAC completo |
| Riscos | driver omite capacidade |
| Estratégia de teste | fixtures + NIC física/virtual |
| Abstração | INetworkAdapterInspector |

### INT003 — PowerShell/APIs nativas

| Campo | Contrato |
| --- | --- |
| Direção | Infra→Windows |
| Objetivo | Usar API nativa primeiro; processo allowlisted apenas quando API suficiente não existir. |
| Entrada | comando enum e ArgumentList |
| Saída | stdout tipado/exit code |
| Autenticação/autorização | broker quando elevado |
| Erros | ERR001,ERR007 |
| Timeout | 10–30 s |
| Retry | somente leitura idempotente |
| Fallback | marcar inconclusivo |
| Logs | hash do executável, exit code |
| Riscos | localização/injeção |
| Estratégia de teste | testes de argumentos; nunca shell |
| Abstração | ISystemCommandRunner |

### INT004 — Tailscale

| Campo | Contrato |
| --- | --- |
| Direção | Launcher/Configurador→cliente |
| Objetivo | Detectar estado, nome/endereço e alcançar nodes; autenticação permanece no cliente oficial. |
| Entrada | node ID/nome |
| Saída | connected, IP, last seen |
| Autenticação/autorização | identidade Tailscale + autenticação própria da app |
| Erros | ERR008 |
| Timeout | 5 s |
| Retry | 2 leituras com backoff |
| Fallback | orientar reconexão |
| Logs | versão/estado, sem auth key |
| Riscos | dependência externa e política de conta |
| Estratégia de teste | fake + duas redes reais |
| Abstração | IVpnAdapter |

### INT005 — OpenSSH

| Campo | Contrato |
| --- | --- |
| Direção | Launcher→bridge |
| Objetivo | Transportar health/wake via chave Ed25519 e host key pinned. |
| Entrada | payload JSON base64url ≤4 KiB |
| Saída | JSON receipt |
| Autenticação/autorização | chave individual + host pinning |
| Erros | ERR009-ERR012 |
| Timeout | connect 5 s; command 10 s |
| Retry | wake não reexecuta sem novo request |
| Fallback | nenhum shell; suporte presencial |
| Logs | fingerprints curtas/código |
| Riscos | forced command mal configurado |
| Estratégia de teste | sshd de teste, MITM, escape |
| Abstração | IBridgeTransport |

### INT006 — Android

| Campo | Contrato |
| --- | --- |
| Direção | Técnico/Bridge |
| Objetivo | Hospedar bridge sem root e sobreviver a boot/Doze conforme matriz. |
| Entrada | bootstrap e permissões guiadas |
| Saída | health/diagnóstico |
| Autenticação/autorização | sandbox Android/Tailscale |
| Erros | ERR009,ERR018 |
| Timeout | health 5 s |
| Retry | backoff no launcher |
| Fallback | instrução por fabricante |
| Logs | versão/fabricante/estado de bateria sem dados pessoais |
| Riscos | processo encerrado |
| Estratégia de teste | hardware real 24 h |
| Abstração | IAndroidBridgeEnvironment |

### INT007 — Termux + Termux:Boot

| Campo | Contrato |
| --- | --- |
| Direção | Bootstrap→Android |
| Objetivo | Instalar wrapper, sshd e boot script a partir de origem compatível. |
| Entrada | pacote assinado/config versionada |
| Saída | installation receipt |
| Autenticação/autorização | acesso físico técnico |
| Erros | ERR018 |
| Timeout | etapa humana; health 60 s após boot |
| Retry | retomar idempotente |
| Fallback | reinstalar da mesma origem |
| Logs | versões e checksums |
| Riscos | plugins com assinaturas diferentes |
| Estratégia de teste | emulador + Pixel/Samsung/Motorola |
| Abstração | IBridgeBootstrapper |

### INT008 — Wake-on-LAN

| Campo | Contrato |
| --- | --- |
| Direção | Wrapper→LAN |
| Objetivo | Construir Magic Packet a partir de target allowlisted e enviá-lo por broadcast calculado. |
| Entrada | target ID autorizado |
| Saída | receipt packet_sent/count/time |
| Autenticação/autorização | autorização anterior no wrapper |
| Erros | ERR012,ERR013 |
| Timeout | UDP imediato |
| Retry | burst 3×250 ms; 1 retry/15 s |
| Fallback | orientar BIOS/energia |
| Logs | target ID, nunca MAC em claro |
| Riscos | receipt não prova entrega/wake |
| Estratégia de teste | parser, captura e hardware |
| Abstração | IMagicPacketSender |

### INT009 — RustDesk

| Campo | Contrato |
| --- | --- |
| Direção | Launcher→executável |
| Objetivo | Validar instalação, sondar prontidão definida e abrir cliente; não armazenar senha. |
| Entrada | perfil preset e argumentos tipados |
| Saída | LaunchResult |
| Autenticação/autorização | segurança própria do RustDesk |
| Erros | ERR015,ERR016 |
| Timeout | probe 120 s; start 3 s |
| Retry | uma abertura |
| Fallback | localizar/reconfigurar |
| Logs | versão/hash/exit |
| Riscos | CLI pode mudar; serviço pode atrasar |
| Estratégia de teste | fake process + instalação real |
| Abstração | IRemoteAppAdapter |

### INT010 — AnyDesk

| Campo | Contrato |
| --- | --- |
| Direção | Launcher→executável |
| Objetivo | Integração futura por adapter; MVP apenas detecta como não suportado. |
| Entrada | perfil futuro |
| Saída | não implementado |
| Autenticação/autorização | segurança do fornecedor |
| Erros | ERR017 |
| Timeout | Fora do MVP; definir com contrato oficial na evolução |
| Retry | nenhum |
| Fallback | perfil personalizado seguro |
| Logs | detecção |
| Riscos | CLI/licença |
| Estratégia de teste | contrato futuro |
| Abstração | IRemoteAppAdapter |

### INT011 — Moonlight

| Campo | Contrato |
| --- | --- |
| Direção | Launcher→cliente |
| Objetivo | Integração futura; abertura e seleção de host/app dependerão de contrato oficial validado. |
| Entrada | host/app tipados |
| Saída | LaunchResult |
| Autenticação/autorização | pareamento Moonlight/Sunshine |
| Erros | ERR017 |
| Timeout | Fora do MVP; definir com contrato oficial na evolução |
| Retry | nenhum |
| Fallback | abrir cliente sem auto-seleção |
| Logs | versão |
| Riscos | URI/CLI variam |
| Estratégia de teste | hardware e contrato futuro |
| Abstração | IRemoteAppAdapter |

### INT012 — Sunshine

| Campo | Contrato |
| --- | --- |
| Direção | Agent→serviço host |
| Objetivo | Integração futura para sonda de serviço e catálogo de apps; não guardar credenciais web. |
| Entrada | service profile |
| Saída | Ready/NotReady |
| Autenticação/autorização | configuração Sunshine existente |
| Erros | ERR015,ERR017 |
| Timeout | 120 s |
| Retry | sonda com backoff |
| Fallback | process/service probe |
| Logs | estado/versão |
| Riscos | porta/serviço configuráveis |
| Estratégia de teste | instalação real futura |
| Abstração | IRemoteServiceProbe |

### INT013 — Executável personalizado

| Campo | Contrato |
| --- | --- |
| Direção | Launcher→processo |
| Objetivo | Abrir caminho canônico com placeholders allowlisted e health probe selecionada. |
| Entrada | path, hash, args tipados |
| Saída | LaunchResult |
| Autenticação/autorização | ACL local |
| Erros | ERR016 |
| Timeout | 3 s |
| Retry | uma vez |
| Fallback | reconfigurar |
| Logs | hash/exit |
| Riscos | injeção/substituição binária |
| Estratégia de teste | argument fuzzing |
| Abstração | IRemoteAppAdapter |

### INT014 — Armazenamento seguro Windows

| Campo | Contrato |
| --- | --- |
| Direção | Application→DPAPI/Credential Manager |
| Objetivo | Guardar privada/referência vinculada ao usuário e apagar na revogação. |
| Entrada | bytes + finalidade |
| Saída | SecretReference |
| Autenticação/autorização | identidade Windows |
| Erros | ERR006 |
| Timeout | 5 s |
| Retry | uma repetição de leitura |
| Fallback | bloquear operação sensível |
| Logs | somente referência/fingerprint |
| Riscos | perfil corrompido/migração |
| Estratégia de teste | outro usuário e backup |
| Abstração | ISecretVault |

### INT015 — SQLite

| Campo | Contrato |
| --- | --- |
| Direção | Application→arquivo local |
| Objetivo | Persistir domínio e histórico transacional. |
| Entrada | entidades/queries |
| Saída | resultados/version |
| Autenticação/autorização | ACL + validação de esquema |
| Erros | ERR005 |
| Timeout | 5 s por operação |
| Retry | busy retry limitado 3× |
| Fallback | modo somente leitura/backup |
| Logs | duração/schema |
| Riscos | corrupção/concorrência |
| Estratégia de teste | integração e fault injection |
| Abstração | IRepository |

### INT016 — Notificações Windows

| Campo | Contrato |
| --- | --- |
| Direção | Application→Windows |
| Objetivo | Notificar sucesso/falha sem dados técnicos sensíveis. |
| Entrada | tipo, título, mensagem localizada |
| Saída | accepted/denied |
| Autenticação/autorização | permissão do usuário |
| Erros | ERR019 não bloqueante |
| Timeout | 2 s |
| Retry | sem duplicar |
| Fallback | feedback in-app |
| Logs | tipo apenas |
| Riscos | spam/permissão negada |
| Estratégia de teste | UI automation |
| Abstração | INotificationService |

### INT017 — Readiness Agent

| Campo | Contrato |
| --- | --- |
| Direção | Launcher→PC |
| Objetivo | Confirmar Windows e serviço remoto após wake. |
| Entrada | ReadinessRequest v1: versão, request ID, computer ID, service ID, timestamp e nonce |
| Saída | ReadinessResponse v1: estados Windows/serviço, agent version, observedAt e nonce ecoado |
| Autenticação/autorização | HTTPS/mTLS; certificados ECDSA P-256 fixados bilateralmente; cliente deve estar ativo |
| Erros | ERR010,ERR014,ERR015,ERR021 |
| Timeout | 5 s por sonda |
| Retry | uma sonda simultânea; backoff 1/2/4/8/10 s até o limite da fase |
| Fallback | nenhum fallback sem autenticação; diagnóstico por camada continua disponível |
| Logs | latência, versão e fingerprint curta; sem certificado/payload bruto |
| Riscos | certificado expirado, porta ocupada, Tailscale atrasado, firewall, clock |
| Estratégia de teste | contract tests, mTLS adversarial, versão, replay, rate limit, firewall e serviço real |
| Abstração | IReadinessProbe |


## Política comum

* Nenhuma integração executa por shell.
* Retries de mutação exigem idempotency key ou novo consentimento.
* Adapters retornam resultado tipado, cancellation token, duração e evidência sanitizada.
* Mudança incompatível em ferramenta externa desabilita o adapter e preserva diagnóstico.
* Documentação oficial prevalece; comportamento não documentado exige teste e versão fixada.

## Referências do INT017

* [Microsoft — Certificate authentication in ASP.NET Core](https://learn.microsoft.com/en-us/aspnet/core/security/authentication/certauth?view=aspnetcore-10.0)
* [Microsoft — Kestrel HTTPS endpoints](https://learn.microsoft.com/en-us/aspnet/core/fundamentals/servers/kestrel/endpoints?view=aspnetcore-10.0)
* [Microsoft — Windows Firewall rules](https://learn.microsoft.com/en-us/windows/security/operating-system-security/network-security/windows-firewall/rules)
