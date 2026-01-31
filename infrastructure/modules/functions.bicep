// ============================================================================
// Azure Functions (Consumption Plan - Free Tier)
// FREE TIER: 1M executions/month, 400,000 GB-seconds/month
// ============================================================================
// COST CONTROLS:
// - Consumption plan (Y1) with pay-per-execution (free tier eligible)
// - Daily memory quota limit to prevent runaway costs
// - Function timeout limited to 5 minutes max
// - LRS storage for lowest cost
// ============================================================================

@description('Function App name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Storage account name for Functions (must be globally unique, 3-24 chars, lowercase/numbers only)')
param storageAccountName string

@description('Application Insights connection string')
param appInsightsConnectionString string

@description('SQL connection string')
@secure()
param sqlConnectionString string

@description('Key Vault name for secret references')
param keyVaultName string

@description('Football Data API Key')
@secure()
param footballDataApiKey string

@description('JWT Secret for authentication')
@secure()
param jwtSecret string

@description('Daily memory time quota in GB-seconds to prevent cost overruns (default: 10000, free tier allows 400000/month = ~13333/day)')
param dailyMemoryTimeQuota int = 10000

// Storage Account (required for Functions - use LRS for lowest cost)
resource storageAccount 'Microsoft.Storage/storageAccounts@2023-05-01' = {
  name: storageAccountName
  location: location
  tags: tags
  sku: {
    name: 'Standard_LRS' // Lowest cost storage option
  }
  kind: 'StorageV2'
  properties: {
    minimumTlsVersion: 'TLS1_2'
    supportsHttpsTrafficOnly: true
    allowBlobPublicAccess: false
    // Disable unnecessary features to reduce costs
    isHnsEnabled: false
    isSftpEnabled: false
    isNfsV3Enabled: false
  }
}

// Consumption Plan (serverless, free tier eligible)
// Y1 = Consumption plan with free grant of 1M executions and 400K GB-s per month
resource hostingPlan 'Microsoft.Web/serverfarms@2023-12-01' = {
  name: '${name}-plan'
  location: location
  tags: tags
  sku: {
    name: 'Y1'  // Consumption plan - free tier
    tier: 'Dynamic'
  }
  properties: {
    reserved: true // Linux
  }
}

// Function App with cost controls
resource functionApp 'Microsoft.Web/sites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  kind: 'functionapp,linux'
  identity: {
    type: 'SystemAssigned'
  }
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    // COST CONTROL: Limit daily GB-seconds to prevent runaway costs
    dailyMemoryTimeQuota: dailyMemoryTimeQuota
    siteConfig: {
      linuxFxVersion: 'DOTNET-ISOLATED|10.0'
      ftpsState: 'Disabled' // Disable FTP for security
      minTlsVersion: '1.2'
      // COST CONTROL: Limit function timeout to prevent long-running executions
      functionAppScaleLimit: 1 // Limit max instances to prevent scale-out costs
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};EndpointSuffix=${environment().suffixes.storage};AccountKey=${storageAccount.listKeys().keys[0].value}'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsightsConnectionString
        }
        {
          name: 'ConnectionStrings__extratime'
          value: sqlConnectionString
        }
        {
          name: 'Jwt__Secret'
          value: jwtSecret
        }
        {
          name: 'Jwt__Issuer'
          value: 'ExtraTime'
        }
        {
          name: 'Jwt__Audience'
          value: 'ExtraTime'
        }
        {
          name: 'Jwt__ExpiryMinutes'
          value: '1440'
        }
        {
          name: 'FootballData__ApiKey'
          value: footballDataApiKey
        }
        {
          name: 'FootballData__BaseUrl'
          value: 'https://api.football-data.org/v4/'
        }
        // COST CONTROL: Disable features that could increase costs
        {
          name: 'AzureWebJobsDisableHomepage'
          value: 'true'
        }
        {
          name: 'WEBSITE_RUN_FROM_PACKAGE'
          value: '1'
        }
      ]
    }
  }
}

// Grant Function App access to Key Vault (for future secret references)
resource keyVault 'Microsoft.KeyVault/vaults@2023-07-01' existing = {
  name: keyVaultName
}

resource keyVaultAccessPolicy 'Microsoft.KeyVault/vaults/accessPolicies@2023-07-01' = {
  name: 'add'
  parent: keyVault
  properties: {
    accessPolicies: [
      {
        tenantId: subscription().tenantId
        objectId: functionApp.identity.principalId
        permissions: {
          secrets: ['get', 'list']
        }
      }
    ]
  }
}

// Outputs
output functionAppName string = functionApp.name
output functionAppHostName string = functionApp.properties.defaultHostName
output principalId string = functionApp.identity.principalId
output storageAccountName string = storageAccount.name
