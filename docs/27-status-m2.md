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
* Notificação nativa do Windows recebe somente eventos tipados de sucesso/ação necessária, agrupa duplicatas por 10 segundos e mantém a tela in-app como fallback quando indisponível.
* Recursos pt-BR, ordem de teclado explícita, foco visível, títulos de nível 1, regiões vivas moderadas e nomes acessíveis para estados e ações.
* Cancelamento ligado ao `CancellationToken`, sem novo retry após a solicitação.
* Adapter RustDesk com caminho absoluto/canônico, pin SHA-256 revalidado, `UseShellExecute=false`, `ArgumentList` vazio e falha tipada ERR016.
* Modo de demonstração exclusivo de `Debug` ou argumento `--demo`, identificado na tela e composto somente por fakes.

## Evidência executada

| Evidência | Resultado |
| --- | --- |
| Testes unitários do launcher | 17 aprovados; fluxo, notificação, métricas locais e contratos de acessibilidade/localização incluídos |
| Testes de contrato RemoteApps | 7 aprovados; caminho/hash, ausência, serviço divergente, falha de start, processo sem shell e 30 aberturas simuladas |
| Testes de Application | 21 aprovados; progresso, correlation ID e atualização PC/bridge independente incluídos |
| Medição local RNF002 | 100 amostras: criação do painel p95 0,0034 ms e decisão com fake p95 0,0010 ms; rede saudável não faz parte desta medição |
| Medição local RNF003 | comando de cancelar em 0,07 ms e encerramento da operação em 1,36 ms; carga real de sondas permanece pendente |
| Medição local RNF004 | adapter allowlisted com processo fake p95 0,34 ms em 30 amostras; inicialização real do RustDesk permanece pendente |
| Revisão visual WPF | painel, progresso e sucesso abertos no Windows após a composição nativa; o sucesso in-app permaneceu visível sem regressão |
| Acessibilidade automatizada | pares essenciais ≥4,5:1, texto comum em recursos, botões nomeados, ordem explícita, headings e regiões vivas aprovados |
| Acessibilidade observada | foco no título do painel → primeiro botão; mudanças para progresso/sucesso focam os respectivos títulos; Tab seguinte alcança a ação esperada |
| Hardware/rede durante testes | nenhuma ação; modo demo não acessa Tailscale, Android, PC alvo ou RustDesk |

## Cobertura parcial

* CT014: adapter e fronteira de processo automatizados; instalação RustDesk real ainda não foi acionada.
* CT015: estado pronto exige a sonda de Windows e serviço; falha nunca é promovida a pronto.
* CT016: VPN desconectada, bridge indisponível e identidade divergente possuem estados e códigos distintos.
* CT019: progresso e cancelamento automatizados; medição formal de cancelamento em até 2 s permanece no gate.
* CT021: abertura segura provada com fake de processo; p95 e instalação real permanecem pendentes.
* CT022: mapeamento acionável e sanitização da tela comum automatizados.
* CT023: adapter nativo, evento tipado sem dado operacional, coalescência de duplicatas e fallback in-app automatizados; a política de notificação desabilitada será conferida no ambiente homologado.
* CT043: contraste, nomes, headings, regiões vivas, foco por tela e teclado foram automatizados/observados; leitor de tela dedicado e zoom 200% permanecem pendentes.
* CT052: todo texto comum do XAML usa recurso/binding e referências foram validadas; pseudo-localização visual permanece pendente.
* RNF002: orçamento da camada de apresentação aprovado em 100 amostras locais; decisão p95 em rede saudável permanece pendente.
* RNF003: retorno do cancelamento e encerramento efetivo aprovados abaixo de 100 ms/2 s; responsividade sob carga real permanece pendente.
* RNF004: overhead do adapter aprovado em 30 amostras com processo fake; p95 do RustDesk real permanece pendente.
* RNF005: confiabilidade de 30 execuções continua reservada ao ambiente homologado, sem substituição por fakes.
* RNF006: timeouts normativos e expiração de Windows/serviço estão automatizados com relógio controlado; adapters reais ainda dependem da homologação.

## Pendências para o gate M2

1. Compor o launcher com o perfil local persistido e os adapters reais já aprovados no M1.
2. Integrar a prontidão autenticada quando o M3 disponibilizar o ReadinessAgent; até lá, o modo comum real permanece bloqueado para não confundir ping com prontidão.
3. Homologar RNF001–RNF006 no ambiente real: usabilidade, decisão em rede saudável, responsividade sob carga, RustDesk p95 e confiabilidade de 30 execuções.
4. Executar checklist completo de acessibilidade, a política de notificação desabilitada e os cenários de falha TEL018.

## Segurança e operação

O executável Release não recebe perfil fake automaticamente. Sem perfil operacional, o botão fica desabilitado e explica a pendência. Nenhuma credencial ou endereço do laboratório foi incorporado ao launcher, aos recursos ou aos testes. Nenhuma ação de energia faz parte deste corte.
