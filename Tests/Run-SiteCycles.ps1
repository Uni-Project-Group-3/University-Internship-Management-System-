[CmdletBinding()]
param(
    [switch]$Browser,
    [switch]$ReadOnlyFirebase,
    [switch]$Http,
    [string]$BaseUrl = 'https://localhost:44372',
    [string]$MSBuildPath,
    [ValidateRange(30,1800)][int]$TimeoutSeconds = 300
)
$ErrorActionPreference = 'Stop'
$repository = [IO.Path]::GetFullPath((Join-Path $PSScriptRoot '..'))
$app = Join-Path $repository 'University-Internship-Management-System'
$reportFolder = Join-Path $PSScriptRoot ('Reports\SiteCycles-' + (Get-Date -Format 'yyyyMMdd-HHmmss-fff'))
[IO.Directory]::CreateDirectory($reportFolder) | Out-Null
$results = New-Object 'System.Collections.Generic.List[object]'
$coverage = @(
    'Isolated C# cycles cover accounts/student management, applications and offer confirmation, task compatibility/board, exam, weekly report and final evaluation workflows present in the existing suites.',
    'HTTP checks are signed-out reachability and security checks, NOT authenticated business workflow verification.',
    'Live Firebase mode is GET-only: no test submissions, approvals, account changes, uploads or fixture imports.',
    'Storage is deferred. Authenticated end-to-end browser cycles and modules without existing tests remain unverified; a green report does not prove every possible site action.'
)
function Quote-Argument([string]$Value) { '"' + $Value.Replace('"','\"') + '"' }
function Add-Result($Name,$Status,$Message,$Log) {
    $results.Add([pscustomobject]@{Check=$Name;Status=$Status;Message=$Message;Log=$Log})
    $color = if($Status -eq 'PASS'){'Green'}elseif($Status -eq 'FAIL'){'Red'}else{'Yellow'}
    Write-Host ('[{0}] {1}: {2}' -f $Status,$Name,$Message) -ForegroundColor $color
}
function Invoke-Check([string]$Name,[string]$Executable,[string[]]$Arguments,[string]$LogName) {
    $log = Join-Path $reportFolder ($LogName + '.log')
    try {
        $info = New-Object Diagnostics.ProcessStartInfo
        $info.FileName=$Executable; $info.Arguments=($Arguments | ForEach-Object {Quote-Argument $_}) -join ' '
        $info.WorkingDirectory=$repository; $info.UseShellExecute=$false; $info.CreateNoWindow=$true
        $info.RedirectStandardOutput=$true; $info.RedirectStandardError=$true
        $process = New-Object Diagnostics.Process; $process.StartInfo=$info
        if(-not $process.Start()){throw 'Process did not start.'}
        $stdout=$process.StandardOutput.ReadToEndAsync(); $stderr=$process.StandardError.ReadToEndAsync()
        if(-not $process.WaitForExit($TimeoutSeconds*1000)) {
            # Stop only this test process tree, never IIS or the running application.
            & "$env:SystemRoot\System32\taskkill.exe" /PID $process.Id /T /F | Out-Null
            $process.WaitForExit(); throw "Timed out after $TimeoutSeconds seconds."
        }
        $content=$stdout.GetAwaiter().GetResult()+[Environment]::NewLine+$stderr.GetAwaiter().GetResult()
        [IO.File]::WriteAllText($log,$content)
        if($process.ExitCode -ne 0){$status=if($process.ExitCode -eq 2){'BLOCKED'}else{'FAIL'};Add-Result $Name $status ('Exit code '+$process.ExitCode+'; inspect the log for the failing cycle.') $log;return $false}
        Add-Result $Name 'PASS' 'Completed successfully.' $log; return $true
    } catch { [IO.File]::AppendAllText($log,[Environment]::NewLine+$_.Exception.Message);Add-Result $Name 'FAIL' $_.Exception.Message $log;return $false }
    finally { if($process){$process.Dispose()} }
}
Push-Location $repository
try {
    if(-not $MSBuildPath){
        $vswhere=Join-Path ${env:ProgramFiles(x86)} 'Microsoft Visual Studio\Installer\vswhere.exe'
        if(Test-Path $vswhere){$MSBuildPath=@(& $vswhere -latest -products '*' -requires Microsoft.Component.MSBuild -find 'MSBuild\**\Bin\MSBuild.exe' | Select-Object -First 1)[0]}
    }
    $built=$false
    if(-not $MSBuildPath -or -not (Test-Path $MSBuildPath)){Add-Result 'Solution build' 'BLOCKED' 'MSBuild not found. Pass -MSBuildPath.' ''}
    else {
        $built=Invoke-Check 'Solution build' $MSBuildPath @('University-Internship-Management-System.slnx','/p:UseSharedCompilation=false','/v:minimal','/nologo') 'build'
        $testBuilt=Invoke-Check 'C# cycle suite build' $MSBuildPath @('Tests\FinalEvaluation\FinalEvaluation.Tests.csproj','/p:UseSharedCompilation=false','/v:minimal','/nologo') 'test-build'
        if($built -and $testBuilt){
            Invoke-Check 'C# business cycles (isolated)' (Join-Path $PSScriptRoot 'FinalEvaluation\bin\FinalEvaluation.Tests.exe') @() 'cycles' | Out-Null
            $compiler=Join-Path (Split-Path $MSBuildPath) 'Roslyn\csc.exe'
            $studentExe=Join-Path $app 'bin\ManageStudents.WorkflowTests.exe'
            if(Test-Path $compiler){
                $compiled=Invoke-Check 'Student management tests build' $compiler @('/nologo','/langversion:7.3','/target:exe',('/out:'+$studentExe),'/r:System.Web.dll',('/r:'+(Join-Path $app 'bin\University-Internship-Management-System.dll')),('/r:'+(Join-Path $app 'bin\Newtonsoft.Json.dll')),(Join-Path $PSScriptRoot 'ManageStudents\StudentWorkflowTests.cs')) 'students-build'
                if($compiled){Invoke-Check 'Student add/delete/authorization cycles (isolated)' $studentExe @() 'students' | Out-Null}
            }else{Add-Result 'Student management tests' 'BLOCKED' 'Roslyn compiler not found.' ''}
            if($ReadOnlyFirebase){Invoke-Check 'Actual Firebase authorized reads (GET only)' (Join-Path $PSScriptRoot 'FinalEvaluation\bin\FinalEvaluation.Tests.exe') @('--final-report-readonly-preflight',(Join-Path $app 'Web.config')) 'firebase-readonly' | Out-Null}
            else{Add-Result 'Actual Firebase authorized reads' 'SKIP' 'Use -ReadOnlyFirebase; existing server credentials are required.' ''}
        }else{Add-Result 'Business cycle execution' 'BLOCKED' 'Build failed; stale binaries were not executed.' ''}
    }
    if($built){
        $aspnet=Join-Path $env:WINDIR 'Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe'
        if(Test-Path $aspnet){Invoke-Check 'ASP.NET Web Forms precompilation' $aspnet @('-v','/','-p',$app,(Join-Path $reportFolder 'precompiled')) 'webforms' | Out-Null}
        else{Add-Result 'Web Forms precompilation' 'BLOCKED' '.NET Framework compiler not found.' ''}
    }
    $node=Get-Command node -ErrorAction SilentlyContinue
    if($node){
        Invoke-Check 'All application JavaScript syntax' $node.Source @((Join-Path $PSScriptRoot 'SiteSyntax.cjs'),$app) 'javascript' | Out-Null
        if($Http){Invoke-Check 'All local pages (signed-out HTTP only)' $node.Source @((Join-Path $PSScriptRoot 'SiteHttp.cjs'),$app,$BaseUrl,(Join-Path $reportFolder 'pages.json')) 'http-pages' | Out-Null}
        else{Add-Result 'Local page HTTP inventory' 'SKIP' 'Start the existing application, then use -Http.' ''}
        if($Browser){
            foreach($script in @('CompanyTrainees.browser.cjs','ExamPopup.browser.cjs','WeeklyPopup.browser.cjs','FinalReportWorkflow.browser.cjs','AcademicAssignment.browser.cjs')){
                Invoke-Check ('Browser isolated cycle: '+$script) $node.Source @((Join-Path $PSScriptRoot ('FinalEvaluation\'+$script))) ('browser-'+$script.Replace('.cjs','')) | Out-Null
            }
        }else{Add-Result 'Browser popup/mobile cycles' 'SKIP' 'Use -Browser; Chrome and the existing Playwright runtime are required.' ''}
    }else{Add-Result 'JavaScript / browser / HTTP checks' 'BLOCKED' 'Node.js not found in PATH.' ''}
} catch {Add-Result 'Runner' 'FAIL' $_.Exception.Message ''}
finally {
    Pop-Location
    $cycleCases = @(foreach($file in @('cycles.log','students.log')) {
        $source = Join-Path $reportFolder $file
        if(Test-Path $source){foreach($line in [IO.File]::ReadAllLines($source)){
            if($line -match '^(PASS|FAIL)\s+(.+)$'){[pscustomobject]@{Cycle=$Matches[2];Status=$Matches[1];Source=$file}}
        }}
    })
    $cycleCases | Export-Csv (Join-Path $reportFolder 'cycle-details.csv') -NoTypeInformation -Encoding UTF8
    $results | Export-Csv (Join-Path $reportFolder 'summary.csv') -NoTypeInformation -Encoding UTF8
    @{generatedAt=(Get-Date).ToString('o');results=@($results.ToArray());coverage=$coverage} | ConvertTo-Json -Depth 8 | Set-Content (Join-Path $reportFolder 'summary.json') -Encoding UTF8
    $html='<html><meta charset="utf-8"><title>UIMS cycle checks</title><style>body{font:15px Segoe UI;margin:35px;color:#203758}table{border-collapse:collapse;width:100%}td,th{padding:12px;border:1px solid #dbe3ef;text-align:left}pre{white-space:pre-wrap}h1{color:#234785}</style><h1>UIMS cycle checks</h1>'
    $html+=($results | ConvertTo-Html -Fragment | Out-String)
    $html+='<h2>Individual isolated cycle results</h2>'+($cycleCases | ConvertTo-Html -Fragment | Out-String)
    $html+='<h2>Coverage and limitations</h2><pre>'+[System.Net.WebUtility]::HtmlEncode(($coverage -join "`n"))+'</pre></html>'
    [IO.File]::WriteAllText((Join-Path $reportFolder 'report.html'),$html)
    Write-Host "Report: $reportFolder\report.html" -ForegroundColor Cyan
}
if(@($results | Where-Object {$_.Status -in @('FAIL','BLOCKED')}).Count -gt 0){exit 1}
exit 0
