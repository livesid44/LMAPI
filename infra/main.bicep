// ─────────────────────────────────────────────────────────────────────────────
// LMAPI — Azure Infrastructure as Code (Bicep)
//
// Deploys:
//   • User-Assigned Managed Identity
//   • Azure Container Registry
//   • Log Analytics Workspace
//   • Application Insights (workspace-based)
//   • Azure Key Vault  (with secrets + RBAC for the identity)
//   • Container Apps Environment
//   • Container App  (LMAPI)
//
// Usage:
//   az group create -n rg-lmapi -l eastus
//   az deployment group create \
//       -g rg-lmapi \
//       -f infra/main.bicep \
//       -p infra/main.parameters.json
// ─────────────────────────────────────────────────────────────────────────────

@description('Short environment tag: dev | staging | prod')
@allowed(['dev', 'staging', 'prod'])
param environmentName string = 'dev'

@description('Azure region for all resources.')
param location string = resourceGroup().location

@description('LogicMonitor account/company name (e.g. "acme").')
@secure()
param lmCompany string

@description('LogicMonitor API Access ID.')
@secure()
param lmAccessId string

@description('LogicMonitor API Access Key.')
@secure()
param lmAccessKey string

@description('Name of the Docker image in ACR (without tag). Defaults to "lmapi".')
param containerImageName string = 'lmapi'

@description('Docker image tag to deploy.')
param containerImageTag string = 'latest'

@description('Minimum number of Container App replicas (0 = scale-to-zero).')
param minReplicas int = 0

@description('Maximum number of Container App replicas.')
param maxReplicas int = 5

// ── Computed names ────────────────────────────────────────────────────────────
var baseName  = 'lmapi-${environmentName}'
var tags      = { application: 'LMAPI', environment: environmentName }

// ── Managed Identity ──────────────────────────────────────────────────────────
resource identity 'Microsoft.ManagedIdentity/userAssignedIdentities@2023-01-31' = {
  name: 'id-${baseName}'
  location: location
  tags: tags
}

// ── Container Registry ────────────────────────────────────────────────────────
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: replace('acr${baseName}', '-', '')   // ACR names may not contain hyphens
  location: location
  tags: tags
  sku: { name: 'Basic' }
  properties: {
    adminUserEnabled: false                  // access via Managed Identity only
  }
}

// Grant the managed identity AcrPull so Container Apps can pull images
var acrPullRoleId = '7f951dda-4ed3-4680-a7ca-43fe172d538d'
resource acrPullAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(acr.id, identity.id, acrPullRoleId)
  scope: acr
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', acrPullRoleId)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// ── Log Analytics Workspace ───────────────────────────────────────────────────
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: 'log-${baseName}'
  location: location
  tags: tags
  properties: {
    sku: { name: 'PerGB2018' }
    retentionInDays: 30
  }
}

// ── Application Insights ──────────────────────────────────────────────────────
resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: 'appi-${baseName}'
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
  }
}

// ── Key Vault ─────────────────────────────────────────────────────────────────
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' = {
  name: 'kv-${baseName}'
  location: location
  tags: tags
  properties: {
    sku: { family: 'A', name: 'standard' }
    tenantId: subscription().tenantId
    enableRbacAuthorization: true          // use RBAC instead of access policies
    enableSoftDelete: true
    softDeleteRetentionInDays: 7
    enabledForTemplateDeployment: true
  }
}

// Grant the managed identity Key Vault Secrets User (read-only)
var kvSecretsUserRoleId = '4633458b-17de-408a-b874-0445c86b69e6'
resource kvSecretsUserAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(keyVault.id, identity.id, kvSecretsUserRoleId)
  scope: keyVault
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', kvSecretsUserRoleId)
    principalId: identity.properties.principalId
    principalType: 'ServicePrincipal'
  }
}

// Store LM credentials as Key Vault secrets
resource secretCompany 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'lm-company'
  properties: { value: lmCompany }
}

resource secretAccessId 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'lm-access-id'
  properties: { value: lmAccessId }
}

resource secretAccessKey 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'lm-access-key'
  properties: { value: lmAccessKey }
}

resource secretAppInsightsConnStr 'Microsoft.KeyVault/vaults/secrets@2023-07-01' = {
  parent: keyVault
  name: 'appinsights-connection-string'
  properties: { value: appInsights.properties.ConnectionString }
}

// ── Container Apps Environment ────────────────────────────────────────────────
resource caEnv 'Microsoft.App/managedEnvironments@2024-03-01' = {
  name: 'cae-${baseName}'
  location: location
  tags: tags
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
  }
}

// ── Container App ─────────────────────────────────────────────────────────────
resource containerApp 'Microsoft.App/containerApps@2024-03-01' = {
  name: 'ca-${baseName}'
  location: location
  tags: tags
  identity: {
    type: 'UserAssigned'
    userAssignedIdentities: { '${identity.id}': {} }
  }
  properties: {
    managedEnvironmentId: caEnv.id
    configuration: {
      ingress: {
        external: true
        targetPort: 8080
        transport: 'http'
        allowInsecure: false
      }
      registries: [
        {
          server: acr.properties.loginServer
          identity: identity.id
        }
      ]
      secrets: [
        // Key Vault reference secrets — resolved at runtime via Managed Identity
        {
          name: 'lm-company'
          keyVaultUrl: secretCompany.properties.secretUri
          identity: identity.id
        }
        {
          name: 'lm-access-id'
          keyVaultUrl: secretAccessId.properties.secretUri
          identity: identity.id
        }
        {
          name: 'lm-access-key'
          keyVaultUrl: secretAccessKey.properties.secretUri
          identity: identity.id
        }
        {
          name: 'appinsights-connection-string'
          keyVaultUrl: secretAppInsightsConnStr.properties.secretUri
          identity: identity.id
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'lmapi'
          image: '${acr.properties.loginServer}/${containerImageName}:${containerImageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1Gi'
          }
          env: [
            // Map Container App secrets → ASP.NET Core configuration
            { name: 'LogicMonitor__Company',   secretRef: 'lm-company'   }
            { name: 'LogicMonitor__AccessId',  secretRef: 'lm-access-id' }
            { name: 'LogicMonitor__AccessKey', secretRef: 'lm-access-key' }
            { name: 'ApplicationInsights__ConnectionString', secretRef: 'appinsights-connection-string' }
            { name: 'ASPNETCORE_ENVIRONMENT', value: environmentName == 'prod' ? 'Production' : 'Development' }
          ]
          probes: [
            {
              type: 'Liveness'
              httpGet: { path: '/health', port: 8080, scheme: 'HTTP' }
              initialDelaySeconds: 10
              periodSeconds: 30
              failureThreshold: 3
            }
            {
              type: 'Readiness'
              httpGet: { path: '/health', port: 8080, scheme: 'HTTP' }
              initialDelaySeconds: 5
              periodSeconds: 10
              failureThreshold: 3
            }
          ]
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
        rules: [
          {
            name: 'http-scaling'
            http: { metadata: { concurrentRequests: '20' } }
          }
        ]
      }
    }
  }
}

// ── Outputs ───────────────────────────────────────────────────────────────────
output containerAppFqdn string = containerApp.properties.configuration.ingress.fqdn
output acrLoginServer    string = acr.properties.loginServer
output keyVaultName      string = keyVault.name
output appInsightsName   string = appInsights.name
