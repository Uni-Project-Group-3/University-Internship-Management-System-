$ErrorActionPreference = 'Stop'
Set-Location -LiteralPath 'C:\Users\user\Desktop\University-Internship-Management-System-'
$resolvedFiles = @(
    'University-Internship-Management-System/University-Internship-Management-System/Assets/coordinator/contact-messages.css',
    'University-Internship-Management-System/University-Internship-Management-System/Company/ManageOpportunities.aspx.cs',
    'University-Internship-Management-System/University-Internship-Management-System/Contact.aspx',
    'University-Internship-Management-System/University-Internship-Management-System/Coordinator/ContactMessages.aspx',
    'University-Internship-Management-System/University-Internship-Management-System/Js_project/Coordinator/contact-messages.js',
    'University-Internship-Management-System/University-Internship-Management-System/Js_project/Public/contact-demo.js',
    'University-Internship-Management-System/University-Internship-Management-System/Student/ApplicationForm.aspx.cs',
    'University-Internship-Management-System/University-Internship-Management-System/Student/ExploreOpportunities.aspx.cs',
    'University-Internship-Management-System/University-Internship-Management-System/Student/MyApplications.aspx.cs',
    'University-Internship-Management-System/University-Internship-Management-System/University-Internship-Management-System.csproj',
    'University-Internship-Management-System/University-Internship-Management-System/Contact.aspx.designer.cs',
    'University-Internship-Management-System/University-Internship-Management-System/Coordinator/ContactMessages.aspx.designer.cs',
    'University-Internship-Management-System/University-Internship-Management-System/Company/ApplicationsReview.aspx.cs',
    'University-Internship-Management-System/University-Internship-Management-System/Coordinator/ContactMessages.aspx.cs',
    'University-Internship-Management-System/University-Internship-Management-System/Contact.aspx.cs'
)
& git add -- @resolvedFiles
if ($LASTEXITCODE -ne 0) { throw 'Could not stage the resolved merge. No commit made.' }
$unmerged = & git diff --name-only --diff-filter=U
if ($unmerged) { $unmerged; throw 'Unmerged paths remain. No commit made.' }
& git diff --cached --check
if ($LASTEXITCODE -ne 0) { throw 'Staged whitespace verification failed. No commit made.' }
& git status --short
Write-Host 'Resolved files staged. Merge remains open; no commit or push performed.'
