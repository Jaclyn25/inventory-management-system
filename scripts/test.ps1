$ErrorActionPreference = 'Stop'
$values = @{}
Get-Content .env | ForEach-Object { if ($_ -match '^([^#=]+)=(.*)$') { $values[$matches[1]] = $matches[2] } }
$env:INVENTORY_TEST_CONNECTION = "Host=localhost;Port=54330;Database=$($values.POSTGRES_DB);Username=$($values.POSTGRES_USER);Password=$($values.POSTGRES_PASSWORD)"
dotnet test --configuration Release @args
exit $LASTEXITCODE
