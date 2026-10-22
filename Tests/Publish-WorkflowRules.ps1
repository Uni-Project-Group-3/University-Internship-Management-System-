param([switch]$Publish)
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $projectRoot
try {
    if (-not (Get-Command java -ErrorAction SilentlyContinue)) {
        $env:JAVA_HOME = 'C:\Program Files\Android\openjdk\jdk-21.0.8'
        if (-not (Test-Path -LiteralPath "$env:JAVA_HOME\bin\java.exe")) { throw 'Java 21 is required. Nothing deployed.' }
        $env:Path = "$env:JAVA_HOME\bin;$env:Path"
    }
    $approvedFile = Join-Path $projectRoot 'firebase.workflow.rules.json'
    $approvedHash = (Get-FileHash -LiteralPath $approvedFile -Algorithm SHA256).Hash
    & npx.cmd --yes firebase-tools@15.0.0 emulators:exec --project demo-uims-workflows --only database --config firebase.workflows.emulator.json "node Tests/workflow-rules.test.cjs"
    if ($LASTEXITCODE -ne 0) { throw 'Workflow Rules tests failed. Nothing deployed.' }
    if ((Get-FileHash -LiteralPath $approvedFile -Algorithm SHA256).Hash -ne $approvedHash) { throw 'Rules changed during testing. Nothing deployed.' }
    if (-not $Publish) { Write-Host 'PASS verification only. No live Rules or records changed. Publication requires explicit approval and -Publish.'; return }
    $backupDir = Join-Path ([IO.Path]::GetTempPath()) ('uims-workflow-rules-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $backupDir | Out-Null
    $before = Join-Path $backupDir 'before.rules.json'
    $after = Join-Path $backupDir 'after.rules.json'
    & npx.cmd --yes firebase-tools@15.0.0 database:get /.settings/rules --project intership-managment-system --instance intership-managment-system-default-rtdb --output $before --non-interactive
    if ($LASTEXITCODE -ne 0) { throw 'Could not back up live Rules. Nothing deployed.' }
    & node Tests/workflow-rules.test.cjs --compare-live $before
    if ($LASTEXITCODE -ne 0) { throw 'Live Rules changed unexpectedly. Nothing deployed.' }
    Copy-Item -LiteralPath $approvedFile -Destination (Join-Path $backupDir 'approved.rules.json')
    if ((Get-FileHash -LiteralPath (Join-Path $backupDir 'approved.rules.json') -Algorithm SHA256).Hash -ne $approvedHash) { throw 'Snapshot differs. Nothing deployed.' }
    $deployConfig = Join-Path $backupDir 'firebase.deploy.json'
    $configJson = @{ database = @(@{ instance = 'intership-managment-system-default-rtdb'; rules = 'approved.rules.json' }) } | ConvertTo-Json -Depth 5
    [IO.File]::WriteAllText($deployConfig, $configJson, (New-Object Text.UTF8Encoding($false)))
    Write-Host "Live Rules backup: $before"
    & npx.cmd --yes firebase-tools@15.0.0 deploy --only database --project intership-managment-system --config $deployConfig --non-interactive
    if ($LASTEXITCODE -ne 0) { throw 'Publication not verified. Inspect Firebase before retrying.' }
    & npx.cmd --yes firebase-tools@15.0.0 database:get /.settings/rules --project intership-managment-system --instance intership-managment-system-default-rtdb --output $after --non-interactive
    if ($LASTEXITCODE -ne 0) { throw 'Readback failed; publication is not verified.' }
    & node Tests/workflow-rules.test.cjs --verify-live $after
    if ($LASTEXITCODE -ne 0) { throw 'Readback differs; publication is not verified.' }
    Write-Host 'PASS scoped opportunity Rules published and verified. No application records modified.'
} finally { Pop-Location }
