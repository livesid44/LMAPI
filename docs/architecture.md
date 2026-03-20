# LMAPI — Azure Architecture & Data Flow

## Overview

LMAPI is a .NET 8 Web API gateway that wraps the **LogicMonitor REST API v3**.  
When deployed on Azure it sits behind **Azure API Management (APIM)** and can be
consumed by any **MCP-compatible orchestrator** (e.g. Claude Desktop, Azure AI Foundry,
Copilot Studio, a custom Semantic Kernel agent) or called directly from an
**Azure Logic App / Azure Function** workflow.

---

## 1. Azure Deployment Topology

```mermaid
graph TB
    subgraph Internet["External Clients"]
        ORC["AI Orchestrator<br/>(Claude / Copilot Studio<br/>/ Azure AI Foundry)"]
        LA["Azure Logic App<br/>/ Azure Function"]
        DEV["Developer / CI-CD"]
    end

    subgraph Azure["Azure Subscription"]
        APIM["Azure API Management<br/>(APIM)<br/>─────────────────<br/>• Rate limiting<br/>• OAuth2 / API-key auth<br/>• Request transformation<br/>• Developer portal"]

        subgraph ACA["Azure Container Apps Environment"]
            APP["LMAPI Container App<br/>(.NET 8)<br/>─────────────────<br/>• /api/devices/{id}<br/>• /api/devices/{id}/events<br/>• /api/devices/{id}/alerts<br/>• /api/logs/events<br/>• /mcp  (MCP server)<br/>• /health"]
        end

        KV["Azure Key Vault<br/>─────────────────<br/>• LM AccessId<br/>• LM AccessKey<br/>• LM Company"]

        AI["Application Insights<br/>+ Log Analytics Workspace<br/>─────────────────<br/>• Request traces<br/>• Dependency calls<br/>• Exceptions<br/>• Custom metrics"]

        MI["User-Assigned<br/>Managed Identity"]

        ACR["Azure Container Registry<br/>(stores LMAPI image)"]
    end

    subgraph LM["LogicMonitor SaaS"]
        LMAPI_EXT["LM REST API v3<br/>santaba/rest/<br/>─────────────────<br/>• /device/devices<br/>• /alert/alerts<br/>• /log/events"]
    end

    ORC -- "HTTPS (JSON-RPC 2.0 / MCP)" --> APIM
    LA  -- "HTTPS (REST)"               --> APIM
    DEV -- "Swagger UI / REST"          --> APIM
    APIM -- "HTTPS (REST/MCP)"          --> APP
    APP  -- "HTTPS + LMv1 HMAC-SHA256"  --> LMAPI_EXT
    APP  -- "reads secrets"             --> KV
    APP  -- "telemetry"                 --> AI
    MI   -- "grants access"             --> KV
    APP  -- "uses identity"             --> MI
    DEV  -- "docker push"               --> ACR
    ACR  -- "pulls image"               --> APP
```

---

## 2. Request Data Flow — REST (direct / via Logic App)

```mermaid
sequenceDiagram
    participant C  as Client<br/>(Logic App / Function)
    participant AP as APIM
    participant LMA as LMAPI<br/>(Container App)
    participant KV as Key Vault
    participant LM as LogicMonitor<br/>REST API v3
    participant AI as App Insights

    C  ->> AP  : GET /api/devices/42/alerts<br/>Authorization: Bearer {APIM_token}
    AP ->> AP  : Validate token, apply rate-limit policy
    AP ->> LMA : GET /api/devices/42/alerts (forwarded)

    Note over LMA,KV: Secrets cached at startup via<br/>Azure Key Vault references
    LMA ->> LMA : Build LMv1 signature<br/>HMAC-SHA256(AccessKey, GET+epoch+path)

    LMA ->> LM  : GET /santaba/rest/alert/alerts?filter=monitorObjectId:42<br/>Authorization: LMv1 {id}:{sig}:{epoch}<br/>X-Version: 3
    LM  -->> LMA: 200 { status:200, data:{ total, items:[] } }

    LMA ->> AI  : Track dependency (LM API call latency)
    LMA -->> AP : 200 { total, items:[] }
    AP  -->> C  : 200 { total, items:[] }
```

---

## 3. MCP / Orchestrator Data Flow

```mermaid
sequenceDiagram
    participant AGT as AI Agent<br/>(Claude / Copilot Studio<br/>/ Semantic Kernel)
    participant MCP as LMAPI /mcp<br/>(MCP Server)
    participant SVC as LogicMonitorService
    participant LM  as LogicMonitor<br/>REST API v3

    AGT ->> MCP : POST /mcp<br/>{ jsonrpc:"2.0", method:"initialize", params:{...} }
    MCP -->> AGT: { result: { protocolVersion, serverInfo, capabilities:{tools:{}} } }

    AGT ->> MCP : POST /mcp<br/>{ method:"tools/list" }
    MCP -->> AGT: { result: { tools: [get_device, get_device_events,<br/>                                  get_device_alerts, search_log_events] } }

    Note over AGT: Agent decides to check alerts<br/>for device 42

    AGT ->> MCP : POST /mcp<br/>{ method:"tools/call",<br/>  params:{ name:"get_device_alerts",<br/>           arguments:{ deviceId:42, severity:2 } } }
    MCP ->> SVC : GetDeviceAlertsAsync(42, filter:"severity:2")
    SVC ->> LM  : GET /alert/alerts?filter=monitorObjectId:42,severity:2
    LM -->> SVC : { total:3, items:[...] }
    SVC -->> MCP: LogicMonitorListData<DeviceAlert>
    MCP -->> AGT: { result: { content:[{ type:"text", text:"3 critical alerts..." }] } }

    Note over AGT: Agent summarises and presents<br/>findings to the user
```

---

## 4. Azure Components Reference

| Component | SKU / Config | Purpose |
|---|---|---|
| **Azure Container Apps** | Consumption plan | Hosts LMAPI; auto-scales to zero; no infrastructure to manage |
| **Azure API Management** | Developer / Standard | Rate-limit, OAuth2/API-key auth, developer portal, request tracing |
| **Azure Key Vault** | Standard | Stores LM `AccessId`, `AccessKey`, `Company` as secrets; accessed via Managed Identity — no secrets in config files or environment variables |
| **Application Insights** | Workspace-based | Distributed tracing, dependency tracking (LM API call latency), exceptions, custom metrics |
| **Log Analytics Workspace** | Pay-per-GB | Backend store for App Insights telemetry |
| **Azure Container Registry** | Basic | Stores the LMAPI Docker image; geo-replicated for HA |
| **User-Assigned Managed Identity** | — | Grants the Container App identity-based access to Key Vault (no stored credentials) |

---

## 5. Security Model

```
┌─────────────────────────────────────────────────────────────────┐
│  APIM (perimeter)                                               │
│  • Mutual TLS on inbound (optional)                             │
│  • JWT / subscription-key validation                            │
│  • IP allow-listing for orchestrator IPs                        │
└────────────────────────┬────────────────────────────────────────┘
                         │ Internal VNET / Private Endpoint
┌────────────────────────▼────────────────────────────────────────┐
│  LMAPI Container App                                            │
│  • HTTPS only (managed cert)                                    │
│  • Reads secrets from Key Vault via Managed Identity            │
│  • LMv1 HMAC-SHA256 signs every outbound LM API call           │
│  • No secrets in appsettings / environment variables            │
└────────────────────────┬────────────────────────────────────────┘
                         │ Outbound to LogicMonitor SaaS
                         │ (HTTPS, egress via NAT Gateway)
                         ▼
               LogicMonitor REST API v3
```

---

## 6. MCP Tool Manifest

The `/mcp` endpoint implements the [Model Context Protocol](https://modelcontextprotocol.io)
JSON-RPC 2.0 server spec.  Any MCP-compatible client can discover and invoke the
following tools:

| Tool Name | Maps To | Description |
|---|---|---|
| `get_device` | `GET /api/devices/{id}` | Return full details for a LogicMonitor device |
| `get_device_events` | `GET /api/devices/{id}/events` | Return paged events/logs for a device |
| `get_device_alerts` | `GET /api/devices/{id}/alerts` | Return active alerts for a device |
| `search_log_events` | `GET /api/logs/events` | Search LM Logs (Log Intelligence) |

### Connecting Claude Desktop

Add this block to `claude_desktop_config.json`:

```json
{
  "mcpServers": {
    "lmapi": {
      "url": "https://<apim-gateway-url>/mcp",
      "headers": {
        "Ocp-Apim-Subscription-Key": "<your-apim-subscription-key>"
      }
    }
  }
}
```

### Connecting Azure AI Foundry / Copilot Studio

Import the OpenAPI spec from `https://<apim-gateway-url>/swagger/v1/swagger.json`
as a **Custom Connector** or use the `/mcp` endpoint as an **MCP server** in an
Agent tool configuration.

---

## 7. CI/CD Pipeline (GitHub Actions — high level)

```mermaid
graph LR
    PR["Pull Request"] --> Build["dotnet build<br/>dotnet test"]
    Build --> Lint["CodeQL scan"]
    Lint --> Docker["docker build<br/>docker push → ACR"]
    Docker --> Infra["az deployment group create<br/>(Bicep)"]
    Infra --> Deploy["Container App<br/>rolling update"]
    Deploy --> Smoke["Smoke test<br/>/health endpoint"]
```

---

## 8. Key Vault Secret Names

| Key Vault Secret | maps to appsettings key |
|---|---|
| `lm-company` | `LogicMonitor__Company` |
| `lm-access-id` | `LogicMonitor__AccessId` |
| `lm-access-key` | `LogicMonitor__AccessKey` |
| `appinsights-connection-string` | `ApplicationInsights__ConnectionString` |
