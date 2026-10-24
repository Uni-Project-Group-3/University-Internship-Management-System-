[CmdletBinding(SupportsShouldProcess = $true)]
param(
    [Parameter(Mandatory = $true)][string]$CredentialFile,
    [Parameter(Mandatory = $true)][string]$QrSigningKeyFile,
    [string]$SitePath,
    [switch]$ValidateOnly
)
$ErrorActionPreference = 'Stop'
if ([string]::IsNullOrWhiteSpace($SitePath)) {
    $SitePath = Join-Path $PSScriptRoot '../University-Internship-Management-System'
}
$expectedProject = 'just-project-4af15'
$expectedDatabase = 'https://just-project-4af15-default-rtdb.firebaseio.com'
$resolvedSite = [IO.Path]::GetFullPath((Resolve-Path -LiteralPath $SitePath).Path).TrimEnd('\', '/')
$credentialPath = (Resolve-Path -LiteralPath $CredentialFile).Path
$qrPath = (Resolve-Path -LiteralPath $QrSigningKeyFile).Path
foreach ($privatePath in @($credentialPath, $qrPath)) {
    if ([IO.Path]::GetFullPath($privatePath).StartsWith($resolvedSite + [IO.Path]::DirectorySeparatorChar, [StringComparison]::OrdinalIgnoreCase)) {
        throw 'Keep credential and signing-key files outside the website directory.'
    }
}
# Inspect metadata only. Never serialize the credential object or print key material.
$credential = [IO.File]::ReadAllText($credentialPath) | ConvertFrom-Json
if ($credential.type -ne 'service_account' -or $credential.project_id -ne $expectedProject -or [string]::IsNullOrWhiteSpace($credential.private_key)) {
    throw 'Use an existing authorized service-account file for just-project-4af15. This script does not change IAM or switch projects.'
}
$credential = $null
if ((Get-Item -LiteralPath $qrPath).Length -ne 32) { throw 'The shared QR signing-key file must contain exactly 32 bytes. Do not generate a different key on each computer.' }
$webConfig = New-Object Xml.XmlDocument
$webConfig.Load((Join-Path $resolvedSite 'Web.config'))
$settings = @{}
foreach ($entry in $webConfig.configuration.appSettings.add) { $settings[[string]$entry.key] = [string]$entry.value }
$localPath = Join-Path $resolvedSite 'Firebase.Local.config'
$localConfig = New-Object Xml.XmlDocument
if (Test-Path -LiteralPath $localPath) { $localConfig.Load($localPath) }
else { $localConfig.LoadXml('<appSettings />') }
if ($localConfig.DocumentElement.Name -ne 'appSettings') { throw 'Firebase.Local.config must have an appSettings root. No configuration was changed.' }
foreach ($entry in $localConfig.appSettings.add) { $settings[[string]$entry.key] = [string]$entry.value }
if ($settings['FirebaseProjectId'] -ne $expectedProject -or $settings['FirebaseDatabaseUrl'].TrimEnd('/') -ne $expectedDatabase) {
    throw 'The effective Firebase project/database does not match the team target. No settings were changed.'
}
if ($ValidateOnly) {
    Write-Output 'PASS local checks: correct team project, external 32-byte QR key, unchanged Firebase connection. This does not verify IAM/network/write permissions.'
    return
}
if ($PSCmdlet.ShouldProcess('Current Windows user and ignored Firebase.Local.config', 'Configure the existing team credential path and shared QR signing-key path')) {
    if (Test-Path -LiteralPath $localPath) {
        $backupFolder = Join-Path (Split-Path $resolvedSite -Parent) '.private/SharedSetup'
        [IO.Directory]::CreateDirectory($backupFolder) | Out-Null
        [IO.File]::Copy($localPath, (Join-Path $backupFolder ('Firebase.Local.before-' + [DateTime]::UtcNow.ToString('yyyyMMdd-HHmmss-fffffff') + '.config')), $false)
    }
    $qrEntry = $localConfig.SelectSingleNode('/appSettings/add[@key="AttendanceQrSigningKeyFile"]')
    if ($null -eq $qrEntry) {
        $qrEntry = $localConfig.CreateElement('add'); $qrEntry.SetAttribute('key', 'AttendanceQrSigningKeyFile')
        [void]$localConfig.DocumentElement.AppendChild($qrEntry)
    }
    $qrEntry.SetAttribute('value', $qrPath)
    # Only a file path is stored, never the contents of either secret file.
    $localConfig.Save($localPath)
    [Environment]::SetEnvironmentVariable('GOOGLE_APPLICATION_CREDENTIALS', $credentialPath, [EnvironmentVariableTarget]::User)
    $env:GOOGLE_APPLICATION_CREDENTIALS = $credentialPath
    Write-Output 'Configured locally. Fully close Visual Studio/IIS Express and reopen. No Firebase data, Rules, IAM, Supabase setting, or tracked connection setting was changed.'
}
