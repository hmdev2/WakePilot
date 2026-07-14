# Matriz de rastreabilidade

## Controle

Versão 1.1.0 — Estado: Aprovado — Data: 14/07/2026.

| Objetivo | Requisito | Regra | Processo/fluxo | Tela | Componente | História | Teste |
| --- | --- | --- | --- | --- | --- | --- | --- |
| OBJ002 | RF001 | RN001,RN017 | PRC-001 | TEL003 | CMP006 | US003 | CT001 |
| OBJ002 | RF002 | RN002,RN003 | PRC-001 | TEL003 | CMP006 | US004 | CT002 |
| OBJ003 | RF003 | RN001,RN004 | PRC-001 | TEL004 | CMP006 | US005 | CT003 |
| OBJ003 | RF004 | RN004,RN017 | PRC-001 | TEL004 | CMP006 | US005 | CT004 |
| OBJ003 | RF005 | RN005 | PRC-003 | TEL006 | CMP001 | US006 | CT005 |
| OBJ006 | RF006 | RN006,RN007 | PRC-002 | TEL005 | CMP001 | US007 | CT006 |
| OBJ002,OBJ006 | RF007 | RN006,RN007,RN016 | PRC-002 | TEL005 | CMP005 | US008 | CT007 |
| OBJ006 | RF008 | RN007,RN014 | PRC-002,PRC-021 | TEL005,TEL021 | CMP009 | US009 | CT008 |
| OBJ001,OBJ005 | RF009 | RN008,RN015 | PRC-004 | TEL007 | CMP012 | US010 | CT009 |
| OBJ001,OBJ004 | RF010 | RN009,RN010 | PRC-005 | TEL008 | CMP013 | US011 | CT010 |
| OBJ005 | RF011 | RN010,RN011,RN012 | PRC-006 | TEL009 | CMP010 | US012 | CT011 |
| OBJ005 | RF012 | RN010,RN011,RN013 | PRC-007 | TEL009 | CMP003,CMP013 | US013 | CT012 |
| OBJ001 | RF013 | RN002,RN015 | PRC-017 | TEL012 | CMP008 | US014 | CT013 |
| OBJ001,OBJ005 | RF014 | RN018,RN019 | PRC-016 | TEL013 | CMP014 | US015 | CT014 |
| OBJ004 | RF015 | RN020,RN001 | PRC-009,PRC-010 | TEL012 | CMP003 | US016 | CT015 |
| OBJ004 | RF016 | RN008,RN013,RN020 | PRC-013 | TEL012,TEL018 | CMP003 | US017 | CT016 |
| OBJ001,OBJ005 | RF017 | RN001,RN009,RN010,RN012 | PRC-009 | TEL014 | CMP003,CMP013 | US018 | CT017 |
| OBJ005 | RF018 | RN012,RN013 | PRC-009 | — | CMP013 | US018 | CT018 |
| OBJ001,OBJ004 | RF019 | RN020,RN005 | PRC-011,PRC-015 | TEL014 | CMP003 | US019 | CT019 |
| OBJ004 | RF020 | RN019,RN020 | PRC-011 | TEL014 | CMP014 | US020 | CT020 |
| OBJ001,OBJ005 | RF021 | RN018,RN019 | PRC-012 | TEL015 | CMP014 | US021 | CT021 |
| OBJ004 | RF022 | RN020 | PRC-013-015 | TEL018 | CMP011 | US022 | CT022 |
| OBJ001 | RF023 | RN015 | PRC-009 | TEL014 | CMP001 | US023 | CT023 |
| OBJ003 | RF024 | RN004,RN006 | PRC-008 | TEL010 | CMP003 | US024 | CT024 |
| OBJ004,OBJ005 | RF025 | RN014,RN015 | PRC-020 | TEL019 | CMP011 | US025 | CT025 |
| OBJ004,OBJ005 | RF026 | RN014,RN015 | PRC-020 | TEL020 | CMP011 | US026 | CT026 |
| OBJ005 | RF027 | RN011,RN013 | PRC-018 | TEL017 | CMP010 | US027 | CT027 |
| OBJ005,OBJ006 | RF028 | RN016 | PRC-019 | TEL022 | CMP015 | US028 | CT028 |
| OBJ006 | RF029 | RN006,RN007 | PRC-021 | TEL021 | CMP005,CMP009 | US029 | CT029 |
| OBJ005,OBJ006 | RF030 | RN011,RN014,RN016 | PRC-022 | TEL023 | CMP015 | US030 | CT030 |

## Coberturas complementares

* RNF001–RNF022 → CMP001–CMP015 → CT031–CT052.
* TEL001–TEL023 possuem requisitos relacionados em 10-telas-e-fluxos.md.
* CMP001–CMP015 possuem justificativa e drivers em 08-arquitetura.md.
* INT001–INT017 possuem erro, timeout, retry, fallback e teste em 09-interfaces-e-integracoes.md.
* RSK001–RSK022 possuem mitigação/contingência em 20-riscos-e-contingencias.md.
* RF018 não precisa de tela própria: seu estado aparece em TEL014/TEL018 pelo fluxo RF017.
* RF025 é visível em TEL019; RF028 em TEL022; RF029 em TEL021; RF030 em TEL023.

## Auditoria de órfãos

* Requisitos sem história: 0.
* Requisitos sem teste: 0.
* Requisitos P0 sem processo/fluxo: 0.
* Telas sem requisito: 0.
* Componentes sem justificativa: 0.
* Testes sem requisito: 0.
* ADR-010 resolve o protocolo do ReadinessAgent. RSK021 passa a risco de implementação mitigado e SPK003 torna-se validação técnica do contrato, sem decisão arquitetural aberta.
