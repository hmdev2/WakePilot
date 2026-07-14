# Manual técnico

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

## Escopo

Instalação, configuração, manutenção, diagnóstico e recuperação do MVP descrito em 03-escopo-mvp.md. Não usar este manual para habilitar shell remoto ou contornar controles.

## Instalação resumida

1. Validar hash/assinatura do pacote.
2. Instalar MSI x64 com credencial administrativa.
3. Executar PRC-001–PRC-008.
4. Guardar relatório e demonstrar jornada ao usuário.
5. Não deixar auth key Tailscale, senha ou chave privada em arquivos.

## Diagnóstico Windows

Coletar por adapters tipados: versão/build/arquitetura, powercfg /a, wake_programmable/wake_armed, interface GUID, PnP/driver e Fast Startup. Usar API nativa quando disponível. Saída de comando é tratada como dado não confiável/localizável. Nunca declarar validado antes de RF024.

## Configuração WoL

Plano→consentimento→snapshot→broker→verificação. Operações devem existir na allowlist. BIOS é manual/guiada. Testar separadamente por S3/S4/S5 aplicável e registrar firmware/driver.

## Android bridge

Termux e Termux:Boot devem vir da mesma origem de assinatura. Abrir Boot uma vez. Bootstrap instala wrapper, configuração SSH restrita e script de boot. Sem senha, PTY ou forwarding para chave do launcher. Reiniciar e testar health; repetir após Doze e 24 h.

## Autenticação

Chave Ed25519 individual; privada DPAPI CurrentUser; host fingerprint confirmado presencialmente; código 128 bits/10 min/uso único. forced command chama wrapper fixo. Pedido v1 fechado, ≤4 KiB, timestamp ±60 s, nonce e target allowlisted.

## ReadinessAgent

O agente é um Windows Service .NET 10 somente leitura. Ele expõe apenas `POST /rwa/v1/readiness` em HTTPS/1.1/Kestrel no IPv4 Tailscale e porta persistida entre 49152–65535. Kestrel exige certificado cliente.

PC e notebook usam certificados ECDSA P-256 autoassinados, não exportáveis e fixados bilateralmente por fingerprint SHA-256. Servidor em `LocalMachine\My` com ACL do service SID; cliente no perfil do usuário. Certificado desconhecido, expirado, revogado, com finalidade errada ou pin divergente é bloqueado sem fallback.

Firewall: programa do agente + TCP + porta exata + endereço local Tailscale + endereço remoto do notebook. Resposta deve ecoar request ID e nonce, usar `Cache-Control: no-store` e informar somente estados Windows/serviço allowlisted. Timeout 5 s e backoff 1/2/4/8/10 s.

Rotação começa 30 dias antes dos 365 dias de validade, com sobreposição máxima de 7 dias. Sem canal mTLS válido, realizar novo pareamento presencial.

## Operação e timeouts

VPN/bridge 5 s; recibo 10 s; Windows 240 s; serviço 120 s. Burst 3 pacotes/250 ms; máximo um retry após 15 s; 3 wakes/5 min e cooldown 15 s. Recibo não prova que o PC ligou.

## Diagnóstico por código

Usar catálogo ERR001–ERR021 em 19-observabilidade-e-suporte.md. Primeiro localizar última fase bem-sucedida pelo correlation ID. Não pedir segredo. ERR010 sempre exige novo pareamento; ERR021 exige verificar versão, certificado, porta, Tailscale e firewall; falhas repetidas de segurança e exportação com segredo bloqueiam release/operação.

## Logs

JSONL sanitizado, 20 MB/30 dias. Trace somente dev. Exportar por TEL020; não copiar arquivos brutos. Verificar scanner e manifesto.

## Backup e restauração

Validar hash do snapshot; comparar estado atual; obter consentimento; aplicar ordem inversa; verificar item a item. Mudança externa não é sobrescrita silenciosamente. Resultado parcial mantém snapshot.

## Atualização

Somente pacote/manifesto assinado em preview/stable. Backup e migração antes de health. Falha reverte uma vez. Não remover version guard nem repetir resultado incerto.

## Desinstalação

Revogar notebook; remover chave pública/wrapper se bridge online; restaurar Windows se escolhido; remover serviço, tarefa, firewall e apps; verificar resíduos; listar pendências offline. Reinstalação usa novas credenciais salvo preservação íntegra explicitamente aprovada.

## Segurança operacional

* Não executar PowerShell livre.
* Não desabilitar antivírus/firewall; criar somente regra mínima.
* Não aceitar host SSH desconhecido.
* Não incluir chave/token no repositório.
* Não rodar testes reais na máquina de desenvolvimento.
* Não usar shell para abrir apps.
* Não avançar marco com controle P0 falho.

## Desenvolvimento e testes

Seguir 13-plano-desenvolvimento.md e 14-plano-testes.md. Adapters externos sempre possuem fake/contract tests. E2E físico exige autorização, inventário e plano de recuperação.

## Fontes e mudança

Consultar documentação oficial antes de alterar integração. Atualizar ADR, requisito, risco, contrato, teste e rastreabilidade na mesma mudança.
