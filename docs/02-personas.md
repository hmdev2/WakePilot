# Personas e perfis de acesso

## Controle

Versão 1.0.0 — Estado: Aprovado — Data: 14/07/2026.

## PER001 — Usuário comum

* **Contexto:** utiliza notebook Windows fora de casa para acessar um único PC previamente configurado.
* **Conhecimentos:** sabe abrir aplicações e interpretar mensagens usuais; não conhece SSH, MAC, broadcast ou estados de energia.
* **Objetivos:** saber se o PC pode ser acessado, ligá-lo e abrir o aplicativo remoto.
* **Dificuldades:** mensagens técnicas, etapas fragmentadas, incerteza sobre quando o PC terminou de iniciar.
* **Necessidades:** botão único, progresso, tentativa cancelável, orientação clara e acessibilidade.
* **Permissões:** consultar estado, solicitar ativação, cancelar espera, abrir app remoto e consultar ajuda básica.
* **Restrições:** não altera segurança, credenciais, hardware, VPN ou configuração administrativa.
* **Jornada:** abre launcher → visualiza estado → aciona “Ligar e conectar” → acompanha → conecta ou recebe orientação.
* **Critério de sucesso:** conclui a jornada sem terminal, MAC, IP ou apoio técnico.

## PER002 — Técnico instalador

* **Contexto:** possui acesso físico durante instalação e responde pela configuração.
* **Conhecimentos:** Windows, rede local, permissões administrativas e leitura de instruções de firmware.
* **Objetivos:** diagnosticar, configurar, parear, testar, exportar evidência e restaurar.
* **Dificuldades:** variações de driver/BIOS/Android e interferência de políticas de segurança.
* **Necessidades:** inventário confiável, justificativa de cada alteração, consentimento, snapshot, logs e teste real.
* **Permissões:** todas as funções de configuração local, mediante autenticação do Windows e elevação pontual.
* **Restrições:** não recebe shell remoto; não ignora verificações de host; não vê segredos em claro.
* **Jornada:** diagnóstico → plano de alterações → consentimento → BIOS guiada → VPN/Android → pareamento → teste → entrega.
* **Critério de sucesso:** ambiente classificado e relatório sem pendências críticas.

## PER003 — Usuário avançado

* **Contexto:** proprietário que deseja ajustar timeouts, aplicativo remoto e dispositivos.
* **Conhecimentos:** entende redes básicas, mas não deve manipular segredos diretamente.
* **Objetivos:** trocar perfil remoto, revisar diagnósticos, revogar e repetir testes.
* **Necessidades:** área técnica progressiva e validações preventivas.
* **Permissões:** configurações não privilegiadas; operações privilegiadas exigem fluxo técnico e consentimento.
* **Restrições:** não contorna políticas de segurança nem edita banco/configuração manualmente.
* **Jornada:** abre configurações avançadas → altera opção permitida → valida → salva → testa.

## PER004 — Suporte

* **Contexto:** atende remotamente sem controlar diretamente o equipamento.
* **Conhecimentos:** códigos de erro, matriz de compatibilidade e procedimentos de recuperação.
* **Objetivos:** localizar a camada de falha e orientar ação mínima.
* **Necessidades:** relatório sanitizado, correlation ID, linha do tempo, versões e passos recomendados.
* **Permissões:** leitura de artefato exportado; nenhuma credencial ou alteração remota implícita.
* **Restrições:** dados técnicos compartilhados apenas com consentimento do usuário.

## Matriz de permissões

| Capacidade | Comum | Técnico | Avançado | Suporte |
| --- | --- | --- | --- | --- |
| Ligar e conectar | Sim | Sim | Sim | Não |
| Ver detalhes técnicos | Limitado | Sim | Sim | Via relatório |
| Alterar app remoto | Não | Sim | Sim | Não |
| Configurar Windows/Android | Não | Sim | Parcial | Não |
| Parear/revogar | Não | Sim | Sim | Não |
| Exportar diagnóstico | Sim, sanitizado | Sim | Sim | Receber |
| Restaurar/desinstalar | Não | Sim | Com elevação | Não |
| Acessar segredos | Não | Não em claro | Não | Não |

## Perfis não criados

Administrador corporativo e operador de frota não são personas do MVP; suas necessidades implicariam multiusuário, políticas centralizadas e backend, fora do escopo.
