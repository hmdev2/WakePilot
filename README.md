# WakePilot

Fundação do Remote Wake Assistant, um produto local-first para ligar um PC Windows por um Android bridge e abrir o RustDesk somente quando Windows e serviço remoto estiverem prontos.

## Estado

O repositório está no marco M0. Existem apenas o domínio, os contratos de aplicação, a máquina de estados, um orquestrador exercitado por fakes e os testes de qualidade. Não há integração real com Tailscale, SSH, Android, Wake-on-LAN, RustDesk, UAC ou hardware.

## Pré-requisito

.NET SDK 10.0.204 ou patch estável compatível, conforme `global.json`.

## Verificação local

```powershell
dotnet restore RemoteWake.slnx --locked-mode
dotnet format RemoteWake.slnx --verify-no-changes --no-restore
dotnet build RemoteWake.slnx --configuration Release --no-restore
dotnet test RemoteWake.slnx --configuration Release --no-build --no-restore
```

Os gates de cobertura são executados separadamente pelo pipeline. A documentação normativa e a ordem de leitura estão em `docs/README.md`.

## Segurança do M0

Os projetos de produção não possuem dependências NuGet externas nem APIs concretas de processo, rede, persistência ou Windows. Todos os efeitos externos são ports e os testes usam somente fakes em memória.
