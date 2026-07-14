# Dependências e auditoria

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

## Política do M0

Domain e Application não possuem dependência NuGet externa. As dependências abaixo existem somente nos projetos de teste, têm versão centralizada e são fixadas também pelos arquivos `packages.lock.json`.

| Dependência | Versão | Finalidade | Licença | Escopo |
| --- | --- | --- | --- | --- |
| [MSTest](https://www.nuget.org/packages/MSTest/4.3.0) | 4.3.0 | Framework e adapter de testes mantidos pela Microsoft | MIT | Testes |
| [Microsoft.NET.Test.Sdk](https://www.nuget.org/packages/Microsoft.NET.Test.Sdk/18.7.0) | 18.7.0 | Descoberta e execução pela plataforma de testes .NET | MIT | Testes |
| [coverlet.msbuild](https://www.nuget.org/packages/coverlet.msbuild/10.0.1) | 10.0.1 | Medição e gate de cobertura no MSBuild | MIT | Testes/CI |

O pipeline utiliza `actions/checkout` e `actions/setup-dotnet` sob licença MIT, fixados pelo SHA completo correspondente ao tag v4 consultado em 14/07/2026. Não há download ou execução de ação referenciada somente por tag mutável.

## Justificativa

MSTest e Microsoft.NET.Test.Sdk fornecem o nível mínimo para testes suportados no ecossistema .NET. Coverlet aplica os limites mensuráveis de RNF018 sem adicionar dependência ao produto. Nenhuma biblioteca de arquitetura foi adicionada: os limites são testados por reflexão para reduzir a superfície de dependências.

## Verificação

Em 14/07/2026, `dotnet list RemoteWake.slnx package --vulnerable --include-transitive` e `--deprecated --include-transitive` não encontraram pacote vulnerável ou preterido nas fontes configuradas. O pipeline repete a auditoria de vulnerabilidades. Nova dependência exige atualização deste documento, justificativa, licença, lockfile e nova auditoria.

