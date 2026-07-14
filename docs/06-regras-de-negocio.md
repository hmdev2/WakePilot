# Regras de negócio

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

## Catálogo

| ID | Nome |
| --- | --- |
| RN001 | Não acordar PC pronto |
| RN002 | MAC automático e oculto |
| RN003 | Priorizar Ethernet físico |
| RN004 | Validação exige teste real |
| RN005 | BIOS é guiada |
| RN006 | Consentimento administrativo |
| RN007 | Snapshot antes de mutação |
| RN008 | VPN não substitui autenticação |
| RN009 | Bridge saudável antes do wake |
| RN010 | Sem shell irrestrito |
| RN011 | Credencial individual e revogável |
| RN012 | Pedido fresco e limitado |
| RN013 | Identidade remota conhecida obrigatória |
| RN014 | Segredos fora de logs e banco |
| RN015 | Minimização de dados |
| RN016 | Software externo validado |
| RN017 | Diagnóstico não é promessa |
| RN018 | Argumentos tipados |
| RN019 | Serviço pronto antes de abrir |
| RN020 | Falhas por camada |

## RN001 — Não acordar PC pronto

| Campo | Especificação |
| --- | --- |
| Descrição | Evitar Magic Packet quando o PC já estiver pronto. |
| Justificativa | Evita operação inútil e estados ambíguos. |
| Condições | Sonda confirma Windows/serviço pronto. |
| Comportamento esperado | Abrir acesso remoto diretamente. |
| Exceções | Estado desconhecido não é considerado pronto; prosseguir somente após política de diagnóstico. |
| Requisitos relacionados | RF003,RF015,RF017 |

## RN002 — MAC automático e oculto

| Campo | Especificação |
| --- | --- |
| Descrição | MAC deve vir do adaptador selecionado e não ser solicitado ao usuário comum. |
| Justificativa | Reduz erro e exposição técnica. |
| Condições | Cadastro ou alteração de adaptador. |
| Comportamento esperado | Sistema lê e valida endereço; área comum mostra apenas nome do PC. |
| Exceções | Técnico pode visualizar forma mascarada e copiar após ação explícita. |
| Requisitos relacionados | RF002,RF013 |

## RN003 — Priorizar Ethernet físico

| Campo | Especificação |
| --- | --- |
| Descrição | Somente Ethernet físico elegível ao fluxo automático do MVP. |
| Justificativa | Wake-on-WLAN está fora do escopo. |
| Condições | Classificação de interfaces. |
| Comportamento esperado | Virtual/VPN/Wi-Fi não é auto selecionado. |
| Exceções | Técnico pode registrar diagnóstico de Wi-Fi, mas perfil não se torna operacional. |
| Requisitos relacionados | RF002 |

## RN004 — Validação exige teste real

| Campo | Especificação |
| --- | --- |
| Descrição | Evidência de driver não autoriza classificação validada. |
| Justificativa | Compatibilidade depende do conjunto real. |
| Condições | Classificação do equipamento. |
| Comportamento esperado | Apenas RF024 bem-sucedido no estado avaliado gera “compatível e validado”. |
| Exceções | Teste posterior pode alterar a classificação sem apagar histórico. |
| Requisitos relacionados | RF003,RF004,RF024 |

## RN005 — BIOS é guiada

| Campo | Especificação |
| --- | --- |
| Descrição | Sistema não afirma configurar BIOS/UEFI automaticamente. |
| Justificativa | Interfaces variam e geralmente exigem presença local. |
| Condições | Firmware necessário ou desconhecido. |
| Comportamento esperado | Exibir checklist e registrar confirmação humana. |
| Exceções | Integração oficial futura de fabricante exige ADR e consentimento próprio. |
| Requisitos relacionados | RF005,RF019 |

## RN006 — Consentimento administrativo

| Campo | Especificação |
| --- | --- |
| Descrição | Toda alteração administrativa exige explicação e confirmação explícita. |
| Justificativa | Princípio de autonomia e segurança. |
| Condições | Antes de mutação ou teste que altera energia. |
| Comportamento esperado | Mostrar ação, motivo, risco e rollback; elevar somente após aceitar. |
| Exceções | Leitura não sensível não exige confirmação. |
| Requisitos relacionados | RF006,RF007,RF024,RF029 |

## RN007 — Snapshot antes de mutação

| Campo | Especificação |
| --- | --- |
| Descrição | Nenhuma alteração gerenciada ocorre sem captura do estado anterior. |
| Justificativa | Permite recuperação. |
| Condições | Operação reversível proposta. |
| Comportamento esperado | Persistir e verificar snapshot antes de executar. |
| Exceções | Ação realmente não reversível deve ser bloqueada no MVP ou declarada e confirmada separadamente. |
| Requisitos relacionados | RF006-RF008,RF029 |

## RN008 — VPN não substitui autenticação

| Campo | Especificação |
| --- | --- |
| Descrição | Alcance pela tailnet não concede autorização de wake. |
| Justificativa | Defesa em profundidade. |
| Condições | Qualquer conexão launcher→bridge. |
| Comportamento esperado | Exigir chave individual, host pinning e target autorizado. |
| Exceções | Nenhuma exceção. |
| Requisitos relacionados | RF009,RF016 |

## RN009 — Bridge saudável antes do wake

| Campo | Especificação |
| --- | --- |
| Descrição | Não solicitar wake se VPN, bridge ou wrapper não estiver saudável. |
| Justificativa | Evita falha previsível. |
| Condições | PC offline. |
| Comportamento esperado | Bloquear ação e apresentar camada indisponível. |
| Exceções | Usuário pode repetir verificação, não forçar comando. |
| Requisitos relacionados | RF010,RF017 |

## RN010 — Sem shell irrestrito

| Campo | Especificação |
| --- | --- |
| Descrição | Chave do launcher só executa wrapper autorizado, sem PTY, forwarding ou comando livre. |
| Justificativa | Reduz impacto de comprometimento. |
| Condições | Instalação/uso de SSH. |
| Comportamento esperado | authorized_keys usa forced command/restrições; wrapper ignora shell. |
| Exceções | Acesso técnico geral usa outra ferramenta/credencial fora do produto e não é gerenciado. |
| Requisitos relacionados | RF010-RF012,RF017 |

## RN011 — Credencial individual e revogável

| Campo | Especificação |
| --- | --- |
| Descrição | Cada notebook possui chave própria; remoção invalida somente aquela identidade. |
| Justificativa | Responsabilização e resposta a perda. |
| Condições | Pareamento, troca ou revogação. |
| Comportamento esperado | Nunca reutilizar privada; remover pública e material local. |
| Exceções | MVP possui um notebook, mas modelo suporta identidade individual. |
| Requisitos relacionados | RF011,RF012,RF027,RF030 |

## RN012 — Pedido fresco e limitado

| Campo | Especificação |
| --- | --- |
| Descrição | Wake exige request ID, nonce, timestamp e alvo permitido. |
| Justificativa | Evita replay e abuso. |
| Condições | Wrapper recebe solicitação. |
| Comportamento esperado | Validar janela ±60 s, nonce 10 min, cooldown 15 s e 3 pedidos/5 min. |
| Exceções | Relógio inválido retorna ERR011 sem executar. |
| Requisitos relacionados | RF011,RF017,RF018 |

## RN013 — Identidade remota conhecida obrigatória

| Campo | Especificação |
| --- | --- |
| Descrição | Host SSH ou certificado mTLS desconhecido, alterado, expirado ou revogado nunca é aceito silenciosamente. |
| Justificativa | Previne MITM. |
| Condições | Primeira conexão, troca de host key ou mudança no certificado do ReadinessAgent/launcher. |
| Comportamento esperado | Fixar fingerprints no pareamento, validar cadeia temporal e revogação local; qualquer divergência bloqueia. |
| Exceções | Troca legítima fora da janela de rotação autorizada exige novo pareamento presencial. |
| Requisitos relacionados | RF012,RF016,RF018,RF027 |

## RN014 — Segredos fora de logs e banco

| Campo | Especificação |
| --- | --- |
| Descrição | Chaves privadas, tokens, senhas e conteúdo de credencial não são persistidos nesses meios. |
| Justificativa | Reduz exposição. |
| Condições | Qualquer evento/persistência/exportação. |
| Comportamento esperado | Guardar somente referência/fingerprint; sanitizar antes de gravar. |
| Exceções | Material público pode ser registrado quando necessário. |
| Requisitos relacionados | RF008,RF025,RF026,RF030 |

## RN015 — Minimização de dados

| Campo | Especificação |
| --- | --- |
| Descrição | Coletar e exibir somente dados necessários ao objetivo e perfil. |
| Justificativa | Privacidade e UX progressiva. |
| Condições | Inventário, notificação ou exportação. |
| Comportamento esperado | Mascarar MAC/IP; solicitar consentimento na exportação. |
| Exceções | Detalhes técnicos completos podem ser incluídos pelo técnico com aviso, nunca segredos. |
| Requisitos relacionados | RF009,RF013,RF023,RF025,RF026 |

## RN016 — Software externo validado

| Campo | Especificação |
| --- | --- |
| Descrição | Executáveis, instaladores e atualizações devem ter origem, caminho e integridade verificados. |
| Justificativa | Evita execução indevida. |
| Condições | Instalação, abertura ou update. |
| Comportamento esperado | Usar APIs de processo sem shell, allowlist de argumentos e assinatura/hash. |
| Exceções | Build de desenvolvimento pode ser não assinado, claramente marcado e nunca canal estável. |
| Requisitos relacionados | RF007,RF028,RF030 |

## RN017 — Diagnóstico não é promessa

| Campo | Especificação |
| --- | --- |
| Descrição | Falha de consulta ou propriedade ausente produz inconclusivo, não incompatível automático. |
| Justificativa | Drivers podem omitir dados. |
| Condições | Coleta parcial/contraditória. |
| Comportamento esperado | Mostrar evidência e confiança separadamente. |
| Exceções | Impedimento confirmado pode gerar incompatível com justificativa. |
| Requisitos relacionados | RF001,RF004 |

## RN018 — Argumentos tipados

| Campo | Especificação |
| --- | --- |
| Descrição | Aplicativo remoto só recebe argumentos definidos pelo adapter. |
| Justificativa | Previne injeção. |
| Condições | Configuração e abertura. |
| Comportamento esperado | Construir ArgumentList; proibir shell e texto livre no MVP. |
| Exceções | Perfil personalizado aceita somente placeholders allowlisted. |
| Requisitos relacionados | RF014,RF021 |

## RN019 — Serviço pronto antes de abrir

| Campo | Especificação |
| --- | --- |
| Descrição | Cliente remoto abre somente após critério de prontidão do adapter. |
| Justificativa | Evita falsa conclusão. |
| Condições | Windows disponível. |
| Comportamento esperado | Executar sonda específica e aguardar sucesso. |
| Exceções | Usuário avançado pode abrir manualmente após aviso; evento registra override. |
| Requisitos relacionados | RF014,RF020,RF021 |

## RN020 — Falhas por camada

| Campo | Especificação |
| --- | --- |
| Descrição | Estados de VPN, bridge, wake, Windows e app remoto são independentes. |
| Justificativa | Suporte acionável. |
| Condições | Qualquer falha operacional. |
| Comportamento esperado | Gerar código específico, causa provável e ação. |
| Exceções | Evidência insuficiente usa ERR020/estado desconhecido, sem inventar causa. |
| Requisitos relacionados | RF015,RF016,RF019,RF020,RF022 |

## Revisão cruzada

As regras cobrem explicitamente PC já ligado, bridge offline, prontidão do aplicativo, consentimento, teste real, proteção de segredos, shell restrito, revogação, rollback, diagnóstico por camada, MAC automático e experiência sem terminal.
