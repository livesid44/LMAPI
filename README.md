# LMAPI

A .NET 8 Web API gateway that proxies the **LogicMonitor REST API v3**, with an integrated **MCP (Model Context Protocol) server** so AI assistants (e.g. Claude) can query your LogicMonitor data directly.

---

## Configuration

Two settings are **required** before the application will start. If any setting still holds its placeholder value the app exits immediately with an error message rather than producing a confusing DNS failure at request time.

| Setting | Environment variable | Description |
|---|---|---|
| `LogicMonitor:Company` | `LogicMonitor__Company` | Your LM account subdomain, e.g. `acme` → `acme.logicmonitor.com` |
| `LogicMonitor:BearerToken` | `LogicMonitor__BearerToken` | LM API Bearer token |

### Option A — Environment variables (recommended for production / containers)

```bash
export LogicMonitor__Company=acme
export LogicMonitor__BearerToken=lmb_xxxxxxxxxxxxxxxxxxxxxxxx
dotnet run --project src/LMAPI
```

### Option B — .NET user secrets (recommended for local development)

```bash
cd src/LMAPI
dotnet user-secrets set "LogicMonitor:Company"     "acme"
dotnet user-secrets set "LogicMonitor:BearerToken" "lmb_xxxxxxxxxxxxxxxxxxxxxxxx"
dotnet run
```

### Option C — `appsettings.Development.json` (never commit real credentials)

Edit `src/LMAPI/appsettings.Development.json`:

```json
{
  "LogicMonitor": {
    "Company":     "acme",
    "BearerToken": "lmb_xxxxxxxxxxxxxxxxxxxxxxxx"
  }
}
```

> **Note:** `appsettings.Development.json` is not tracked by `.gitignore` by default — use user secrets or environment variables if you don't want credentials in your working tree.

### Azure Container Apps (production)

The Bicep template in `infra/main.bicep` provisions Key Vault secrets and wires them into the Container App automatically via Managed Identity.

---

## API endpoints

| Method | Path | Description |
|---|---|---|
| `GET` | `/api/devices` | List all devices (paged: `size`, `offset`, `filter`) |
| `GET` | `/api/devices/{id}` | Get single device by ID |
| `GET` | `/api/devices/{id}/events` | Get device events |
| `GET` | `/api/devices/{id}/alerts` | Get device alerts |
| `POST` | `/mcp` | MCP (Model Context Protocol) server |
| `GET` | `/health` | Liveness / readiness probe |

Swagger UI is available at `/swagger` in the Development environment.

## MCP tools

| Tool | Description |
|---|---|
| `list_devices` | List all org devices (optional `size`, `offset`, `filter`) |
| `get_device` | Get a device by ID |
| `get_device_events` | Get events for a device |
| `get_device_alerts` | Get alerts for a device |
| `search_log_events` | Search LM Logs |
