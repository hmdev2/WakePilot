# Harness assistido do M1

Esta ferramenta reduz o uso técnico normal do M1 a dois comandos: `health` e `wake`. Ela deve ser executada no notebook emissor, não no PC que ficará desligado.

O pareamento inicial continua exigindo uma confirmação presencial do fingerprint SSH. Depois disso, a chave privada Ed25519 é protegida com DPAPI `CurrentUser`; o OpenSSH recebe apenas uma cópia temporária com ACL restrita durante cada chamada.

## Publicar a ferramenta

```powershell
dotnet publish tools/RemoteWake.M1.Harness/RemoteWake.M1.Harness.csproj `
  --configuration Release `
  --self-contained false `
  --output artifacts/m1-harness
```

O executável gerado é `artifacts/m1-harness/RemoteWake.M1.Harness.exe`.

## Pareamento único

No Android, obtenha a chave pública e o fingerprint do host do bridge:

```sh
cp ~/.remote-wake/ssh_host_ed25519_key.pub ~/storage/downloads/remote-wake-host.pub
ssh-keygen -lf ~/.remote-wake/ssh_host_ed25519_key.pub -E sha256
```

Transfira somente `remote-wake-host.pub` ao notebook e confira presencialmente o fingerprint exibido. No notebook, importe a chave privada do launcher e fixe a identidade do Android:

```powershell
artifacts/m1-harness/RemoteWake.M1.Harness.exe configure `
  --host 100.64.0.10 `
  --port 8023 `
  --user u0_a123 `
  --target-id 11111111-1111-1111-1111-111111111111 `
  --identity-file C:\caminho\launcher_ed25519 `
  --host-key-file C:\caminho\remote-wake-host.pub `
  --confirm-host-fingerprint SHA256:FINGERPRINT_CONFERIDO `
  --remove-identity-file
```

`--remove-identity-file` só apaga o arquivo privado de origem depois que a cópia DPAPI e o perfil forem gravados com sucesso. A chave pública pode ser preservada.

## Uso normal

```powershell
artifacts/m1-harness/RemoteWake.M1.Harness.exe health
artifacts/m1-harness/RemoteWake.M1.Harness.exe wake
```

O recibo de `wake` confirma o burst de três Magic Packets pelo Android. A confirmação de que o PC realmente acordou ainda deve ser feita de forma independente no M1; o ReadinessAgent pertence ao M3.

## Dados locais

O perfil fica em `%LOCALAPPDATA%\WakePilot\M1Lab`. Ele contém endpoint, IDs e uma referência DPAPI, mas não contém a chave privada em texto puro. Não copie a pasta para outro usuário ou computador.
