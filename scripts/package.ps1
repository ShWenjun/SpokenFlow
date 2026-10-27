# Creates local review artifacts only. Does not upload or publish anything.
$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$version = (Get-Content -LiteralPath (Join-Path $root 'VERSION') -Raw).Trim()
if ($version -notmatch '^\d+\.\d+\.\d+(-[a-zA-Z0-9.]+)?$') { throw 'Invalid VERSION.' }
& (Join-Path $root 'build.ps1')
& (Join-Path $PSScriptRoot 'test.ps1') -NoBuild
& (Join-Path $PSScriptRoot 'check-source.ps1')
$artifactRoot = Join-Path $root 'artifacts'
$stage = Join-Path $artifactRoot ('stage-' + [Guid]::NewGuid().ToString('N'))
$runtime = Join-Path $stage ('SpokenFlow-' + $version + '-windows-x64')
$source = Join-Path $stage ('SpokenFlow-' + $version + '-source')
$null = New-Item -ItemType Directory -Path $runtime,$source -Force
# Explicit lists prevent local settings, scratch files and .git from leaking.
foreach ($name in @('README.md','README.en.md','LICENSE','NOTICE.md','BRANDING.md','PRIVACY.md','SECURITY.md','SUPPORT.md','CONTRIBUTING.md','CODE_OF_CONDUCT.md','GOVERNANCE.md','CHANGELOG.md','THIRD_PARTY_NOTICES.md','VERSION')) {
    Copy-Item -LiteralPath (Join-Path $root $name) -Destination $runtime
}
Copy-Item -LiteralPath (Join-Path $root 'docs') -Destination $runtime -Recurse
Copy-Item -LiteralPath (Join-Path $root 'assets') -Destination $runtime -Recurse
Copy-Item -LiteralPath (Join-Path $root 'artifacts/bin/SpokenFlow.exe'),(Join-Path $root 'artifacts/bin/SpokenFlow.exe.config') -Destination $runtime
$sourceItems = @('source','assets','scripts','docs','.github','.gitignore','.gitattributes','.editorconfig','build.ps1','SpokenFlow.exe.config','VERSION','README.md','README.en.md','LICENSE','NOTICE.md','BRANDING.md','PRIVACY.md','SECURITY.md','SUPPORT.md','CONTRIBUTING.md','CODE_OF_CONDUCT.md','GOVERNANCE.md','CHANGELOG.md','THIRD_PARTY_NOTICES.md')
foreach ($name in $sourceItems) { Copy-Item -LiteralPath (Join-Path $root $name) -Destination $source -Recurse }
Add-Type -AssemblyName System.IO.Compression.FileSystem
$archives = @()
foreach ($folder in @($runtime,$source)) {
    $archive = Join-Path $artifactRoot ((Split-Path $folder -Leaf) + '.zip')
    if (Test-Path -LiteralPath $archive) { Remove-Item -LiteralPath $archive }
    [IO.Compression.ZipFile]::CreateFromDirectory($folder,$archive,[IO.Compression.CompressionLevel]::Optimal,$true)
    $archives += $archive
}
$checksums = foreach ($archive in $archives) { (Get-FileHash -LiteralPath $archive -Algorithm SHA256).Hash.ToLowerInvariant() + '  ' + (Split-Path $archive -Leaf) }
$checksums | Set-Content -LiteralPath (Join-Path $artifactRoot 'SHA256SUMS.txt') -Encoding ASCII
Write-Output $archives
