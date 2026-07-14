# Distribuição e atualizações

## Controle

Versão 1.1.0 — Estado: Aprovado com condição de assinatura — Data: 14/07/2026.

## Pacotes

* MSI/WiX x64 self-contained para desktop.
* Componentes: Launcher, Configurator, ReadinessAgent, PrivilegedBroker, runtime/bibliotecas e uninstaller.
* Bootstrap Android versionado, checksums e manifesto; não inclui auth key.
* Certificados privados do ReadinessAgent e do launcher nunca integram pacote, backup distribuível ou artefato de suporte.
* Símbolos separados e não distribuídos ao usuário final.

## Canais

dev (não assinado, marca d'água, somente testes); preview (assinatura obrigatória, testers); stable (assinatura, SBOM, suíte completa e rollback).

## Versionamento

SemVer MAJOR.MINOR.PATCH. Compatibilidade de protocolo e schema separadas. Launcher/bridge e Launcher/ReadinessAgent negociam versão; major incompatível bloqueia wake/readiness e orienta atualização.

## Integridade e assinatura

SHA-256 por artefato; manifesto assinado; Authenticode no MSI/EXEs stable; certificados/chaves fora do repositório e em ambiente protegido; timestamping. Build reproduzível quando viável e SBOM CycloneDX/SPDX.

## Atualização

Consulta manual no MVP; mostra versão/notas/impacto; baixa por TLS; verifica manifesto, assinatura e hash; fecha componentes; backup; instala; migra; health check; confirma ou restaura. Nunca instalar silenciosamente nem aceitar somente hash servido junto sem assinatura confiável.

## Compatibilidade

Matriz de versão para launcher, ReadinessAgent, bridge protocol, readiness protocol e schema. Atualizar bridge/agente antes do launcher quando o contrato exigir. Migrações são testadas de N-1 para N; downgrade com schema incompatível é bloqueado. Rotação de certificado começa 30 dias antes do vencimento, com sobreposição máxima de 7 dias.

## Rollback

Pacote anterior e backup mantidos até health check. Falha executa rollback automático uma vez e relata. Resultado incerto não é repetido cegamente.

## Desinstalação

Revogar credenciais, oferecer restaurar Windows, remover serviço/tarefas/firewall/app e apagar dados conforme escolha. Pendência do Android offline fica explícita.

## Gate de release

Assinatura válida; SBOM; zero vulnerabilidade crítica/alta não aceita; testes CT de instalação/update/rollback; scanner de segredo; licença de dependências; notas e manual atualizados.
