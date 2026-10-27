$ErrorActionPreference = 'Stop'
$root = Split-Path $PSScriptRoot
$files = Get-ChildItem -LiteralPath $root -File -Recurse -Force | Where-Object {
    $relative = $_.FullName.Substring($root.Length + 1).Replace('\','/')
    -not ($relative.StartsWith('artifacts/') -or $relative.StartsWith('.git/'))
}
$issues = @()
foreach ($file in $files) {
    $relative = $file.FullName.Substring($root.Length + 1)
    if ($file.Name -in @('settings.json','appearance.json','bubble-position.json') -or $file.Name -like '.env*' -or $file.Extension -in @('.exe','.dll','.pfx','.p12','.pem','.key','.log')) {
        $issues += 'Unexpected private/build file: ' + $relative
    }
    if ($file.Extension -in @('.cs','.md','.txt','.json','.config','.manifest','.ps1','.yml') -or $file.Name -eq 'LICENSE') {
        $content = [IO.File]::ReadAllText($file.FullName)
        $patterns = @('sk-[A-Za-z0-9_-]{20,}','gh[pousr]_[A-Za-z0-9]{20,}','github_pat_[A-Za-z0-9_]{20,}','-----BEGIN [A-Z ]*PRIVATE KEY-----','[A-Za-z0-9._%+-]+@[A-Za-z0-9.-]+\.[A-Za-z]{2,}','[A-Za-z]:\\Users\\[^\\\s]+\\')
        foreach ($pattern in $patterns) { if ($content -match $pattern) { $issues += 'Review possible secret/personal data: ' + $relative; break } }
    }
}
if ($issues.Count) { $issues | Write-Output; throw 'Source hygiene check failed. No matched values were printed.' }
Write-Output ('Source hygiene checks passed for ' + $files.Count + ' files. Pattern checks do not guarantee absence of secrets.')
