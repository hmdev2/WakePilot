# WakePilot

Fundação do Remote Wake Assistant, um produto local-first para ligar um PC Windows por um Android bridge e abrir o RustDesk somente quando Windows e serviço remoto estiverem prontos.

## Estado

O M0 está integrado em `main`. A branch `milestone/m1-vertical-proof` contém os adapters Tailscale/OpenSSH, protocolo bridge v1, bootstrap Termux e Magic Packet sender. A parte automatizável do M1 está implementada; o marco permanece aberto até validar DPAPI e a cadeia em Android/PC de laboratório autorizados. O M2 não deve começar antes desse gate.

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

## Segurança dos marcos M0–M1

Os projetos de produção não possuem dependências NuGet externas. Processos usam caminho absoluto, `ArgumentList`, timeout e saída limitada, sempre sem shell. O SSH exige host pinning, ignora configurações externas e desativa senha, agente, PTY e forwardings. Testes nunca enviam Magic Packet à LAN real. O estado detalhado do gate está em `docs/26-evidencias-m1.md`.
