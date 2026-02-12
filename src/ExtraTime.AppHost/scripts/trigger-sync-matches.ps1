param(
    [string]$ApiUrl
)

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  Football Matches Sync" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Starting match sync..." -ForegroundColor Yellow
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Calling: POST $ApiUrl/dev/trigger/sync-matches" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "$ApiUrl/dev/trigger/sync-matches" -Method Post -ErrorAction Stop
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] SUCCESS!" -ForegroundColor Green
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Response: $($response.message)" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Sync matches completed" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Restart this resource to trigger again" -ForegroundColor DarkGray
Write-Host ""

# Keep running so logs are visible in dashboard
while ($true) {
    Start-Sleep -Seconds 3600
}
