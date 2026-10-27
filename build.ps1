param([string]$OutputPath)
$ErrorActionPreference = 'Stop'
$frameworkPath = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
$compilerPath = Join-Path $frameworkPath 'csc.exe'
if (-not (Test-Path -LiteralPath $compilerPath)) { throw 'Windows x64 and .NET Framework 4.8 are required.' }
$binaryPath = Join-Path $PSScriptRoot 'artifacts/bin/SpokenFlow.exe'
if ($OutputPath) { $binaryPath = [IO.Path]::GetFullPath($OutputPath) }
$null = New-Item -ItemType Directory -Path (Split-Path $binaryPath) -Force
$sourcePath = Join-Path $PSScriptRoot 'source'
$references = @('System.dll','System.Core.dll','System.Drawing.dll','System.Windows.Forms.dll','System.Net.Http.dll','System.Web.Extensions.dll','System.Security.dll','Microsoft.CSharp.dll','WPF/WindowsBase.dll','WPF/UIAutomationClient.dll','WPF/UIAutomationTypes.dll')
$compilerArgs = @('/nologo','/target:winexe','/platform:x64','/optimize+','/utf8output',('/out:' + $binaryPath),('/win32manifest:' + (Join-Path $sourcePath 'app.manifest')))
$iconPath = Join-Path $PSScriptRoot 'assets/SpokenFlow.ico'
if (Test-Path -LiteralPath $iconPath) { $compilerArgs += '/win32icon:' + $iconPath }
foreach ($ref in $references) { $compilerArgs += '/reference:' + (Join-Path $frameworkPath $ref) }
$compilerArgs += '/resource:' + (Join-Path $sourcePath 'Locales.json') + ',SpokenFlow.Locales.json'
$compilerArgs += @(Get-ChildItem -LiteralPath $sourcePath -Filter '*.cs' | Sort-Object Name | ForEach-Object { $_.FullName })
& $compilerPath $compilerArgs
if ($LASTEXITCODE -ne 0) { throw 'Build failed.' }
Copy-Item -LiteralPath (Join-Path $PSScriptRoot 'SpokenFlow.exe.config') -Destination ($binaryPath + '.config') -Force
Write-Output ('Built: ' + $binaryPath)
