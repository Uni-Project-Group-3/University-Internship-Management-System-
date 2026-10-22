$ErrorActionPreference='Stop'
Set-Location -LiteralPath $PSScriptRoot
function RunGit([string[]]$Arguments){ & git @Arguments; if($LASTEXITCODE -ne 0){throw ('Git stopped: '+($Arguments -join ' ')+'. No force push attempted.')} }
function CheckBuild {
 $finder=Join-Path ([Environment]::GetFolderPath('ProgramFilesX86')) 'Microsoft Visual Studio\Installer\vswhere.exe'
 $builder=& $finder -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1
 if(-not $builder){throw 'MSBuild unavailable. Nothing pushed.'}
 & $builder 'University-Internship-Management-System\University-Internship-Management-System.slnx' /t:Build /p:Configuration=Debug /v:minimal /nologo
 if($LASTEXITCODE -ne 0){throw 'Build failed. Nothing pushed.'}
 foreach($test in @('Tests/create-task-button.test.cjs','Tests/exam-question-totals.test.cjs')){ & node $test; if($LASTEXITCODE -ne 0){throw ('Test failed: '+$test+'. Nothing pushed.')} }
 & node Tests/training-rules.test.cjs --static-only
 if($LASTEXITCODE -ne 0){throw 'Rules diff failed. Nothing pushed.'}
}
$branch=(& git branch --show-current).Trim()
if($LASTEXITCODE -ne 0 -or $branch -ne 'tariq-final-merge'){throw 'Expected tariq-final-merge. No branch switched.'}
$unmerged=@(& git diff --name-only --diff-filter=U)
if($LASTEXITCODE -ne 0 -or $unmerged.Count){throw 'Unresolved conflicts remain. Nothing committed or pushed.'}
foreach($saved in @('47e4adb','591b25f')){ RunGit @('cat-file','-e',($saved+'^{commit}')); & git merge-base --is-ancestor $saved HEAD; if($LASTEXITCODE -ne 0){throw ('Saved history missing: '+$saved)} }
$stamp=Get-Date -Format 'yyyyMMdd-HHmmss'
RunGit @('branch',('codex/uims-before-publish-'+$stamp),'HEAD')
$backup=Join-Path ([IO.Path]::GetTempPath()) ('uims-final-publish-'+[Guid]::NewGuid().ToString('N'))
New-Item -ItemType Directory -Path $backup | Out-Null
$files=@(& git -c core.quotepath=false ls-files --modified --deleted --others --exclude-standard) | Sort-Object -Unique
if($LASTEXITCODE -ne 0){throw 'Cannot list changed files.'}
$extensions=@('.cs','.aspx','.ascx','.ashx','.css','.js','.cjs','.json','.rules','.config','.csproj','.sln','.slnx','.md','.ps1','.svg','.png','.jpg','.jpeg','.webp','.ico','.woff','.woff2')
foreach($file in $files){
 if($file -match '(^|/)(\.git|\.vs|node_modules|packages|bin|obj)(/|$)' -or $file -match '(?i)(firebase-debug|database-debug|ui-debug|login-diagnostics|service[-_]?account|credential|\.env)(\.|/|$)'){continue}
 if($extensions -notcontains [IO.Path]::GetExtension($file).ToLowerInvariant()){continue}
 $full=Join-Path $PSScriptRoot $file
 if(Test-Path -LiteralPath $full -PathType Leaf){$copy=Join-Path $backup $file;New-Item -ItemType Directory -Path (Split-Path -Parent $copy) -Force|Out-Null;Copy-Item -LiteralPath $full -Destination $copy}
 RunGit @('add','--',$file)
}
$staged=@(& git -c core.quotepath=false diff --cached --name-only --diff-filter=ACMR)
foreach($file in $staged){
 if($file -match '(^|/)(\.vs|node_modules|bin|obj)(/|$)'){throw ('Generated staged file: '+$file+'. Nothing pushed.')}
 if(@('.cs','.aspx','.ashx','.js','.cjs','.json','.config','.md','.ps1','.rules') -contains [IO.Path]::GetExtension($file).ToLowerInvariant()){
  $content=Get-Content -LiteralPath (Join-Path $PSScriptRoot $file) -Raw
  if($content -match '(?m)^(<<<<<<< |=======\s*$|>>>>>>> )'){throw ('Conflict marker: '+$file)}
  if($content -match '-----BEGIN (RSA |EC )?PRIVATE KEY-----|"private_key"\s*:\s*"-----'){throw ('Private credential detected in '+$file+'. Its content was not printed. Nothing pushed.')}
 }
}
RunGit @('diff','--cached','--check')
CheckBuild
Write-Host ('Local source backup: '+$backup)
$merge=(& git rev-parse --git-path MERGE_HEAD).Trim()
if(Test-Path -LiteralPath $merge){RunGit @('commit','-m','Merge team changes and preserve UIMS Firebase authentication and workflows')}
else { & git diff --cached --quiet; if($LASTEXITCODE -eq 1){RunGit @('commit','-m','Connect UIMS Firebase workflows with authenticated account ownership')}elseif($LASTEXITCODE -ne 0){throw 'Cannot inspect staged changes.'} }
RunGit @('fetch','origin','main')
& git merge-base --is-ancestor origin/main HEAD
if($LASTEXITCODE -eq 1){ & git merge --no-edit origin/main; if($LASTEXITCODE -ne 0){throw 'New main conflicts. Merge preserved; nothing pushed.'};CheckBuild }
elseif($LASTEXITCODE -ne 0){throw 'Cannot verify main ancestry.'}
foreach($saved in @('47e4adb','591b25f','origin/main')){ & git merge-base --is-ancestor $saved HEAD;if($LASTEXITCODE -ne 0){throw ('Required history missing: '+$saved+'. Nothing pushed.')} }
RunGit @('push','origin','HEAD:refs/heads/tariq-final-merge')
RunGit @('push','origin','HEAD:refs/heads/main')
RunGit @('fetch','origin','main')
$local=(& git rev-parse HEAD).Trim();$remote=(& git rev-parse origin/main).Trim()
if($local -ne $remote){throw 'main changed concurrently; inspect remote. No force push used.'}
Write-Host ('PASS: main matches '+$local+'. History preserved. No force push. Firebase records and Rules unchanged by this script.')
Write-Host 'Live training readiness remains separate from Git publication.'
