param([switch]$Test)
$ErrorActionPreference = 'Stop'
$project = $PSScriptRoot
$framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework64/v4.0.30319'
if (-not (Test-Path -LiteralPath (Join-Path $framework 'csc.exe'))) {
    $framework = Join-Path $env:WINDIR 'Microsoft.NET/Framework/v4.0.30319'
}
$compiler = Join-Path $framework 'csc.exe'
if (-not (Test-Path -LiteralPath $compiler)) { throw '.NET Framework C# compiler is unavailable. Install .NET Framework 4.8 Developer Pack.' }
$references = @('System.dll','System.Core.dll','System.Xml.dll','System.Windows.Forms.dll','System.Drawing.dll') | ForEach-Object { '/reference:' + (Join-Path $framework $_) }
foreach ($name in @('UIAutomationClient','UIAutomationTypes','WindowsBase')) {
    $library = Join-Path $framework ('WPF/' + $name + '.dll')
    if (-not (Test-Path -LiteralPath $library)) { throw "Missing required framework library: $library" }
    $references += '/reference:' + $library
}
$destination = Join-Path $project 'dist'
New-Item -ItemType Directory -Force -Path $destination | Out-Null
$sources = @(Get-ChildItem -LiteralPath (Join-Path $project 'src') -Filter '*.cs' | ForEach-Object { $_.FullName })
& $compiler /nologo /target:winexe /platform:anycpu /optimize+ "/out:$destination/YodaFy.exe" @references @sources
if ($LASTEXITCODE -ne 0) { throw "Build failed with exit code $LASTEXITCODE" }
Copy-Item -LiteralPath (Join-Path $project 'src/YodaFy.exe.config') -Destination $destination -Force
Write-Output "Built $destination/YodaFy.exe"
if ($Test) {
    $tests = @(Get-ChildItem -LiteralPath (Join-Path $project 'tests') -Filter '*.cs' | ForEach-Object { $_.FullName })
    & $compiler /nologo /target:exe /platform:anycpu "/out:$destination/YodaFy.Tests.exe" /main:YodaFy.Tests @references @sources @tests
    if ($LASTEXITCODE -ne 0) { throw 'Test build failed.' }
    & (Join-Path $destination 'YodaFy.Tests.exe')
    if ($LASTEXITCODE -ne 0) { throw 'Tests failed.' }
}
