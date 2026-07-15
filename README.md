# WakePilot

Fundação do Remote Wake Assistant, um produto local-first para ligar um PC Windows por um Android bridge e abrir o RustDesk somente quando Windows e serviço remoto estiverem prontos.

## Estado

O M0 está integrado em `main`. A linha do M1 contém os adapters Tailscale/OpenSSH, cofre DPAPI, protocolo bridge v1, bootstrap Termux, Magic Packet sender e um harness técnico assistido. Depois do pareamento único, o laboratório usa somente `health` ou `wake`. O marco permanece aberto até validar a cadeia em Android/PC autorizados. O M2 não deve começar antes desse gate.

## Pré-requisito

.NET SDK 10.0.204 ou patch estável compatível, conforme `global.json`.

## Verificação local

```powershell
dotnet restore RemoteWake.slnx --locked-mode
dotnet format RemoteWake.slnx --verify-no-changes --no-restore
dotnet build RemoteWake.slnx --configuration Release --no-restore
dotnet test RemoteWake.slnx --configuration Release --no-build --no-restore
python -m unittest discover -s android/termux-bootstrap/tests -v
```

Os gates de cobertura são executados separadamente pelo pipeline. A documentação normativa e a ordem de leitura estão em `docs/README.md`.

## Fluxo reduzido do M1

Para Android com Termux, Termux:Boot e Tailscale já ativos, o bootstrap agora registra chave, porta SSH isolada e PC alvo em uma execução. O [guia do bridge](android/termux-bootstrap/README.md) cobre essa etapa; o [harness assistido](tools/RemoteWake.M1.Harness/README.md) protege a chave no Windows e oferece os comandos únicos de verificação e ativação.

## Segurança dos marcos M0–M1

Os projetos de produção não possuem dependências NuGet externas. Processos usam caminho absoluto, `ArgumentList`, timeout e saída limitada, sempre sem shell. O SSH exige host pinning, ignora configurações externas e desativa senha, agente, PTY e forwardings. Testes nunca enviam Magic Packet à LAN real. O estado detalhado do gate está em `docs/26-evidencias-m1.md`.
