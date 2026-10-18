# Builds the developer wiki (ADR 0008): generates the reference pages from Core and content, then runs DocFX.
# The site lands in tools/wiki/_site. -Serve serves it at http://localhost:8080; -Strict fails on missing strings or docs.
param([switch]$Serve, [switch]$Strict)
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)

dotnet build tools/__GAME__.Wiki/__GAME__.Wiki.csproj -c Debug --nologo -v quiet
if ($LASTEXITCODE) { exit $LASTEXITCODE }

$commit = git rev-parse --short HEAD 2>$null
if (-not $commit) { $commit = 'local' }
$generatorArgs = @('--commit', $commit)
if ($Strict) { $generatorArgs += '--strict' }
dotnet run --no-build --project tools/__GAME__.Wiki -- @generatorArgs
if ($LASTEXITCODE) { exit $LASTEXITCODE }

$docfxArgs = @('tools/wiki/docfx.json', '--warningsAsErrors')
if ($Serve) { $docfxArgs += '--serve' }
dotnet docfx @docfxArgs
exit $LASTEXITCODE
