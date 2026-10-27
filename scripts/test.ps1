param([switch]$NoBuild)
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
if (-not $NoBuild) { & (Join-Path $root 'build.ps1') }
$exe = Join-Path $root 'artifacts/bin/SpokenFlow.exe'
$report = Join-Path $root 'artifacts/self-test.txt'
if (-not (Test-Path -LiteralPath $exe)) { throw 'Build the program before using -NoBuild.' }
if (Test-Path -LiteralPath $report) { Remove-Item -LiteralPath $report }
$testProcess = Start-Process -FilePath $exe -ArgumentList @('--self-test',('"' + $report + '"')) -WindowStyle Hidden -PassThru
if (-not $testProcess.WaitForExit(60000)) { $testProcess.Kill(); throw 'Self-tests timed out after 60 seconds.' }
$testProcess.Refresh()
if ($testProcess.ExitCode -ne 0) { if (Test-Path $report) { Get-Content $report }; throw 'Self-tests failed.' }
if (-not (Test-Path -LiteralPath $report)) { throw 'Self-test report was not created.' }
$text = Get-Content -LiteralPath $report -Raw -Encoding UTF8
if ($text -notmatch 'Result: 34 passed, 0 failed') { throw 'Unexpected self-test results; inspect the report.' }
Write-Output $text
