# WakePilot — pacote de documentação 1.1.0

Este arquivo acompanha a documentação completa do projeto, incluindo a decisão definitiva do protocolo do ReadinessAgent no ADR-010.

## Como juntar ao projeto no seu PC

1. Faça uma cópia de segurança da pasta `docs` atual.
2. Extraia o ZIP na raiz do projeto, isto é, na pasta que já contém ou deverá conter `docs`.
3. Autorize a mesclagem e a substituição dos arquivos com o mesmo nome.
4. Abra `docs/README.md` para consultar o índice e a ordem de leitura.

O pacote preserva a estrutura completa `docs/`, `docs/adrs/`, `docs/processos/` e `docs/wireframes/`.

## Decisão incorporada

O ReadinessAgent usa HTTPS/1.1 JSON sobre Tailscale, autenticação mTLS bilateral, certificados X.509 ECDSA P-256 fixados por dispositivo, chaves privadas não exportáveis, porta dinâmica persistida e firewall restrito. O serviço é somente leitura e não oferece fallback sem autenticação.

## Nome sugerido

`WakePilot` é um nome curto e memorável para o produto. Antes de uso comercial, valide disponibilidade de marca, domínio e nome de aplicativo.
