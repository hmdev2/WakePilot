# Requisitos funcionais

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

## Convenções

Prioridades: P0 obrigatória para a jornada/segurança; P1 obrigatória para conclusão do MVP, podendo entrar após a prova vertical; P2 evolução. Cada requisito possui critério verificável e rastreabilidade até teste.

## Catálogo

| ID | Nome | Prioridade |
| --- | --- | --- |
| RF001 | Inventariar ambiente Windows | P0 |
| RF002 | Detectar adaptadores de rede | P0 |
| RF003 | Avaliar evidências de Wake-on-LAN | P0 |
| RF004 | Diagnosticar energia e Inicialização Rápida | P0 |
| RF005 | Orientar BIOS/UEFI | P0 |
| RF006 | Apresentar plano e obter consentimento | P0 |
| RF007 | Aplicar configuração Windows autorizada | P0 |
| RF008 | Capturar snapshot de restauração | P0 |
| RF009 | Assistir instalação e autenticação da VPN | P0 |
| RF010 | Preparar Android bridge | P0 |
| RF011 | Gerar autenticação por dispositivo | P0 |
| RF012 | Parear launcher, bridge e agente | P0 |
| RF013 | Cadastrar computador-alvo | P0 |
| RF014 | Configurar aplicativo remoto | P0 |
| RF015 | Verificar estado do PC | P0 |
| RF016 | Verificar estado do bridge e VPN | P0 |
| RF017 | Solicitar ativação remota | P0 |
| RF018 | Impedir repetição e abuso | P0 |
| RF019 | Acompanhar inicialização | P0 |
| RF020 | Verificar prontidão do serviço remoto | P0 |
| RF021 | Abrir aplicativo remoto | P0 |
| RF022 | Apresentar erros acionáveis | P0 |
| RF023 | Notificar mudança de estado | P1 |
| RF024 | Executar teste ponta a ponta | P0 |
| RF025 | Registrar logs estruturados | P0 |
| RF026 | Exportar diagnóstico sanitizado | P1 |
| RF027 | Revogar dispositivo | P0 |
| RF028 | Verificar e aplicar atualização | P1 |
| RF029 | Restaurar configurações | P0 |
| RF030 | Desinstalar com segurança | P0 |

## RF001 — Inventariar ambiente Windows

| Campo | Especificação |
| --- | --- |
| Descrição | Coletar versão, edição, build, arquitetura, fabricante, modelo e estados de energia sem alteração. |
| Ator principal | Técnico |
| Motivação | Fundamentar compatibilidade. |
| Entradas | Windows local. |
| Pré-condições | Aplicação executada localmente. |
| Fluxo principal resumido | Coletar APIs/consultas permitidas; normalizar; exibir fontes e horário. |
| Fluxos alternativos | Falha parcial produz item inconclusivo, não falha total. |
| Pós-condições | Diagnóstico persistido. |
| Regras relacionadas | RN001,RN017 |
| Prioridade | P0 |
| Critérios de aceite | Dado exibido corresponde ao sistema; falhas individuais são identificadas; nenhuma elevação é solicitada para leitura comum. |
| Dependências | Windows APIs |
| Rastreabilidade | OBJ002;PRC-001;TEL003;CMP006;US003;CT001 |

## RF002 — Detectar adaptadores de rede

| Campo | Especificação |
| --- | --- |
| Descrição | Listar adaptadores e classificar físico/virtual, Ethernet/Wi-Fi/VPN, estado e MAC. |
| Ator principal | Técnico |
| Motivação | Eliminar seleção e MAC manuais. |
| Entradas | Inventário de interfaces. |
| Pré-condições | RF001 concluído. |
| Fluxo principal resumido | Enumerar interfaces; pontuar candidatos; pedir confirmação técnica do Ethernet. |
| Fluxos alternativos | Ambiguidade mantém seleção pendente; Wi-Fi recebe aviso. |
| Pós-condições | Adaptador-alvo salvo sem expor MAC ao comum. |
| Regras relacionadas | RN002,RN003 |
| Prioridade | P0 |
| Critérios de aceite | Virtual nunca é selecionado automaticamente; MAC vem do sistema; comum não digita MAC. |
| Dependências | RF001 |
| Rastreabilidade | OBJ002;PRC-001;TEL003;CMP006;US004;CT002 |

## RF003 — Avaliar evidências de Wake-on-LAN

| Campo | Especificação |
| --- | --- |
| Descrição | Consultar driver, propriedades e capacidade de wake sem declarar validação definitiva. |
| Ator principal | Técnico |
| Motivação | Evitar falso positivo. |
| Entradas | Adaptador-alvo. |
| Pré-condições | RF002. |
| Fluxo principal resumido | Coletar propriedades, wake_programmable/wake_armed e disponibilidade; atribuir evidência. |
| Fluxos alternativos | Propriedade ausente ou contraditória resulta inconclusivo. |
| Pós-condições | Classificação preliminar registrada. |
| Regras relacionadas | RN001,RN004 |
| Prioridade | P0 |
| Critérios de aceite | Resultado distingue detectado, inferido e testado; propriedade isolada nunca gera validado. |
| Dependências | RF002 |
| Rastreabilidade | OBJ003;PRC-001;TEL004;CMP006;US005;CT003 |

## RF004 — Diagnosticar energia e Inicialização Rápida

| Campo | Especificação |
| --- | --- |
| Descrição | Identificar estados disponíveis, Fast Startup e autorização de wake. |
| Ator principal | Técnico |
| Motivação | Explicar variações S3/S4/S5. |
| Entradas | Saídas powercfg e registro autorizado. |
| Pré-condições | RF001. |
| Fluxo principal resumido | Consultar estados; correlacionar adaptador; apresentar impacto documentado. |
| Fluxos alternativos | Comando indisponível vira inconclusivo e oferece coleta manual guiada. |
| Pós-condições | Diagnóstico energético registrado. |
| Regras relacionadas | RN004,RN017 |
| Prioridade | P0 |
| Critérios de aceite | S3/S4/S5 não são tratados como equivalentes; estado de Fast Startup é mostrado. |
| Dependências | RF001,RF002 |
| Rastreabilidade | OBJ003;PRC-001;TEL004;CMP006;US005;CT004 |

## RF005 — Orientar BIOS/UEFI

| Campo | Especificação |
| --- | --- |
| Descrição | Gerar checklist por fabricante/modelo e registrar confirmação humana. |
| Ator principal | Técnico |
| Motivação | Tratar etapa não automatizável. |
| Entradas | Fabricante/modelo e base de orientações. |
| Pré-condições | RF001. |
| Fluxo principal resumido | Mostrar aviso; orientar nomes possíveis; solicitar resultado; nunca alterar firmware. |
| Fluxos alternativos | Modelo desconhecido usa guia genérico e marca incerteza. |
| Pós-condições | Confirmação/pendência registrada. |
| Regras relacionadas | RN005 |
| Prioridade | P0 |
| Critérios de aceite | Interface afirma que nomes variam; nenhuma alegação de alteração automática; técnico pode marcar não localizado. |
| Dependências | RF001 |
| Rastreabilidade | OBJ003;PRC-003;TEL006;CMP001;US006;CT005 |

## RF006 — Apresentar plano e obter consentimento

| Campo | Especificação |
| --- | --- |
| Descrição | Listar cada alteração administrativa, motivo, risco e rollback antes da execução. |
| Ator principal | Técnico |
| Motivação | Garantir consentimento informado. |
| Entradas | Alterações propostas. |
| Pré-condições | Diagnóstico concluído. |
| Fluxo principal resumido | Agrupar ações; permitir seleção; pedir confirmação e UAC apenas ao executar. |
| Fluxos alternativos | Recusa mantém configuração incompleta sem penalidade. |
| Pós-condições | Consentimento auditado por ação. |
| Regras relacionadas | RN006,RN007 |
| Prioridade | P0 |
| Critérios de aceite | Nenhuma mutação antes de confirmação; recusa não bloqueia exportar diagnóstico. |
| Dependências | RF001-RF005 |
| Rastreabilidade | OBJ006;PRC-002;TEL005;CMP001;US007;CT006 |

## RF007 — Aplicar configuração Windows autorizada

| Campo | Especificação |
| --- | --- |
| Descrição | Executar somente ações selecionadas por broker privilegiado com allowlist. |
| Ator principal | Técnico |
| Motivação | Configurar WoL com menor privilégio. |
| Entradas | Plano consentido. |
| Pré-condições | RF006 e snapshot válido. |
| Fluxo principal resumido | Enviar operação tipada ao broker; validar; aplicar; verificar; registrar. |
| Fluxos alternativos | Falha interrompe lote e oferece rollback; operação desconhecida é rejeitada. |
| Pós-condições | Estado verificado e auditado. |
| Regras relacionadas | RN006,RN007,RN016 |
| Prioridade | P0 |
| Critérios de aceite | Broker rejeita comando/argumento fora da allowlist; resultado é verificado por leitura. |
| Dependências | RF006,RF008 |
| Rastreabilidade | OBJ002,OBJ006;PRC-002;TEL005;CMP005;US008;CT007 |

## RF008 — Capturar snapshot de restauração

| Campo | Especificação |
| --- | --- |
| Descrição | Registrar valores anteriores às alterações gerenciadas. |
| Ator principal | Técnico |
| Motivação | Viabilizar rollback. |
| Entradas | Chaves/estados afetados. |
| Pré-condições | Plano aprovado. |
| Fluxo principal resumido | Ler estado; persistir snapshot com integridade; associar correlation ID. |
| Fluxos alternativos | Se snapshot falhar, mutação correspondente não inicia. |
| Pós-condições | Snapshot íntegro disponível. |
| Regras relacionadas | RN007,RN014 |
| Prioridade | P0 |
| Critérios de aceite | Toda ação mutável possui valor anterior ou justificativa de não reversibilidade; hash é verificado. |
| Dependências | RF006 |
| Rastreabilidade | OBJ006;PRC-002,PRC-021;TEL005,TEL021;CMP009;US009;CT008 |

## RF009 — Assistir instalação e autenticação da VPN

| Campo | Especificação |
| --- | --- |
| Descrição | Detectar Tailscale, orientar instalação oficial e verificar dispositivo na tailnet. |
| Ator principal | Técnico |
| Motivação | Criar conectividade privada sem chave embutida. |
| Entradas | Estado do cliente VPN. |
| Pré-condições | Internet e conta do proprietário. |
| Fluxo principal resumido | Detectar; abrir instalador/página oficial com consentimento; aguardar autenticação; validar IP/nome. |
| Fluxos alternativos | Sem internet ou autenticação cancelada deixa pendente e oferece retomar. |
| Pós-condições | VPN associada ao perfil sem token salvo. |
| Regras relacionadas | RN008,RN015 |
| Prioridade | P0 |
| Critérios de aceite | Produto não guarda auth key; dispositivo é alcançável pela tailnet antes de avançar. |
| Dependências | RF006 |
| Rastreabilidade | OBJ001,OBJ005;PRC-004;TEL007;CMP012;US010;CT009 |

## RF010 — Preparar Android bridge

| Campo | Especificação |
| --- | --- |
| Descrição | Conduzir instalação compatível de Termux, Boot, OpenSSH e bootstrap verificável. |
| Ator principal | Técnico |
| Motivação | Disponibilizar ponte sem comandos manuais do usuário comum. |
| Entradas | Android físico e pacote bootstrap. |
| Pré-condições | RF009; acesso físico. |
| Fluxo principal resumido | Exibir etapas visuais/QR; validar origem; instalar pacote/script; configurar boot e bateria; testar health. |
| Fluxos alternativos | Fabricante bloqueia background: orientar exceção e marcar risco; falha permite retomar. |
| Pós-condições | Bridge instalado e diagnosticado. |
| Regras relacionadas | RN009,RN010 |
| Prioridade | P0 |
| Critérios de aceite | Origem incompatível entre Termux/plugins é detectada; boot e health são testados após reinício. |
| Dependências | RF009 |
| Rastreabilidade | OBJ001,OBJ004;PRC-005;TEL008;CMP013;US011;CT010 |

## RF011 — Gerar autenticação por dispositivo

| Campo | Especificação |
| --- | --- |
| Descrição | Criar par de chaves exclusivo no launcher e instalar apenas a pública no bridge. |
| Ator principal | Técnico |
| Motivação | Evitar credencial compartilhada. |
| Entradas | Identidade do notebook. |
| Pré-condições | Bridge preparado. |
| Fluxo principal resumido | Gerar Ed25519; proteger privada com DPAPI; exibir fingerprint; instalar pública restrita. |
| Fluxos alternativos | Falha de proteção destrói material temporário; nunca exporta privada em claro. |
| Pós-condições | Credencial individual ativa. |
| Regras relacionadas | RN010,RN011,RN012 |
| Prioridade | P0 |
| Critérios de aceite | Privada não aparece no banco/log; pública contém forced command e restrições; fingerprint confirmado. |
| Dependências | RF010 |
| Rastreabilidade | OBJ005;PRC-006;TEL009;CMP010;US012;CT011 |

## RF012 — Parear launcher, bridge e agente

| Campo | Especificação |
| --- | --- |
| Descrição | Trocar identificadores, host key SSH, certificados públicos mTLS e perfil por canal presencial guiado. |
| Ator principal | Técnico |
| Motivação | Estabelecer confiança sem aceitar host desconhecido. |
| Entradas | QR/código de pareamento de curta duração, fingerprint SSH e fingerprints dos certificados mTLS. |
| Pré-condições | RF011. |
| Fluxo principal resumido | Comparar fingerprints; fixar host key e certificado do agente; registrar certificado público do launcher; testar SSH health e readiness mTLS; encerrar sessão. |
| Fluxos alternativos | Divergência cancela e revoga artefatos parciais; expiração exige novo código. |
| Pós-condições | Pareamento validado e auditado. |
| Regras relacionadas | RN010,RN011,RN013 |
| Prioridade | P0 |
| Critérios de aceite | Host key SSH e certificado do agente são fixados antes do primeiro wake/readiness; agente aceita somente o certificado do launcher pareado; código expira em 10 min e é de uso único. |
| Dependências | RF010,RF011 |
| Rastreabilidade | OBJ005;PRC-007;TEL009;CMP003,CMP013;US013;CT012 |

## RF013 — Cadastrar computador-alvo

| Campo | Especificação |
| --- | --- |
| Descrição | Associar nome amigável, adaptador/MAC protegido, bridge e estado testado. |
| Ator principal | Técnico |
| Motivação | Criar perfil consumido pelo launcher. |
| Entradas | Diagnóstico e pareamento. |
| Pré-condições | RF002,RF012. |
| Fluxo principal resumido | Validar unicidade; salvar identificadores; ocultar detalhes na área comum. |
| Fluxos alternativos | Dados incompletos salvam rascunho não operacional. |
| Pós-condições | Perfil ativo ou rascunho. |
| Regras relacionadas | RN002,RN015 |
| Prioridade | P0 |
| Critérios de aceite | Perfil operacional exige bridge, MAC, broadcast calculado e app remoto; comum vê apenas nome. |
| Dependências | RF002,RF012 |
| Rastreabilidade | OBJ001;PRC-017;TEL012;CMP008;US014;CT013 |

## RF014 — Configurar aplicativo remoto

| Campo | Especificação |
| --- | --- |
| Descrição | Selecionar preset RustDesk ou executável personalizado e definir sonda de prontidão. |
| Ator principal | Técnico/Avançado |
| Motivação | Abrir acesso correto com argumentos seguros. |
| Entradas | Caminho, preset e parâmetros permitidos. |
| Pré-condições | RF013. |
| Fluxo principal resumido | Localizar executável; validar assinatura/hash quando aplicável; configurar health probe; testar abertura. |
| Fluxos alternativos | Executável ausente/inválido bloqueia ativação do perfil; argumentos livres são rejeitados. |
| Pós-condições | Perfil remoto validado. |
| Regras relacionadas | RN018,RN019 |
| Prioridade | P0 |
| Critérios de aceite | Caminho canônico e allowlist de argumentos; teste inicia somente o binário selecionado. |
| Dependências | RF013 |
| Rastreabilidade | OBJ001,OBJ005;PRC-016;TEL013;CMP014;US015;CT014 |

## RF015 — Verificar estado do PC

| Campo | Especificação |
| --- | --- |
| Descrição | Determinar offline, rede disponível, Windows pronto ou desconhecido por sondas separadas. |
| Ator principal | Usuário comum |
| Motivação | Evitar wake desnecessário e falso pronto. |
| Entradas | Perfil do PC. |
| Pré-condições | Perfil operacional. |
| Fluxo principal resumido | Executar sondas com timeout; combinar evidências; atualizar UI. |
| Fluxos alternativos | Bloqueio de ICMP usa sondas alternativas; conflito resulta desconhecido. |
| Pós-condições | Estado com timestamp exibido. |
| Regras relacionadas | RN020,RN001 |
| Prioridade | P0 |
| Critérios de aceite | PC pronto só com sonda de agente/serviço; ausência de ICMP isolada não prova offline. |
| Dependências | RF013 |
| Rastreabilidade | OBJ004;PRC-009,PRC-010;TEL012;CMP003;US016;CT015 |

## RF016 — Verificar estado do bridge e VPN

| Campo | Especificação |
| --- | --- |
| Descrição | Distinguir VPN local desconectada, bridge inalcançável e serviço bridge indisponível. |
| Ator principal | Usuário comum |
| Motivação | Indicar causa correta antes do wake. |
| Entradas | Perfil e estado Tailscale. |
| Pré-condições | RF012. |
| Fluxo principal resumido | Verificar cliente VPN; alcance Tailscale; host key; health restrito. |
| Fluxos alternativos | Timeout, host key divergente e serviço parado têm códigos distintos. |
| Pós-condições | Estado do bridge exibido. |
| Regras relacionadas | RN008,RN013,RN020 |
| Prioridade | P0 |
| Critérios de aceite | Nenhum wake se bridge não estiver autenticado e saudável; causas têm ERR distintos. |
| Dependências | RF012 |
| Rastreabilidade | OBJ004;PRC-013;TEL012,TEL018;CMP003;US017;CT016 |

## RF017 — Solicitar ativação remota

| Campo | Especificação |
| --- | --- |
| Descrição | Enviar ao bridge pedido autenticado para o alvo cadastrado. |
| Ator principal | Usuário comum |
| Motivação | Executar Wake-on-LAN sem terminal. |
| Entradas | Target ID, request ID, timestamp e nonce. |
| Pré-condições | PC não pronto; bridge saudável. |
| Fluxo principal resumido | Gerar pedido; autenticar por SSH pinned; wrapper valida; enviar burst de Magic Packets; retornar recibo. |
| Fluxos alternativos | Uma repetição controlada após política; cancelamento impede novas repetições. |
| Pós-condições | Tentativa correlacionada. |
| Regras relacionadas | RN001,RN009,RN010,RN012 |
| Prioridade | P0 |
| Critérios de aceite | MAC nunca é enviado pela UI; recibo não equivale a PC ligado; burst limitado. |
| Dependências | RF015,RF016 |
| Rastreabilidade | OBJ001,OBJ005;PRC-009;TEL014;CMP003,CMP013;US018;CT017 |

## RF018 — Impedir repetição e abuso

| Campo | Especificação |
| --- | --- |
| Descrição | Validar janela temporal, nonce, credencial, alvo e rate limit no bridge. |
| Ator principal | Sistema |
| Motivação | Reduzir replay e negação de serviço. |
| Entradas | Pedido de wake. |
| Pré-condições | Relógios dentro da tolerância definida. |
| Fluxo principal resumido | Rejeitar expirado/repetido/não autorizado; persistir nonces recentes; limitar 3/5 min e cooldown 15 s. |
| Fluxos alternativos | Relógio divergente retorna erro próprio; suporte pode orientar sincronização. |
| Pós-condições | Decisão auditada sem segredo. |
| Regras relacionadas | RN012,RN013 |
| Prioridade | P0 |
| Critérios de aceite | Mesmo request/nonce nunca executa duas vezes; limite aplicado por chave e alvo. |
| Dependências | RF017 |
| Rastreabilidade | OBJ005;PRC-009;CMP013;US018;CT018 |

## RF019 — Acompanhar inicialização

| Campo | Especificação |
| --- | --- |
| Descrição | Monitorar transição até Windows pronto com progresso e timeout. |
| Ator principal | Usuário comum |
| Motivação | Eliminar espera cega. |
| Entradas | Tentativa ativa. |
| Pré-condições | RF017 aceito. |
| Fluxo principal resumido | Sondar com backoff; mostrar fases; permitir cancelar; encerrar em 240 s para Windows. |
| Fluxos alternativos | PC já pronto avança; timeout classifica wake não confirmado/Windows indisponível conforme evidência. |
| Pós-condições | Estado final registrado. |
| Regras relacionadas | RN020,RN005 |
| Prioridade | P0 |
| Critérios de aceite | UI permanece responsiva; cancelamento em ≤2 s; sondas não excedem política. |
| Dependências | RF015,RF017 |
| Rastreabilidade | OBJ001,OBJ004;PRC-011,PRC-015;TEL014;CMP003;US019;CT019 |

## RF020 — Verificar prontidão do serviço remoto

| Campo | Especificação |
| --- | --- |
| Descrição | Executar a sonda configurada após Windows pronto. |
| Ator principal | Sistema |
| Motivação | Não abrir cliente prematuramente. |
| Entradas | Perfil remoto. |
| Pré-condições | RF019 indica Windows pronto. |
| Fluxo principal resumido | Testar processo/serviço/porta/agent conforme adaptador por até 120 s. |
| Fluxos alternativos | Indisponível oferece repetir sonda ou abrir manualmente com aviso técnico. |
| Pós-condições | Pronto ou erro específico. |
| Regras relacionadas | RN019,RN020 |
| Prioridade | P0 |
| Critérios de aceite | Sonda só inicia após Windows pronto; sucesso exige critério do adapter, não apenas ping. |
| Dependências | RF014,RF019 |
| Rastreabilidade | OBJ004;PRC-011;TEL014;CMP014;US020;CT020 |

## RF021 — Abrir aplicativo remoto

| Campo | Especificação |
| --- | --- |
| Descrição | Iniciar executável validado com argumentos tipados após prontidão. |
| Ator principal | Usuário comum |
| Motivação | Concluir jornada automaticamente. |
| Entradas | Perfil remoto validado. |
| Pré-condições | RF020 pronto ou confirmação manual excepcional. |
| Fluxo principal resumido | Resolver caminho; revalidar; iniciar; registrar resultado. |
| Fluxos alternativos | Falha de processo retorna ERR e ação para localizar/reconfigurar; não faz fallback inseguro. |
| Pós-condições | Cliente iniciado. |
| Regras relacionadas | RN018,RN019 |
| Prioridade | P0 |
| Critérios de aceite | Abertura p95 ≤3 s após pronto; argumentos não são concatenados em shell. |
| Dependências | RF014,RF020 |
| Rastreabilidade | OBJ001,OBJ005;PRC-012;TEL015;CMP014;US021;CT021 |

## RF022 — Apresentar erros acionáveis

| Campo | Especificação |
| --- | --- |
| Descrição | Mapear falhas para códigos, linguagem simples, causa provável e próxima ação. |
| Ator principal | Todos |
| Motivação | Facilitar recuperação. |
| Entradas | Erro técnico e contexto. |
| Pré-condições | Catálogo de erros. |
| Fluxo principal resumido | Sanitizar; mapear; exibir resumo; permitir detalhes conforme perfil. |
| Fluxos alternativos | Erro desconhecido recebe ERR020 e correlation ID. |
| Pós-condições | Mensagem e evento registrados. |
| Regras relacionadas | RN020 |
| Prioridade | P0 |
| Critérios de aceite | Nenhuma stack/segredo na UI comum; todo erro contém ação e correlation ID. |
| Dependências | RF001-RF021 |
| Rastreabilidade | OBJ004;PRC-013-015;TEL018;CMP011;US022;CT022 |

## RF023 — Notificar mudança de estado

| Campo | Especificação |
| --- | --- |
| Descrição | Exibir notificações locais para sucesso, timeout e ação necessária. |
| Ator principal | Usuário comum |
| Motivação | Permitir acompanhar sem manter janela em foco. |
| Entradas | Evento de operação. |
| Pré-condições | Notificações autorizadas. |
| Fluxo principal resumido | Publicar notificação sem dado sensível; focar operação ao clicar. |
| Fluxos alternativos | Permissão negada mantém feedback in-app. |
| Pós-condições | Usuário informado. |
| Regras relacionadas | RN015 |
| Prioridade | P1 |
| Critérios de aceite | Notificação não contém IP/MAC; duplicatas são coalescidas. |
| Dependências | RF019-RF022 |
| Rastreabilidade | OBJ001;PRC-009;TEL014;CMP001;US023;CT023 |

## RF024 — Executar teste ponta a ponta

| Campo | Especificação |
| --- | --- |
| Descrição | Orquestrar desligamento/estado escolhido, wake, Windows, serviço e abertura controlada. |
| Ator principal | Técnico |
| Motivação | Validar compatibilidade real. |
| Entradas | Perfil completo e estado selecionado. |
| Pré-condições | RF014 e consentimento; acesso físico de contingência. |
| Fluxo principal resumido | Registrar baseline; solicitar estado de energia; executar cadeia; coletar tempos; classificar. |
| Fluxos alternativos | Falha mantém classificação inferior e recomenda etapa; teste não força desligamento sem confirmação. |
| Pós-condições | Resultado imutável anexado ao diagnóstico. |
| Regras relacionadas | RN004,RN006 |
| Prioridade | P0 |
| Critérios de aceite | Somente sucesso integral marca validado; estado de energia e evidências ficam registrados. |
| Dependências | RF001-RF021 |
| Rastreabilidade | OBJ003;PRC-008;TEL010;CMP003;US024;CT024 |

## RF025 — Registrar logs estruturados

| Campo | Especificação |
| --- | --- |
| Descrição | Gravar eventos locais com tempo UTC, nível, código, componente e correlation ID. |
| Ator principal | Sistema/Suporte |
| Motivação | Dar observabilidade sem vazar dados. |
| Entradas | Eventos de componentes. |
| Pré-condições | Armazenamento disponível. |
| Fluxo principal resumido | Sanitizar; gravar; rotacionar; proteger ACL. |
| Fluxos alternativos | Falha de disco degrada com aviso e buffer limitado; nunca bloqueia wake por log informativo. |
| Pós-condições | Linha do tempo consultável. |
| Regras relacionadas | RN014,RN015 |
| Prioridade | P0 |
| Critérios de aceite | Segredos são redigidos; rotação em 20 MB/30 dias; operações críticas têm início/fim. |
| Dependências | Todos |
| Rastreabilidade | OBJ004,OBJ005;PRC-020;TEL019;CMP011;US025;CT025 |

## RF026 — Exportar diagnóstico sanitizado

| Campo | Especificação |
| --- | --- |
| Descrição | Gerar pacote com inventário, configurações não secretas, testes e logs selecionados. |
| Ator principal | Usuário/Técnico |
| Motivação | Permitir suporte seguro. |
| Entradas | Período e consentimento. |
| Pré-condições | RF025. |
| Fluxo principal resumido | Pré-visualizar categorias; sanitizar; gerar ZIP com manifesto/hash; salvar em destino escolhido. |
| Fluxos alternativos | Categoria sensível pode ser excluída; falha informa arquivo parcial e o remove. |
| Pós-condições | Pacote exportado e auditado. |
| Regras relacionadas | RN014,RN015 |
| Prioridade | P1 |
| Critérios de aceite | Scanner de segredos não encontra chave/token/MAC completo por padrão; manifesto lista arquivos. |
| Dependências | RF025 |
| Rastreabilidade | OBJ004,OBJ005;PRC-020;TEL020;CMP011;US026;CT026 |

## RF027 — Revogar dispositivo

| Campo | Especificação |
| --- | --- |
| Descrição | Desabilitar chave do notebook/bridge e impedir novas solicitações. |
| Ator principal | Técnico/Avançado |
| Motivação | Responder a perda ou troca. |
| Entradas | Dispositivo e motivo. |
| Pré-condições | Autorização local. |
| Fluxo principal resumido | Confirmar; remover autorização; atualizar configuração; testar rejeição; registrar. |
| Fluxos alternativos | Dispositivo offline entra em revogação pendente e alerta até sincronizar localmente. |
| Pós-condições | Credencial inativa. |
| Regras relacionadas | RN011,RN013 |
| Prioridade | P0 |
| Critérios de aceite | Pedido posterior é rejeitado; revogação não afeta outras chaves; privada local é apagada quando aplicável. |
| Dependências | RF011,RF012 |
| Rastreabilidade | OBJ005;PRC-018;TEL017;CMP010;US027;CT027 |

## RF028 — Verificar e aplicar atualização

| Campo | Especificação |
| --- | --- |
| Descrição | Consultar manifesto assinado, informar mudanças e instalar versão compatível com rollback. |
| Ator principal | Técnico/Avançado |
| Motivação | Corrigir falhas com integridade. |
| Entradas | Canal e manifesto. |
| Pré-condições | Internet; pacote assinado. |
| Fluxo principal resumido | Baixar; validar assinatura/hash; verificar compatibilidade; consentir; instalar; health check. |
| Fluxos alternativos | Assinatura inválida rejeita; health falho reverte; MVP permite fluxo manual. |
| Pós-condições | Versão nova saudável ou anterior restaurada. |
| Regras relacionadas | RN016 |
| Prioridade | P1 |
| Critérios de aceite | Nenhum pacote não assinado em canal estável; migração possui backup; downgrade incompatível bloqueado. |
| Dependências | RF008 |
| Rastreabilidade | OBJ005,OBJ006;PRC-019;TEL022;CMP015;US028;CT028 |

## RF029 — Restaurar configurações

| Campo | Especificação |
| --- | --- |
| Descrição | Reaplicar snapshot selecionado com verificação e relatório. |
| Ator principal | Técnico/Avançado |
| Motivação | Reverter impacto do produto. |
| Entradas | Snapshot íntegro. |
| Pré-condições | RF008; elevação e consentimento. |
| Fluxo principal resumido | Validar integridade; mostrar diferenças; aplicar inversamente; verificar; registrar. |
| Fluxos alternativos | Item ausente/alterado externamente exige decisão e não sobrescreve silenciosamente. |
| Pós-condições | Estado restaurado ou parcial explícito. |
| Regras relacionadas | RN006,RN007 |
| Prioridade | P0 |
| Critérios de aceite | Rollback é idempotente; falhas por item não são ocultadas; snapshot permanece até confirmação. |
| Dependências | RF008 |
| Rastreabilidade | OBJ006;PRC-021;TEL021;CMP005,CMP009;US029;CT029 |

## RF030 — Desinstalar com segurança

| Campo | Especificação |
| --- | --- |
| Descrição | Remover aplicações, serviço, chaves, tarefas e dados conforme opção do usuário. |
| Ator principal | Técnico/Avançado |
| Motivação | Encerrar uso sem acesso residual. |
| Entradas | Escopo de remoção. |
| Pré-condições | Autorização e inventário de instalação. |
| Fluxo principal resumido | Oferecer rollback Windows; revogar; parar serviços; remover; verificar; emitir resumo. |
| Fluxos alternativos | Bridge offline gera instrução/pendência explícita; logs podem ser preservados com consentimento. |
| Pós-condições | Produto removido e acessos invalidados. |
| Regras relacionadas | RN011,RN014,RN016 |
| Prioridade | P0 |
| Critérios de aceite | Sem chave autorizada residual; serviço/tarefa inexistentes; dados preservados somente se selecionados. |
| Dependências | RF027,RF029 |
| Rastreabilidade | OBJ005,OBJ006;PRC-022;TEL023;CMP015;US030;CT030 |
## Revisão cruzada

* Todos os RFs possuem ator, aceite, dependência e rastreabilidade.
* RF017 não considera recibo do bridge como prova de inicialização.
* RF024 é a única origem da classificação “compatível e validado por teste”.
* RF021 impede concatenação de argumentos em shell.
* RF029 e RF030 cobrem reversibilidade e encerramento de acesso.
