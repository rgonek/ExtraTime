using './main.bicep'

// Environment
param environment = 'prod'
param location = 'northeurope'

// SQL Server credentials (will be prompted or set via environment variable)
param sqlAdminUsername = readEnvironmentVariable('SQL_ADMIN_USERNAME', 'extratimeadmin')
param sqlAdminPassword = readEnvironmentVariable('SQL_ADMIN_PASSWORD', '')

// Secrets (will be prompted or set via environment variable)
param jwtSecret = readEnvironmentVariable('JWT_SECRET', '')
param footballDataApiKey = readEnvironmentVariable('FOOTBALL_DATA_API_KEY', '')

// GitHub (optional - for automated SWA deployment)
param githubRepoUrl = readEnvironmentVariable('GITHUB_REPO_URL', '')
param githubToken = readEnvironmentVariable('GITHUB_TOKEN', '')
