$ErrorActionPreference='Stop'
$app=Join-Path $PSScriptRoot '..\..\University-Internship-Management-System'
$msbuild='C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'
$csc=Join-Path (Split-Path $msbuild) 'Roslyn\csc.exe'
& $msbuild (Join-Path $app 'University-Internship-Management-System.csproj') /t:Build /p:Configuration=Debug /nologo /verbosity:minimal
if($LASTEXITCODE -ne 0){throw 'Build failed'}
$bin=Join-Path $app 'bin'
& $csc /nologo /target:exe "/out:$bin\AccountSetupRunner.exe" (Join-Path $PSScriptRoot 'TestRunner.cs')
if($LASTEXITCODE -ne 0){throw 'Runner compilation failed'}
foreach($name in @('CompanyPhoneTests','LinkOnlyTests','FirebaseEmailTests')){
 & $csc /nologo /target:exe "/out:$bin\$name.exe" /r:System.Net.Http.dll "/r:$bin\Newtonsoft.Json.dll" "/r:$bin\University-Internship-Management-System.dll" (Join-Path $PSScriptRoot "$name.cs")
 if($LASTEXITCODE -ne 0){throw "Compilation failed: $name"}
 Push-Location $bin
 try{& '.\AccountSetupRunner.exe' "$name.exe";if($LASTEXITCODE -ne 0){throw "Tests failed: $name"}}finally{Pop-Location}
}
