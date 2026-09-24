[CmdletBinding()]
param(
    [switch]$PreflightOnly,
    [string]$OutputRoot = (Join-Path ([System.IO.Path]::GetTempPath()) 'agent-integration-test-bin')
)

$ErrorActionPreference = 'Stop'
$project = Join-Path $PSScriptRoot 'Agent.Api.IntegrationTests.csproj'
$outputPath = [System.IO.Path]::GetFullPath($OutputRoot).Replace('\', '/').TrimEnd('/') + '/'
$common = @(
    'test', $project, '-m:1', '-nodeReuse:false',
    '-p:UseSharedCompilation=false', "-p:BaseOutputPath=$outputPath",
    '--verbosity', 'minimal'
)

Write-Host 'Checking required services before acceptance scenarios...'
& dotnet @common --filter 'Category=Preflight'
if ($LASTEXITCODE -ne 0) {
    Write-Host 'Preflight failed. Acceptance scenarios were NOT started.' -ForegroundColor Red
    exit $LASTEXITCODE
}
if ($PreflightOnly) { exit 0 }

Write-Host 'Preflight passed. Running acceptance scenarios...'
& dotnet @common --no-build --no-restore --filter 'Category!=Preflight'
exit $LASTEXITCODE
