param([switch]$RunLiveDevelopmentSmoke)
$ErrorActionPreference = 'Stop'
if (-not $RunLiveDevelopmentSmoke) { throw 'Explicit -RunLiveDevelopmentSmoke opt-in is required.' }
$credentialPath = 'C:\Users\user\FirebaseSecrets\uims-roaa-service-account.json'
$stage = 'credential validation'
$testId = [Guid]::NewGuid().ToString('N')
$client = $null
try {
    if (-not (Test-Path -LiteralPath $credentialPath -PathType Leaf)) { throw 'Missing credential.' }
    $credential = Get-Content -Raw -LiteralPath $credentialPath | ConvertFrom-Json
    if ($credential.type -ne 'service_account' -or $credential.project_id -ne 'uims-roa-a-development' -or
        $credential.token_uri -ne 'https://oauth2.googleapis.com/token') { throw 'Unexpected credential type or project.' }
    foreach ($field in @('private_key_id', 'private_key', 'client_email', 'client_id')) {
        if ([string]::IsNullOrWhiteSpace($credential.$field)) { throw 'Missing credential field.' }
    }
    Remove-Variable credential
    $configuredCredential = [Environment]::GetEnvironmentVariable('GOOGLE_APPLICATION_CREDENTIALS', 'Process')
    if ([string]::IsNullOrWhiteSpace($configuredCredential)) { $configuredCredential = [Environment]::GetEnvironmentVariable('GOOGLE_APPLICATION_CREDENTIALS', 'User') }
    if ($configuredCredential -ne $credentialPath) { throw 'ADC must point to the expected external development file.' }
    $env:GOOGLE_APPLICATION_CREDENTIALS = $configuredCredential
    Write-Output 'External development credentials are accessible and ADC configuration matches.'
    Add-Type -AssemblyName System.Net.Http
    $bin = (Resolve-Path 'University-Internship-Management-System/University-Internship-Management-System/bin').Path
    foreach ($assembly in @('Newtonsoft.Json', 'Google.Apis.Core', 'Google.Apis', 'Google.Apis.Auth', 'University-Internship-Management-System')) {
        [void][Reflection.Assembly]::LoadFrom((Join-Path $bin ($assembly + '.dll')))
    }
    $configuration = New-Object University_Internship_Management_System.Services.FinalEvaluation.FirebaseFinalEvaluationConfiguration
    if ($configuration.DatabaseRoot.AbsoluteUri -ne 'https://uims-roa-a-development-default-rtdb.firebaseio.com/') { throw 'Unexpected database.' }
    $provider = New-Object University_Internship_Management_System.Services.FinalEvaluation.GoogleAdcAccessTokenProvider
    $client = New-Object System.Net.Http.HttpClient
    $client.Timeout = [TimeSpan]::FromSeconds(15)
    $uri = [Uri]::new($configuration.DatabaseRoot, ('_devTests/finalEvaluation/' + $testId + '.json'))
    function Invoke-IsolatedRequest($method, $payload, $version) {
        $request = [System.Net.Http.HttpRequestMessage]::new([System.Net.Http.HttpMethod]::new($method), $uri)
        try {
            $token = $provider.GetAccessTokenAsync().GetAwaiter().GetResult()
            $request.Headers.Authorization = [System.Net.Http.Headers.AuthenticationHeaderValue]::new('Bearer', $token)
            $token = $null
            [void]$request.Headers.TryAddWithoutValidation('X-Firebase-ETag', 'true')
            if ($version) { [void]$request.Headers.TryAddWithoutValidation('if-match', $version) }
            if ($null -ne $payload) { $request.Content = [System.Net.Http.StringContent]::new($payload, [Text.Encoding]::UTF8, 'application/json') }
            $response = $client.SendAsync($request).GetAwaiter().GetResult()
            try {
                if (-not $response.IsSuccessStatusCode) { throw 'Firebase request failed.' }
                return [pscustomobject]@{
                    Body = $response.Content.ReadAsStringAsync().GetAwaiter().GetResult()
                    ETag = if ($response.Headers.Contains('ETag')) { @($response.Headers.GetValues('ETag'))[0] } else { $null }
                }
            } finally { $response.Dispose() }
        } finally { $request.Dispose() }
    }
    $stage = 'conditional isolated creation'
    $payload = @{ testRunId = $testId; fictional = $true; purpose = 'Phase5Connectivity' } | ConvertTo-Json -Compress
    $null = Invoke-IsolatedRequest 'PUT' $payload 'null_etag'
    $stage = 'readback verification'
    $read = Invoke-IsolatedRequest 'GET' $null $null
    $record = $read.Body | ConvertFrom-Json
    if ($record.testRunId -ne $testId -or $record.fictional -ne $true -or $record.purpose -ne 'Phase5Connectivity' -or
        @($record.PSObject.Properties).Count -ne 3 -or [string]::IsNullOrWhiteSpace($read.ETag)) { throw 'Readback mismatch.' }
    $stage = 'conditional deletion of verified record'
    $null = Invoke-IsolatedRequest 'DELETE' $null $read.ETag
    $stage = 'cleanup verification'
    $after = Invoke-IsolatedRequest 'GET' $null $null
    if ($after.Body.Trim() -ne 'null') { throw 'Cleanup not confirmed.' }
    Write-Output ('PASS isolated write/read/delete and cleanup: _devTests/finalEvaluation/' + $testId)
} catch {
    # Do not print the exception, HTTP headers, response bodies or credential contents.
    Write-Output ('FAILED at ' + $stage + '; test ID: ' + $testId + '. Live smoke test did not pass.')
    throw 'Live verification stopped. Credential material and token details were suppressed.'
} finally {
    if ($null -ne $client) { $client.Dispose() }
    foreach ($name in @('credential', 'token')) {
        if (Test-Path ('Variable:' + $name)) { Remove-Variable -Name $name }
    }
}

