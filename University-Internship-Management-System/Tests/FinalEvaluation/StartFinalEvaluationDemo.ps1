[CmdletBinding()]
param([ValidateSet('IisExpress','VisualStudio')][string]$HostMode = 'IisExpress', [int]$Port = 61877)
$ErrorActionPreference = 'Stop'
$app = (Resolve-Path (Join-Path $PSScriptRoot '..\..\University-Internship-Management-System')).Path
$solution = (Resolve-Path (Join-Path $app '..\University-Internship-Management-System.slnx')).Path
$credential = [Environment]::GetEnvironmentVariable('GOOGLE_APPLICATION_CREDENTIALS')
if (-not $credential) { $credential = [Environment]::GetEnvironmentVariable('GOOGLE_APPLICATION_CREDENTIALS','User') }
if (-not $credential -or -not (Test-Path -LiteralPath $credential -PathType Leaf)) { throw 'External development credentials are unavailable. Configure GOOGLE_APPLICATION_CREDENTIALS first.' }
$names = @('UIMS_FINAL_EVALUATION_FIREBASE_DEMO','UIMS_FINAL_EVALUATION_LOCAL','UIMS_STUDENT_COMPLETION_IN_MEMORY')
$old = @{}
foreach ($name in $names) { $old[$name] = [Environment]::GetEnvironmentVariable($name) }
try {
    [Environment]::SetEnvironmentVariable('UIMS_FINAL_EVALUATION_FIREBASE_DEMO','1')
    [Environment]::SetEnvironmentVariable('UIMS_FINAL_EVALUATION_LOCAL',$null)
    [Environment]::SetEnvironmentVariable('UIMS_STUDENT_COMPLETION_IN_MEMORY',$null)
    if ($HostMode -eq 'VisualStudio') {
        Write-Host 'Close previous Visual Studio/IIS Express instances for this solution first. Select Debug, IIS Express, and open Company/ReportsFinalEvaluation.aspx.'
        Start-Process -FilePath 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe' -ArgumentList ('"' + $solution + '"')
    } else {
        if ($Port -lt 1024 -or $Port -gt 65535) { throw 'Choose a local development port between 1024 and 65535.' }
        if (Get-NetTCPConnection -LocalPort $Port -State Listen -ErrorAction SilentlyContinue) { throw 'The selected port is already in use; choose another port.' }
        $process = Start-Process -FilePath 'C:\Program Files\IIS Express\iisexpress.exe' -ArgumentList @('/path:"' + $app + '"', '/port:' + $Port, '/systray:false') -WindowStyle Hidden -PassThru
        Write-Host ('LOCAL Firebase DEMO PID: ' + $process.Id)
        Write-Host ('Open http://localhost:' + $Port + '/Company/ReportsFinalEvaluation')
        Write-Host 'Use the same browser session for Academic/FinalAcademicEvaluation, Coordinator/FinalCompletionReview and Student/MyProgressEvaluation.'
        Write-Host ('Disable: stop only this IIS Express process with Stop-Process -Id ' + $process.Id + ', then launch normally without the demo flag.')
    }
} finally { foreach ($name in $names) { [Environment]::SetEnvironmentVariable($name,$old[$name]) } }
