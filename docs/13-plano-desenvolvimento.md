# Plano de desenvolvimento

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

## Estratégia

Fatia vertical primeiro; infraestrutura real atrás de ports; mocks/fakes por padrão; hardware somente em testes explicitamente autorizados. Um marco não avança sem seus testes.

| Marco | Entrega | Dependências | Riscos principais | Entrada | Saída/testes | Demonstração |
| --- | --- | --- | --- | --- | --- | --- |
| M0 Fundação | Solution, domínio, erros, estado, CI, logs básicos | ADR-002/008 | RSK020 | docs aprovados | CT048-050; build | máquina de estados simulada |
| M1 Prova vertical | CLI técnica/har ness de teste: Tailscale→SSH→wrapper→WoL→sonda | M0, Android físico | RSK001,004,006,008 | fakes e autorização do laboratório | CT010-018 em ambiente controlado | PC acorda por outra rede |
| M2 Launcher mínimo | WPF dashboard/progresso/erros, PC/bridge, RustDesk | M1 | RSK016 | cadeia comprovada | CT015-023, RNF001-006 | botão liga e abre |
| M3 Diagnóstico/Agent | diagnóstico Windows e ReadinessAgent HTTPS/mTLS do ADR-010 | M2; ADR-010 | RSK002,003,007,009,017,022 | threat model e certificados de teste | CT001-005, CT014-020, CT038 e segurança de contrato | diferencia rede/Windows/serviço |
| M4 Configurador seguro | consentimento, broker, snapshot, BIOS, pareamento | M3 | RSK007,009,013,014 | IPC especificado | CT006-013, CT027,029 | instalar/configurar/revogar/restaurar |
| M5 Produto instalável | MSI, update manual, uninstall, export, manuais | M4; certificado preview | RSK012,018,019 | versão integrada | CT025-030, CT040-047, CT051 | ciclo completo em Windows limpo |
| M6 Homologação MVP | matriz hardware, usabilidade, 30 execuções, auditoria | M5 | todos | zero risco crítico aberto | MVP-001–007 e CT001-052 | instalação até desinstalação |

## Ordem interna por marco

1. Teste/fake do contrato.
2. Domínio/aplicação.
3. Adapter mínimo.
4. UI/experiência.
5. Integração controlada.
6. Testes de falha e segurança.
7. Documentação e demonstração.

## Spikes obrigatórios

* SPK001: confirmar propriedades WoL em 3 NICs.
* SPK002: resiliência Termux em 3 OEMs por 24 h.
* SPK003 — resolvido pelo ADR-010: validar em código Kestrel/mTLS, provisionamento, firewall, rotação e contrato v1.
* SPK004: confirmar detecção/prontidão RustDesk sem API não documentada.
* SPK005: prova WiX de serviço + rollback.

## Gestão de risco

RSK021 foi mitigado pelo ADR-010. RSK007/009/022 bloqueiam a saída de M3 se os testes mTLS/firewall falharem; RSK008/013 bloqueiam M4; RSK012/018 bloqueiam M5; RSK004/017 bloqueiam homologação se metas não forem atendidas.

## Ambientes

Desenvolvimento usa fakes. Integração usa VMs e sshd isolado. E2E real requer aprovação explícita, PC de laboratório e plano de recuperação. Nunca alterar a máquina do desenvolvedor em teste automatizado.
