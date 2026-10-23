[CmdletBinding()]
param(
    [string]$ProjectPath = (Join-Path $PSScriptRoot '..\..\University-Internship-Management-System'),
    [string]$HostName,
    [string]$RuntimeSid = [Security.Principal.WindowsIdentity]::GetCurrent().User.Value
)
# Read-only: no installs, firewall/URLACL/binding/certificate changes, file writes or Firebase requests.
$ErrorActionPreference = 'Stop'
$checks = [Collections.Generic.List[object]]::new()
function Record([string]$Name, [bool]$Passed, [string]$Detail) {
    $checks.Add([pscustomobject]@{ Check = $Name; Passed = $Passed; Detail = $Detail })
}
$project = [IO.Path]::GetFullPath($ProjectPath).TrimEnd('\')
$configPath = Join-Path $project 'Web.config'
if (!(Test-Path -LiteralPath $configPath)) { throw 'Supply the existing deployed Web Forms project directory.' }
[xml]$web = [IO.File]::ReadAllText($configPath)
$settings = @{}
foreach ($item in $web.SelectNodes('/configuration/appSettings/add')) { $settings[$item.key] = $item.value }
$external = $web.configuration.appSettings.GetAttribute('file')
if ($external) {
    $localPath = [IO.Path]::GetFullPath((Join-Path $project $external))
    if (!(Test-Path -LiteralPath $localPath)) { throw 'The configured local settings file is unavailable.' }
    [xml]$local = [IO.File]::ReadAllText($localPath)
    foreach ($item in $local.SelectNodes('/appSettings/add')) { $settings[$item.key] = $item.value }
}
Record 'Firebase project' ($settings.FirebaseProjectId -eq 'just-project-4af15') 'Must retain the approved team project.'
Record 'Firebase Storage disabled' ($settings.FirebaseStorageEnabled -ne 'true' -and $settings.FirebaseStorageLiveWritesEnabled -ne 'true') 'No paid Storage provider is enabled by this script.'
Record 'Private report provider' ($settings.WeeklyReportFileProvider -eq 'PrivateDisk') 'Reports must use the selected private disk provider.'
$root = $settings.WeeklyReportPrivateRoot
$privateOk = $false
try {
    if ($root -and [IO.Path]::IsPathRooted($root) -and !$root.StartsWith('\\')) {
        $absolute = [IO.Path]::GetFullPath($root).TrimEnd('\')
        $outside = !$absolute.StartsWith($project + '\', [StringComparison]::OrdinalIgnoreCase) -and $absolute -ne $project
        $dir = Get-Item -LiteralPath $absolute
        $noLinks = $true
        for ($parent = $dir; $null -ne $parent; $parent = $parent.Parent) {
            if (($parent.Attributes -band [IO.FileAttributes]::ReparsePoint) -ne 0) { $noLinks = $false }
        }
        $acl = Get-Acl -LiteralPath $absolute
        $allowed = @($RuntimeSid, 'S-1-5-18', 'S-1-5-32-544')
        $unapproved = @($acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]) | Where-Object {
            $_.AccessControlType -eq 'Allow' -and $_.IdentityReference.Value -notin $allowed
        })
        $runtimeAccess = @($acl.GetAccessRules($true, $true, [Security.Principal.SecurityIdentifier]) | Where-Object {
            $_.AccessControlType -eq 'Allow' -and $_.IdentityReference.Value -eq $RuntimeSid -and
            ($_.FileSystemRights -band [Security.AccessControl.FileSystemRights]::Modify) -eq [Security.AccessControl.FileSystemRights]::Modify
        }).Count -gt 0
        $privateOk = $outside -and $noLinks -and $acl.AreAccessRulesProtected -and $unapproved.Count -eq 0 -and $runtimeAccess
    }
} catch { $privateOk = $false }
Record 'Persistent private folder ACL' $privateOk 'Existing folder outside webroot; no links; only actual runtime identity, SYSTEM and administrators. No probe/file created.'
$service = Get-Service W3SVC -ErrorAction SilentlyContinue
Record 'Full IIS available' ([bool]$service) 'Current IIS Express development mode alone does not establish a durable network deployment.'
$forms = $web.SelectSingleNode('/configuration/system.web/authentication/forms')
$cookies = $web.SelectSingleNode('/configuration/system.web/httpCookies')
Record 'HTTPS-only auth cookies' ($null -ne $forms -and $forms.GetAttribute('requireSSL') -eq 'true') 'Deployment copy must use forms requireSSL=true; localhost HTTP config is not changed.'
Record 'Secure HttpOnly cookies' ($null -ne $cookies -and $cookies.GetAttribute('requireSSL') -eq 'true' -and $cookies.GetAttribute('httpOnlyCookies') -eq 'true') 'Deployment copy must require SSL and HttpOnly cookies.'
$validHost = $HostName -and [Uri]::CheckHostName($HostName) -eq [UriHostNameType]::Dns -and $HostName.Contains('.') -and $HostName -notmatch '[/;:*\s]' -and !$HostName.EndsWith('.invalid')
Record 'Approved DNS hostname supplied' ([bool]$validHost) 'Pass the exact approved reachable hostname, without scheme, path or wildcard; no hostname is guessed.'
$allow = @(([string]$settings.AttendanceAllowedHttpsHosts).Split(';') | ForEach-Object { $_.Trim() })
Record 'Exact attendance HTTPS allowlist' ([bool]($validHost -and $HostName -in $allow)) 'Only configure after the real HTTPS endpoint is available.'
if ($validHost) {
    $tlsOk = $false
    try {
        Add-Type -AssemblyName System.Net.Http
        $handler = [Net.Http.HttpClientHandler]::new(); $handler.AllowAutoRedirect = $false
        $http = [Net.Http.HttpClient]::new($handler); $http.Timeout = [TimeSpan]::FromSeconds(15)
        try {
            # Default OS certificate trust and hostname validation. No bypass/callback or credential use.
            $response = $http.GetAsync('https://' + $HostName + '/Auth/Login').GetAwaiter().GetResult()
            try {
                $status = [int]$response.StatusCode
                $tlsOk = $status -eq 200
                if ($status -in @(301,302,303,307,308) -and $response.Headers.Location) {
                    $redirect = [Uri]::new([Uri]('https://' + $HostName + '/Auth/Login'), $response.Headers.Location)
                    $tlsOk = $redirect.Scheme -eq 'https' -and $redirect.Host -eq $HostName
                }
            } finally { $response.Dispose() }
        } finally { $http.Dispose() }
    } catch { $tlsOk = $false }
    Record 'Trusted HTTPS Login reachable' $tlsOk 'Normal certificate validation; only a public Login GET, no sign-in, credentials or attendance writes.'
}
$checks | Format-Table -AutoSize -Wrap
if (@($checks | Where-Object { !$_.Passed }).Count) { exit 2 }
Write-Output 'Static deployment preflight passed. Phone camera, authenticated cookies, app-pool credentials, Firebase reads and private-report restore/download still require real testing.'
exit 0
