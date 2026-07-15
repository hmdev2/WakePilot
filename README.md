# WakePilot

Fundação do Remote Wake Assistant, um produto local-first para ligar um PC Windows por um Android bridge e abrir o RustDesk somente quando Windows e serviço remoto estiverem prontos.

## Estado

O M0 está integrado em `main`. O M1 concluiu a prova física Tailscale/OpenSSH → Android → Wake-on-LAN em S3 e S5 no laboratório autorizado, incluindo reboot do Android, Doze, operação observada por mais de uma semana e negativos de identidade, indisponibilidade e rate limit. O M2 está em andamento na branch `milestone/m2-launcher`: os cortes atuais incluem dashboard WPF com atualização somente leitura, progresso, cancelamento, sucesso, falhas acionáveis e abertura segura do preset RustDesk. A matriz ampliada de hardware permanece no M6.

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

## Fluxo validado do M1

Para Android com Termux, Termux:Boot e Tailscale já ativos, o bootstrap agora registra chave, porta SSH isolada e PC alvo em uma execução. O [guia do bridge](android/termux-bootstrap/README.md) cobre essa etapa; o [harness assistido](tools/RemoteWake.M1.Harness/README.md) protege a chave no Windows e oferece os comandos únicos de verificação e ativação.

## Segurança dos marcos M0–M1

Os projetos de produção não possuem dependências NuGet externas. Processos usam caminho absoluto, `ArgumentList`, timeout e saída limitada, sempre sem shell. O SSH verifica previamente a chave Ed25519 apresentada, mantém o host pinning do OpenSSH, ignora configurações externas e desativa senha, agente, PTY e forwardings. Testes automatizados nunca enviam Magic Packet à LAN real. O estado detalhado do gate está em `docs/26-evidencias-m1.md`.

## Launcher M2 em desenvolvimento

O launcher de produção abre em estado não configurado e não executa ações externas. Para revisar somente a experiência com dependências simuladas, execute o projeto em configuração `Debug` ou use `--demo`; o cabeçalho identifica explicitamente a demonstração. Esse modo não acessa Tailscale, Android, PC alvo nem RustDesk real. O escopo entregue e as pendências do marco estão em `docs/27-status-m2.md`.
