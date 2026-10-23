$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..\..'))
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'
$csc = Join-Path (Split-Path $msbuild) 'Roslyn\csc.exe'
$app = Join-Path $repository 'University-Internship-Management-System'
& $msbuild (Join-Path $app 'University-Internship-Management-System.csproj') /t:Build /p:Configuration=Debug /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Application build failed.' }
$exe = Join-Path $app 'bin\ManageStudents.WorkflowTests.exe'
& $csc /nologo /langversion:7.3 /target:exe "/out:$exe" /r:System.Web.dll "/r:$app\bin\University-Internship-Management-System.dll" "/r:$app\bin\Newtonsoft.Json.dll" (Join-Path $PSScriptRoot 'StudentWorkflowTests.cs')
if ($LASTEXITCODE -ne 0) { throw 'Test compilation failed.' }
& $exe
if ($LASTEXITCODE -ne 0) { throw 'Manage Students workflow tests failed.' }