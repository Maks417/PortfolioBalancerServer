# Platform roadmap (deferred)

The Portfolio Balancer API is intentionally **stateless**. It does not use a database, background workers, or user accounts.

## Deferred capabilities

- Authenticated saved portfolios
- Calculation history
- Broker import and trade execution
- Tax-lot aware recommendations

These require persistent storage, identity management, and significantly broader compliance scope. They should not be added until product usage validates the need.

## Current API surface

| Endpoint | Purpose |
| --- | --- |
| `GET /api/portfolio/rates` | Live FX metadata for preview and transparency |
| `POST /api/portfolio/calculate` | Contribution or full rebalance calculation |
| `GET /health` | Liveness |
| `GET /health/ready` | Readiness (FX provider availability) |

See `Contracts/` for golden request/response fixtures used in tests.
