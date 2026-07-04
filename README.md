# Portfolio Balancer Server

Backend API for [portfolio-balancer-client](https://github.com/Maks417/portfolio-balancer-client). Given current stock/bond holdings, a target allocation ratio, and a new contribution, the API returns how much of the contribution should go to stocks vs. bonds.

**Live client:** https://maks417.github.io/portfolio-balancer-client

## API

### `POST /api/portfolio/calculate`

**Request body:**

```json
{
  "ratio": "70/30",
  "stockValues": [{ "value": 1000, "currency": "usd" }],
  "bondValues": [{ "value": 500, "currency": "rub" }],
  "contributionAmount": { "value": 200, "currency": "usd" }
}
```

**Ratio formats:** `100` (all stocks), `0` (all bonds), or `X/Y` where `X + Y = 100` (e.g. `70/30`).

**Supported currencies:** `rub`, `usd`, `eur` (case-insensitive). Empty position rows (`value: ""` or `0`) are ignored.

**Success response (`200`):**

```json
{
  "stocksDiff": 40.0,
  "bondsDiff": 60.0,
  "currency": "usd"
}
```

**Validation errors (`400`):** RFC 7807 `ValidationProblemDetails` with an `errors` object keyed by camelCase field names (`ratio`, `stockValues`, etc.).

**Exchange rate failures (`503`):** ProblemDetails when CBR rates are unavailable.

### Health

| Endpoint | Purpose |
|----------|---------|
| `GET /health` | Liveness |
| `GET /health/ready` | Readiness (checks exchange rate availability) |

## Configuration

| Key | Description | Default |
|-----|-------------|---------|
| `CurrencyServiceUrl` | CBR daily rates base URL | `https://www.cbr-xml-daily.ru/` |
| `EnableSwagger` | Expose Swagger UI | `false` (enabled in Development) |
| `EnableHttpsRedirection` | Redirect HTTP to HTTPS | `false` |
| `Cors:AllowedOrigins` | Allowed browser origins | GitHub Pages + `localhost:3000` |

## Local development

```bash
dotnet run
```

- API: http://localhost:5000
- Swagger: http://localhost:5000/swagger (Development)

## Docker

```bash
docker compose up --build
```

API available at http://localhost:5000

## Tests

```bash
dotnet test PortfolioBalancerServer.sln
```

## Tech stack

- .NET 10, ASP.NET Core Minimal APIs
- Exchange rates from [CBR XML Daily](https://www.cbr-xml-daily.ru/)
- Deployed to Azure Web App via GitHub Actions
