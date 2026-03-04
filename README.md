# Smart Payment Router (ML-based)

Payment router built with **.NET** (microservices) to pick the **best acquirer/processor** per transaction and increase the **approval rate**.

The idea is straightforward: given **brand**, **amount**, **installments**, **BIN**, **time-of-day**, etc., the system computes a score per processor and returns the `processor_id` with the highest estimated chance of approval.

## How this fits into the payment flow

This system **does not execute payments**.

A different service (Checkout / Payment Orchestrator / Payments API) executes the payment. It uses this Router like this:

1. Before sending the transaction to any acquirer, it calls:
   - `POST /processors/route`
2. It receives the recommended `processor_id`.
3. It runs the payment against the chosen acquirer.
4. After the outcome is known, it sends the result back here:
   - `POST /transactions`

No feedback loop = no history = models don’t improve.

## Microservices

### API Gateway
Single public entrypoint. Routes traffic to internal services and can centralize the “boring stuff” (auth, rate limiting, correlation-id, logs).

### Selector Service (Decision)
Where the routing decision happens (this needs to be fast).

Endpoints:
- `POST /processors` — register a processor
- `POST /processors/{id}/deactivate` — disable a processor
- `POST /processors/route` — choose a processor for a transaction

Also consumes RabbitMQ events:
- `ProcessorModelUpdated` — a new model arrived, update the active version

Database (PostgreSQL):
- `processors` — enabled/disabled processors
- `processor_models` — versioned models

### Transactions Service (Ingestion)
Just stores what actually happened.

Endpoint:
- `POST /transactions`

Database (MongoDB):
- transactions + outcomes (APPROVED/DECLINED, etc.)

### Analytics/Training
Offline training. Reads history from Mongo, trains models, publishes:
- `ProcessorModelUpdated`

## Models / inference (pluggable runtimes)

The Selector shouldn’t be locked into one ML algorithm forever. It just calls an executor.

- **Now: `LinearJsonModelExecutor`**
  - linear model (logistic regression style)
  - `intercept` + `weights_json`
  - easy to inspect and debug

- **Later: `OnnxModelExecutor`**
  - for GBDT / bigger models / deep learning
  - ONNX Runtime in .NET
  - versioned artifacts (e.g. `artifact_uri` + `sha256`)

## Endpoints (v1)

Selector (via Gateway):
- `POST /processors`
- `POST /processors/{id}/deactivate`
- `POST /processors/route`

Transactions (via Gateway):
- `POST /transactions`

> `/processors/route` is POST (not GET) because the payload is usually large and may include sensitive data (e.g. BIN). Querystring is not a good place for that.

## Dev infra

- PostgreSQL (Selector)
- MongoDB (Transactions)
- RabbitMQ (model update event)
