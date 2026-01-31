#!/usr/bin/env pwsh
<#
.SYNOPSIS
    Removes all ExtraTime Azure resources
.DESCRIPTION
    Deletes the resource group and all contained resources.
    This action is IRREVERSIBLE.
#>

param(
    [switch]$Force
)

$ErrorActionPreference = "Stop"

$ResourceGroupName = "extratime-prod-rg"

Write-Host "========================================" -ForegroundColor Red
Write-Host "WARNING: Resource Deletion" -ForegroundColor Red
Write-Host "========================================" -ForegroundColor Red
Write-Host ""
Write-Host "This will delete resource group: $ResourceGroupName" -ForegroundColor Yellow
Write-Host "This action is IRREVERSIBLE!" -ForegroundColor Red
Write-Host ""

if (-not $Force) {
    $confirmation = Read-Host "Type 'DELETE' to confirm"
    if ($confirmation -ne 'DELETE') {
        Write-Host "Aborted." -ForegroundColor Yellow
        exit 0
    }
}

Write-Host ""
Write-Host "Deleting resource group..." -ForegroundColor Yellow

az group delete --name $ResourceGroupName --yes --no-wait

Write-Host "Deletion initiated. Resources will be removed in the background." -ForegroundColor Green
Write-Host "Use 'az group show -n $ResourceGroupName' to check status." -ForegroundColor Gray
