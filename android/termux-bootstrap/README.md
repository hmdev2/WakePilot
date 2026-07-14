# Bridge Termux — M1

O pacote implementa o forced command do protocolo bridge v1 sem shell remoto. O bootstrap é idempotente e apenas aplica mudanças com `--apply` explícito.

## Pré-requisitos

Termux e Termux:Boot devem vir da mesma origem compatível. O técnico precisa de acesso físico ao Android, uma chave pública Ed25519 do launcher e um UUID de identidade não vazio.

## Aplicação e recuperação

Execute primeiro sem `--apply` para revisar o plano. Depois, no Termux:

```sh
./bootstrap.sh --apply --public-key-file /caminho/launcher.pub --key-id UUID
```

O bootstrap cria um `sshd` isolado na porta 8022, sem senha, PTY, forwarding, túnel, user rc ou shell geral. Para recuperar, execute `~/.remote-wake/uninstall.sh`. A remoção preserva os pacotes Termux compartilhados e remove somente o bridge.

`~/.remote-wake/config.json` é uma allowlist local fechada. Cada target usa `mac` maiúsculo (`AA:BB:CC:DD:EE:FF`), broadcast IPv4 e porta UDP. Nunca transporte o MAC no pedido remoto.

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
