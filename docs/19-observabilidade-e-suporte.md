# Observabilidade e suporte

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

## Eventos e formato

JSON Lines UTF-8: utc, level, eventName, code, component, correlationId, durationMs, outcome, properties allowlisted. Níveis Trace (dev), Debug, Information, Warning, Error, Critical. Stable não registra payload bruto.

## Retenção

Rotação total em 20 MB; expiração 30 dias; limite de 5 arquivos; limpeza na inicialização e diária. Falha de log usa buffer de 100 eventos e aviso, sem bloquear operação comum.

## Métricas locais

Contagem de tentativas/sucesso/falha por camada; duração p50/p95; disponibilidade observada do bridge; quantidade de retries/replay rejeitado; falhas de rollback/update. Sem envio externo no MVP.

## Catálogo de erros

| Código | Significado | Ação principal |
| --- | --- | --- |
| ERR001 | Falha ao inventariar Windows | Repetir como técnico/consultar detalhes |
| ERR002 | Nenhum Ethernet físico elegível | Conectar/verificar cabo/adaptador |
| ERR003 | Adaptador ambíguo | Selecionar na área técnica |
| ERR004 | Energia/WoL inconclusivo | Revisar driver, powercfg e teste |
| ERR005 | Banco/configuração indisponível | Abrir modo recuperação/backup |
| ERR006 | Cofre/DPAPI indisponível | Não operar; reparar perfil |
| ERR007 | Operação privilegiada rejeitada | Revisar allowlist/consentimento |
| ERR008 | VPN local desconectada | Reconectar Tailscale |
| ERR009 | Android bridge inalcançável | Verificar energia, Wi-Fi e Tailscale |
| ERR010 | Identidade remota divergente — host SSH ou certificado mTLS | Bloquear e reparar presencialmente |
| ERR011 | Pedido expirado/repetido | Sincronizar relógio/nova tentativa |
| ERR012 | Wake rejeitado/sem recibo | Ver autenticação/rate limit/bridge |
| ERR013 | Ativação não confirmada | Revisar BIOS, energia, Ethernet |
| ERR014 | PC acessível, Windows não pronto | Aguardar/recuperação local |
| ERR015 | Serviço remoto indisponível | Ver RustDesk/serviço no PC |
| ERR016 | Cliente remoto não abriu | Revalidar caminho/instalação |
| ERR017 | Integração não suportada | Selecionar RustDesk/personalizado |
| ERR018 | Bootstrap/boot Android falhou | Revisar origem, Boot e bateria |
| ERR019 | Notificação indisponível | Usar feedback no aplicativo |
| ERR020 | Falha desconhecida | Exportar diagnóstico com correlation ID |
| ERR021 | ReadinessAgent/protocolo incompatível ou indisponível | Verificar versão, certificado, porta, Tailscale e firewall |

## Relatório de diagnóstico

Manifesto, versão, inventário sanitizado, matriz de estados, testes, alterações/snapshots sem valores secretos, eventos do período e hashes. Prévia obrigatória; scanner; ZIP local; envio é responsabilidade do usuário.

## Runbook de suporte

1. Obter ERR e correlation ID.
2. Confirmar versão e classificação.
3. Localizar última fase bem-sucedida.
4. Seguir ação do catálogo sem pedir segredo.
5. Se necessário, solicitar exportação sanitizada.
6. Escalar segurança para ERR006/007/010/012 repetido/018 de integridade/021 com falha de identidade.
7. Registrar solução e recomendar revalidação se hardware/versão mudou.

## Modo suporte

Somente leitura; detalhes técnicos ampliados após consentimento; expira ao fechar; não habilita shell, elevação ou envio automático.
