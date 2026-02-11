param(
    [string]$ApiUrl
)

Write-Host "===========================================" -ForegroundColor Cyan
Write-Host "  Bet Results Calculation" -ForegroundColor Cyan
Write-Host "===========================================" -ForegroundColor Cyan
Write-Host ""
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Starting bet calculation..." -ForegroundColor Yellow
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Calling: POST $ApiUrl/dev/trigger/calculate-bets" -ForegroundColor Gray
Write-Host ""

try {
    $response = Invoke-RestMethod -Uri "$ApiUrl/dev/trigger/calculate-bets" -Method Post -ErrorAction Stop
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] SUCCESS!" -ForegroundColor Green
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Message: $($response.message)" -ForegroundColor Green
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Matches processed: $($response.matchesProcessed)/$($response.totalMatches)" -ForegroundColor Green
    Write-Host ""
} catch {
    Write-Host "[$(Get-Date -Format 'HH:mm:ss')] ERROR: $($_.Exception.Message)" -ForegroundColor Red
    Write-Host ""
    exit 1
}

Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Bet calculation completed" -ForegroundColor Cyan
Write-Host "[$(Get-Date -Format 'HH:mm:ss')] Restart this resource to trigger again" -ForegroundColor DarkGray
Write-Host ""

while ($true) {
    Start-Sleep -Seconds 3600
}
