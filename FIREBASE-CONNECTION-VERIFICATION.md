# Firebase connection and final reports

## Verified deployment (2026-10-23)

The configured project, database URL and Authentication configuration were preserved. The deployed Rules were read back: only the top-level `.read` and `.write` permissions on `tasks` and `weeklyReports` were changed to `false`. Existing Rules and indexes were preserved. These collections are served through the existing authorized C# services using trusted server credentials; direct public access is intentionally denied.

Real, read-only checks found 95 tasks, 19 placements and 19 valid attendance-assignment bindings. Two existing company identities returned respectively 50 tasks / 10 trainees and 45 tasks / 8 trainees. No weekly report records existed. The three authorized service reads together took approximately 1.5 seconds per company in this check; this is a measurement, not a latency guarantee.

Enabled verified task reads and task writes using `TasksEnabled`, `FirebaseTaskSchemaValidated`, and `TasksLiveWritesEnabled`. Enabled weekly report reads using `WeeklyReportsEnabled` and `WeeklyReportsSchemaValidated`. Weekly report writes remain disabled; its existing document upload workflow needs Storage. No Storage configuration was changed. No business records were created, overwritten, or deleted for verification.

## Final report schema

`finalEvaluationWorkflows/{placementId}` is the stable, ETag-protected aggregate. It stores the student, company and supervisor relationships, required minutes (18000), submission minutes snapshots, current revision, immutable company revisions, revision-specific academic reviews, admin reviews, request digests, finalization intent and audit history. Draft content is versioned to reject stale saves.

`finalTrainingOutcomes/{placementId}` stores the approved grade (explicit admin entry, 0–100), approved revision, finalization request and completion notice. `notifications/{studentId}/final-training-{placementId}` stores one completion notification. Placement completion is updated conditionally without altering task, exam, attendance or weekly report statuses.

Attachments store `fileName`, optional `contentType`, and `uploadStatus: nameOnly`. Missing storageProvider/storagePath mean null. There are no uploaded bytes, local paths, fabricated download links or Storage requirements in this new final report workflow.

| Actor / action | From | To |
| --- | --- | --- |
| Company submits saved report | draft | pendingAcademicReview |
| Academic requests company revision | pendingAcademicReview or academicRevisionRequested | companyRevisionRequested |
| Company responds and submits new revision | companyRevisionRequested | pendingAcademicReview |
| Academic approves exact current revision | pendingAcademicReview | pendingAdminReview |
| Admin requests corrections | pendingAdminReview | academicRevisionRequested |
| Academic addresses admin feedback without company content changes | academicRevisionRequested | pendingAdminReview |
| Admin finalizes approved current revision | pendingAdminReview | completed |

Completed records are read-only. Submission and approvals recheck current relationships and approved attendance minutes. The shared attendance calculator excludes invalid, pending, rejected, overlapping, duplicate, incomplete and unrelated records. No rounding up to 300 hours occurs.

Finalization first persists its intent with a conditional write, then idempotently materializes the outcome, notification and placement update, then completes the aggregate. An interrupted operation can be reconciled by the authorized admin. Student projections withhold completion and grade if required finalization records are missing. This is a recoverable multi-step protocol, not a single transaction across attendance, assignments and all completion records.

Final workflow collections inherit existing private root Rules. Server authorization is mandatory because trusted credentials bypass Rules. Direct-path server reads do not require new query indexes. `final-report-rules.fragment.json` is documentation for explicit private permissions; it was not included in the limited module deployment.

## Verification and use

Solution build passed. The isolated C# suite passed 226 tests, including eligibility boundaries, authorization, revision cycles, stale versions, concurrency, idempotency, lost responses, reconciliation and student result privacy. JavaScript syntax check passed. Actual IIS checks returned JSON 401 for unauthenticated API requests and loaded the Web Forms page. Web Forms precompilation passed with existing Profile-hiding warnings. Earlier isolated browser checks passed; a later browser rerun and Firebase Emulator execution were not performed because their approval requests were declined. No live submission/approval test writes were performed.

Use existing authenticated accounts and open `/FinalReports.aspx` (also linked from role dashboards and final evaluation pages). Company selects its confirmed placement, saves report evaluation/comments/filename, then submits if eligible. Its currently assigned academic supervisor approves or requests revision with feedback. Company resubmissions require a response and create a new immutable revision. Admin reviews academic approval, enters the final grade and student notice filename, then confirms final approval or routes corrections back to the academic supervisor. Student sees permitted status and receives the grade/notice only after complete finalization. Each operation reloads persisted server data.

For task and trainee verification, open `/Company/ManageTasks` and `/Company/MyTrainees` using an existing company account. Confirmed placement relationships determine student visibility. The weekly report list correctly shows an empty state when Firebase has no reports. Do not import fixtures into production to populate these lists.

Implementation files: `Services/FinalEvaluation/FinalReportWorkflow.cs`, `FinalReportApi.cs`, `Services/Attendance/ApprovedTrainingMinutes.cs`, `FinalReports.aspx`, `FinalReportsApi.ashx`, `Js_project/final-reports.js`, `final-report-summary.js`, and `Assets/final-reports.css`. Existing dashboard/final evaluation pages link to the new workflow. `Shared/FirebaseServerCredential.cs` reuses the SDK-managed credential/token; TaskService parallelizes independent reads and CompanyTraineesService reads current authorized Firebase records. No persistent business-data cache or parallel database was introduced.
