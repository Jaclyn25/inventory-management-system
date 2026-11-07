$ErrorActionPreference = 'Stop'
if (Test-Path .env) { Write-Host '.env already exists; preserved.'; exit 0 }
$secret = [Convert]::ToHexString([System.Security.Cryptography.RandomNumberGenerator]::GetBytes(24))
@("POSTGRES_USER=stockroom", "POSTGRES_DB=stockroom", "POSTGRES_PASSWORD=$secret") | Set-Content .env
Write-Host 'Created ignored local .env.'
