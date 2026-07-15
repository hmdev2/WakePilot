# Dependências e auditoria

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

## Política dos marcos M0–M1

Domain e Application não possuem dependência NuGet externa. As dependências abaixo existem somente nos projetos de teste, têm versão centralizada e são fixadas também pelos arquivos `packages.lock.json`.

| Dependência | Versão | Finalidade | Licença | Escopo |
| --- | --- | --- | --- | --- |
| [MSTest](https://www.nuget.org/packages/MSTest/4.3.0) | 4.3.0 | Framework e adapter de testes mantidos pela Microsoft | MIT | Testes |
| [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.7.0) | 18.7.0 | Descoberta e execução pela plataforma de testes .NET | MIT | Testes |
| [coverlet.msbuild](https://www.nuget.org/packages/coverlet.msbuild/10.0.1) | 10.0.1 | Medição e gate de cobertura no MSBuild | MIT | Testes/CI |

O pipeline utiliza `actions/checkout`, `actions/setup-dotnet` e `actions/setup-python` sob licença MIT, todos fixados por SHA completo. `setup-python` está fixado em v6.2.0 (`a309ff8b426b58ec0e2a45f0f869d46889d02405`) e fornece Python 3.12 apenas para os testes do wrapper Termux, sem pacote pip. Não há download ou execução de ação referenciada somente por tag mutável.

## Justificativa

MSTest e Microsoft.NET.Test.Sdk fornecem o nível mínimo para testes suportados no ecossistema .NET. Coverlet aplica os limites mensuráveis de RNF018 sem adicionar dependência ao produto. O M1 usa somente bibliotecas padrão do .NET e do Python; OpenSSH e Tailscale são processos externos atrás de adapters tipados. Nenhuma biblioteca de arquitetura ou SSH foi adicionada, reduzindo a superfície de supply chain.

## Verificação

Em 14/07/2026, `dotnet list RemoteWake.slnx package --vulnerable --include-transitive` e `--deprecated --include-transitive` não encontraram pacote vulnerável ou preterido nas fontes configuradas. O pipeline repete a auditoria de vulnerabilidades. Nova dependência exige atualização deste documento, justificativa, licença, lockfile e nova auditoria.
