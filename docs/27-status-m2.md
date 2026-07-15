# Estado do M2 — Launcher mínimo

## Controle

Versão 0.1.0 — Estado: Em elaboração — Data: 15/07/2026.

## Decisão do marco

O M2 está em andamento e ainda não passou pelo gate final. Este documento registra o primeiro corte executável sem antecipar a conclusão dos critérios CT015–CT023 e RNF001–RNF006.

## Entregue neste corte

* Projeto WPF .NET 10 com dashboard, progresso por fases, cancelamento, sucesso e falha acionável conforme TEL012, TEL014, TEL015 e TEL018.
* Atualização somente leitura e concorrente dos estados do computador e bridge, sem enviar wake; o serviço remoto só é consultado depois de Windows pronto.
* Canal de progresso neutro em `WakeOrchestrator`, preservando Application sem dependência de WPF.
* Linha do tempo textual Solicitação → Computador → Windows → RustDesk, sem porcentagem inventada.
* Códigos ERR008–ERR016 e ERR021 traduzidos para título, consequência e ação; causa técnica não é exibida na tela comum.
* Correlation ID copiável em detalhes recolhidos.
* Fronteira de notificação recebe somente eventos tipados de sucesso/ação necessária; indisponibilidade mantém a tela in-app como fallback.
* Recursos pt-BR, ordem de teclado, foco visível e nomes acessíveis para os estados e a ação principal.
* Cancelamento ligado ao `CancellationToken`, sem novo retry após a solicitação.
* Adapter RustDesk com caminho absoluto/canônico, pin SHA-256 revalidado, `UseShellExecute=false`, `ArgumentList` vazio e falha tipada ERR016.
* Modo de demonstração exclusivo de `Debug` ou argumento `--demo`, identificado na tela e composto somente por fakes.

## Evidência executada

| Evidência | Resultado |
| --- | --- |
| Testes unitários do launcher | 6 aprovados; painel bloqueado, atualização independente, sucesso, falha sanitizada, cancelamento e identidade divergente |
| Testes de contrato RemoteApps | 6 aprovados; caminho/hash, ausência, serviço divergente, falha de start e processo sem shell |
| Testes de Application | 21 aprovados; progresso, correlation ID e atualização PC/bridge independente incluídos |
| Revisão visual WPF | painel, progresso e sucesso abertos no Windows; vínculo somente leitura corrigido após falha observada |
| Acessibilidade observada | nomes acessíveis encontrados para computador, celular e botão “Ligar e conectar” |
| Hardware/rede durante testes | nenhuma ação; modo demo não acessa Tailscale, Android, PC alvo ou RustDesk |

## Cobertura parcial

* CT014: adapter e fronteira de processo automatizados; instalação RustDesk real ainda não foi acionada.
* CT015: estado pronto exige a sonda de Windows e serviço; falha nunca é promovida a pronto.
* CT016: VPN desconectada, bridge indisponível e identidade divergente possuem estados e códigos distintos.
* CT019: progresso e cancelamento automatizados; medição formal de cancelamento em até 2 s permanece no gate.
* CT021: abertura segura provada com fake de processo; p95 e instalação real permanecem pendentes.
* CT022: mapeamento acionável e sanitização da tela comum automatizados.
* CT023: evento tipado, sem dado operacional, e fallback in-app automatizados; o adapter nativo do Windows permanece pendente.
* CT043 e CT052: recursos, nomes e teclado iniciados; leitor de tela, zoom 200%, contraste medido e pseudo-localização permanecem pendentes.

## Pendências para o gate M2

1. Compor o launcher com o perfil local persistido e os adapters reais já aprovados no M1.
2. Integrar a prontidão autenticada quando o M3 disponibilizar o ReadinessAgent; até lá, o modo comum real permanece bloqueado para não confundir ping com prontidão.
3. Adicionar o adapter nativo de notificação do Windows e medir coalescência; o fallback in-app já está ativo.
4. Medir RNF001–RNF006, incluindo decisão inicial, responsividade, abertura p95 e 30 execuções no ambiente homologado.
5. Executar checklist completo de acessibilidade e os cenários de falha TEL018.

## Segurança e operação

O executável Release não recebe perfil fake automaticamente. Sem perfil operacional, o botão fica desabilitado e explica a pendência. Nenhuma credencial ou endereço do laboratório foi incorporado ao launcher, aos recursos ou aos testes. Nenhuma ação de energia faz parte deste corte.
