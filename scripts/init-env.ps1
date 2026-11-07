$ErrorActionPreference = 'Stop'
if (Test-Path .env) { Write-Host '.env already exists; preserved.'; exit 0 }
$bytes = New-Object byte[] 24
$rng = [System.Security.Cryptography.RandomNumberGenerator]::Create()
try { $rng.GetBytes($bytes) } finally { $rng.Dispose() }
$secret = [BitConverter]::ToString($bytes).Replace('-', '').ToLowerInvariant()
@("POSTGRES_USER=stockroom", "POSTGRES_DB=stockroom", "POSTGRES_PASSWORD=$secret") | Set-Content -Encoding ascii .env
Write-Host 'Created ignored local .env.'
