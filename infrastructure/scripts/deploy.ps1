#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Deploys ExtraTime infrastructure to Azure using Bicep
.DESCRIPTION
    This script deploys all Azure resources for ExtraTime MVP.
    All resources use FREE TIER with cost safeguards enabled.
.PARAMETER SubscriptionId
    Azure subscription ID to deploy to
.PARAMETER SqlAdminPassword
    Password for SQL Server administrator
.PARAMETER JwtSecret
    JWT signing secret (minimum 32 characters)
.PARAMETER FootballDataApiKey
    API key for football-data.org
.PARAMETER WhatIf
    Preview changes without deploying
.EXAMPLE
    ./deploy.ps1 -SqlAdminPassword "YourPassword123!" -JwtSecret "your-32-char-secret-key-here!!" -FootballDataApiKey "abc123"
#>

param(
    [Parameter(Mandatory = $false)]
    [string]$SubscriptionId,

    [Parameter(Mandatory = $true)]
    [SecureString]$SqlAdminPassword,

    [Parameter(Mandatory = $true)]
    [SecureString]$JwtSecret,

    [Parameter(Mandatory = $true)]
    [SecureString]$FootballDataApiKey,

    [switch]$WhatIf
)

$ErrorActionPreference = "Stop"

# Configuration
$Location = "northeurope"
$Environment = "prod"
$ResourceGroupName = "extratime-$Environment-rg"

Write-Host "========================================" -ForegroundColor Cyan
Write-Host "ExtraTime Infrastructure Deployment" -ForegroundColor Cyan
Write-Host "========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "Environment: $Environment" -ForegroundColor Yellow
Write-Host "Location: $Location" -ForegroundColor Yellow
Write-Host "Resource Group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host ""

# Verify Azure CLI is installed and logged in
Write-Host "Checking Azure CLI..." -ForegroundColor Gray
$azVersion = az version 2>$null | ConvertFrom-Json
if (-not $azVersion) {
    throw "Azure CLI is not installed. Install with: winget install Microsoft.AzureCLI"
}
Write-Host "Azure CLI version: $($azVersion.'azure-cli')" -ForegroundColor Green

# Verify logged in
$account = az account show 2>$null | ConvertFrom-Json
if (-not $account) {
    Write-Host "Not logged in to Azure. Running 'az login'..." -ForegroundColor Yellow
    az login
    $account = az account show | ConvertFrom-Json
}
Write-Host "Logged in as: $($account.user.name)" -ForegroundColor Green
Write-Host "Subscription: $($account.name)" -ForegroundColor Green

# Set subscription if specified
if ($SubscriptionId) {
    Write-Host "Setting subscription to: $SubscriptionId" -ForegroundColor Yellow
    az account set --subscription $SubscriptionId
}

# Verify Bicep is installed
Write-Host "Checking Bicep..." -ForegroundColor Gray
$bicepVersion = az bicep version 2>$null
if (-not $bicepVersion) {
    Write-Host "Installing Bicep..." -ForegroundColor Yellow
    az bicep install
}
Write-Host "Bicep installed" -ForegroundColor Green

# Convert SecureStrings to plain text for Azure CLI
$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($SqlAdminPassword)
$SqlAdminPasswordPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($JwtSecret)
$JwtSecretPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

$BSTR = [System.Runtime.InteropServices.Marshal]::SecureStringToBSTR($FootballDataApiKey)
$FootballDataApiKeyPlain = [System.Runtime.InteropServices.Marshal]::PtrToStringAuto($BSTR)

# Validate JWT secret length
if ($JwtSecretPlain.Length -lt 32) {
    throw "JWT secret must be at least 32 characters long"
}

# Build deployment command
$deploymentName = "extratime-deployment-$(Get-Date -Format 'yyyyMMdd-HHmmss')"
$templateFile = Join-Path $PSScriptRoot ".." "main.bicep"

Write-Host ""
Write-Host "Starting deployment: $deploymentName" -ForegroundColor Cyan

$deployArgs = @(
    "deployment", "sub", "create",
    "--name", $deploymentName,
    "--location", $Location,
    "--template-file", $templateFile,
    "--parameters", "sqlAdminUsername=extratimeadmin",
    "--parameters", "sqlAdminPassword=$SqlAdminPasswordPlain",
    "--parameters", "jwtSecret=$JwtSecretPlain",
    "--parameters", "footballDataApiKey=$FootballDataApiKeyPlain"
)

if ($WhatIf) {
    $deployArgs += "--what-if"
    Write-Host "Running in What-If mode (no changes will be made)" -ForegroundColor Yellow
}

# Execute deployment
az @deployArgs

if ($LASTEXITCODE -ne 0) {
    throw "Deployment failed with exit code $LASTEXITCODE"
}

# Clear sensitive data from memory
$SqlAdminPasswordPlain = $null
$JwtSecretPlain = $null
$FootballDataApiKeyPlain = $null
[System.GC]::Collect()

if (-not $WhatIf) {
    Write-Host ""
    Write-Host "========================================" -ForegroundColor Green
    Write-Host "Deployment completed successfully!" -ForegroundColor Green
    Write-Host "========================================" -ForegroundColor Green

    # Get outputs
    Write-Host ""
    Write-Host "Fetching deployment outputs..." -ForegroundColor Gray
    $outputs = az deployment sub show --name $deploymentName --query properties.outputs | ConvertFrom-Json

    Write-Host ""
    Write-Host "Resource URLs:" -ForegroundColor Cyan
    Write-Host "  API: $($outputs.appServiceDefaultHostName.value)" -ForegroundColor White
    Write-Host "  Frontend: https://$($outputs.staticWebAppDefaultHostName.value)" -ForegroundColor White
    Write-Host "  Key Vault: $($outputs.keyVaultUri.value)" -ForegroundColor White

    Write-Host ""
    Write-Host "Next steps:" -ForegroundColor Yellow
    Write-Host "  1. Grant Key Vault access to App Service managed identity"
    Write-Host "  2. Run database migrations"
    Write-Host "  3. Deploy application code"
    Write-Host "  4. Configure custom domain"
}
