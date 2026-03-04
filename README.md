# Smart Payment Router (ML-based) — Microserviços para maximizar aprovação de adquirentes

Sistema em **.NET** (microserviços) para **maximizar a taxa de aprovação** (approval rate) escolhendo, para cada tentativa de pagamento, o **melhor adquirente/processor** com base em dados históricos (bandeira, valor, parcelas, BIN, horário, etc.).

## Como o sistema é utilizado (contexto)

Este sistema **não executa o pagamento**.

Ele é chamado por um **sistema externo** (ex.: Checkout / Payment Orchestrator) **antes** de decidir qual adquirente usar:

1. O sistema externo recebe uma tentativa de pagamento.
2. Ele chama este Router para recomendação:
   - `POST /processors/route`
3. O Router responde com o `processor_id` recomendado (e metadados como `score`).
4. O sistema externo executa o pagamento no adquirente escolhido.
5. Depois, o sistema externo registra o resultado (aprovado/negado) para alimentar aprendizado:
   - `POST /transactions`

## Arquitetura de microserviços (alto nível)

### Microserviços

**1) API Gateway**
- Único entrypoint público
- Responsável por roteamento para os microserviços internos
- (Opcional) autenticação, rate limit, correlation-id, logs

**2) Selector Service (Routing/Decision)**
- Responsável pela decisão online (baixa latência): **qual processor maximiza aprovação**
- Expõe:
  - `POST /processors` — cadastra processor
  - `POST /processors/{id}/deactivate` — desativa processor
  - `POST /processors/route` — recomenda processor para uma transação
- Consome eventos (RabbitMQ):
  - `ProcessorModelUpdated` — atualiza o modelo ativo por processor
- Banco (PostgreSQL):
  - `processors` (estado/config do adquirente)
  - `processor_models` (modelos versionados para inferência)

**3) Transactions Service (Ingestion)**
- Responsável por registrar transações executadas e seus resultados
- Expõe:
  - `POST /transactions`
- Banco (MongoDB):
  - histórico de transações (fonte para treino offline)

**4) Analytics/Training (offline)**
- Treina modelos para prever `P(aprovado | features)`
- Publica evento (RabbitMQ):
  - `ProcessorModelUpdated`

## Fluxo de modelos (offline → online)

1. O **Analytics** treina um modelo por processor (versão + janela de treino).
2. Publica `ProcessorModelUpdated`.
3. O **Selector Service** consome o evento e faz upsert em `processor_models` no Postgres (ativando a versão nova).
4. No `POST /processors/route`, o Selector carrega modelos ativos, calcula scores e retorna o `processor_id` com maior valor.

## Endpoints (v1)

### Selector Service (via Gateway)
- `POST /processors`
- `POST /processors/{id}/deactivate`
- `POST /processors/route`

### Transactions Service (via Gateway)
- `POST /transactions`

> Nota: `POST /processors/route` é POST (não GET) para evitar expor dados sensíveis (ex.: BIN) em querystring e por ser uma decisão calculada.

## Model runtime (pluggable)

O Selector suporta múltiplos executores de modelos (runtimes), permitindo evolução do algoritmo sem reescrever o serviço.

- **Hoje: `LinearJsonModelExecutor`**
  - Modelo linear (ex.: regressão logística) representado por:
    - `intercept`
    - `weights_json` (peso por feature bucketizada)
  - Inferência rápida (dot-product + sigmoid)

- **Futuro: `OnnxModelExecutor`**
  - Para modelos mais complexos (GBDT / deep learning) exportados para ONNX
  - Inferência via ONNX Runtime em .NET
  - Artefato versionado por `artifact_uri` + `sha256` (recomendado)

## Infra (dev)

- PostgreSQL: configurações e modelos (Selector)
- MongoDB: histórico de transações (Transactions Service)
- RabbitMQ: distribuição de modelos (Analytics → Selector)
