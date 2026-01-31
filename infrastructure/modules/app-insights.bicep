// ============================================================================
// Application Insights (with Daily Cap for Zero Cost)
// ============================================================================
// FREE TIER LIMITS:
// - 5 GB data ingestion/month
// - 90 days data retention (free)
// - Daily cap prevents overage charges
// ============================================================================

@description('Application Insights name')
param name string

@description('Azure region')
param location string

@description('Resource tags')
param tags object

@description('Daily ingestion cap in GB (0.16 = ~5 GB/month)')
param dailyCapGb string = '0.16'

// ============================================================================
// LOG ANALYTICS WORKSPACE (Required for App Insights)
// ============================================================================

resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2023-09-01' = {
  name: '${name}-workspace'
  location: location
  tags: tags
  properties: {
    sku: {
      name: 'PerGB2018' // Pay-as-you-go (but we cap ingestion)
    }
    retentionInDays: 30 // Minimum to save costs
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    // Daily cap is set at Application Insights level
  }
}

// ============================================================================
// APPLICATION INSIGHTS
// ============================================================================

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: name
  location: location
  tags: tags
  kind: 'web'
  properties: {
    Application_Type: 'web'
    WorkspaceResourceId: logAnalytics.id
    publicNetworkAccessForIngestion: 'Enabled'
    publicNetworkAccessForQuery: 'Enabled'
    // Sampling reduces data volume
    SamplingPercentage: 50
  }
}

// ============================================================================
// DAILY CAP (CRITICAL FOR ZERO COST)
// ============================================================================

resource dailyCap 'Microsoft.Insights/components/CurrentBillingFeatures@2015-05-01' = {
  name: '${appInsights.name}/CurrentBillingFeatures'
  properties: {
    CurrentBillingFeatures: ['Basic']
    DataVolumeCap: {
      Cap: json(dailyCapGb)
      ResetTime: 0
      StopSendNotificationWhenHitCap: false
      WarningThreshold: 80
      StopSendNotificationWhenHitThreshold: false
    }
  }
}

// ============================================================================
// OUTPUTS
// ============================================================================

output name string = appInsights.name
output instrumentationKey string = appInsights.properties.InstrumentationKey
output connectionString string = appInsights.properties.ConnectionString
output workspaceId string = logAnalytics.id
