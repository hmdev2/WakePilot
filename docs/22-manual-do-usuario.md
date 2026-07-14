# Manual do usuário

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

## Antes de começar

O Remote Wake Assistant liga um computador já configurado e abre o aplicativo de acesso remoto. A configuração inicial deve ser feita por alguém com conhecimento técnico e acesso físico ao computador e ao celular de ativação.

O celular de ativação precisa permanecer ligado, carregando e conectado ao Wi-Fi do local onde está o computador.

## Ligar e conectar

1. Abra o Remote Wake Assistant no notebook.
2. Aguarde a verificação dos dois estados: “Computador principal” e “Celular de ativação”.
3. Selecione **Ligar e conectar**.
4. Acompanhe as etapas: enviando solicitação, iniciando computador, aguardando Windows e aguardando RustDesk.
5. Quando tudo estiver pronto, o RustDesk será aberto.

Você não precisa informar endereço, número da placa de rede ou executar comandos.

## Se o computador já estiver ligado

O aplicativo não enviará uma nova solicitação para ligar. Ele verificará o RustDesk e o abrirá.

## Cancelar

Durante a espera, selecione **Cancelar**. A verificação para e nenhuma nova repetição será enviada. Cancelar não desliga um computador que já começou a iniciar.

## Mensagens comuns

| Mensagem | Significado | O que fazer |
| --- | --- | --- |
| VPN desconectada | O notebook não está conectado à rede privada | Abra/reconecte o Tailscale e tente novamente |
| Celular de ativação offline | O celular não respondeu | Verifique energia, Wi-Fi e Tailscale do celular |
| Identidade do celular mudou | A chave de identificação não coincide | Não aceite; peça novo pareamento ao técnico |
| Identidade do computador mudou | O certificado de prontidão não coincide, expirou ou foi revogado | Não prossiga; peça ao técnico para verificar e refazer o pareamento presencial |
| Ativação não confirmada | A solicitação foi enviada, mas o PC não respondeu | Confira energia/cabo e peça diagnóstico de BIOS/Windows |
| Windows não ficou pronto | O PC pode ter ligado, mas não iniciou normalmente | Aguarde ou solicite verificação física |
| RustDesk indisponível | O Windows respondeu, mas o serviço remoto não | Verifique o RustDesk no PC ou chame o técnico |
| Cliente não abriu | O aplicativo do notebook está ausente/alterado | Reinstale ou reconfigure o RustDesk |

## Atualizar estado

Use **Atualizar** no dashboard. Essa ação apenas verifica; não liga o computador.

## Exportar diagnóstico

Em Ajuda → Exportar diagnóstico, escolha o período, revise as categorias e gere o arquivo. O aplicativo mascara dados técnicos e não inclui senhas ou chaves. Compartilhe somente com uma pessoa de confiança.

## Privacidade

O MVP não envia telemetria para um servidor do Remote Wake Assistant. Dados ficam nos dispositivos. Um diagnóstico só sai do notebook quando você o compartilha.

## Segurança

Nunca envie arquivos de chave, certificado privado ou senha ao suporte. Se perder o notebook ou celular, peça a revogação do dispositivo. Se aparecer “identidade do celular mudou” ou “identidade do computador mudou”, interrompa e faça novo pareamento presencial.

## Limitações

A ativação depende de energia, cabo Ethernet, firmware, Windows, celular, Wi-Fi e Tailscale. Alguns computadores não acordam de todos os tipos de desligamento. “Compatível e validado” vale para o equipamento e estado realmente testados.

## Acessibilidade

Todas as ações podem ser feitas por teclado; estados possuem texto além de cor; a interface oferece leitor de tela e ampliação. Se uma notificação estiver desativada, o resultado continua visível na janela.
