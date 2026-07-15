# Evidências e gate do M1

## Controle

Versão 1.2.0 — Estado: Revisar — Data: 15/07/2026. Gate de hardware parcialmente aprovado.

## Decisão de avanço

O M1 não está concluído. A parte automatizável da prova vertical foi implementada na branch `milestone/m1-vertical-proof`, mas a documentação normativa exige Android físico, PC de laboratório, consentimento explícito e plano de recuperação. Até essas evidências existirem, M2 permanece bloqueado.

Nenhuma instalação, mudança de VPN, serviço, firewall, energia ou driver foi executada na máquina do desenvolvedor. Os testes de Magic Packet usam socket fake e não alcançam a LAN.

## Evidência automatizada disponível

| Caso | Evidência atual | Estado |
| --- | --- | --- |
| CT010 | Bootstrap idempotente com gate `--apply`, porta isolada configurável, cadastro atômico do target, boot script e verificação estática | Parcial; reboot/Doze em Android real pendente |
| CT011 | Privada protegida por DPAPI CurrentUser, lease temporário com ACL restrita e remoção; pública com forced command e `restrict` | Automatizado |
| CT012 | Cliente bloqueia host key divergente com ERR010; correlação fechada | Automatizado |
| CT013 | Pedido transporta apenas target ID; target fora da allowlist não inicia SSH/UDP | Automatizado |
| CT014 | Processo exige caminho canônico e preserva argumentos sem shell | Automatizado |
| CT015 | Máquina de estados exige readiness de Windows/serviço; ping não é prova | Automatizado no M0; agente real pertence ao M3 |
| CT016 | Estado Tailscale, bridge, host divergente e falha de comando têm resultados tipados distintos | Automatizado |
| CT017 | Wrapper constrói 102 bytes e envia burst simulado 3×250 ms; cliente exige recibo de 3 pacotes | Automatizado; envio/boot real pendente |
| CT018 | JSON fechado, 4 KiB, versão, timestamp, nonce, replay persistente, cooldown e 3/5 min | Automatizado |

O harness técnico do M1 importa a identidade Ed25519 para DPAPI `CurrentUser`, exige confirmação do fingerprint Ed25519 antes de criar `known_hosts` e oferece operações fechadas `health`/`wake`; ele não disponibiliza comando remoto arbitrário.

Na verificação local de 15/07/2026 passaram 31 testes M0, 21 testes de contrato M1 e 12 testes Python do bridge/configurador, com build Release sem avisos. O gate completo e as auditorias devem ser repetidos antes do merge.

## Evidência física parcial — 15/07/2026

Foi autorizado um laboratório com PC Ethernet Intel I219-V, Android Samsung SM-A205G/Android 11 e notebook Windows na mesma tailnet. A execução preservou acesso físico e não suspendeu nem desligou o PC durante o provisionamento.

Resultados observados:

* Android alcançável pelo Tailscale e conectado à LAN do PC;
* Termux `0.118.3` e Termux:Boot `0.8.1` instalados com o mesmo certificado F-Droid, conferido por SHA-256 antes da instalação do add-on;
* bootstrap aplicado na porta isolada `8023`, com target allowlisted e chave pública dedicada do notebook;
* host Ed25519 do bridge fixado pelo canal USB/ADB e conferido antes da conexão;
* `health` real aceito com `code: OK`, `packetCount: 0` e correlação válida;
* chave temporária de provisionamento removida; tentativa posterior rejeitada com exit `255`/`publickey`;
* chave restrita do notebook preservada exatamente uma vez;
* SSH administrativo padrão do Termux encerrado na porta `8022`; somente o bridge isolado permaneceu alcançável na porta `8023`;
* após o hardening, a negociação real na porta `8023` preservou o fingerprint Ed25519 fixado, anunciou somente autenticação `publickey` e rejeitou um cliente sem a chave dedicada;
* Termux, Termux:Boot e Tailscale liberados da otimização agressiva de bateria;
* script de conclusão entregue ao notebook por Taildrop, com hash SHA-256 registrado localmente.
* harness instalado no perfil do notebook, chave privada de origem removida após proteção DPAPI e `health` autenticado aprovado pelo próprio notebook;
* comportamento específico do OpenSSH para Windows com resposta em pipe anônimo corrigido e validado no hardware real;
* `WakeOnPattern` desabilitado após um despertar prematuro diagnosticado, preservando `WakeOnMagicPacket` habilitado e a Intel I219-V em `wake_armed`;
* PC colocado em S3 às `12:31:45-03:00`; o notebook iniciou o wake às `12:32:25.556-03:00`, recebeu recibo de 3 pacotes às `12:32:27.608-03:00` e o PC retomou às `12:32:29.451-03:00`;
* `powercfg /lastwake` confirmou a Intel I219-V como origem da retomada, aproximadamente 3,9 segundos após o pedido do notebook;
* tarefa agendada e script de teste físico removidos do notebook após a coleta da evidência; o atalho comum foi preservado.

Esta evidência prova `Notebook → Tailscale → Android → Magic Packet → retomada S3 do PC` no hardware descrito. Ainda não prova reinício do Android, Doze/24 h, outros estados de energia ou a matriz de fabricantes. O recibo do bridge e a confirmação independente do Windows permanecem registrados como evidências distintas.

## Fluxo assistido recomendado para o laboratório

Com Termux, Termux:Boot e Tailscale existentes, o operador não repete instalação ou login. O caminho mínimo é:

1. gerar uma chave Ed25519 e dois UUIDs no notebook;
2. copiar a pasta do bootstrap e somente a chave pública para o Android;
3. executar um único bootstrap com chave, porta, target, MAC e broadcast;
4. confirmar presencialmente o fingerprint do host e importar a privada no harness DPAPI;
5. executar `health`, suspender o PC e executar `wake`.

Os testes negativos, reboot, Doze/24 h, estados adicionais e repetição estatística pertencem à homologação do ambiente, não ao uso cotidiano.

## Registro antes do teste físico

Antes de executar o bootstrap ou enviar um wake real, registrar:

1. responsável e consentimento para Android e PC dedicados;
2. modelos, versões, origem compatível de Termux/Termux:Boot e versão Tailscale;
3. estado de energia a testar (S3, S4 ou S5) e baseline de boot local;
4. fingerprints Ed25519 conferidas presencialmente e caminho de recuperação local;
5. acesso físico/USB ao Android, acesso local ao PC e janela para reinício;
6. procedimento de remoção `~/.remote-wake/uninstall.sh` e cópia do baseline do PC;
7. critério de interrupção: identidade divergente, relógio inválido, target inesperado ou ausência de recuperação local.

## Evidência necessária para fechar o M1

O relatório do laboratório deve conter, sem segredos ou MAC completo:

* hash do pacote testado e commits;
* fingerprint abreviada do host confirmada por canal presencial;
* resultado de health antes/depois do reboot Android;
* captura sanitizada do recibo correlacionado;
* confirmação independente de que o PC acordou, com tempo até rede/Windows;
* replay, flood controlado, host divergente e tentativa de escape rejeitados;
* restauração executada e verificada;
* aprovação explícita do responsável pelo laboratório.

Somente após esse relatório o merge do M1 em `main` e a criação de `milestone/m2-launcher` são permitidos.
