# Bridge Termux — M1

O pacote implementa o forced command do protocolo bridge v1 sem shell remoto. O bootstrap é idempotente e apenas aplica mudanças com `--apply` explícito.

## Pré-requisitos

Termux e Termux:Boot devem vir da mesma origem compatível. Abra o Termux:Boot uma vez após a instalação para habilitar seu receptor de inicialização. O técnico precisa de acesso físico ao Android, uma chave pública Ed25519 do launcher e um UUID de identidade não vazio.

Para restaurar o acesso remoto depois de reiniciar o telefone, configure o Tailscale como VPN sempre ativa e mantenha o lockdown desligado. Em aparelhos Samsung, a fila de inicialização pode levar alguns minutos depois que a tela principal já apareceu; aguarde até cinco minutos antes de declarar falha do bridge.

## Aplicação assistida e recuperação

Se Termux, Termux:Boot e Tailscale já estão configurados, não é necessário reinstalá-los. Copie esta pasta e a chave pública do launcher para o Android. Execute primeiro sem `--apply` para revisar o plano e depois repita com `--apply`:

```sh
./bootstrap.sh \
  --public-key-file /caminho/launcher.pub \
  --key-id UUID-DA-CHAVE \
  --sshd-port 8023 \
  --target-id UUID-DO-PC \
  --target-mac AA:BB:CC:DD:EE:FF \
  --target-broadcast 192.168.1.255

./bootstrap.sh --apply \
  --public-key-file /caminho/launcher.pub \
  --key-id UUID-DA-CHAVE \
  --sshd-port 8023 \
  --target-id UUID-DO-PC \
  --target-mac AA:BB:CC:DD:EE:FF \
  --target-broadcast 192.168.1.255
```

Use `8023` quando seu SSH normal do Termux já estiver na porta `8022`; qualquer porta livre entre 1024 e 65535 pode ser escolhida. O bootstrap cria um `sshd` isolado, sem senha, PTY, forwarding, túnel, user rc ou shell geral, e cadastra o PC na allowlist na mesma execução. Para recuperar, execute `~/.remote-wake/uninstall.sh`. A remoção revoga a chave restrita, preserva os pacotes Termux compartilhados e remove somente o bridge.

`~/.remote-wake/config.json` é uma allowlist local fechada. Cada target usa `mac` maiúsculo (`AA:BB:CC:DD:EE:FF`), broadcast IPv4 e porta UDP 9 por padrão. Nunca transporte o MAC no pedido remoto.

Depois da instalação, use o [harness Windows](../../tools/RemoteWake.M1.Harness/README.md) para proteger a chave privada, fixar a identidade do Android e reduzir a operação a `health` ou `wake`.

## Exit codes

| Código | Significado |
| --- | --- |
| 0 | health ou wake aceito |
| 10 | envelope/JSON/versão inválidos |
| 11 | timestamp ou nonce inválido/repetido |
| 12 | target ou ação não permitidos |
| 13 | cooldown ou rate limit |
| 20 | falha ao enviar o burst UDP |
| 21 | configuração, estado ou falha interna |

O recibo `accepted` com `packetCount: 3` comprova somente que o bridge processou o pedido e enviou o burst. A prontidão do PC deve ser confirmada separadamente.
