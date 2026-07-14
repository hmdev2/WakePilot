# Briefing do produto — Remote Wake Assistant

## Controle do documento

| Campo | Valor |
| --- | --- |
| Documento | `docs/00-briefing.md` |
| Produto | Remote Wake Assistant — nome provisório |
| Versão | 1.1.0 |
| Estado | Aprovado com ajustes pontuais |
| Data de elaboração | 14 de julho de 2026 |
| Responsável pela elaboração | Equipe de análise e documentação |
| Próxima revisão | Quando houver mudança material de escopo |

## 1. Finalidade

Este documento consolida o contexto inicial, o problema, a oportunidade, a solução imaginada, o público, os cenários de uso, as restrições, as premissas, as dúvidas e o glossário preliminar do Remote Wake Assistant.

O briefing constitui a fonte inicial para a elaboração progressiva dos documentos de visão do produto, personas, escopo do MVP, requisitos funcionais, requisitos não funcionais e regras de negócio. Ele não estabelece decisões arquiteturais definitivas nem autoriza a implementação de código de produção.

## 2. Resumo executivo

O Remote Wake Assistant deverá simplificar a ativação remota de um computador Windows localizado em outra rede e, em seguida, facilitar a abertura do aplicativo de acesso remoto configurado. O produto deverá esconder do usuário comum os detalhes técnicos de Wake-on-LAN, endereços MAC e IP, VPN, SSH, chaves de autenticação, comandos e terminais.

A hipótese inicial utiliza um celular Android permanentemente ligado à rede local do computador como ponte para o envio de um Magic Packet. Um launcher no notebook remoto deverá verificar o estado dos dispositivos, solicitar a ativação do computador, acompanhar a disponibilidade do Windows e do serviço de acesso remoto e abrir automaticamente o aplicativo selecionado.

A solução deverá incluir configuração visual, diagnóstico, pareamento seguro, teste de ponta a ponta, tratamento de falhas, restauração de alterações e suporte técnico. A compatibilidade com Wake-on-LAN somente poderá ser declarada como validada após um teste real de ativação no equipamento configurado.

## 3. Problema atual

### 3.1 Situação observada

Ligar remotamente um computador localizado em outra rede é tecnicamente possível, porém a configuração e a operação atuais exigem conhecimentos que não são razoáveis para um usuário comum. O procedimento pode envolver:

* identificação manual do endereço MAC do adaptador de rede;
* verificação e alteração de configurações do Windows, do driver e da BIOS/UEFI;
* instalação e autenticação em uma VPN privada;
* preparação de um dispositivo que permaneça conectado à rede local;
* instalação e configuração do Termux, Termux:Boot e OpenSSH;
* criação, distribuição, validação, rotação e revogação de chaves;
* abertura de terminal e execução de comandos;
* estabelecimento manual de uma conexão SSH;
* envio manual do Magic Packet;
* espera sem indicação clara sobre o estado da inicialização;
* abertura separada do aplicativo de acesso remoto;
* diagnóstico fragmentado quando alguma etapa falha.

### 3.2 Consequências

O procedimento atual apresenta as seguintes consequências:

* elevada barreira de entrada para pessoas sem conhecimento de redes e sistemas;
* risco de configuração incorreta ou insegura;
* dependência recorrente de uma pessoa técnica;
* dificuldade para identificar em qual componente ocorreu uma falha;
* possibilidade de exposição de shell remoto, chaves ou credenciais;
* baixa previsibilidade da experiência, pois ligar o computador não significa que o Windows e o serviço remoto já estejam disponíveis;
* risco de afirmar compatibilidade com base apenas em propriedades do driver, sem validação real;
* dificuldade para restaurar o estado anterior da máquina.

### 3.3 Formulação do problema

Uma pessoa comum precisa ligar e acessar remotamente um computador Windows sem conhecer ou operar os mecanismos técnicos necessários para Wake-on-LAN, conectividade privada, autenticação e acompanhamento da inicialização, enquanto o técnico precisa de um fluxo configurável, seguro, diagnosticável e reversível.

## 4. Oportunidade

Existe oportunidade de transformar um procedimento composto por várias ferramentas e comandos em uma experiência assistida, local-first e orientada a estados. O produto poderá:

* reduzir a operação cotidiana a uma ação principal: “Ligar e conectar”;
* centralizar diagnóstico, configuração, pareamento, ativação e suporte;
* traduzir estados técnicos em mensagens compreensíveis;
* separar a experiência do usuário comum das funções técnicas;
* aplicar controles de segurança por padrão;
* diminuir erros de configuração e solicitações de suporte;
* fornecer evidência verificável de compatibilidade por meio de teste real;
* permitir integração progressiva com diferentes aplicativos de acesso remoto;
* preservar a possibilidade de evolução do componente Android sem descartar o MVP.

## 5. Solução imaginada

### 5.1 Conceito

O produto imaginado é um conjunto coordenado de aplicações e componentes locais que auxilia a configuração e permite a ativação remota de um computador Windows. A experiência principal deverá ser visual e não deverá exigir terminal do usuário comum.

### 5.2 Hipótese inicial de funcionamento

```text
Notebook remoto → rede privada → celular Android → rede local → Magic Packet → computador Windows
```

Após a ativação, o notebook remoto deverá acompanhar a disponibilidade do computador e do serviço de acesso remoto antes de abrir RustDesk, AnyDesk, Moonlight ou outro aplicativo configurado.

### 5.3 Componentes conceituais iniciais

Os componentes abaixo representam hipóteses a serem avaliadas nos documentos de arquitetura e ADRs:

1. configurador visual do computador principal;
2. componente Windows com privilégios limitados às operações que realmente os exigirem;
3. launcher instalado no notebook remoto;
4. componente intermediário executado no Android;
5. mecanismo de rede privada entre os dispositivos;
6. mecanismo autenticado para solicitar o envio do Magic Packet;
7. adaptadores para Wake-on-LAN e aplicativos de acesso remoto;
8. armazenamento local de configurações e referências seguras para segredos;
9. logs, diagnóstico, restauração, desinstalação e atualização.

### 5.4 Experiência operacional desejada

1. O usuário abre o launcher.
2. O launcher identifica o computador configurado e verifica seu estado.
3. Se o computador estiver pronto para acesso, o launcher abre o aplicativo remoto.
4. Se o computador estiver offline, o launcher verifica a disponibilidade do celular intermediário e da comunicação privada.
5. O launcher envia ao celular uma solicitação autenticada e autorizada de ativação.
6. O celular envia o Magic Packet na rede local.
7. O launcher acompanha separadamente a disponibilidade de rede do computador, a inicialização do Windows e a disponibilidade do serviço remoto.
8. O launcher comunica progresso, sucesso ou falha em linguagem compreensível.
9. Quando o serviço estiver pronto, o launcher abre o aplicativo remoto configurado.

### 5.5 Experiência de configuração desejada

O técnico instalador ou usuário avançado deverá utilizar um configurador visual capaz de:

* inventariar Windows, hardware e adaptadores de rede;
* distinguir adaptadores físicos, virtuais, Ethernet, Wi-Fi e VPN;
* identificar automaticamente o endereço MAC relevante;
* levantar evidências de suporte a Wake-on-LAN sem confundi-las com validação definitiva;
* verificar configurações de energia e autorização de ativação;
* identificar o estado da Inicialização Rápida;
* orientar configurações não automatizáveis de BIOS/UEFI;
* realizar alterações administrativas somente após consentimento explícito;
* instalar ou orientar a instalação da rede privada;
* preparar e parear o celular intermediário e o notebook;
* configurar o aplicativo de acesso remoto;
* executar teste de ponta a ponta;
* registrar o estado anterior para restauração;
* exportar diagnóstico sem expor segredos.

### 5.6 Princípios preliminares da solução

* local-first;
* experiência sem terminal para o usuário comum;
* segurança por padrão e menor privilégio;
* autenticação e autorização próprias, mesmo sobre uma VPN;
* compatibilidade declarada conforme evidência disponível;
* validação por teste real;
* diagnóstico orientado à etapa que falhou;
* alterações administrativas explícitas, mínimas e reversíveis;
* separação entre interface normal e operações privilegiadas;
* extensibilidade por adaptadores de integração;
* ausência de backend em nuvem enquanto sua necessidade não for demonstrada.

## 6. Público e partes interessadas

### 6.1 Públicos principais

| Perfil | Relação com o produto | Necessidade central |
| --- | --- | --- |
| Usuário comum | Opera o launcher para ligar e acessar o computador | Concluir a operação com uma ação simples e mensagens compreensíveis |
| Técnico instalador | Realiza instalação, configuração, validação e manutenção | Dispor de diagnóstico confiável, configuração guiada e rollback |
| Usuário avançado | Ajusta opções técnicas adicionais dentro de limites seguros | Obter controle adicional sem depender de edição manual de arquivos |
| Suporte | Analisa falhas e orienta a recuperação | Receber logs correlacionados, códigos de erro e relatórios sanitizados |

### 6.2 Partes interessadas adicionais

As partes abaixo poderão influenciar o produto, mas ainda não constituem personas aprovadas:

* proprietário ou administrador dos dispositivos;
* mantenedor individual do software;
* fornecedores de Windows, Android, VPN, Termux e aplicativos remotos;
* fabricantes do computador, placa-mãe, adaptador de rede e celular.

A criação de novos perfis dependerá de necessidade comprovada durante a análise de personas.

## 7. Cenários de uso

### 7.1 Cenário principal — computador desligado e infraestrutura disponível

O usuário está fora da rede do computador principal. O celular intermediário permanece ligado e conectado à rede local. O usuário abre o launcher, seleciona “Ligar e conectar” e acompanha o progresso. O celular recebe uma solicitação válida, envia o Magic Packet e o computador inicia. O launcher aguarda o serviço remoto ficar disponível e abre o aplicativo configurado.

Resultado esperado: o usuário acessa o computador sem abrir terminal, informar MAC ou interpretar detalhes da rede.

### 7.2 Computador já disponível

O launcher detecta que o computador e o serviço remoto já estão disponíveis. Nenhum Magic Packet é enviado. O aplicativo remoto é aberto diretamente.

### 7.3 Celular intermediário offline

O computador está offline e o launcher não alcança o celular. O sistema não tenta uma ativação que dependa do celular e informa a causa provável, as verificações possíveis e a ação recomendada.

### 7.4 Rede privada indisponível

O launcher identifica que a comunicação privada necessária não está operacional. O sistema diferencia essa condição de celular offline, falha de Wake-on-LAN e falha do serviço remoto.

### 7.5 Magic Packet enviado sem ativação confirmada

O celular confirma o processamento da solicitação, mas o computador não se torna disponível dentro do período definido. O sistema registra a tentativa, aplica a política limitada de repetição e informa que a ativação não foi confirmada. O diagnóstico deverá considerar energia da placa de rede, BIOS/UEFI, driver, estado de energia, firewall e conectividade.

### 7.6 Computador liga, mas o Windows não fica pronto

O hardware aparenta iniciar, porém o sistema operacional ou a rede não fica disponível. O produto deverá encerrar a espera após timeout e distinguir essa situação de ausência total de ativação sempre que houver evidência suficiente.

### 7.7 Windows disponível e serviço remoto indisponível

O computador responde pela rede, mas RustDesk, AnyDesk, Sunshine, Área de Trabalho Remota ou outro serviço configurado não está pronto. O launcher não deverá abrir prematuramente o cliente remoto como se a conexão estivesse disponível.

### 7.8 Configuração e validação inicial

O técnico executa o configurador no computador principal, revisa o diagnóstico, autoriza alterações administrativas específicas, segue orientação de BIOS/UEFI, prepara o celular e o notebook, realiza o pareamento e conduz um teste de ponta a ponta. Somente o teste bem-sucedido permite classificar o equipamento como “compatível e validado por teste”.

### 7.9 Revogação ou perda de dispositivo

Um notebook ou celular anteriormente autorizado deixa de ser confiável. O técnico ou usuário autorizado revoga seu acesso. Solicitações posteriores do dispositivo revogado são rejeitadas e auditadas.

### 7.10 Restauração e desinstalação

O técnico solicita restauração ou desinstalação. O sistema informa as alterações que serão revertidas, preserva somente os dados que devam permanecer por justificativa explícita e remove acessos, tarefas, serviços e segredos associados de forma segura.

## 8. Escopo preliminar do problema

### 8.1 Capacidades candidatas ao MVP

As capacidades abaixo são candidatas e deverão ser confirmadas em `03-escopo-mvp.md`:

* um fluxo completo Notebook → rede privada → celular → Magic Packet → computador;
* configuração assistida para Windows 10 e Windows 11;
* computador principal conectado por Ethernet;
* um celular Android como intermediário;
* launcher visual em notebook Windows;
* identificação automática do adaptador e do endereço MAC;
* diagnóstico com classificação de compatibilidade;
* pareamento autenticado;
* envio restrito de solicitação de ativação;
* acompanhamento de estados e timeouts;
* integração inicial com uma quantidade limitada de aplicativos remotos;
* teste real de ponta a ponta;
* logs e exportação de diagnóstico sanitizado;
* restauração das alterações realizadas pelo configurador.

### 8.2 Fora da definição atual

Os itens abaixo não estão aprovados para o MVP:

* backend próprio em nuvem;
* suporte geral a todos os sistemas operacionais;
* garantia de Wake-on-LAN por Wi-Fi;
* alteração automática e universal de BIOS/UEFI;
* administração remota irrestrita do celular;
* shell remoto de uso geral;
* substituição dos aplicativos de acesso remoto;
* suporte simultâneo ilimitado a computadores, celulares e usuários;
* automação de hardware incompatível com Wake-on-LAN;
* publicação imediata em lojas de aplicativos.

## 9. Restrições

### 9.1 Restrições funcionais e de experiência

* O usuário comum não deverá abrir terminal, executar comandos ou editar arquivos de configuração.
* O usuário comum não deverá conhecer ou informar MAC, IP interno, porta, comando SSH ou detalhes da VPN.
* A interface deverá traduzir falhas técnicas em estados, causas prováveis e ações compreensíveis.
* Informações técnicas deverão permanecer ocultas por padrão e acessíveis somente em contexto apropriado.
* O sistema não deverá abrir o aplicativo remoto antes de verificar a disponibilidade do serviço correspondente.

### 9.2 Restrições técnicas

* O escopo inicial considera computadores com Windows 10 ou Windows 11.
* A hipótese inicial depende de adaptador Ethernet energizado e configurado para Wake-on-LAN.
* BIOS/UEFI normalmente exige orientação e interação humana; seus nomes e recursos variam por fabricante e modelo.
* Propriedades expostas por driver constituem evidência de diagnóstico, não prova definitiva de compatibilidade.
* Estados de energia S3, S4 e S5, Inicialização Rápida, firmware, driver e fornecimento de energia ao adaptador podem alterar o resultado.
* O comportamento real deverá ser validado no hardware-alvo.
* O Android impõe limites a trabalho e rede em segundo plano; fabricantes podem aplicar políticas adicionais de bateria.
* Termux:Boot requer instalação compatível, uma primeira abertura do aplicativo e configuração específica para executar scripts no boot.
* A autenticação da VPN poderá exigir navegador ou interação do usuário.
* Endereços de rede local poderão mudar; a solução não deverá depender de entrada manual recorrente.
* VPN, celular, Windows e serviços remotos poderão iniciar ou reconectar em tempos diferentes.
* Firewall, antivírus, políticas corporativas e atualizações do sistema poderão interferir.

### 9.3 Restrições de segurança e privacidade

* A VPN não substitui autenticação e autorização entre componentes.
* Não deverão existir credenciais fixas no código, no repositório ou em arquivos de configuração sem proteção.
* Chaves privadas e tokens não deverão ser armazenados diretamente no banco de dados ou nos logs.
* A identidade do host SSH deverá ser verificada; hosts desconhecidos não poderão ser aceitos silenciosamente.
* O componente Android não deverá oferecer shell remoto irrestrito.
* O comando autorizado deverá ter escopo mínimo e argumentos validados.
* Cada dispositivo deverá possuir credencial própria, revogável e rotacionável.
* Solicitações deverão ser protegidas contra repetição e abuso de frequência.
* Logs deverão mascarar ou omitir segredos e dados desnecessários.
* Operações administrativas deverão ser isoladas e executadas somente após consentimento explícito.
* Executáveis externos e argumentos deverão ser validados antes da execução.
* Configurações e pacotes deverão possuir mecanismos de integridade, atualização segura e rollback.

### 9.4 Restrições de projeto

* A primeira versão deverá permanecer viável para desenvolvimento e manutenção por uma equipe pequena ou desenvolvedor individual.
* A documentação deverá preceder a implementação e registrar decisões relevantes.
* Tecnologias de desktop e Android ainda deverão ser comparadas formalmente.
* O produto deverá preferir arquitetura local-first.
* Código de produção não deverá ser implementado durante a fase atual de documentação.

## 10. Classificação preliminar de compatibilidade

| Classificação | Significado preliminar |
| --- | --- |
| Compatível e validado por teste | A configuração foi concluída e um teste real de ativação de ponta a ponta obteve sucesso no equipamento |
| Provavelmente compatível | Existem evidências favoráveis no hardware, driver e sistema, mas falta teste real bem-sucedido |
| Incompatível | Existe impedimento confirmado e documentado para o cenário avaliado |
| Diagnóstico inconclusivo | As evidências disponíveis são insuficientes ou contraditórias |
| Configuração incompleta | Uma ou mais etapas obrigatórias ainda não foram concluídas |

As condições exatas de transição entre classificações serão formalizadas em requisitos, regras de negócio, processos e plano de testes.

## 11. Premissas resolvidas

As premissas desta seção foram usadas para avançar a análise e posteriormente resolvidas em `03-escopo-mvp.md`, `08-arquitetura.md` e ADR-001–ADR-010. Os riscos derivados permanecem em `20-riscos-e-contingencias.md`.

| Código | Premissa original | Motivo para prosseguir | Resolução documental |
| --- | --- | --- | --- |
| PP-001 | [PREMISSA RESOLVIDA] O MVP terá um computador principal, um celular intermediário e um notebook autorizado por instalação. | Limita a complexidade inicial e permite validar o fluxo essencial. | Confirmar em `03-escopo-mvp.md`. |
| PP-002 | [PREMISSA RESOLVIDA] O computador principal utilizará Ethernet cabeada. | É o cenário inicial informado e apresenta maior aderência ao Wake-on-LAN tradicional. | Confirmar público-alvo e matriz de hardware. |
| PP-003 | [PREMISSA RESOLVIDA] O notebook remoto utilizará Windows 10 ou Windows 11. | Permite compartilhar tecnologia e práticas de distribuição do configurador. | Confirmar os dispositivos reais de uso. |
| PP-004 | [PREMISSA RESOLVIDA] Tailscale será a referência inicial para a prova de conceito da rede privada. | A hipótese original já considera Tailscale e endereçamento estável por dispositivo. | Comparar com alternativas e registrar ADR. |
| PP-005 | [PREMISSA RESOLVIDA] Termux, Termux:Boot e OpenSSH serão considerados para a primeira prova técnica, sem compromisso de permanência no produto final. | Reduz o custo de validar o caminho crítico antes de criar aplicativo Android nativo. | Executar prova em Android real e comparar com serviço nativo. |
| PP-006 | [PREMISSA RESOLVIDA] O MVP integrará inicialmente apenas um aplicativo de acesso remoto, mantendo abstração para outros. | Evita ampliar prematuramente escopo e testes. | Selecionar o primeiro aplicativo no escopo e registrar ADR. |
| PP-007 | [PREMISSA RESOLVIDA] O produto funcionará sem backend próprio em nuvem. | O fluxo pode ser atendido inicialmente por componentes locais e rede privada. | Demonstrar se pareamento, atualização ou suporte exigem serviço adicional. |
| PP-008 | [PREMISSA RESOLVIDA] O técnico terá acesso físico ao computador e ao celular durante a configuração inicial. | BIOS/UEFI, permissões Android e autenticação podem exigir interação local. | Confirmar o modelo operacional de instalação. |
| PP-009 | [PREMISSA RESOLVIDA] O celular permanecerá alimentado, conectado ao Wi-Fi local e disponível para executar o componente intermediário. | O celular é a ponte da hipótese inicial. | Definir requisitos operacionais, monitoramento e contingência. |
| PP-010 | [PREMISSA RESOLVIDA] O produto será inicialmente distribuído em português do Brasil. | Corresponde ao público inicial conhecido e reduz o escopo do MVP. | Confirmar necessidade de outro idioma no lançamento. |

## 12. Dúvidas originais e encaminhamento

As dúvidas abaixo não bloqueiam a elaboração inicial. Cada uma deverá ser respondida no documento indicado antes de afetar implementação.

> Situação consolidada: DP-002–DP-015 foram resolvidas em `03-escopo-mvp.md`, `08-arquitetura.md` e ADR-001–ADR-010. DP-001 permanece apenas como decisão de identidade comercial e não bloqueia implementação. O protocolo detalhado do ReadinessAgent foi resolvido pelo ADR-010; RSK021 passou a risco de conformidade da implementação e está mitigado.

| Código | Questão | Impacto | Documento responsável |
| --- | --- | --- | --- |
| DP-001 | [PENDENTE DE DECISÃO] Qual nome definitivo será adotado para o produto? | Identidade, instaladores e documentação. | `01-visao-do-produto.md` ou decisão posterior de produto. |
| DP-002 | [DECISÃO RESOLVIDA] Quais edições, versões mínimas e arquiteturas de Windows 10 e 11 serão suportadas? | Compatibilidade, testes e distribuição. | `03-escopo-mvp.md` e `16-compatibilidade-e-diagnostico.md`. |
| DP-003 | [DECISÃO RESOLVIDA] Qual versão mínima do Android e quais fabricantes serão priorizados? | Execução em segundo plano e matriz de testes. | `03-escopo-mvp.md` e `16-compatibilidade-e-diagnostico.md`. |
| DP-004 | [DECISÃO RESOLVIDA] Quantos computadores, celulares intermediários e notebooks autorizados o MVP suportará por instalação? | Modelo de dados, UX, autenticação e escopo. | `03-escopo-mvp.md`. |
| DP-005 | [DECISÃO RESOLVIDA] Qual aplicativo remoto será a primeira integração funcional? | Contratos, monitoramento e testes ponta a ponta. | `03-escopo-mvp.md` e ADR específica. |
| DP-006 | [DECISÃO RESOLVIDA] Qual mecanismo de rede privada será adotado? | Instalação, autenticação, conectividade e suporte. | ADR específica. |
| DP-007 | [DECISÃO RESOLVIDA] O componente Android do MVP usará Termux ou aplicativo nativo? | Segurança, confiabilidade, distribuição e esforço. | ADR específica. |
| DP-008 | [DECISÃO RESOLVIDA] Qual tecnologia será utilizada nos aplicativos desktop? | Integração Windows, instalador, UI, manutenção e tamanho. | ADR específica após comparação técnica. |
| DP-009 | [DECISÃO RESOLVIDA] Quais alterações do Windows o configurador poderá aplicar automaticamente após consentimento? | Segurança, privilégios e rollback. | Requisitos, arquitetura e segurança. |
| DP-010 | [DECISÃO RESOLVIDA] Qual política de timeout e repetição será usada em cada etapa? | Experiência, tráfego, confiabilidade e diagnóstico. | Requisitos não funcionais e regras de negócio. |
| DP-011 | [DECISÃO RESOLVIDA] Como o launcher determinará que cada aplicativo remoto está efetivamente pronto? | Evita abertura prematura e falso sucesso. | Interfaces, integrações e ADRs. |
| DP-012 | [DECISÃO RESOLVIDA] Qual será o mecanismo de atualização e assinatura dos instaladores? | Integridade, distribuição e resposta a vulnerabilidades. | `18-distribuicao-e-atualizacoes.md`. |
| DP-013 | [DECISÃO RESOLVIDA] Qual será a política de retenção e exportação de logs? | Privacidade, suporte e armazenamento. | `19-observabilidade-e-suporte.md`. |
| DP-014 | [DECISÃO RESOLVIDA] Quais requisitos de acessibilidade serão obrigatórios no MVP? | UX, componentes de interface e testes. | Requisitos não funcionais e telas. |
| DP-015 | [DECISÃO RESOLVIDA] O produto terá uso exclusivamente pessoal no MVP ou também contemplará ambientes corporativos gerenciados? | Políticas, permissões, licenciamento e suporte. | Visão do produto e escopo. |

## 13. Hipóteses que exigem prova técnica

| Código | Hipótese | Evidência esperada |
| --- | --- | --- |
| HT-001 | Um notebook conectado à rede privada consegue alcançar de forma estável o celular intermediário fora da rede local. | Teste repetido em redes distintas, com registro de latência e reconexão. |
| HT-002 | O componente Android permanece disponível após reinicialização, bloqueio de tela e período prolongado de inatividade. | Testes por versão e fabricante Android, incluindo otimizações de bateria. |
| HT-003 | O celular consegue enviar o Magic Packet corretamente para o segmento local relevante. | Captura ou confirmação do pacote e ativação real do computador. |
| HT-004 | O computador-alvo mantém o adaptador de rede em condição adequada para acordar nos estados de energia suportados. | Testes reais separados por estado de energia e configuração. |
| HT-005 | O launcher consegue diferenciar computador online, Windows pronto e serviço remoto pronto. | Sondas independentes e cenários controlados de falha. |
| HT-006 | É possível restringir o componente baseado em SSH a uma operação de ativação sem disponibilizar shell geral. | Teste de autorização, comandos forçados, argumentos inválidos e tentativas de escape. |
| HT-007 | O pareamento e o armazenamento seguro podem ser realizados sem manipulação manual de chaves pelo usuário comum. | Protótipo de pareamento, inspeção de armazenamento e testes de revogação. |
| HT-008 | As alterações administrativas realizadas pelo configurador podem ser registradas e revertidas de forma confiável. | Testes de backup, rollback, reinstalação e desinstalação. |

## 14. Critérios preliminares de sucesso do briefing

Este briefing estará apto à aprovação no primeiro checkpoint quando:

* o problema e o público forem confirmados;
* as premissas provisórias forem aceitas, alteradas ou rejeitadas;
* as questões que afetam diretamente o MVP forem encaminhadas ao documento de escopo;
* nenhuma tecnologia hipotética estiver apresentada como decisão arquitetural definitiva;
* as limitações de Wake-on-LAN, Android, VPN e aplicativos remotos estiverem representadas sem promessa indevida;
* os princípios de segurança e reversibilidade estiverem preservados;
* houver base suficiente para derivar visão, personas, requisitos e regras de negócio.

## 15. Glossário inicial

| Termo | Definição inicial |
| --- | --- |
| Acesso remoto | Uso de um aplicativo para visualizar ou controlar um computador a partir de outro dispositivo. |
| Adaptador de rede | Componente físico ou virtual que conecta o dispositivo a uma rede. |
| Android intermediário | Celular Android que permanece na rede local e executa a solicitação autorizada de Wake-on-LAN. |
| Aplicativo remoto | Cliente ou serviço usado após a inicialização, como RustDesk, AnyDesk, Moonlight/Sunshine ou Área de Trabalho Remota. |
| BIOS/UEFI | Firmware que inicializa o hardware e pode conter opções necessárias para Wake-on-LAN. |
| DPAPI | Mecanismo do Windows para proteger dados vinculando sua descriptografia a um usuário ou computador. |
| Ethernet | Tecnologia de rede cabeada considerada para o adaptador físico do computador principal no cenário inicial. |
| Host SSH | Dispositivo que oferece um serviço SSH e possui uma identidade criptográfica que deve ser verificada. |
| Inicialização Rápida | Recurso do Windows que utiliza desligamento híbrido e pode alterar o comportamento esperado de Wake-on-LAN. |
| Launcher | Aplicativo visual no notebook remoto que coordena verificação, ativação, espera e abertura do acesso remoto. |
| Magic Packet | Quadro ou pacote com padrão específico utilizado por Wake-on-LAN para solicitar a ativação de um dispositivo. |
| Menor privilégio | Princípio que concede a cada componente apenas as permissões indispensáveis à sua função. |
| Pareamento | Processo de estabelecer confiança e credenciais exclusivas entre dispositivos autorizados. |
| PC principal | Computador Windows que deverá ser ligado e acessado remotamente. |
| Rede privada/VPN | Camada de conectividade entre dispositivos autorizados em redes físicas distintas; não substitui autorização da aplicação. |
| S3 | Estado tradicional de suspensão em que parte do hardware permanece energizada. Sua disponibilidade depende do equipamento. |
| S4 | Estado de hibernação; também é associado ao desligamento híbrido da Inicialização Rápida, embora os comportamentos de Wake-on-LAN possam diferir. |
| S5 | Estado de desligamento suave. O suporte prático a ativação depende do conjunto de sistema, firmware, driver e hardware. |
| Serviço privilegiado | Componente isolado autorizado a executar operações administrativas específicas no Windows. |
| SSH | Protocolo de comunicação segura. Nesta solução, qualquer uso deverá ser estritamente autenticado e restrito. |
| Tailscale | Produto de rede privada considerado como hipótese inicial, sujeito a decisão arquitetural. |
| Termux | Ambiente de terminal para Android considerado para a prova inicial do componente intermediário. |
| Termux:Boot | Complemento do Termux que permite iniciar scripts após a inicialização do Android, sujeito às regras do sistema e do fabricante. |
| Timeout | Limite máximo de espera por uma etapa antes que o sistema a classifique como não concluída. |
| Wake-on-LAN | Mecanismo que permite a um adaptador de rede solicitar a ativação do computador ao reconhecer um evento compatível. |

## 16. Referências fornecidas

### 16.1 Fonte primária do projeto

* Texto de contexto e instruções fornecido pelo idealizador do produto em 14 de julho de 2026, contendo objetivo, experiência esperada, perfis, configuração inicial, limitações, requisitos de segurança, diretrizes arquiteturais, estrutura documental e método de execução.

### 16.2 Tecnologias e produtos mencionados na fonte

* Microsoft Windows 10 e Windows 11;
* Android;
* Wake-on-LAN e Magic Packet;
* Tailscale ou tecnologia equivalente;
* Termux, Termux:Boot e OpenSSH;
* RustDesk;
* AnyDesk;
* Moonlight e Sunshine;
* Área de Trabalho Remota;
* Windows Credential Manager e DPAPI.

## 17. Referências oficiais consultadas

As referências desta seção sustentam apenas o contexto técnico preliminar. Decisões de arquitetura e matrizes completas de compatibilidade deverão possuir pesquisa própria nos documentos correspondentes.

| Fonte | Classificação do uso | Contribuição para o briefing |
| --- | --- | --- |
| [Microsoft — Wake on LAN behavior in Windows](https://learn.microsoft.com/en-us/troubleshoot/windows-client/setup-upgrade-and-drivers/wake-on-lan-feature) | Comportamento oficialmente documentado | Registra a influência do desligamento híbrido, dos estados de energia e do armamento do adaptador. |
| [Microsoft — System power states](https://learn.microsoft.com/en-us/windows/win32/power/system-power-states) | Comportamento oficialmente documentado | Define estados de energia e o comportamento documentado de Wake-on-LAN. |
| [Microsoft — Powercfg command-line options](https://learn.microsoft.com/en-us/windows-hardware/design/device-experiences/powercfg-command-line-options) | Comportamento oficialmente documentado | Confirma mecanismos de consulta de dispositivos capazes, programáveis e autorizados a acordar o sistema. |
| [Android Developers — Background optimization](https://developer.android.com/topic/performance/background-optimization) | Comportamento oficialmente documentado | Confirma que restrições de bateria podem limitar trabalho em segundo plano. |
| [Android Developers — Background execution limits](https://developer.android.com/about/versions/oreo/background) | Comportamento oficialmente documentado | Confirma limites aplicáveis a serviços e execução em segundo plano. |
| [Tailscale — How Tailscale assigns IP addresses](https://tailscale.com/docs/concepts/ip-and-dns-addresses) | Comportamento oficialmente documentado | Confirma endereçamento estável enquanto o dispositivo permanece registrado, sem transformar isso em decisão de produto. |
| [Tailscale — Auth keys](https://tailscale.com/docs/features/access-control/auth-keys) | Comportamento oficialmente documentado | Fornece contexto para autenticação automatizada e ciclo de vida de chaves da VPN. |
| [Termux:Boot — documentação oficial do projeto](https://github.com/termux/termux-boot/blob/master/README.md) | Comportamento oficialmente documentado | Confirma requisitos de instalação, primeira execução e scripts de inicialização. |

## 18. Distinção entre evidência, inferência e decisão

| Categoria | Tratamento neste briefing |
| --- | --- |
| Comportamento oficialmente documentado | É associado a uma fonte primária e limitado ao que a fonte sustenta. |
| Inferência técnica | É apresentada como consequência provável, sujeita a validação. |
| Hipótese que precisa de teste | É identificada na seção 13 e não fundamenta declaração definitiva de compatibilidade. |
| Limitação conhecida | É registrada como restrição ou cenário de falha a ser tratado. |
| Decisão de projeto | Somente será considerada aprovada após registro no documento competente e, quando arquitetural, em ADR. |

## 19. Dependências documentais imediatas

Este documento deverá alimentar, nesta ordem lógica:

1. `01-visao-do-produto.md`, para formalizar objetivos, não objetivos, proposta de valor e indicadores;
2. `02-personas.md`, para aprofundar necessidades, capacidades e permissões dos perfis;
3. `03-escopo-mvp.md`, para converter premissas de escopo em limites aprovados;
4. `04-requisitos-funcionais.md`, para detalhar comportamentos verificáveis;
5. `05-requisitos-nao-funcionais.md`, para definir atributos mensuráveis;
6. `06-regras-de-negocio.md`, para formalizar restrições e decisões operacionais.

## 20. Histórico de revisões

| Versão | Data | Alteração | Estado resultante |
| --- | --- | --- | --- |
| 0.1.0 | 14/07/2026 | Criação inicial a partir do contexto fornecido e validação preliminar em fontes oficiais. | Em elaboração |
| 1.0.0 | 14/07/2026 | Premissas encaminhadas ao escopo/ADRs e briefing aprovado após os quatro ciclos. | Aprovado com ajustes pontuais |
