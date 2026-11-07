$ErrorActionPreference = 'Stop'
$repositoryRoot = Split-Path -Parent $PSScriptRoot
Push-Location $repositoryRoot
try {
    $dirty = git status --porcelain --untracked-files=normal
    if ($LASTEXITCODE -ne 0) { throw 'Cannot inspect Git status.' }
    if ($dirty) { throw 'Commit or resolve working changes before packaging committed source.' }
    $output = Join-Path $repositoryRoot 'submission/stockroom-source.zip'
    git archive --format=zip --output=$output HEAD
    if ($LASTEXITCODE -ne 0) { throw 'Source archive failed.' }
    Get-FileHash -Algorithm SHA256 -LiteralPath $output
} finally { Pop-Location }
