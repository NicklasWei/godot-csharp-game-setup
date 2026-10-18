# Points git at the repository's hooks (.githooks/), installs Git LFS, and restores the local .NET tools.
$ErrorActionPreference = 'Stop'
Set-Location (git rev-parse --show-toplevel)
git lfs install --skip-repo
git config core.hooksPath .githooks
dotnet tool restore
Write-Host 'Git hooks enabled (core.hooksPath = .githooks).'
