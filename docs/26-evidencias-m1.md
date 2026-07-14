# Evidências e gate do M1

## Controle

Versão 1.0.1 — Estado: Revisar — Data: 14/07/2026. Gate de hardware pendente.

## Decisão de avanço

O M1 não está concluído. A parte automatizável da prova vertical foi implementada na branch `milestone/m1-vertical-proof`, mas a documentação normativa exige Android físico, PC de laboratório, consentimento explícito e plano de recuperação. Até essas evidências existirem, M2 permanece bloqueado.

Nenhuma instalação, mudança de VPN, serviço, firewall, energia ou driver foi executada na máquina do desenvolvedor. Os testes de Magic Packet usam socket fake e não alcançam a LAN.

## Evidência automatizada disponível

| Caso | Evidência atual | Estado |
| --- | --- | --- |
| CT010 | Bootstrap idempotente com gate `--apply`, sshd isolado, boot script e verificação estática | Parcial; reboot/Doze em Android real pendente |
| CT011 | Privada protegida por DPAPI CurrentUser, lease temporário com ACL restrita e remoção; pública com forced command e `restrict` | Automatizado |
| CT012 | Cliente bloqueia host key divergente com ERR010; correlação fechada | Automatizado |
| CT013 | Pedido transporta apenas target ID; target fora da allowlist não inicia SSH/UDP | Automatizado |
| CT014 | Processo exige caminho canônico e preserva argumentos sem shell | Automatizado |
| CT015 | Máquina de estados exige readiness de Windows/serviço; ping não é prova | Automatizado no M0; agente real pertence ao M3 |
| CT016 | Estado Tailscale, bridge, host divergente e falha de comando têm resultados tipados distintos | Automatizado |
| CT017 | Wrapper constrói 102 bytes e envia burst simulado 3×250 ms; cliente exige recibo de 3 pacotes | Automatizado; envio/boot real pendente |
| CT018 | JSON fechado, 4 KiB, versão, timestamp, nonce, replay persistente, cooldown e 3/5 min | Automatizado |

Na verificação local de 14/07/2026 passaram 31 testes M0, 17 testes de contrato M1 e 9 testes do wrapper Termux, com build Release sem avisos.

## Plano recomendado para o laboratório

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
