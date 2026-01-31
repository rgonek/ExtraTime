#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Grants App Service managed identity access to Key Vault
#>

param(
    [Parameter(Mandatory = $true)]
    [string]$ResourceGroupName,

    [Parameter(Mandatory = $true)]
    [string]$WebAppName,

    [Parameter(Mandatory = $true)]
    [string]$KeyVaultName
)

$ErrorActionPreference = "Stop"

Write-Host "Granting Key Vault access..." -ForegroundColor Cyan

# Get the Web App's managed identity principal ID
$principalId = az webapp identity show `
    --name $WebAppName `
    --resource-group $ResourceGroupName `
    --query principalId `
    --output tsv

if (-not $principalId) {
    throw "Could not get managed identity for Web App: $WebAppName"
}

Write-Host "Web App Principal ID: $principalId" -ForegroundColor Gray

# Get Key Vault resource ID
$keyVaultId = az keyvault show `
    --name $KeyVaultName `
    --resource-group $ResourceGroupName `
    --query id `
    --output tsv

# Assign Key Vault Secrets User role
Write-Host "Assigning 'Key Vault Secrets User' role..." -ForegroundColor Gray
az role assignment create `
    --role "Key Vault Secrets User" `
    --assignee-object-id $principalId `
    --assignee-principal-type ServicePrincipal `
    --scope $keyVaultId

Write-Host "Key Vault access granted successfully!" -ForegroundColor Green
