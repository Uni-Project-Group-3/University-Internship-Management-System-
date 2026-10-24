# Final evaluation and completion certificate

The current company, student, placement, and academic assignments remain authoritative. No Firebase configuration, deployed Rules, live records, or Storage configuration was changed for this update.

## Transitions

| Actor/action | From | To |
|---|---|---|
| Company saves | draft / companyRevisionRequested | same state |
| Company submits immutable revision | draft / companyRevisionRequested | pendingAcademicReview |
| Academic requests changes with feedback | pendingAcademicReview | companyRevisionRequested |
| Academic evaluates 0–100 and approves exact revision | pendingAcademicReview | pendingAdminReview |
| Admin returns with mandatory feedback | pendingAdminReview | companyRevisionRequested |
| Company responds and resubmits | companyRevisionRequested | pendingAcademicReview (new revision) |
| Admin approves current academically approved revision | pendingAdminReview | completed, after consistent finalization |

Legacy academicRevisionRequested records can still be handled by their currently assigned academic supervisor. Newly returned admin reports go directly to the company. Old revisions and both supervisors' reviews are retained. Admin can view the correction history of a report they already reviewed, but cannot approve a returned/unreviewed revision.

Company submission, academic review and final administration approval snapshot the actual approved minutes. There is no minimum-hours gate for final approval, per the updated policy. The 18,000-minute reference remains a progress target, not an approval restriction. Missing/conflicting attendance bindings still fail closed; no hours are fabricated. Academic grades are independent 0–100 reviews, not an invented weighted formula. Administration explicitly confirms the final grade.

## Persistence

Existing aggregate: `finalEvaluationWorkflows/{placementId}`. ETag conditional writes and existing requestId digests remain in use.

- `academicReviews/{requestId}/academicGrade`: exact-revision academic evaluation, required for new approvals.
- `finalization/certificate`: immutable student name, optional company/period metadata, PDF filename/contentType, templateVersion and generationStatus `serverGenerated`.
- `outcome/certificate` and `finalTrainingOutcomes/{placementId}/certificate`: certificate metadata, finalized with the existing grade, placement completion and notification reconciliation.
- `certificateEmail`: delivery status, finalization requestId, actorUid and trusted timestamps. No SMTP credentials or file bytes are stored in Firebase.

The company assessment report remains separate from the student completion certificate. The certificate is a real server-generated PDF available through an authorized, no-cache `GET FinalReportsApi.ashx?action=certificate&placementId=...`. It requires completed finalization, current account ownership/assignment and a persisted certificate decision. This does not require enabling Storage. Existing company assessment PDF generation retains the existing private Storage integration.

Student responses exclude internal academic/admin feedback, audit and email delivery metadata. Certificate access before final approval is rejected on the server. Completed decision data remains read-only.

## Email

The API attempts certificate delivery after final approval. An administrator can use the **Send completion certificate by email** action for blocked setup after configuration is corrected. It resolves the recipient from the current authorized student's existing `users` profile, never from the browser.

Uses existing server environment variables `UIMS_SMTP_HOST`, `UIMS_SMTP_FROM`, `UIMS_SMTP_USERNAME`, `UIMS_SMTP_PASSWORD`, optional `UIMS_SMTP_PORT` (default 587), with TLS. A real, deliverable student email is required; `.invalid` addresses are intentionally blocked. No emails are sent by automated tests.

Statuses: blocked (missing SMTP/real email), sending (CAS-claimed), sent (SMTP acceptance), deliveryUnconfirmed (network/send uncertainty). A sent, sending or uncertain operation is never resent automatically. SMTP cannot guarantee exactly-once inbox delivery. If the process stops after claiming or SMTP acknowledgment is lost, an operator must reconcile with mail server logs before any manual recovery. No unsafe reset/resend control was added.

## Local verification and outstanding prerequisites

Changed implementation files: `Shared/CoordinatorSidebar.ascx` (one navigation link), `Services/FinalEvaluation/FinalReportWorkflow.cs` (grading, return transition and certificate metadata), `Services/FinalEvaluation/FinalReportDocuments.cs` (certificate PDF and delivery), `Services/FinalEvaluation/FinalReportApi.cs` (authorized endpoints), `Js_project/final-reports.js` (review fields/actions and downloads), `FinalReports.aspx` (script cache version). Regression coverage is in `Tests/FinalEvaluation/FinalReportWorkflowTests.cs` and `FinalReportWorkflow.browser.cjs`.

- Solution/test build, isolated C# workflows, isolated browser lifecycle, JavaScript syntax and ASP.NET precompilation are executable locally.
- `python Tests/check-certificate-prerequisites.py` reports configuration presence only, never secrets.
- `python Tests/preview-certificate.py` renders a certificate using isolated test data only into ignored Tests/Reports; this is not a real student's certificate.
- Current checked configuration has general Firebase schema/live-write gates disabled, despite final-workflow flags being enabled. Trusted gateway writes remain blocked until approved setup is verified and enabled by the operator. This update does not bypass these gates.
- Current shell process has no configured UIMS_SMTP_* variables. IIS may have its own environment; verify the actual worker process before sending.
- No production Firebase mutation, live account lifecycle, real SMTP delivery, or Rules deployment was performed.

## Exercise with authorized accounts

1. Company opens the assigned student's final report, saves and sends. Academic sees pendingAcademicReview with the immutable company/weekly-report snapshots.
2. Academic enters grade and feedback; return requires a company response/new revision, while approval sends the exact revision to administration.
3. Admin opens **Final Evaluations** from the shared coordinator sidebar. Return routes to the company, invalidates the academic approval and requires fresh academic review.
4. After eligibility and final grade confirmation, admin approves. Student receives the existing in-app notification and can download the separate certificate; the internal company assessment remains private.
5. Check persisted certificateEmail delivery state. Missing mail prerequisites do not falsely claim delivery and do not hide the certificate from the authorized student portal.
