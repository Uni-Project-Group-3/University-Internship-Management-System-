param()
$ErrorActionPreference = 'Stop'
$projectRoot = Split-Path -Parent $PSScriptRoot
Push-Location -LiteralPath $projectRoot
try {
    if (-not (Get-Command java -ErrorAction SilentlyContinue)) {
        $env:JAVA_HOME = 'C:/Program Files/Android/openjdk/jdk-21.0.8'
        if (-not (Test-Path -LiteralPath "$env:JAVA_HOME/bin/java.exe")) { throw 'Java 21 is required. Nothing published.' }
        $env:Path = "$env:JAVA_HOME/bin;$env:Path"
    }
    $rulesFile = Join-Path $projectRoot 'firebase.training.rules.json'
    $hash = (Get-FileHash -LiteralPath $rulesFile -Algorithm SHA256).Hash
    $applicationHash = (Get-FileHash -LiteralPath (Join-Path $projectRoot 'firebase.internship.rules.json') -Algorithm SHA256).Hash
    & npx.cmd --yes firebase-tools@15.0.0 emulators:exec --project demo-uims-internship --only database --config firebase.internship.emulator.json "node Tests/application-rules.test.cjs"
    if ($LASTEXITCODE -ne 0) { throw 'Application permissions failed. Nothing published.' }
    & npx.cmd --yes firebase-tools@15.0.0 emulators:exec --project demo-uims-training --only database --config firebase.training.emulator.json "node Tests/training-rules.test.cjs"
    if ($LASTEXITCODE -ne 0) { throw 'Training permissions failed. Nothing published.' }
    if ((Get-FileHash -LiteralPath $rulesFile -Algorithm SHA256).Hash -ne $hash -or (Get-FileHash -LiteralPath (Join-Path $projectRoot 'firebase.internship.rules.json') -Algorithm SHA256).Hash -ne $applicationHash) { throw 'Rules changed during testing. Nothing published.' }
    $backup = Join-Path ([IO.Path]::GetTempPath()) ('uims-training-rules-' + [Guid]::NewGuid().ToString('N'))
    New-Item -ItemType Directory -Path $backup | Out-Null
    $before = Join-Path $backup 'before.rules.json'
    $after = Join-Path $backup 'after.rules.json'
    & npx.cmd --yes firebase-tools@15.0.0 database:get /.settings/rules --project intership-managment-system --instance intership-managment-system-default-rtdb --output $before --non-interactive
    if ($LASTEXITCODE -ne 0) { throw 'Live backup failed. Nothing published.' }
    & node Tests/training-rules.test.cjs --compare-live $before
    if ($LASTEXITCODE -ne 0) { throw 'Live Rules contain unexpected changes. Nothing published.' }
    Copy-Item -LiteralPath $rulesFile -Destination (Join-Path $backup 'approved.rules.json')
    if ((Get-FileHash -LiteralPath (Join-Path $backup 'approved.rules.json') -Algorithm SHA256).Hash -ne $hash) { throw 'Snapshot changed. Nothing published.' }
    $deployConfig = Join-Path $backup 'firebase.deploy.json'
    $configuration = @{ database = @(@{ instance = 'intership-managment-system-default-rtdb'; rules = 'approved.rules.json' }) } | ConvertTo-Json -Depth 5
    [IO.File]::WriteAllText($deployConfig, $configuration, (New-Object Text.UTF8Encoding($false)))
    Write-Host "Live Rules backup: $before"
    & npx.cmd --yes firebase-tools@15.0.0 deploy --only database --project intership-managment-system --config $deployConfig --non-interactive
    if ($LASTEXITCODE -ne 0) { throw 'Publication did not report success. Inspect Firebase before retrying.' }
    & npx.cmd --yes firebase-tools@15.0.0 database:get /.settings/rules --project intership-managment-system --instance intership-managment-system-default-rtdb --output $after --non-interactive
    if ($LASTEXITCODE -ne 0) { throw 'Readback failed. Publication is not verified.' }
    & node Tests/training-rules.test.cjs --verify-live $after
    if ($LASTEXITCODE -ne 0) { throw 'Readback differs. Publication is not verified.' }
    Write-Host 'PASS: application and training Rules published and verified. No user or internship records were modified. No Git commit or push performed.'
} finally { Pop-Location }
