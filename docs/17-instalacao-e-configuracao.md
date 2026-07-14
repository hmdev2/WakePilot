# Instalação e configuração

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

## Pré-requisitos

PC/notebook x64 suportado; Ethernet no PC; Android 10+ alimentado; Wi-Fi na LAN; internet durante setup; conta Tailscale; acesso físico e credencial administrativa; RustDesk ou executável remoto instalado.

## Ordem do configurador

1. Verificar integridade/versão do instalador.
2. Instalar launcher/configurador e componentes compartilhados.
3. Executar diagnóstico não destrutivo.
4. Apresentar classificação preliminar.
5. Capturar snapshot e solicitar consentimento por alteração.
6. Configurar Windows por broker e verificar.
7. Orientar BIOS/UEFI.
8. Instalar/autenticar Tailscale visualmente.
9. Preparar Android e testar reinício.
10. Gerar chave SSH, parear e fixar o host Android.
11. Provisionar certificados mTLS do ReadinessAgent e pins bilaterais.
12. Escolher porta livre 49152–65535, instalar regra restrita de firewall e testar HTTPS.
13. Cadastrar PC e RustDesk.
14. Executar teste ponta a ponta.
15. Entregar resumo e relatório.

## Alterações permitidas

Somente operações presentes na allowlist e aprovadas: autorização de wake do adaptador; propriedades WoL suportadas pelo driver quando controláveis; configuração do ReadinessAgent/firewall estritamente necessária; instalação/remoção dos componentes. Fast Startup só muda com consentimento específico e snapshot.

## Não automatizado

BIOS/UEFI; login Tailscale; desativação de antivírus/firewall; senha de aplicativo remoto; estado de energia destrutivo sem confirmação.

## Backup e rollback

Snapshot por lote, hash e tool version. Rollback aplica ordem inversa, compara mudança externa e verifica. Instalador mantém pacote anterior até health check. Falha de snapshot bloqueia mutação.

## Android

Instalar Termux e plugin da mesma origem; abrir Boot uma vez; aplicar bootstrap verificado; autenticar Tailscale; configurar política de bateria com instrução específica; reiniciar; validar sshd/wrapper; nunca habilitar senha ou shell para a chave do launcher.

## Notebook

Instalar launcher; autenticar Tailscale; gerar chave SSH e certificado cliente ECDSA P-256 não exportável; parear presencialmente; fixar certificado público do agente; validar RustDesk; testar dashboard.

## ReadinessAgent e mTLS

O configurador elevado instala o Windows Service sob conta virtual/service SID, gera certificado servidor ECDSA P-256 não exportável em `LocalMachine\My`, concede acesso da private key somente ao serviço e registra o certificado cliente autorizado.

Uma porta livre é selecionada entre 49152 e 65535 e persistida. A regra de firewall permite apenas o executável assinado do agente, TCP, essa porta, endereço local Tailscale do PC e endereço remoto Tailscale do notebook. O agente não escuta enquanto o endereço Tailscale não existir.

O teste de instalação deve provar: HTTP inexistente; mTLS obrigatório; pins corretos; `POST /rwa/v1/readiness` retorna request ID/nonce correspondentes; certificado desconhecido é rejeitado; serviço remoto allowlisted muda corretamente de estado. Snapshot inclui serviço, certificado, ACL, porta e regra de firewall para rollback.

## Reinstalação/migração

Detectar instalação e schema; nunca sobrescrever configuração sem backup; reuso de chave/certificado somente se cofre íntegro e explicitamente preservado. Troca de notebook cria nova chave SSH e novo certificado cliente. Reinstalação do PC/agente exige novo certificado servidor e novo pin. Troca/reinstalação do Android exige novo host pinning/pareamento.

## Checklist de conclusão

Todos os componentes verdes; classificação registrada; nenhum ERR crítico; rollback disponível; relatório sanitizado; usuário comum demonstra a jornada; pendências aceitas listadas.
