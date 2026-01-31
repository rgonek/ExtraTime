// ============================================================================
// Azure Static Web App (Free Tier)
// ============================================================================
// FREE TIER LIMITS:
// - 100 GB bandwidth/month
// - 2 custom domains
// - Free SSL certificates
// - 0.5 GB storage per app
// ============================================================================

@description('Static Web App name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Backend API base URL')
param apiBaseUrl string

// ============================================================================
// STATIC WEB APP
// ============================================================================

resource staticWebApp 'Microsoft.Web/staticSites@2023-12-01' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: 'Free'
    tier: 'Free'
  }
  properties: {
    stagingEnvironmentPolicy: 'Disabled' // Save resources
    allowConfigFileUpdates: true
    buildProperties: {
      appLocation: 'web'
      outputLocation: 'dist'
      skipGithubActionWorkflowGeneration: true // We manage our own workflow
    }
  }
}

// ============================================================================
// APP SETTINGS
// ============================================================================

resource staticWebAppSettings 'Microsoft.Web/staticSites/config@2023-12-01' = {
  parent: staticWebApp
  name: 'appsettings'
  properties: {
    NEXT_PUBLIC_API_URL: apiBaseUrl
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output name string = staticWebApp.name
output defaultHostName string = staticWebApp.properties.defaultHostname
output resourceId string = staticWebApp.id
