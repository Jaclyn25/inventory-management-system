$ErrorActionPreference = 'Stop'
$values = @{}
Get-Content .env | ForEach-Object { if ($_ -match '^([^#=]+)=(.*)$') { $values[$matches[1]] = $matches[2] } }
$env:ConnectionStrings__Inventory = "Host=localhost;Port=54330;Database=$($values.POSTGRES_DB);Username=$($values.POSTGRES_USER);Password=$($values.POSTGRES_PASSWORD)"
$env:Database__Migrate = 'true'
$env:Database__Seed = 'true'
dotnet run --project backend/Stockroom.Api --no-launch-profile --urls http://127.0.0.1:5081
exit $LASTEXITCODE
