# atsign-negative-test.ps1 - @<tag>(){} negative-case driver (Front compile MUST fail)
# Usage: powershell -NoProfile -ExecutionPolicy Bypass -File test\Other\AtSignLabel\atsign-negative-test.ps1
# Assertions (test/Other/AtSignLabel/AtSignNegativeTest.sl, 7 blocks):
#   out/export/AtSignNegativeTest/Logs/Front.txt contains exactly
#   6 x [ProcessAtSignLabelChannelSyntaxError] (LID 20055)
#   1 x [ProcessAtSignLabelOutChannelMultiple] (LID 20056)
# Note: Front CLI compile always exits 0 (errors surface via Log.errorCount /
# Front.txt, not the process exit code) - LID counts are the assertion.
param()
$ErrorActionPreference = 'Stop'

# Script lives at <repo>\simple_language\test\Other\AtSignLabel\ -> simple_language root is three levels up
$langRoot = (Resolve-Path (Join-Path $PSScriptRoot '..\..\..')).Path

$proj = Join-Path $langRoot 'test\Other\AtSignLabel\AtSignNegativeTest'
$frontLog = Join-Path $langRoot 'out\export\AtSignNegativeTest\Logs\Front.txt'

Write-Host "=== AtSign negative test (expect Front compile failure) ==="
Write-Host "project: $proj"

# Front compile (standalone project; not part of ProjectTest.jsonc regression list)
& dotnet run --project (Join-Path $langRoot 'source\Front\SimpleLanguageFront.csproj') -- `
    compile -p $proj --no-banner | Out-Null
$exit = $LASTEXITCODE
Write-Host "Front exit code: $exit (informational; CLI always exits 0)"

if (-not (Test-Path $frontLog))
{
    Write-Host "[FAIL] Front log not found: $frontLog"
    exit 1
}

$e55 = (Select-String -Path $frontLog -Pattern 'ProcessAtSignLabelChannelSyntaxError' -SimpleMatch).Count
$e56 = (Select-String -Path $frontLog -Pattern 'ProcessAtSignLabelOutChannelMultiple' -SimpleMatch).Count
Write-Host "LID 20055 (channel syntax error): $e55 (expected 6)"
Write-Host "LID 20056 (multiple out-channels): $e56 (expected 1)"

$failed = $false
if ($e55 -ne 6)  { Write-Host '[FAIL] 20055 count mismatch'; $failed = $true }
if ($e56 -ne 1)  { Write-Host '[FAIL] 20056 count mismatch'; $failed = $true }

if ($failed) { exit 1 }
Write-Host '=== AtSign negative test: PASS (all 7 cases reported expected LIDs) ==='
exit 0
