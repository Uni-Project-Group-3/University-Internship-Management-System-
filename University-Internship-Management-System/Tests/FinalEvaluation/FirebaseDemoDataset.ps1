[CmdletBinding()]
param(
    [ValidateSet('Seed','Inspect','Reset')][string]$Mode = 'Inspect',
    [string]$DatasetId,
    [switch]$EnableDevelopmentDemoOperations
)
$ErrorActionPreference = 'Stop'
$client = $null
$stage = 'explicit opt-in'
try {
    if (-not $EnableDevelopmentDemoOperations) { throw 'Explicit opt-in required.' }
    if ([string]::IsNullOrWhiteSpace($DatasetId)) {
        if ($Mode -ne 'Seed') { throw 'Specify the exact dataset identifier.' }
        $DatasetId = [Guid]::NewGuid().ToString('N')
    }
    $stage = 'loading existing module'
    $bin = (Resolve-Path (Join-Path $PSScriptRoot '..\..\University-Internship-Management-System\bin')).Path
    Add-Type -AssemblyName System.Net.Http
    foreach ($name in @('Newtonsoft.Json','Google.Apis.Core','Google.Apis','Google.Apis.Auth','University-Internship-Management-System')) {
        [void][Reflection.Assembly]::LoadFrom((Join-Path $bin ($name + '.dll')))
    }
    $stage = 'external development credential and project verification'
    [University_Internship_Management_System.Services.FinalEvaluation.FirebaseDevelopmentDemoRepository]::VerifyExternalDevelopmentCredentials()
    $configuration = New-Object University_Internship_Management_System.Services.FinalEvaluation.FirebaseFinalEvaluationConfiguration
    $provider = New-Object University_Internship_Management_System.Services.FinalEvaluation.GoogleAdcAccessTokenProvider
    $handler = New-Object System.Net.Http.HttpClientHandler
    $handler.AllowAutoRedirect = $false
    $client = New-Object System.Net.Http.HttpClient -ArgumentList $handler
    $client.Timeout = [TimeSpan]::FromSeconds(20)
    $repository = New-Object University_Internship_Management_System.Services.FinalEvaluation.FirebaseDevelopmentDemoRepository -ArgumentList $client,$provider,$configuration,$DatasetId
    $stage = 'inspecting exact owned dataset'
    $before = $repository.InspectAsync().GetAwaiter().GetResult()
    Write-Output ('Exact demo scope: ' + $repository.DatasetPath)
    if ($Mode -eq 'Reset') {
        if ($null -eq $before.Value) { throw 'No owned dataset exists at this exact identifier.' }
        Write-Output 'Reset deletes only this complete fictional dataset, then reseeds it. No parent or official path is deleted.'
        $confirmation = Read-Host ('To authorize destructive cleanup, type DELETE ' + $DatasetId)
        if ($confirmation -cne ('DELETE ' + $DatasetId)) { Write-Output 'Reset cancelled. Nothing was deleted.'; exit 0 }
        $stage = 'conditionally resetting exactly the confirmed dataset'
        $null = $repository.DeleteExactDatasetAsync($before.Version).GetAwaiter().GetResult()
        $null = $repository.SeedAsync().GetAwaiter().GetResult()
    } elseif ($Mode -eq 'Seed') {
        $stage = 'idempotent conditional seed'
        $null = $repository.SeedAsync().GetAwaiter().GetResult()
    }
    $stage = 'readback verification'
    $after = $repository.InspectAsync().GetAwaiter().GetResult()
    if ($null -eq $after.Value) { throw 'Dataset is not present.' }
    if ($Mode -eq 'Seed' -and $null -ne $before.Value -and $before.Version -ne $after.Version) { throw 'Existing seed changed concurrently; unchanged-state verification failed.' }
    $reviews = 0
    foreach ($evaluation in $after.Value.Evaluations.Values) { $reviews += $evaluation.Reviews.Count }
    $documents = 0
    foreach ($internship in $after.Value.Documents.Values) { $documents += $internship.Count }
    Write-Output ('PASS ' + $Mode + ': ' + $after.Value.Evaluations.Count + ' Company evaluation aggregates; ' + $reviews + ' Academic reviews; ' + $after.Value.Completions.Count + ' completion records; ' + $documents + ' real-workflow PDF metadata records.')
    Write-Output 'Definition verified: 3 supervisor/coordinator actors, 3 fictional Students/placements, assignments/scope, one DEMO policy, 6 hour entries, 9 tasks and 3 reports. PDF bytes are never in Firebase.'
    Write-Output ('DatasetId: ' + $DatasetId)
} catch {
    Write-Output ('FAILED at ' + $stage + '. DatasetId: ' + $DatasetId + '. Credential, token, HTTP body and exception details were suppressed.')
    exit 1
} finally {
    if ($null -ne $client) { $client.Dispose() }
}
