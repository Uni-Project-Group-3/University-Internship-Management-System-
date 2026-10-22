# Final Evaluations, Completion & Certification

## Current requirements supersede the historical reports below (22 October 2026)

**Current workflow:** Company owns one final percentage (0–100) and comments. Academic approves or requests revision of an exact submitted Company version; it cannot enter an independent academic score. Coordinator independently checks the approved policy/passing threshold, eligibility, approved hours, tasks and reports before an audited completion decision. Student projection excludes confidential evaluations/reasons and exposes only policy-permitted outcome and authorized private report downloads.

The four pages now share an explicitly gated Debug + loopback development composition. `UIMS_FINAL_EVALUATION_FIREBASE_DEMO=1` uses only `_devTests/roaaFinalEvaluationDemo/v1/sessions/{server-generated-id}` in the owner's development Firebase. No official UI factory is registered; service-account access is never application authentication. `UIMS_FINAL_EVALUATION_LOCAL=1` remains a separate session-private in-memory alternative. The fixtures contain three fictional students and an explicitly fictional approved policy; they are not university rules.

**Current verification:** solution build passed; 173 no-network automated tests passed. In-memory/default-disabled/isolated Student postback suites passed 129 checks. Live scoped Firebase postback checks additionally verified the shared workflow, persistence and private PDF download; see the current audit for the final run count. Actual desktop browser actions were executed, but a complete mobile/other-module navigation audit is not claimed. The historical 185/117 numbers and independent Academic scoring descriptions below are superseded, not current verification results.

**Manual launch, browser steps, reset and disable:** [BROWSER-TESTING.md](../../../Tests/FinalEvaluation/BROWSER-TESTING.md). **Control coverage and remaining gaps:** [CONTROL-AUDIT.md](../../../Tests/FinalEvaluation/CONTROL-AUDIT.md).

Persistence uses root ETag compare-and-set across evaluation, completion and audit changes. A protected session-bound selection ticket prevents browser identity/version forgery. Submitted versions and Academic reviews are append-only; only an exact Academic Request Revision can open a new Company Draft. Terminal completion decisions lock further changes. Partial Drafts remain valid. Write failures do not report success and preserve entered form fields. Firebase omits empty arrays; approval audit blockers now deserialize as an empty list while a missing decision kind is rejected.

Legacy criterion-based evaluation reads remain solely for explicit detection. Active old Company/Academic repository mutation methods and independent Academic submission services throw before writes. Existing historical records are not converted, combined or reinterpreted. If only legacy data exists, the new workflow blocks with an explicit migration requirement. No live migration was run. A future authorized migration must archive old evidence, resolve approved policy/identity and create auditable v2 data; never treat an old academic score as an Academic approval. Historical tests retained for shared fixtures are not executed as obsolete scoring acceptance tests.

Remaining production integrations: trusted login/roles, Company/Academic assignments, Coordinator scope and Student ownership, placement/eligibility, immutable approved university policy, authoritative approved-hour/task/report adapters and a concurrency guard spanning those sources, verified report profile and persistent private PDF storage/locator. Demo source definitions are persisted and validated immutably in the owned dataset; they do not modify the team's Hours/Tasks/Auth modules. PDF metadata persists in Firebase; bytes remain private session memory and require explicit regeneration after memory loss. Certificates and public delivery were not added.

---

## Historical milestone reports (superseded where they conflict with the current workflow)

## Current: Phase 5 development verification and Company row-action repair

Verified on 2026-10-22 on branch `Roaa`. **Development verification is complete; official Firebase UI integration is not complete.** Company, Academic, Coordinator and Student remain fail-closed without trusted server composition. No team shared Firebase was accessed. No rules, credentials, unrelated modules, CSS, Sidebar, page layout or branch were changed; no commit/push/merge was performed.

### Actual integration state

- The existing Firebase configuration defaults to the owner's development database, `https://uims-roa-a-development-default-rtdb.firebaseio.com/`. A conflicting process URL is rejected by the new live smoke script before any request. The existing ADC provider authenticated successfully using the external Windows credential file; its contents and access tokens were never printed, copied or committed.
- Live testing created only `_devTests/finalEvaluation/bee3bf8f9fb44107be7a8ac2bdb821e5`, conditionally using `null_etag`, verified all three fictional values, deleted only that record using its exact ETag, and confirmed a subsequent GET returned null. No official evaluation/policy/placement/completion/document path was read or written by this run. PDFs remain in private memory, never Firebase blobs or public URLs.
- No user-facing workflow was activated against Firebase. `Auth/Login.aspx.cs` explicitly declines to issue an authenticated session until authorization is configured; `Global.asax.cs` registers routes/bundles only. The four FinalEvaluation page factories are not registered. The team task pages inspected still use browser demonstration data, which cannot supply authoritative requirements or permission checks.
- Missing adapters: verified current identity and role; Company/Academic assignment and eligibility; Coordinator scope and Student ownership; authoritative placement and immutable approved policy; approved-hour ledger and required hours; required task/report completion; a concurrency guard covering authoritative readiness inputs; verified report profile/period/hours; private persistent object storage, atomic metadata publication and trusted report locator/progress/publication adapters. Service-account access proves none of these application permissions.

### Company buttons: diagnosis and repair

Previously the three named rows were frontend placeholders with `demo-*` IDs, while the explicit in-memory workflow offered a single unrelated `dev-company-internship` placement. Without the process opt-in there was no trusted workflow at all. Also, overview status looked only at the currently selected evaluation, so other rows incorrectly showed `Not connected`. ASP.NET command names/events and the JavaScript modal navigation were already wired; browser testing found no JavaScript errors.

The existing local Debug + loopback + process-opt-in gate now creates three session-private placements backed by the existing in-memory repository: Lina (`dev-company-internship`, initial partial Draft), Omar (`demo-omar`, initial Submitted and read-only), Noor (`demo-noor`, no initial evaluation). The legacy development test internship ID remains valid. Every row status is loaded through authorized C# workflow reads, and aggregate counts reflect those records. Existing fictional report/detail data is available only as labeled demonstration data when that mode is active; authoritative production report/hour/task integrations remain unavailable.

`View Reports` selects the row and opens its report panel (Noor correctly has no reports). `Continue draft` opens Lina's policy-bound draft; existing Save Draft persists in session-private server memory and reloads after navigation/refresh. Omar opens a locked submitted evaluation; Noor can start a partial draft. `View Details` opens the selected trainee's existing fictional task/hour/assessment summary. No button writes Firebase. Without trusted composition or explicit local mode, row actions are disabled and the page explains the missing integrations and opt-in. Forged disabled row events are recognized only to return the disabled page through the guarded handler; they confer no permission. A server session marker establishes the session cookie before user-bound postback MACs; repositories remain isolated between sessions.

### Verification

Full solution Debug build passed without warnings/errors. **185 automated tests passed** (183 existing plus shared-repository end-to-end and missing-adapter privilege tests). **117 Web Forms checks passed** (101 existing plus 14 Company row-action checks and two default-disabled Company checks). Actual browser clicks verified View Reports, Continue Draft, partial Save Draft and View Details; captured browser error logs were empty. HTTP checks also cover all three actions for each trainee, session isolation, Submitted locking and forged identity/events. These checks do not replace a comprehensive visual browser regression suite.

The new end-to-end test starts with one empty in-memory repository and an explicitly fictional approved policy, creates/updates/submits Company, reviews that submitted Company from Academic, creates/updates/submits Academic, retrieves both as Coordinator, verifies requirements and approves, generates a private PDF, then loads/downloads as the authorized Student. It verifies the PDF hash, unchanged approval history and denial for another student. It does not seed an already-approved completion or substitute separate repositories for each stage. The four manual page fixtures remain independent; navigating between roles in the browser is not a shared persistent internship journey.

Repeat the live check from the repository root in **Windows PowerShell 5.1**, after building:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\University-Internship-Management-System\Tests\FinalEvaluation\FirebaseDevelopment.smoke.ps1 -RunLiveDevelopmentSmoke
```

This explicit opt-in script reuses the existing Firebase configuration and ADC provider. It requires the configured process (or User-level fallback) `GOOGLE_APPLICATION_CREDENTIALS` to match `C:\Users\user\FirebaseSecrets\uims-roaa-service-account.json`, verifies file structure/project without output, and permits requests only to one generated record under the exact development root. Failure reports only the stage and test ID; if cleanup cannot be confirmed, investigate that exact ID, never delete a parent namespace. It never configures application-user authorization or activates UI factories.

### Exact manual Visual Studio steps

1. Stop debugging and exit **all** Visual Studio instances for this solution and their IIS Express process before relaunching. Existing processes do not inherit new process environment variables. Do not change the branch or discard working changes.
2. Open PowerShell and run the following (the variables are process-only; no credential file or Web.config edit is needed for fictional UI testing):

```powershell
Set-Location 'C:\Users\user\Desktop\University-Internship-Management-System-'
$env:UIMS_COMPANY_DRAFT_IN_MEMORY = '1'
# Optional, for testing the other independent fictional pages:
$env:UIMS_ACADEMIC_DRAFT_IN_MEMORY = '1'
$env:UIMS_COORDINATOR_REVIEW_IN_MEMORY = '1'
$env:UIMS_COORDINATOR_DECISIONS_IN_MEMORY = '1'
$env:UIMS_STUDENT_COMPLETION_IN_MEMORY = '1'
& 'C:\Program Files\Microsoft Visual Studio\18\Community\Common7\IDE\devenv.exe' '.\University-Internship-Management-System\University-Internship-Management-System.slnx'
```

3. Set the Web Forms application as Startup Project. Select **Debug**, **IIS Express**. Right-click `Company/ReportsFinalEvaluation.aspx` → **Set As Start Page**; press F5. Use the actual local origin Visual Studio opens (project default is `https://localhost:44372/`; a local user override may differ). Do not use a remote host. Confirm the page explicitly says **Isolated local test mode** and displays Lina Draft, Omar Submitted and Noor Not Started. If it instead explains missing authentication/assignments, the opt-in was not inherited or ASP.NET debugging/loopback is not active; restart as above.
4. Click Lina **View Reports**: her two fictional reports appear. **View Report** opens its excerpt; use the existing close/back controls to return. Click **Continue draft**: policy criteria and editable comments appear. Enter a comment and optionally leave scores blank; **Save Draft** must say saved in isolated server memory. Close/reopen or refresh, then reopen Lina: the saved comment remains. Partial drafts are valid.
5. Click Lina **View Details**: her existing sample summary shows 72/160 approved/required hours and 4/1/3 assigned/completed/pending tasks. Omar's details show his own values (120/160); his evaluation is Submitted and locked. Noor's reports show an explicit empty state; her **Start evaluation** allows a partial draft. These are fictional examples, not university requirements.
6. Open a separate private browser session: it starts from its own fixtures, without your saved Lina comments. Ending the session or restarting IIS resets the memory store. No Firebase credentials are needed for these UI tests.
7. Optional independent pages on the same actual VS origin: `/Academic/FinalAcademicEvaluation.aspx?internshipId=dev-academic-internship`, `/Coordinator/FinalCompletionReview.aspx` (select its fictional ready case and follow acknowledgment/reason requirements), and `/Student/MyProgressEvaluation.aspx?internshipId=dev-student-ready` (Approved and private PDF Download). The Coordinator page does not generate reports; Student PDF fixtures are independently generated in private memory. Other Student cases include `dev-student-no-report`, `dev-student-pending`, `dev-student-corrupt`.
8. To verify the normal fail-closed mode, stop/exit VS and its IIS process again, remove these variables from the launching PowerShell using `Remove-Item Env:UIMS_COMPANY_DRAFT_IN_MEMORY` (and the optional flags), relaunch VS and F5. Company row actions and official draft/submission remain disabled. Deployment must use ASP.NET debugging disabled and must not carry any development flags; a query parameter or browser role cannot enable fixtures.

Additional Company checks (use the existing dedicated test-server ports, or substitute the actual localhost origin):

```powershell
node .\University-Internship-Management-System\Tests\FinalEvaluation\CompanyActionsPage.smoke.cjs http://localhost:61873
node .\University-Internship-Management-System\Tests\FinalEvaluation\CompanyActionsPage.smoke.cjs http://localhost:61874 --disabled
```

Files changed for Phase 5 and this follow-up repair: `Company/ReportsFinalEvaluation.aspx`, `Company/ReportsFinalEvaluation.aspx.cs`, `Company/ReportsFinalEvaluation.Drafts.cs`, `Services/FinalEvaluation/CompanyDraftPageIntegration.cs`, this README; tests `DevelopmentIntegrationTests.cs`, `Program.cs`, `FinalEvaluation.Tests.csproj`, `FirebaseDevelopment.smoke.ps1`, `CompanyActionsPage.smoke.cjs`; visual evidence `output/company-actions/details.png` at repository root. Designer/control references and JavaScript were inspected and required no changes. All other existing working changes belong to prior milestones.

The development scope is verified. Shared-system/production integration remains blocked on the trusted adapters and persistent storage listed above. Certificates, notifications and student delivery beyond the existing authorized local PDF download remain outside this work.

## Current: Phase 4 milestone 2 — Student completion status and secure PDF UI

Implemented on 2026-10-22. **Owner-only Student completion display and authenticated PDF attachment responses work in isolated tests. Official Firebase Student access remains disabled.** No certificates, notifications/email, unrelated team-module changes, Firebase rule changes, real Firebase operations, commits or branch changes were made.

### Read-only student projection and trusted integrations

`StudentCompletionWorkflow` authorizes trusted Student role and internship ownership through the existing `ICompletionReportAccess` before privileged completion/metadata reads. It then obtains an authoritative placement through `IStudentCompletionPlacementReader`, rechecks ownership against that placement's actual student, and rejects mismatched completion identity. Neither browser student IDs, roles, document IDs nor demo flags authorize access. A requested internship from the URL always goes through these checks; the normal default internship comes from trusted server composition.

The returned `StudentCompletionView` is an allowlisted read projection: real completion status, approval date only for Approved, verified approved/required hours, mathematical approved-hour percentage, separately trusted overall progress, published student-visible task results, safe availability messages and a verified report-available flag. Missing review is `Not recorded`, not invented Pending. Missing/untrusted source values remain Unknown/Not supplied; unverified task lists do not become zero task totals. Hours sum only explicitly approved, valid unique ledger entries. Hours percentage is derived only when verified required hours are positive; an explicit zero requirement has no invented percentage. Overall progress is never inferred from hours/tasks: `IStudentOverallProgressReader` must explicitly supply it in the 0–100 range.

Existing `IApprovedTrainingHoursReader`, `IRequiredTrainingHoursReader`, `TrustedCompletionData<T>` and placement/policy references are reused. New read-only integration contracts are `IStudentCompletionPlacementReader`, `IStudentVisibleTaskReader`, `IStudentOverallProgressReader` and `IStudentCompletionReportLocator`. Requirement/task/progress snapshots must be trusted and match the exact internship, student and applicable policy ID/version. Adapters must resolve authoritative module data and publication rules; no team Hours/Tasks/Authentication module was changed. No university weights, score limits, progress percentages or completion states are invented.

Only visible tasks are projected. A task's grade and explicitly student-visible feedback are copied only when `IsResultPublished` is true; unpublished/private values are discarded, not hidden in the page. Each supplied grade retains its own supplied maximum (if any), without a fixed university scale or combined grade. Task history remains explicitly unavailable until its trusted integration exists. The workflow never reads Company/Academic final evaluations, and never returns Coordinator reasons/audit history, completion models, private storage references or document IDs to the page.

### Student Web Forms and secure attachment response

`Student/MyProgressEvaluation.aspx` retains its Sidebar, header, Bootstrap, CSS, task filters, existing exam controls and page structure. Its code-behind/designer remain consistent. Server controls now display completion, hours, separately supplied overall progress, safe task results, approval date and PDF availability. Loading/unavailable/empty/denied states are explicit. Existing exam layout remains intact but its illustrative-data preview is disabled until a separate trusted exam integration is connected; no example exam grades are substituted for real records. The page-owned demo-store dependency, browser authorization/data replacement and demonstration HTML-certificate download logic were removed. JavaScript handles responsive Sidebar, task filtering and dialog close behavior only.

`DownloadCompletionReport` is rendered only when completion is Approved and the existing backend has verified a generated PDF's metadata and bytes. An Approved completion without PDF remains Approved with a separate not-yet-available message. Corrupt/missing objects similarly do not change Approved back to Pending. Print/certificate behavior is not enabled.

On every Download postback, `StudentCompletionWorkflow.DownloadAsync` repeats trusted ownership/placement/completion checks, resolves the document ID through the trusted server locator and invokes the **existing** `CompletionReportService.DownloadForStudentAsync`. That service repeats Student ownership, policy/Issued metadata binding, PDF validity, SHA-256/length checks and final authorization. Browser document/storage fields are ignored. No public object URLs are returned.

Successful responses are buffered `application/pdf` attachments with a generated safe filename, exact Content-Length, `no-store` and `X-Content-Type-Options: nosniff`. The page suppresses its Render phase after writing bytes; `CompleteRequest` is used without `Response.End`, so no HTML/ViewState is appended and no ThreadAbortException flow is needed. Download errors are plain text: 403 unauthorized, 404 absent PDF, 409 completion not Approved, 503 unavailable/integrity-failed storage or missing integrations (400 for malformed requests). No raw exception, object path, credential or private record is exposed.

The page disables ViewState for review data, sets a session-bound postback MAC, encodes text and uses no-store responses. Session holds only a postback binding flag in ordinary composition; protected record content is not placed in HTML/JavaScript/ViewState/hidden fields. The authorized published task data and completion summary are intentionally student-visible. Existing private completion reports are downloaded; ordinary Student requests never generate or mutate them.

### Development composition, verification and remaining blockers

`StudentCompletionPageIntegration.Factory` is an unregistered trusted async request-level composition point. Its context supplies the workflow, authorized default internship and verified display name. With no factory the page shows unknown data, no Download, and denies forged download postbacks. Real Firebase access is not activated by a URL or browser identity. Real composition must use the same trusted current student and ownership adapters in the page workflow and existing report service and provide all authoritative source/locator integrations.

Only local requests + ASP.NET debug + process-only `UIMS_STUDENT_COMPLETION_IN_MEMORY=1` create a fictional in-memory context. Its private user/scope adapters and fixture data cannot be selected for production authorization. Independent fixtures cover Approved with PDF, Approved without PDF, Pending, corrupt PDF, missing object and untrusted progress. Existing report generation/storage is reused solely to prepare those in-memory test PDFs; no official paths or credentials are used. Fixture context/storage is session-local and disappears with the test session/process. No new document/PDF library, storage system or duplicate download infrastructure was introduced.

Full solution build passed. **183 automated tests passed** (161 previous + 22 Student tests). **101 Web Forms checks passed** (75 previous + 22 isolated Student/download + 4 default-disabled Student checks). Tests cover own/other/anonymous/non-student access, status without PDF, Pending/ChangesRequested/Rejected, mathematical hours versus separately trusted overall progress, unapproved-hour exclusion, missing/untrusted/mismatched sources, publication filtering, no confidential projection, PDF integrity and missing/corrupt storage, safe HTTP statuses, no mutations and default-disabled browser spoofing. Actual HTTP attachment checks verify header values, exact length, PDF header/EOF without HTML suffix, repeat-download SHA-256 equality and no private values in hidden state. Existing Company/Academic/Coordinator checks all pass. JavaScript syntax, designer consistency and diff whitespace checks also pass. These are HTTP checks, not a browser visual regression suite.

With an isolated local debug IIS process and the Student-only flag enabled:

    node University-Internship-Management-System/Tests/FinalEvaluation/StudentCompletionPage.smoke.cjs http://localhost:61873

Against a separate process with all development flags absent:

    node University-Internship-Management-System/Tests/FinalEvaluation/StudentCompletionPage.smoke.cjs http://localhost:61874 --disabled

Remaining official activation dependencies: trusted authenticated Student role/identity and ownership/assignment adapters; authoritative placement/policy references, approved/required hours, student-visible task publication and overall progress; approved immutable report policy; private object storage and issued-document locator/metadata publication integrations from milestone 1; and reviewed server composition. Trusted published exams/history remain unavailable, with no demonstration substitution. **Phase 4 milestone 2 is complete for the required isolated implementation.** Official storage access, certificates, notifications/email and other team-module work remain outside this milestone.

Files changed: `Student/MyProgressEvaluation.aspx`, `.aspx.cs`, `.aspx.designer.cs`; `Js_project/Student/my-progress-evaluation.js`; `Services/FinalEvaluation/StudentCompletionWorkflow.cs`, `StudentCompletionPageIntegration.cs` and this README; application `.csproj`; `Tests/FinalEvaluation/StudentCompletionTests.cs`, `StudentCompletionPage.smoke.cjs`, `Program.cs`, `FinalEvaluation.Tests.csproj`. Existing CSS, PDF renderer/service, policies/models and other pages/modules were not changed.

## Historical: Phase 4 milestone 1 — Internship Completion Report PDF

Implemented and verified on 2026-10-22. **Approved-only report generation, private storage/metadata publication and student-owner download authorization work in isolated tests. Official report generation/storage/download UI remains disabled.** Student/Coordinator pages, delivery, notifications, email and certificates were not implemented or changed. No real Firebase operations, rules/credential changes or unrelated module edits were made. The older milestone sections below describe their historical scope.

### Eligibility, trusted data and privacy

`CompletionReportService.GenerateAsync` requires a trusted nonblank current identity and `ICompletionReportAccess.CanGenerateAsync` before privileged reads. The adapter must implement real server role/scope authorization; service-account credentials, browser roles, demo users and the existing Student page's illustrative records are not application authorization. No production adapter/factory/endpoint was registered.

The service reads the existing authoritative completion review, requires `Approved`, checks the requested internship and validates existing decision/audit consistency. Missing, Pending, UnderReview, ChangesRequested and Rejected records cannot generate PDFs. Approval date, applicable approved immutable policy and explicit report permission are required. Successful or failed PDF generation never updates `completionReviews`, evaluations, hours, tasks, student or placement records. Existing approval remains independent of PDF availability.

`ICompletionReportDataReader` supplies a detached `VerifiedCompletionReportData` allowlist bound to the exact internship, student, policy ID/version and approval timestamp. Required verified fields are student display name/number, organization, a valid training period and nonnegative approved hours. If the decision audit records approved hours, report hours must match that evidence. University/college names are optional unless required by the policy; absent optional names are omitted. No university name, logo, dates, hours, student record, signature, seal or certification claim is fabricated. Fields have 512-character technical limits and reject control characters, invalid Unicode and explicit bidi overrides.

`EvaluationPolicy.CompletionReport` is additive and absent/null disables reports. It explicitly defines `enabled`, `requireUniversityName` and `requireCollegeName` (required boolean JSON fields when the rule is present). Data and policy trust/applicability must be established by real adapters. The report DTO has no Company/Academic forms, numeric grades, scores, private feedback, Coordinator reason/notes or decision-history fields. None are passed to the renderer; numeric grades are intentionally unsupported in this milestone even if another policy model later allows them.

### Rendering and dependency choice

The existing project had no PDF library. Added pinned **PDFsharp-MigraDoc-GDI 6.2.4 / PDFsharp-GDI 6.2.4**, targeting .NET Framework 4.6.2 and compatible with this Windows .NET Framework 4.7.2 application. The pinned 6.2.4 source is MIT-licensed; do not infer later-version licensing from this choice. References are pinned to restored package DLL identities, not Visual Studio's own assemblies. Necessary transitive packages are recorded in `packages.config`; scoped runtime binding redirects were added to `Web.config`, and the test executable generates binding redirects.

`CompletionReportPdfRenderer` uses literal document-model text, never HTML, URLs, arbitrary files or template execution. It creates A4 pages, structured rows, clear title **Internship Completion Report**, report ID, UTC generation/approval dates, repeated table headings and page numbering. Fonts are embedded for ordinary text. Windows Segoe UI is required. MigraDoc does not natively shape Arabic: complex-script field values use Windows GDI shaping in high-resolution embedded PNGs, preserving the original verified text without invented transliteration. Those values are visually readable but are raster images, so they are not selectable/searchable as text or accessible tagged text. Long unbroken values use the same bounded image path to avoid overflow. Other ordinary text remains searchable. Production deployment must validate its installed fonts, Windows GDI permissions and any additional language/accessibility requirements.

The normal, Arabic and two-page long-content PDFs were independently parsed with pypdf/pdfplumber, rendered with Poppler and visually inspected. The sample contains only explicitly fictional test values. No grades/private feedback or active PDF actions/links were present.

Library references: [6.2.4 package and Framework targets](https://www.nuget.org/packages/PDFsharp-MigraDoc-GDI/6.2.4), [pinned MIT license](https://github.com/empira/PDFsharp/blob/v6.2.4/LICENSE), [font embedding](https://docs.pdfsharp.net/PDFsharp/Topics/Fonts/Font-Embedding.html), [in-memory image support](https://docs.pdfsharp.net/MigraDoc/DOM/Contents/Images.html).

### Private bytes, metadata and failure behavior

`IPrivateCompletionReportStorage` defines atomic private create-if-absent, authorized backend reads and deletion of only an exact key/hash. `InMemoryPrivateCompletionReportStorage` is the isolated adapter; it clones bytes, validates canonical object keys and PDFs, never overwrites an object and exposes no public URLs. Keys are `completion-reports/{escapedInternshipId}/{128-bitRandomDocumentId}.pdf`. PDF bytes never enter Realtime Database.

The separate `ICompletionDocumentMetadataRepository.CreateIssuedCompletionReportAsync` capability is implemented only by the existing in-memory repository. It uses the existing logical `completionDocuments/{internshipId}/{documentId}` read contract and atomically creates immutable metadata under its lock, requiring matching Approved completion identity/date. Existing issued metadata cannot be overwritten. The real Firebase repository retains read-only document access and deliberately has **no report metadata write capability** until approved private object storage and trusted publication adapters exist. No demo document records were sent to official paths.

`CompletionDocument` retains its existing fields/statuses and adds nullable `ByteLength`, `ContentType` and `CompletionApprovedAtUtc`. New report metadata requires the document/internship/student IDs, issuer, exact policy, `InternshipCompletionReport` type, canonical private object reference, server UTC generation/issue times, `Issued` status, SHA-256 hash and byte length. Existing unrelated document contracts are not tightened. New metadata is published only after the PDF is valid, its private object write succeeds and readback matches the hash/length. Authorization/approval are rechecked before storage publication.

If metadata fails, the service checks the actual record before deleting or reporting success. A lost metadata acknowledgment is accepted only after exact persisted metadata and private bytes are verified. If no metadata exists, cleanup deletes only this attempt's exact object/hash and generation fails without an Issued record. A retry uses a fresh collision-resistant ID, preserving all valid earlier documents. Uncertain storage-write acknowledgment, unavailable metadata reads or cleanup failure may leave a private orphan for trusted reconciliation; the service does not report false success or delete possibly valid published objects. No parent/bulk cleanup exists.

### Student backend contract only

`DownloadForStudentAsync` checks trusted Student role and authoritative internship ownership before completion/metadata reads, then binds ownership to the approved review's actual student. It requires Approved completion and applicable approved report policy, Issued metadata with matching student/internship/policy/approval date, valid PDF bytes and exact hash/length. Missing, revoked, foreign-student or tampered documents are denied. Authorization is checked again before returning bytes. `CompletionReportDownload` contains bytes, fixed `application/pdf` content type and a safe generated filename, not object keys, public links, forms or supervisor feedback. No HTTP route, Student-page button or actual delivery was added; a future endpoint must use this authorized contract and private/no-cache response headers.

### Verification and remaining dependencies

Full solution and test builds passed without warnings. **161 automated tests passed** (129 previous + 32 report tests). **All 75 existing Web Forms checks passed**: 22 Academic, 14 Company, 15 readiness, 18 completion decision and six default-disabled checks. New coverage includes Approved-only generation, authorization, missing/untrusted/binding-invalid data, missing/unapproved policy, malformed metadata/PDFs, Unicode/Arabic/long pagination, privacy allowlisting, storage/read/metadata failures and retry, lost metadata acknowledgment, no overwrites, integrity, student ownership/foreign metadata/revocation denial, and unchanged approval state/history. Tests use explicit in-memory fixtures or existing mocked HTTP only; no real Firebase requests were made. Web Forms checks do not constitute a visual browser regression suite.

The existing build/test commands below still apply. To export fictional visual-QA samples, set process-only `UIMS_PDF_TEST_ARTIFACT_DIRECTORY` to a local scratch directory before running the test executable. Normal test runs generate PDFs only in memory. A reviewed fictional sample is at `output/pdf/uims-fictional-completion-report.pdf` relative to the repository root; it is not an official document, stored system record or student delivery.

NuGet's legacy CLI hit Windows path-length limits while extracting a transitive package in this long checkout path. Restore succeeded using a short temporary package directory followed by copying to the existing solution `packages` folder. Future restores on similarly configured Windows machines should use a short checkout/temporary restore path or a long-path-capable NuGet host; no global Windows settings were changed.

Remaining production dependencies: trusted authenticated user/role/scope and student ownership adapters; authoritative Approved completion and immutable approved report policy; verified report-profile/period/approved-hour data; approved private object storage; atomic create-only metadata publication and orphan reconciliation; deployed Windows fonts/GDI and any required additional accessibility support; and a separately authorized future endpoint. **Phase 4 milestone 1 is complete for the required isolated backend implementation.** Certificates, official storage activation, Student page changes and delivery remain outside this milestone.

### Files changed in this milestone

- `Models/FinalEvaluation/CompletionDocument.cs`, `Models/FinalEvaluation/EvaluationPolicy.cs`.
- `Services/FinalEvaluation/CompletionReportService.cs`, `CompletionReportPdfRenderer.cs`, `InMemoryPrivateCompletionReportStorage.cs`, `InMemoryFinalEvaluationRepository.cs`, and this README.
- Application `.csproj`, `packages.config`, `Web.config` (PDF references/dependencies and binding redirects only).
- `Tests/FinalEvaluation/CompletionReportTests.cs`, `Program.cs`, `FinalEvaluation.Tests.csproj`.
- Fictional visual-QA sample `output/pdf/uims-fictional-completion-report.pdf` at the repository root.

## Historical: Phase 3 milestone 2 — Coordinator completion decisions

Implemented on 2026-10-22. This section supersedes historical milestone statements below that completion decisions are unavailable. **Approve, Request Changes and policy-permitted Reject work in isolated backend tests. Existing approval/confirmation controls work in isolated Web Forms tests. Official Firebase decisions remain disabled.** No PDF, certificate, Student delivery, unrelated module change, real Firebase operation, rule or credential change was made.

### Decisions, authorization and freshness

`CompletionDecisionWorkflow` reuses `CoordinatorReadinessWorkflow`, `CoordinatorEvaluationReader`, the existing models and repositories. Every load/decision verifies trusted Coordinator identity, role and internship scope before privileged reads. Decisions require explicit confirmation, a stable unique decision ID, the original review revision, expected student ID and applicable approved policy ID/version. Browser identity, role and timestamps are never used as authorization or persisted as decision timestamps. Approval reruns all readiness sources immediately before the write, including Submitted evaluations, approved hours only and mandatory tasks/reports. Missing/untrusted requirements block approval. Role/scope are checked again before persistence.

All writes additionally require `ICompletionDecisionGuard` / `ICompletionDecisionLease`. A real guard must protect current identity/scope, placement, immutable policy, evaluation and requirement revisions through the conditional repository write and affirm the exact reviewed data is still current. A timer or local semaphore over changing remote data is insufficient. No production guard exists and missing/unverified guards fail closed. The workflow passes detached evidence to the guard so callback mutations cannot alter the saved decision. The local guard is explicitly limited to immutable, isolated in-memory fixtures. This contract is a required integration blocker, not a claim that separate Firebase/team services already support an atomic transaction.

Request Changes always requires a clear reason, records actual readiness blockers and uses additive `CompletionStatus.ChangesRequested`. It does not mark the internship Completed, unlock submitted evaluations, or alter hours/tasks/reports. Reject is distinct, always requires a reason, and is allowed only when the approved policy explicitly enables it. Approved and Rejected reviews are terminal. Pending, UnderReview and ChangesRequested reviews can receive authorized policy-permitted decisions. Existing status names/numeric values are preserved; no separate student/internship completion record is changed.

### Explicit policy configuration

`EvaluationPolicy.CompletionDecisions` is optional for backward-compatible reading, but required for any new decision. It contains independent `Approve`, `RequestChanges` and `Reject` rules. Each rule explicitly defines `enabled`, `requireReason` and `requireSupportingNotes`; these boolean fields are required when a rule is deserialized. Missing/null/disabled rules deny that action. Request Changes and Reject require reasons regardless of the optional policy reason flag; approval follows its configured reason requirement. Supporting notes follow the selected rule. Reason and notes are limited to 1,000 characters each as form-field constraints, not university grading requirements. No action permissions, hours, weights or university rules are inferred from missing data. No real approved policy was authored or modified.

### Conditional persistence and audit

The separate `ICompletionDecisionRepository` capability extends the two existing repositories without adding writes to the old draft/read interface. `GetCompletionReviewSnapshotAsync` exposes the original revision; `ApplyCompletionDecisionAsync` atomically replaces **only** `completionReviews/{internshipId}` with the new current state and appended `decisionHistory`. Firebase uses the exact original ETag for conditional creation/update; HTTP 412 is a conflict. In-memory storage checks and writes under its existing lock. No parent/path-wide updates or writes to evaluations, hours, students, placements or documents occur.

Repositories preserve existing creation time and generate UTC decision/update/review timestamps on the application server. Each actual event records decision ID/kind, prior status (null for no prior record), Coordinator, policy reference, reason, supporting notes, actual readiness blockers, approved/required totals and SHA-256 evidence digest. State and history are stored in one CAS, so no split audit write can succeed independently. Existing history is retained; old records without history are not backfilled or given invented events. Present history must agree with the current state; inconsistent state/history fails closed.

An exact persisted retry (same ID, actor, action, policy, reason and notes) returns an acknowledged already-recorded result without another write/event. Reusing an ID for different input conflicts. Different stale/concurrent decisions cannot replace a newer review; Approved cannot be overwritten. A lost acknowledgment returns failure/uncertainty until the same request is retried and its actual persisted event is confirmed. Conflict-path retry reads also validate agreement between current state and audit history before acknowledging an existing event; inconsistent acknowledgments fail without reporting success. No PDF is required for Approved status.

### Existing page and development safety

The current page has Approve and Confirm controls, **no Request Changes or Reject controls**. Only the existing approval path was connected; no extra action buttons were invented. The existing acknowledgment, notes, modal, Sidebar, header, CSS and Bootstrap layout are retained. A reason field within the existing confirmation modal supports policy-required reasons. Current blockers display before confirmation. Signed/encrypted `MachineKey` references bind the original revision, student, policy, unique request ID and internship to server identity/session. Confirmation repeats domain authorization/readiness checks; client fields cannot authorize approval. Fixed disabled-control event shapes are registered so forged posts receive fail-closed feedback while event validation, protected tickets and all server checks remain active.

Success appears only after repository acknowledgment or verification of the exact existing audit event. Failures preserve reason, supporting notes and the original retry reference. Refresh loads saved state/history and locks terminal decisions. Submitted evaluations remain read-only. Document generation/print/delivery controls remain disabled. No decisions/evaluation values are persisted in ViewState, browser storage, or ordinary production Session. Isolated development Session holds the explicitly created in-memory fixture context so refresh can test persistence; it is not production truth.

No production factory or guard is registered. The existing review gate (localhost + ASP.NET debug + process `UIMS_COORDINATOR_REVIEW_IN_MEMORY=1`) remains required; independent process `UIMS_COORDINATOR_DECISIONS_IN_MEMORY=1` opts into isolated decision fixtures. Without that second flag, readiness-only development behavior remains unchanged. Both flags are ignored as authorization outside the explicit development branch. Fixture repositories are memory-only and never construct Firebase or read credentials. Restarting IIS/ending the test session discards fixture decisions.

### Verification and activation blockers

Full solution build passed. **129 automated tests passed** (95 previous + 34 decision tests). **75 Web Forms checks passed**: 22 Academic, 14 Company, 15 Coordinator readiness, 3 default-disabled review, 18 isolated approval and 3 default-disabled decision checks. Coverage includes approval prerequisites, missing/unapproved policy, role/scope/student denial, explicit confirmation, required reasons/notes, Request Changes/rejection policy rules, audit correctness, stale/concurrent attempts, exact retry/lost acknowledgment, terminal protection, fresh-data guard denial, encoded/preserved input, persistence failures and saved-state reload. Firebase writes are tested with mocked HTTP only; no real Firebase requests were made. These HTTP checks are not a visual browser regression suite.

Repeat the existing build/test commands below. On an isolated local IIS process with both Coordinator flags enabled:

    node University-Internship-Management-System/Tests/FinalEvaluation/CompletionDecisionPage.smoke.cjs http://localhost:61875

Run the existing readiness, Company and Academic regression scripts against a separate process without the decision flag. Against a default process with all development flags absent:

    node University-Internship-Management-System/Tests/FinalEvaluation/CompletionDecisionPage.smoke.cjs http://localhost:61874 --disabled

All official decisions remain disabled pending trusted authentication/Coordinator role, internship scope and placement, approved immutable decision policy, authoritative approved/required hours and task/report sources, a guard that keeps these authoritative inputs current through persistence, and reviewed server composition. Request Changes and policy-permitted Reject are backend-only because this page has no corresponding controls. **Phase 3 milestone 2 is complete for the required isolated implementation.** No PDF/certificate/delivery phase was begun.

## Historical: Phase 3 milestone 1 — Coordinator readiness review only

Implemented on 2026-10-22. This section supersedes historical statements below that no Coordinator page or readiness workflow exists. **Isolated readiness review is implemented. Official Firebase UI review remains disabled by default.** No completion approval/rejection/request-changes action, completion transition, PDF, certificate, student delivery or notification is implemented. No real Firebase operations, rules/credential changes or changes to Authentication, Hours, Tasks or unrelated team modules were made for this milestone.

### Read-only authorization and readiness

`CoordinatorReadinessWorkflow.ReviewAsync(internshipId)` verifies a nonblank trusted user, Coordinator role and authorized internship scope before any placement, policy, hours, tasks, reports or evaluation reads. It reuses `CoordinatorEvaluationReader` for detached Submitted Company and Academic records. Students, anonymous callers, non-Coordinators, missing authorization adapters and out-of-scope users are denied. Missing/Draft evaluations produce explicit "missing or not Submitted" blockers; Draft scores/comments are never exposed. Submitted records must match the trusted placement's student and exact policy ID/version and include a submission timestamp. Identity mismatches block readiness and suppress evaluation details. Repository/source errors produce safe blockers without exception details; authorization failures still deny access.

Readiness requires both Submitted evaluations, the applicable approved policy, sufficient approved hours, completed required tasks and completed required reports. `CompletionReadiness` contains submitted evaluations, placement, approved/required totals, specific blockers and a computed `IsReady`; it has no completion status or write API. A ready result never means Approved. Reads across sources are not atomic: this is a review snapshot, not an approval token. Any future decision workflow must reauthorize and revalidate current authoritative data.

### Trusted server integration contracts

- `ICompletionPlacementReader`: authoritative internship/student relationship and applicable immutable policy ID/version. Return null if trust/applicability cannot be established.
- `IApprovedTrainingHoursReader`: uniquely identified decimal hour entries and explicit approval state. Only `IsApproved=true` entries count. Tasks never create approved hours. Missing, negative, duplicate or overflowing ledger data blocks readiness.
- `IRequiredTrainingHoursReader`: explicit nullable decimal requirement. Missing data never defaults to zero; an explicitly trusted zero is allowed. When the policy defines `RequiredHours`, it must agree with the trusted requirement source.
- `IRequiredTasksReader` and `IRequiredReportsReader`: separate required-ID and completed-ID lists. Missing/null/untrusted requirements block readiness. Missing required IDs produce task/report-specific reasons. Explicit empty requirements are valid only if the authoritative source actually defines none.

Requirement snapshots use `TrustedCompletionData<T>` with explicit trusted provenance and matching internship, student, policy ID and policy version. Real adapters must verify current applicable data and approvals; stale/unverifiable inputs must return null/untrusted or fail. These are server contracts, never browser attestations. No university hours, grading rules, weights or completion statuses are invented. The local 10-hour example is fictional test data only.

### Existing Coordinator page integration

`Coordinator/FinalCompletionReview.aspx` now renders server-authorized placement selection and refresh/direct-visit readiness. Submitted evaluator IDs, policy versions, UTC dates, scores, criterion feedback and general feedback are HTML encoded and read-only. Unknown profile fields remain unavailable. Readiness, approved/required hours and blockers come from the workflow. Sidebar, header, CSS, Bootstrap, navigation structure and existing element IDs are retained. JavaScript handles responsive navigation and search filtering; browser demo authorization, simulated approvals and report-print logic were removed. Existing approval/report/notes/acknowledgment controls remain disabled, including Confirm and Print. No approval or notes-saving handler exists.

Page ViewState is disabled. A session flag establishes a session cookie solely for user-bound postback MACs; no evaluation data is persisted in Session, ViewState or browser storage. Responses use no-store/no-cache. Authorized placement names are filtered before rendering, and every review rechecks role/scope. `CoordinatorReviewPageIntegration.Factory` is the unregistered request-level server composition point: future composition must use the same verified identity/access adapters for the workflow and page context and supply authoritative placement candidates. With no factory, official review stays disabled regardless of browser role, supervisor, demo or approval fields.

Local + ASP.NET debug + process-only `UIMS_COORDINATOR_REVIEW_IN_MEMORY=1` enables independent ready, blocked-hours/tasks and missing/Draft fixtures. This development branch constructs only `InMemoryFinalEvaluationRepository`, never Firebase or credentials. It is separate from official records and the other page fixtures.

### Verification and blockers

Full solution build passed. **95 automated tests passed** (70 previous + 25 readiness tests). **54 Web Forms checks passed**: 22 Academic, 14 Company, 15 isolated Coordinator and 3 Coordinator default-disabled checks. Tests cover authorization/student denial before reads, missing/Draft evaluations, missing/unapproved/version-mismatched policies, sufficient/insufficient approved hours, unapproved-hour exclusion, missing/untrusted requirements, incomplete tasks/reports, explicit empty requirements, source failures, and unchanged existing/absent completion/document records. Mocked Firebase HTTP permits GET only; no real Firebase calls were made. Coordinator HTTP checks cover encoding, selection/refresh postbacks, direct/new visits, stale-display clearing, forged action rejection and default-disabled identity spoofing. Existing Company/Academic submission regressions and JavaScript syntax checks pass. HTTP checks are not browser visual regression testing.

With an isolated local debug IIS process, run:

    node University-Internship-Management-System/Tests/FinalEvaluation/CoordinatorReviewPage.smoke.cjs http://localhost:61873
    node University-Internship-Management-System/Tests/FinalEvaluation/AcademicDraftPage.smoke.cjs http://localhost:61873 --submit
    node University-Internship-Management-System/Tests/FinalEvaluation/CompanyDraftPage.smoke.cjs http://localhost:61873 --submit

The latter two require their independent `UIMS_ACADEMIC_DRAFT_IN_MEMORY=1` and `UIMS_COMPANY_DRAFT_IN_MEMORY=1` process flags. Against a separate process with all development flags absent:

    node University-Internship-Management-System/Tests/FinalEvaluation/CoordinatorReviewPage.smoke.cjs http://localhost:61874 --disabled

Production blockers: real trusted authentication/Coordinator role, current authorized internship scope and placement/policy reference, approved immutable university policy, authoritative approved-hours ledger and required-hours configuration, authoritative task/report requirements and completion data, and reviewed server composition. **Phase 3 milestone 1 is complete for the required isolated implementation.** Production activation and later completion decisions remain outside this milestone.

## Phase 2, milestone 2: Academic final submission and Coordinator handoff

Implemented and verified on 2026-10-22. **This section supersedes earlier statements that academic final submission is unimplemented or always disabled.** Phase 2 milestone 2 now has an implemented, tested submission workflow and authorized Coordinator retrieval. Official Firebase UI access/submission still fails closed because real authentication, assignment/eligibility/policy adapters and an approved university policy are not connected. No Phase 3 page, readiness review, completion action, approval, notification, PDF or certificate was implemented. No official Firebase records, rules, credentials or unrelated team modules were modified.

### Academic submission contract

`AcademicDraftWorkflow.SubmitAsync` reuses the existing draft form parser, trusted placements and academic service checks. `AcademicSubmissionService` additionally requires a trusted current Academic Supervisor role and current internship assignment through the existing academic methods of `ICompanyFinalEvaluationAccess`. The new `IAcademicSubmissionRequirements` adapter must explicitly confirm internship eligibility and that all additional policy requirements are satisfied for the exact policy ID/version. Its policy check receives detached copies of the actual final candidate and approved policy, so it can inspect current unsaved form scores/comments rather than a stale Draft; callback mutations cannot alter what the repository writes. Missing adapters deny submission; unavailable requirements must throw or return false, never default to satisfied. No hours/task/grade eligibility rule is invented here. Real adapters must obtain authoritative server data and may confirm no additional requirements only when the approved policy actually defines none.

Submission requires an existing academic Draft with matching student/evaluator/internship/policy identity and the exact original revision. Required academic criteria must be answered; every supplied score must satisfy the policy's inclusive bounds. Unknown criteria and invalid feedback are rejected. Policy fields added/reused:

| Field | Submission behavior |
| --- | --- |
| `isApproved` | Must be true; absent/false blocks submission |
| `academicCommentsRequired` | New nullable boolean; an explicit true/false is required. True requires nonblank academic comments. Missing configuration blocks submission without imposing a university rule |
| `academicCriteria/{id}/required` | Requires that criterion's score for final submission only |
| `academicCriteria/{id}/requireFeedback` | Requires nonblank feedback for that academic criterion |

`AcademicFinalEvaluation` now has an optional additive `criterionFeedback` JSON dictionary (`{criterionId: string}`), independent of the Company dictionary. Old records without it initialize as empty. Existing score/comment contracts are retained. Criterion feedback controls use the existing evaluation-field styling; academic drafts still permit incomplete required scores/comments/feedback. The 2,000-character general-comment and 1,000-character criterion-feedback limits are field constraints, not grading rules. No grading weights or thresholds were invented.

The separate `IAcademicSubmissionRepository` capability is implemented by both existing repositories without changing Company submission behavior or the base draft/read interface. The in-memory method checks/writes under one lock. Firebase rereads identity/status/version and policy, then performs one PUT to `academicFinalEvaluations/{internshipId}` with the **original exact ETag**. A concurrent write produces HTTP 412 / ConflictException. The application server sets `submittedAtUtc` and `updatedAtUtc` using UTC and preserves the original creation timestamp; timestamps are ISO 8601 and never accepted from browser fields. Published policies remain immutable; coordination of eligibility/assignment changes belongs to trusted adapters, not a new cross-module completion workflow. Duplicate submissions and later Draft updates/recreates of Submitted records are rejected.

### Academic page and safe failure handling

The existing `reviewEvaluation` control opens the existing confirmation dialog. `submitAcademicEvaluation` is now a server Button bound to `SubmitAcademicEvaluation_Click`. Server enablement requires a saved Draft, configured submission adapter and explicit academic final-comment policy. Backend checks are repeated at submission even if events/fields are forged. The same protected session/user/internship/policy/revision ticket used for drafting carries the original expected version. Current form scores, comments and criterion feedback can be finalized atomically without a separate intermediate save.

Success is shown only after repository acknowledgment. Validation, authorization, conflict and unconfirmed-write failures preserve form input and the original revision and show no submission success. After acknowledgment the UI reports Submitted, shows server submission time, disables editing/Save/Submit and reloads the read-only record on refresh. If acknowledgment succeeds but reload fails, the message distinguishes confirmed submission, editing remains locked and refresh is requested. There is no claim of notification delivery. Layout, Sidebar, header, CSS and existing control IDs are preserved; the page script handles confirmation/back/discard interactions without authorizing users or persisting evaluations in the browser.

Existing request-specific `AcademicDraftPageContext` composition accepts an optional final `IAcademicSubmissionRequirements` argument. Legacy draft-only composition keeps submission disabled. No official server factory is registered by this change. The local/debug/process-opt-in in-memory integration explicitly provides fictional eligibility/policy confirmations and an explicit academic comment rule for its fictional policy. It never constructs a Firebase repository or uses service-account credentials as an application user.

### Coordinator read-only handoff

`ICoordinatorEvaluationReader` / `CoordinatorEvaluationReader.GetSubmittedAsync(internshipId)` require a verified nonblank user plus `ICoordinatorEvaluationAccess.IsCoordinatorAsync` and `CanReviewInternshipAsync` before any repository read. Students, non-Coordinators, out-of-scope users, missing adapters and anonymous callers are denied. The service retrieves Company and Academic evaluations and returns only Submitted records; missing/Draft entries are null and their details are not exposed. Both records' internship identity and, when both exist, student identity must match. Retrieval errors fail explicitly rather than returning a false complete result.

The returned `CoordinatorSubmittedEvaluations` contains detached Company/Academic data without ETags, write methods or completion decisions. Caller mutations do not change stored records. The two reads are a handoff view, not an atomic readiness/approval decision. No Coordinator page was connected and no completion state was created or changed. Real endpoints must use this authorized service, never expose privileged repository operations directly. The Coordinator role/scope adapter is unregistered until the team's real authorization is available.

### Verification and current activation blockers

Full solution build passed. **70 automated tests passed** (the previous 52 plus 18 academic submission/handoff tests). **36 Web Forms checks passed**: 22 Academic checks (the prior 13 plus nine submission checks) and all 14 Company regression checks. The old academic check expecting submission to remain disabled everywhere was updated to the new intended behavior: a saved, editable local test Draft can submit; new/Submitted/default-disabled records cannot. A separate development-disabled IIS Express instance additionally confirmed forged role/supervisor/frontendDemo fields cannot enable official academic submission. Designer consistency and JavaScript syntax checks pass.

Coverage includes actual Draft-to-Submitted transitions, backend timestamps, required/optional scores/comments/feedback, invalid criteria, missing/unapproved/unspecified policies, role/assignment/eligibility/policy requirement denial, missing Drafts, duplicate/stale/concurrent submissions, terminal edit locking, write/read/HTTP failures, Coordinator retrieval of actual submitted academic data alongside Company data, read-only/no-completion-mutation behavior, student/scope denial and identity mismatches. Page tests verify confirmation events, input preservation, refresh state, saved criterion feedback and forged duplicate/edit rejection. Firebase conditional writes were checked with mocked HTTP, **not live official-path submission**. Local HTTP/postback checks are not a browser visual regression suite.

For a dedicated local debug IIS Express process with process-only `UIMS_ACADEMIC_DRAFT_IN_MEMORY=1`, run:

    node University-Internship-Management-System/Tests/FinalEvaluation/AcademicDraftPage.smoke.cjs http://localhost:61873 --submit

The existing Company regression command still requires its independent process-only `UIMS_COMPANY_DRAFT_IN_MEMORY=1` setting. Both suites write only per-session server memory. The same original console test project builds/runs all 70 automated tests.

Files changed for this milestone: Academic page markup/code-behind/designer and page JavaScript; `Models/FinalEvaluation/AcademicFinalEvaluation.cs` and `EvaluationPolicy.cs`; new `Services/FinalEvaluation/AcademicSubmissionService.cs` and `CoordinatorEvaluationReader.cs`; existing AcademicDraftWorkflow, AcademicDraftPageIntegration, FirebaseFinalEvaluationRepository, InMemoryFinalEvaluationRepository and this README; application csproj; test Program.cs/project, AcademicDraftPage.smoke.cjs and new AcademicSubmissionTests.cs.

Phase 2 milestone 2 is genuinely complete as an isolated, integration-ready implementation: Academic Draft-to-Submitted and authorized Coordinator retrieval work in tests. Real Firebase UI activation remains blocked by verified user/role/assignment adapters, authoritative eligibility and policy-requirement adapters, approved immutable university policy configuration, and Coordinator scope authorization. No branch switch, commit, push or merge occurred. **Phase 3 has not begun.**

## Phase 2, milestone 1: Academic review and independent Academic Draft

Implemented on 2026-10-22. **Official academic UI review/writes remain disabled** until trusted authentication, academic internship assignments and an approved university policy are supplied. The preceding Company milestones and Phase 0 behavior are retained below as implementation history. No Company business logic, shared authentication, Firebase Security Rules, CSS, Sidebar, other team modules or production records changed in this milestone.

### Authorized backend workflow

`AcademicDraftWorkflow` composes the existing `AcademicEvaluationService`, `AcademicFinalEvaluation`, `IFinalEvaluationRepository`, and `SubmittedCompanyEvaluationReader`. It reuses `IAuthenticatedUser`, `IInternshipAssignmentAuthorizer`, and the academic-role/assignment methods on `ICompanyFinalEvaluationAccess`. A verified nonblank user, the Academic Supervisor role, a trusted internship/student placement, and current academic assignment are required before privileged reads or writes. Generic academic read/write capabilities are also checked by the existing academic service. Unknown internships, missing role/assignment adapters, students and unassigned supervisors are denied. The Firebase service account never supplies application-user authorization.

`ReviewCompanyAsync` uses the existing authorized **submitted-only** company reader, verifies the company record's student matches the trusted placement, and retrieves its original policy/version for criterion labels and score ranges. The page displays Company Submitted status/time, recorded scores, criterion feedback and general feedback in the existing Company feedback section of Student details. No company edit controls or write service are exposed. Missing or Draft company records produce an explicit "No submitted Company Evaluation" message without revealing Draft scores/comments. Repository failures show an unavailable message. If the original policy is missing, recorded values and criterion IDs remain available with an explicit explanation; no criterion/range/weight is invented. Company review is independent of academic drafting and cannot prefill academic marks.

`LoadAsync` / `SaveAsync` load, conditionally create and update the separate academic record at `academicFinalEvaluations/{internshipId}`. A new draft uses the placement's assigned policy; an existing record retains its referenced policy/version. The policy must exist, be approved (`isApproved: true`), match the version and contain valid academic criteria. Provided scores use inclusive academic policy bounds; unknown/company criterion IDs and invalid numbers are rejected. Required scores may remain unanswered in Drafts, and general comments may be blank. The existing 2,000-character academic UI comment limit is a field-length constraint, not a university grading requirement. Company scores/feedback are never copied into academic fields or changed by an academic save. No model duplication, new grading weights, schema changes or academic submission operation were introduced.

The workflow and existing repository enforce identity, Draft status and exact revision tokens. Creation is conditional on absence and updates use the original ETag/version; stale/concurrent writes cannot overwrite newer records. Submitted academic evaluations are read-only, cannot be changed/recreated, and remain locked on revisit. The existing Firebase repository remains the intended durable store; it was not exercised against official records for this milestone.

### Web Forms integration and UI preservation

The Academic page retains its layout, header, Sidebar, CSS assets, classes, existing dialog sections/footer, shared navigation script, filters and draft/comment control IDs. Existing HTML comments/save controls were upgraded in place to server controls, and approved academic criteria are rendered through a server Repeater. Server events recreate assigned-row controls in Init and use a registered asynchronous page task for reads/saves. Designer fields and IDs compile consistently. The Submit/Send-to-Coordinator controls are explicitly disabled and have no submission handler. Existing submitted fixtures are displayed in the independent academic editor as read-only data, not simulated new submissions.

The page-owned JavaScript now handles dialogs, discard/continue controls, filtering and character counts only. Its browser-demo persistence/submission interception and demo-role redirects were removed, and this page no longer loads demo-store.js. No localStorage or ViewState evaluation record is a persistent source of truth. On GET/revisit values load from the server repository. Dynamic score inputs and academic comments have ViewState disabled; a protected, user/session/internship/policy-bound revision ticket prevents client alteration and cross-page token replay. Session caches policy rendering metadata solely to preserve posted fields during read failures, never to authorize or persist evaluation values. Plain-text comments and invalid score input are handled through field-scoped request-validation exceptions and encoded server controls; global request validation is unchanged.

Success is shown only after the save operation is acknowledged. Validation/conflict/write errors preserve entered values and the old revision, with no optimistic success. An unknown transport outcome prompts a reload to inspect the saved state before retrying. An acknowledged save followed by a reload failure is reported distinctly, clears the revision and locks saving until refresh.

### Isolated development integration and team handoff

`AcademicDraftPageIntegration.Factory` is unregistered by default. Future trusted server composition should return a request-specific `AcademicDraftPageContext` constructed with the existing Firebase repository, verified user, generic academic assignment authorizer, academic role/assignment adapter, and server-resolved `AcademicDraftPlacement` records. Never create these records from browser supervisor/role/student fields. The placements carry internship/student identity, display names/company, and assigned policy ID/version. The real role and assignment adapters must resolve current relationships consistently; approved policies must remain immutable as in prior milestones.

The optional test mode requires all of: process environment `UIMS_ACADEMIC_DRAFT_IN_MEMORY=1`, ASP.NET debugging enabled and Request.IsLocal. No query parameter enables it. That branch constructs **only** the existing InMemoryFinalEvaluationRepository, with per-session storage and clearly fictional users/placements/policies. It seeds a submitted Company record, a confidential Company Draft (not displayed), a missing Company case and a historical Submitted Academic record solely for read-only testing. The latter is a fixture, not an academic submission feature. Scores/ranges and approval flags are fictional test data, never university policy. No ADC credentials or official Firebase paths are used. Memory survives refresh in the same session and resets on session expiry/application restart.

With a dedicated local debug IIS Express instance launched with that process-only setting, open `http://localhost:61873/Academic/FinalAcademicEvaluation.aspx?internshipId=dev-academic-internship`. The repeatable localhost-only HTTP smoke test is:

    node University-Internship-Management-System/Tests/FinalEvaluation/AcademicDraftPage.smoke.cjs http://localhost:61873

The existing Company page regression test remains unchanged and can be rerun with its independent `UIMS_COMPANY_DRAFT_IN_MEMORY=1` server process setting:

    node University-Internship-Management-System/Tests/FinalEvaluation/CompanyDraftPage.smoke.cjs http://localhost:61873 --submit

### Verification and files

Full solution build passed. **52 automated tests passed** (the existing 37 plus 15 academic tests). **27 Web Forms checks passed**: 13 academic checks and the existing 14 Company regression checks. A separate development-disabled IIS Express instance additionally confirmed that forged academic role/supervisor/frontendDemo parameters cannot enable company review or academic writes. New tests cover assigned review, unassigned/student denial, Draft/missing company handling, independent partial academic create/update/reload, policy approval, invalid scores, stale/concurrent saves, Submitted protection, repository read/write failures and missing integrations. Actual page postbacks cover row selection, error value preservation, tampered tokens, refresh/read-only behavior and disabled academic submission. Node syntax and designer consistency checks also pass. This is HTTP/postback verification, not a browser visual regression suite.

Files changed for Phase 2 milestone 1:

* `Academic/FinalAcademicEvaluation.aspx`, `.aspx.cs`, `.aspx.designer.cs`.
* `Js_project/Academic/final-academic-evaluation.js`.
* New `Services/FinalEvaluation/AcademicDraftWorkflow.cs` and `AcademicDraftPageIntegration.cs`; this README.
* Application `.csproj` compile entries.
* `Tests/FinalEvaluation/Program.cs`, `FinalEvaluation.Tests.csproj`, new `AcademicDraftTests.cs` and `AcademicDraftPage.smoke.cjs`.

Company review and Academic Draft create/load/update work in isolated testing. Phase 2 milestone 1 is complete as an integration-ready implementation; official activation is blocked on trusted authentication/assignment adapters and approved university policy. Academic final submission, Coordinator approval and PDFs/certificates remain unimplemented. No branch switches, commits, pushes, production data changes or credential exposure occurred.

## Phase 1, milestone 2: Submit Company Final Evaluation

Implemented and verified on 2026-10-22. The sections below this milestone record earlier Phase 0/Milestone 1 behavior; this section describes the current submission capability. **Official Firebase draft/submission UI operations remain disabled** because real authentication, placement/eligibility adapters and an approved university policy are not connected. No official evaluation records, security rules, authentication settings, notifications or other team modules were changed. No live Firebase submission test was run.

### Submission and server integration

`CompanyDraftWorkflow.SubmitAsync` reuses the existing policy-based form parser and trusted placement lookup, then invokes `ICompanySubmissionService`. `CompanySubmissionService` requires a verified nonblank current user matching the evaluator, an explicit Company Supervisor role, a matching supervisor/internship/student assignment, and eligibility for final evaluation. These answers come from the new `ICompanyFinalEvaluationAccess` adapter, never from browser role/supervisor fields or the Firebase service account. No hours/task/date eligibility calculation is invented. A missing adapter denies submission. The server factory can pass this adapter through the new optional last argument to `CompanyDraftPageContext`; older draft-only composition still works but cannot submit.

A Draft must already exist with the expected version and unchanged internship/student/evaluator/policy identity. Current form values may be finalized atomically without an intermediate save, but all final requirements must pass first. The applicable policy must exist, match the referenced version, and be approved. Additive policy JSON fields are:

| Field | Contract |
| --- | --- |
| `isApproved` | Boolean; absent/default false prohibits submission |
| `companyCommentsRequired` | Boolean or null; must explicitly be true or false before submission; true requires nonblank general comments |
| `companyCriteria/{criterionId}/requireFeedback` | Optional boolean, default false; true requires nonblank feedback for that criterion |

All required criterion scores must be present and within the policy's inclusive bounds. Unknown criteria, invalid scores and missing policy-required feedback/comments are rejected. Existing field lengths remain UI/data constraints (3,000 general-comment characters and 1,000 per-criterion feedback characters), not grading rules. Partial Draft saves remain valid, and older policy records still support drafting; no new policy is silently approved. The local in-memory fixture explicitly marks its **fictional test-only policy** approved and specifies comments required; this has no relationship to university approval.

Both repositories implement a separate `ICompanySubmissionRepository` capability, so existing read/draft contracts and test doubles remain compatible. The in-memory repository checks identity/status/version and writes under one lock. The Firebase implementation checks the same invariants and sends one conditional PUT with the original exact ETag. A competing draft save/submission causes HTTP 412, not an overwrite. Status changes only Draft -> Submitted. The application server sets `submittedAtUtc` and `updatedAtUtc` at submission using UTC (existing ISO 8601 contracts are preserved), retains the original creation timestamp, and ignores browser-created timestamps. Submitted records cannot be updated/recreated through Draft APIs or submitted again. Published policies must remain immutable; authoritative eligibility/assignment changes and policy publication coordination remain responsibilities of the real team adapters, not an invented cross-node transaction.

### UI and failure behavior

The existing `ReviewSubmission` button is now labeled **Submit Evaluation** and opens the existing confirmation dialog. `ConfirmSubmission` invokes its existing C# click handler, which queues the asynchronous submission with the same protected user/session/internship/policy/revision ticket as draft saves. Review/confirm are disabled until a Draft exists, submission adapters are configured and the policy is approved with an explicit comment rule. Backend checks remain authoritative even if a control/event is forged. No layout, CSS, Sidebar or designer control IDs were changed.

On an acknowledged submission, the page displays Submitted status/server timestamp and disables editing, Save Draft and Submit. Refresh/new visits reload the persisted read-only record. Validation/conflict/write failures preserve entered values and the original revision; they produce an error and no submission-success message. If acknowledgment succeeds but reload fails, the page explicitly reports confirmed submission, locks editing and asks for a reload rather than falsely reporting failure or allowing another write. Transport failures with unknown outcome advise reloading to inspect the actual status. No notification delivery was added or claimed.

### Academic handoff and confidentiality

`ISubmittedCompanyEvaluationReader` / `SubmittedCompanyEvaluationReader.GetSubmittedAsync(internshipId)` provide backend-only, read-only retrieval. The adapter must verify both the Academic Supervisor role and the current academic assignment **before any repository read**. Draft/missing records return null; Submitted records return detached data without an update operation or version token. Students, unassigned academics, anonymous users and missing adapters are denied. The Academic Evaluation page is untouched. Real endpoints must use this service, not expose privileged repository methods directly; the existing generic assignment adapter must likewise deny confidential company-read capabilities to students.

### Verification and scope

The full solution builds, **37 automated tests pass** (the existing 24 plus 13 submission tests), and **14 IIS Express Web Forms checks pass** (seven Draft checks plus seven submission checks). Tests cover required/optional fields, invalid/unknown criteria, approval absence, role/assignment/eligibility denial, duplicates, stale/concurrent submissions, terminal edit protection, authorized academic retrieval and student denial, failure propagation, server timestamps and mocked Firebase ETag/412 behavior. Live postbacks verify value preservation, submission acknowledgment/status, refresh locks and forged duplicate/edit rejection. A separate development-disabled IIS Express instance confirmed that forged browser role/supervisor/frontendDemo parameters cannot enable official submission. JavaScript syntax and page/control consistency were checked; these are HTTP/postback checks, not a browser visual regression suite.

Run the existing console test project as documented below. For the full isolated page test, start the local debug IIS Express instance with process-only `UIMS_COMPANY_DRAFT_IN_MEMORY=1` and run:

    node University-Internship-Management-System/Tests/FinalEvaluation/CompanyDraftPage.smoke.cjs http://localhost:61873 --submit

All fixture writes are to the per-session in-memory repository. No demonstration evaluations are inserted into official Firebase paths. Milestone 2 is complete as an isolated, integration-ready implementation; official activation remains blocked until the team supplies trusted user roles, company/academic assignments, eligibility and an approved immutable policy. Coordinator approval, academic evaluation authoring, PDFs/certificates and notifications remain out of scope.

Files changed in milestone 2: `Company/ReportsFinalEvaluation.aspx`, `.aspx.cs`, `ReportsFinalEvaluation.Drafts.cs`; page JavaScript; `Models/FinalEvaluation/EvaluationPolicy.cs`; new `Services/FinalEvaluation/CompanySubmissionService.cs`; existing CompanyDraftWorkflow, CompanyDraftPageIntegration, FirebaseFinalEvaluationRepository, InMemoryFinalEvaluationRepository and this README; application csproj; `Tests/FinalEvaluation/Program.cs`, `FinalEvaluation.Tests.csproj`, `CompanyDraftPage.smoke.cjs`, and new `CompanySubmissionTests.cs`. No branch changes, commits or pushes.

## Phase 1, milestone 1: Company draft loading and saving

The Company/ReportsFinalEvaluation page now uses `CompanyDraftWorkflow`, which calls the existing `CompanyEvaluationService` and `IFinalEvaluationRepository`. The page is asynchronous and retains its existing layout, control IDs, designer, Sidebar, CSS, Bootstrap assets, modal navigation, filters and report-preview controls. Its page-owned JavaScript now manages interactions and leaves Save/Preview to Web Forms postbacks; it no longer rewrites the page from browser demo records, redirects based on demo roles, stores drafts in localStorage, or simulates submission. The browser demo-store script is no longer loaded by this page. Other team pages/scripts/styles are unchanged.

### Current production state

**Official Firebase company draft reads/writes remain disabled.** No real authentication/placement adapters are registered. A query string, Session FullName, hidden field, posted evaluator/role, browser demo login, or `frontendDemo=1` cannot enable access. The default page displays an explicit unavailable message and disables Save Draft. No demonstration records were written to official Firebase paths during this milestone. Phase 0 live Firebase connectivity remains verified, but this company UI milestone was tested against the isolated in-memory repository only.

To connect production later, register `CompanyDraftPageIntegration.Factory` in trusted server startup/composition code. The factory receives HttpContext and must return a fresh `CompanyDraftPageContext` for the **verified current request**, constructed with the existing FirebaseFinalEvaluationRepository, real IAuthenticatedUser, real IInternshipAssignmentAuthorizer, and trusted CompanyDraftPlacement records (internship/student identifiers, student display information, assigned policy ID/version). Return null or deny unauthorized requests. Resolve placements using server records before rendering; never construct them from request role/supervisor/student fields or demo data. Repositories/HttpClient can be shared safely as appropriate; the user and assignment context must reflect the current request. No factory is installed by this change.

`?internshipId={assignedInternshipId}` opens a draft on a fresh visit. The value is only a lookup selector into trusted placements. Loading also checks the current assignment through the existing service, verifies evaluator/student identity, and loads the applicable policy. Existing drafts retain their referenced policy; new drafts use the placement's assigned policy. Policies must remain immutable as required by Phase 0. Empty/unavailable policies disable editing instead of inventing criteria. Company/academic types and paths remain separate.

### Draft behavior and concurrency

Partial scores and blank comments are valid. Supplied numeric values use invariant decimal parsing, inclusive policy ranges and preserved criterion IDs; no university limits, weights or final grades are invented. Existing criterion feedback controls are persisted through an **optional additive company-only** `criterionFeedback` JSON object (`{criterionId: string}`). Older Phase 0 records without this field still load with empty feedback; academic contracts are unchanged. General comments continue using `feedback`. There is no submission, completion approval or PDF operation.

The asynchronous page task recreates policy-based controls and restores their posted values explicitly, including invalid values, without overwriting them on a failed save. Feedback/comments are plain text; the page accepts them through field-scoped request-validation exceptions and renders them using encoded TextBox/Literal controls. No global request-validation or security setting was changed. Evaluation TextBoxes and the criteria repeater have ViewState disabled; persisted values come from the repository. Session holds only rendering policy metadata so posted edits can remain visible if a reload fails. Session metadata never authorizes or saves a record.

A MachineKey-protected, session- and user-bound revision token carries the original internship, policy and repository version. It contains no credentials and cannot grant authorization. An unanswered initial form carries a protected create intention; subsequent saves carry the original ETag. The workflow checks status/version and the repository still conditionally creates/updates the record, including a final server-side compare-and-swap. A Submitted record is read-only and cannot be recreated or changed by a forged event. Stale/tampered tokens are rejected; failure responses preserve entered values and the old token, so they cannot silently advance to a new version. Successful save messages occur only after the repository acknowledges the write. If acknowledgment succeeds but reloading fails, the message distinguishes those outcomes and the revision is cleared until reloading. Firebase transport failures never fall back to local memory.

### Isolated localhost development mode

Before launching a **local debug** IIS Express process, set only that launcher process's `UIMS_COMPANY_DRAFT_IN_MEMORY=1`. The context is created only if all three conditions hold: the server variable is exactly 1, ASP.NET debugging is enabled, and Request.IsLocal is true. No URL parameter enables this mode. This branch can construct only the existing InMemoryFinalEvaluationRepository; it never constructs a Firebase repository. One fictional assignment, a fictional evaluator and a clearly labeled test-only policy are confined to this branch. The test policy's arbitrary ranges are not university policy. A per-session repository survives refresh and page revisits in the same session and resets on session expiry/app restart. It is not durable storage or production authorization. Production must keep this variable unset.

For example, from the repository root in PowerShell, launch a dedicated hidden test server with the process variable set and visit `http://localhost:61873/Company/ReportsFinalEvaluation.aspx?internshipId=dev-company-internship`. Use a separate local IIS Express instance for testing; do not change a shared server's environment. The smoke test requires a running local instance in the explicit mode and refuses non-localhost origins:

    node University-Internship-Management-System/Tests/FinalEvaluation/CompanyDraftPage.smoke.cjs http://localhost:61873

The console test project now runs **24 tests**: the original 16 plus eight company workflow tests covering initial load, partial save, feedback, a new workflow instance/reload, updates, policy errors, duplicate/stale saves, Submitted locks, authorization, and repository read/write failures. Build/run commands below still apply. Seven actual HTTP Web Forms checks passed through IIS Express: create, refresh reload, update/new visit, invalid-score value preservation without false success, stale-save preservation, tampered revisions, and unchanged repository data after rejected writes. A separate IIS Express instance with development mode disabled confirmed Save Draft remains disabled despite forged frontendDemo and supervisor parameters. JavaScript syntax was checked with Node. These are HTTP/postback tests, not a browser visual regression suite. The full solution builds successfully. No Firebase records were written in these tests.

Files changed for this milestone:

* `Company/ReportsFinalEvaluation.aspx`, `.aspx.cs`, `.aspx.designer.cs`, and new `ReportsFinalEvaluation.Drafts.cs`.
* `Js_project/Company/reports-final-evaluation.js` (page interactions only; no browser storage persistence).
* `Models/FinalEvaluation/CompanyFinalEvaluation.cs` (optional company criterion feedback).
* New `Services/FinalEvaluation/CompanyDraftWorkflow.cs` and `CompanyDraftPageIntegration.cs`; this README.
* Application `.csproj` compile entries.
* `Tests/FinalEvaluation/Program.cs`, `FinalEvaluation.Tests.csproj`, new `CompanyDraftWorkflowTests.cs`, and new `CompanyDraftPage.smoke.cjs`.

Team dependencies remain verified authentication, supervisor/placement/student relationships and approved immutable policies. No frontend interaction currently saves real company drafts to Firebase; once those adapters are supplied through the server factory, the same load/save workflow uses the existing Firebase repository without changing the UI or bypassing service authorization. Final submission and all other Phase 1 features remain out of scope.

## Scope and inspection

The solution is `University-Internship-Management-System.slnx` (there is no tracked `.sln`). The Web Forms project targets .NET Framework 4.7.2; its root namespace is `University_Internship_Management_System`. Existing opportunity code uses `UIMS` namespaces; this module retains the namespace of the existing empty `CompanyFinalEvaluation` class and completes that class in place.

Inspected existing Models, Services, Shared/FirebaseAuthService, NuGet references, and Company/ReportsFinalEvaluation, Academic/FinalAcademicEvaluation, Coordinator/FinalCompletionReview and Student/MyProgressEvaluation markup and code behind. Existing pages contain preview/demo behavior and do not supply trusted authorization or approved grading policy. They remain unchanged and are not wired to these services. Shared authentication, sidebar, scripts, CSS, tasks and hours are unchanged.

No Firebase Admin SDK was installed. The .NET Admin API does not provide a Realtime Database client for this module; REST avoids adding an unrelated database library. `Google.Apis.Auth`, `Google.Apis` and `Google.Apis.Core` 1.77.0 use their compatible **net462** assemblies. Their Newtonsoft.Json dependency requires 13.0.4, so the existing 13.0.3 reference was updated to 13.0.4. The assembly version remains 13.0.0.0; the existing Web.config binding redirect remains valid and unchanged.

## Configuration and composition

Default development root: `https://uims-roa-a-development-default-rtdb.firebaseio.com/`.

Optional process environment variable `UIMS_FINAL_EVALUATION_DATABASE_URL` replaces the root. Only HTTPS Firebase database roots with no path, query, credentials, fragment or custom port are accepted. Do not append a collection name.

Use `GOOGLE_APPLICATION_CREDENTIALS` pointing to a **local service-account JSON file outside the repository and web root**, or Application Default Credentials on a server with an attached service account. Grant the service account only the development project permissions it needs. Do not put JSON, private keys, access tokens or refresh tokens in source, Web.config, logs, browser code or screenshots. Do not make database rules public. Existing Firebase Authentication settings and API keys are unrelated and unchanged. Ignore patterns provide an additional guard, not a substitute for keeping credentials outside the checkout.

`GoogleAdcAccessTokenProvider` obtains and refreshes short-lived OAuth2 tokens through Google's library with both scopes:

* `https://www.googleapis.com/auth/firebase.database`
* `https://www.googleapis.com/auth/userinfo.email`

Tokens are sent in the `Authorization: Bearer` header, never URL parameters. Credential errors do not include secrets. A missing credential is an error; Firebase failures never silently select the fake repository.

Construct a long-lived HttpClient with a bounded timeout (e.g. 15 seconds), `GoogleAdcAccessTokenProvider`, `FirebaseFinalEvaluationConfiguration`, and `FirebaseFinalEvaluationRepository`. Inject the repository and real implementations of `IAuthenticatedUser` and `IInternshipAssignmentAuthorizer` into the four services. No default authorization implementation exists. An absent/anonymous user, a denied assignment, or a mismatched evaluator is rejected. Services are the application entry point; do not expose the privileged repository directly to page callers.

The assignment adapter must obtain role, internship, student and supervisor relationships from trusted server records. On reads, `studentId` is null and the adapter must resolve the internship's student itself. On writes, it must verify the supplied student belongs to that internship. Supported capability strings are `companyEvaluation.read`, `companyEvaluation.write`, `academicEvaluation.read`, `academicEvaluation.write`, `completionReview.read`, and `completionDocument.read`. A student read must check ownership; supervisor reads/writes must check role and assignment; coordinator reads must check their authorized scope. Fictional test identities are never authorization adapters for production.

## Exact storage contracts

All field names below are case-sensitive camelCase. Dictionary keys are preserved verbatim. Keys are nonblank strings, at most 768 UTF-8 bytes, excluding `.`, `$`, `#`, `[`, `]`, `/`, ASCII controls and DEL. Identifiers and versions are also validated as keys. Paths are internal constants; no method accepts a path from a caller. REST adds `.json` automatically.

| Path | Type | Phase 0 operations |
| --- | --- | --- |
| `companyFinalEvaluations/{internshipId}` | CompanyFinalEvaluation | Read, create Draft, update Draft |
| `academicFinalEvaluations/{internshipId}` | AcademicFinalEvaluation | Read, create Draft, update Draft |
| `completionReviews/{internshipId}` | CompletionReview | Read only |
| `completionDocuments/{internshipId}/{documentId}` | CompletionDocument | Read one document's metadata only |
| `evaluationPolicies/{policyId}` | EvaluationPolicy | Read only |

### CompanyFinalEvaluation and AcademicFinalEvaluation

Separate nodes and CLR types share these fields through a data-only `EvaluationRecord`:

| Field | JSON type | Meaning |
| --- | --- | --- |
| internshipId | string | Must match path key |
| studentId | string | Assigned student identifier |
| evaluatorId | string | Assigned, authenticated supervisor identifier |
| policyId | string | Existing policy node key |
| policyVersion | string | Exact referenced policy version |
| scores | object | `{criterionId: number or null}`; empty object allowed for Draft |
| feedback | string or null | Free text feedback |
| createdAtUtc | ISO 8601 timestamp | Set by repository on creation, preserved on updates |
| updatedAtUtc | ISO 8601 timestamp | Set by repository on each successful save |
| submittedAtUtc | ISO 8601 timestamp or null | Must be null for Draft writes |
| status | string | `Draft` or `Submitted`; only Draft writes exposed |

The first five fields and status are required on deserialization. Firebase removes empty objects and null children: an absent scores map is restored as an empty dictionary; absent/null criterion scores both mean unanswered. Unknown JSON members, malformed JSON, missing required fields, invalid identifiers and record/path mismatches raise RepositoryException. Supplied scores must reference the matching company's/academic's criterion map and satisfy its inclusive minimum/maximum. Missing scores and null scores are permitted for drafts. `Evaluation(..., final: true)` also checks required criteria and refuses an empty criterion configuration; it does not submit anything.

### EvaluationPolicy

| Field | JSON type | Meaning |
| --- | --- | --- |
| policyId | string | Must match path key |
| version | string | Required immutable version identifier |
| companyCriteria | object | Map of criterion IDs to CriterionPolicy |
| academicCriteria | object | Separate map of academic criterion IDs |
| requiredHours | number or null | No default or invented hours |
| passingThreshold | number or null | No default or invented threshold |
| updatedAtUtc | ISO 8601 timestamp | Policy administration timestamp |

Each CriterionPolicy has `label` (string or null), required `minimum` and `maximum` (numbers), required `required` (boolean), and `weight` (number or null). Minimum must not exceed maximum; an optional weight cannot be negative. No total score, weighted grade, hour eligibility or pass/fail decision is calculated. Policy identity and version are required on deserialization. Firebase can omit empty criterion maps; missing maps initialize as empty dictionaries and final validation refuses an empty applicable map. Policies are supplied by authorized university administration, never seeded by this module. Treat published policy IDs/versions as immutable: changed rules should use a new policy ID/version, preserving the policy referenced by existing drafts and future submissions. Cross-node policy publication/immutability enforcement is an integration responsibility.

### CompletionReview

Fields: `internshipId`, `studentId`, `reviewerId`, `policyId`, `policyVersion` (strings); `status` (`Pending`, `UnderReview`, `Approved`, `Rejected`); `feedback` (string or null); `createdAtUtc`, `updatedAtUtc` (timestamps); `reviewedAtUtc` (timestamp or null). Internship/student/policy/version/status are required during deserialization. Reviewer is required by validation after Pending. Internship ID must match the path.

Future transition guard permits only Pending -> UnderReview and UnderReview -> Approved/Rejected. Approved and Rejected are terminal. There is **no completion write or approval API** in Phase 0, so approved records cannot be overwritten through this module.

### CompletionDocument

Fields: `internshipId`, `documentId`, `studentId`, `issuerId`, `policyId`, `policyVersion`, `documentType`, `storageObjectPath`, `sha256` (strings, optional values nullable); `status` (`Pending`, `Issued`, `Revoked`); `createdAtUtc` (timestamp); `issuedAtUtc` (timestamp or null). Internship/document/student/policy/version/status are required on deserialization; document type is required by validation. Both path keys must match. Issued metadata additionally requires issuer, issued timestamp and storage object path. `storageObjectPath` is an object reference, not a public URL or embedded PDF. Hash verification and storage access belong to later phases.

Future transition guard permits Pending -> Issued and Issued -> Revoked. No document writes, file downloads or PDF generation are implemented.

## Concurrency, missing records and failures

Evaluation reads return `RecordSnapshot<T>` containing `Value` and an opaque `Version` (Firebase ETag). A missing evaluation has a null Value; other missing reads return null. Missing records are not connection failures. Draft creation uses conditional PUT with `if-match: null_etag`, rejecting duplicates. Draft update requires the snapshot version; it reads the current record to check Draft status and immutable identities, then conditionally PUTs with **the same expected ETag**. Any intervening write, including a future final submission, causes HTTP 412 / ConflictException. The current creation timestamp is preserved. Inputs and fake outputs are cloned to prevent caller mutation across pending saves.

No automatic retry overwrites a conflict. Reload and reconcile explicitly. A future Submit operation must use the same compare-and-swap pattern after final policy validation; Submitted -> Submitted and Submitted -> Draft are forbidden. Final submission and completion approval are intentionally absent. Future cross-record completion decisions will require authoritative hours/tasks/evaluation snapshots and a transaction strategy; separate record reads alone are insufficient for approval.

HTTP/network/timeouts and invalid record contracts raise RepositoryException. HTTP status codes are retained in safe error messages, without response bodies or tokens. HTTP 412 raises ConflictException. Caller validation raises ArgumentException; authorization raises UnauthorizedAccessException. The in-memory repository has per-instance storage, locked conditional saves, version tokens, cloned values, constructor-only fixtures and explicit `SimulateFailure`; it never contacts Firebase or automatically seeds official records.

## Local tests and verification

The separate .NET Framework 4.7.2 console test project is `Tests/FinalEvaluation/FinalEvaluation.Tests.csproj`. It requires no test framework packages and is not deployed with the website. Build it with Visual Studio MSBuild, then run `Tests/FinalEvaluation/bin/FinalEvaluation.Tests.exe`. Nonzero exit indicates failure. Restore the application's packages.config first on a clean checkout.

Tests use fictional IDs and a deliberately fictional policy with arbitrary test-only bounds. They cover both evaluation draft lifecycles, partial drafts, invalid/unknown scores, missing records, duplicate creates, stale versions, submitted-record protection, duplicate-final-transition rejection, terminal approved records, authorization denial, evaluator spoofing, repository failures, invalid keys, malformed REST data, HTTP errors, connection failures, timeouts, bearer headers, conditional creation and ETag/412 updates. Simulated HTTP handlers ensure no network access.

The complete solution and test executable build using Visual Studio 18 MSBuild. All 16 existing local tests passed again on **2026-10-21**, including regression assertions for Firebase null_etag responses. In the final verification, the Windows user-level ADC setting and external service-account JSON were accessible; required fields, credential type, token endpoint and development project identity were validated without displaying credential material. The restricted process did not inherit the setting, so the isolated Windows PowerShell test explicitly set its own process path. OAuth token acquisition and an authenticated read through the existing repository succeeded. No persistent Windows environment settings were changed during this verification.

Exactly one fictional record was conditionally created at `_devTests/finalEvaluation/a7e706c5a6ce4c4ea0ddf70d7412a422`. Readback matched all three expected fields, the exact verified record was conditionally deleted using its ETag, and an independent authenticated GET confirmed HTTP 200 with a null body. No real evaluation, student, completion or policy records were written. Firebase Security Rules and Authentication configuration were unchanged. This verifies development connectivity and isolated test-path permissions; it does not verify production authorization or access to every module path.

The first attempts revealed that Firebase returns raw ETag values which .NET may not expose through its typed ETag property (including unquoted null_etag). The repository and smoke recipe now read the raw header. After the smoke recipe initially stopped at readback, verification resumed using the same test ID and completed cleanup; no second record was created. The final independent cleanup check exited successfully. Existing conditional-write and Submitted protections are preserved.

### Credential setup (now verified)

Move the existing downloaded service-account file to `C:\Users\user\FirebaseSecrets\uims-roaa-service-account.json`, outside the repository. Do not create another key or paste its contents into this README. In PowerShell, run this command only once the file is present:

```powershell
$credentialPath = 'C:\Users\user\FirebaseSecrets\uims-roaa-service-account.json'
if (-not (Test-Path -LiteralPath $credentialPath -PathType Leaf)) { throw 'Credential file is missing.' }
[Environment]::SetEnvironmentVariable('GOOGLE_APPLICATION_CREDENTIALS', $credentialPath, 'User')
$env:GOOGLE_APPLICATION_CREDENTIALS = [Environment]::GetEnvironmentVariable('GOOGLE_APPLICATION_CREDENTIALS', 'User')
if ($env:GOOGLE_APPLICATION_CREDENTIALS -ne $credentialPath) { throw 'ADC environment configuration did not match.' }
```

The user-level setting persists for new processes. The process assignment refreshes only this PowerShell session. Restart Visual Studio/IIS Express (and other parent applications that launch the website) to inherit the new setting. This command changes no unrelated environment variable. Setting a path does not validate the file or prove authentication succeeded.

### Repeat the existing build and local tests

From the repository root, using the installed Visual Studio MSBuild:

```powershell
$msbuild = 'C:\Program Files\Microsoft Visual Studio\18\Community\MSBuild\Current\Bin\MSBuild.exe'
& $msbuild 'University-Internship-Management-System/University-Internship-Management-System.slnx' /t:Build /p:Configuration=Debug /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Solution build failed.' }
& $msbuild 'University-Internship-Management-System/Tests/FinalEvaluation/FinalEvaluation.Tests.csproj' /t:Build /v:minimal /nologo
if ($LASTEXITCODE -ne 0) { throw 'Local test build failed.' }
& './University-Internship-Management-System/Tests/FinalEvaluation/bin/FinalEvaluation.Tests.exe'
if ($LASTEXITCODE -ne 0) { throw 'Local tests failed.' }
```

### Isolated live connectivity and smoke test

After supplying the missing file and successfully building, run the following in a fresh **Windows PowerShell 5.1** session from the repository root. It reuses the existing configuration, ADC provider and repository for a read-only connectivity probe. Because the production repository intentionally disallows arbitrary paths, the separate smoke requests use its same configuration/token provider with a fixed `_devTests/finalEvaluation` prefix. The initial policy probe reads a unique, expected-missing policy only; it creates no policy or evaluation records.

The script validates credential structure without printing it, requires the exact development root, obtains an authenticated repository response before writing, conditionally creates one unique fictional record, verifies it, conditionally deletes that exact version, and verifies absence. No bulk or parent deletion is used. On any failure it stops and reports only the safe stage and test ID; a record may remain if failure occurred after creation. Investigate that exact ID before any manual cleanup. The live run was verified as described above. The recipe includes the raw-ETag and final variable-cleanup corrections discovered during that run.

```powershell
$ErrorActionPreference = 'Stop'
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
    $env:GOOGLE_APPLICATION_CREDENTIALS = $credentialPath
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
    $repository = New-Object University_Internship_Management_System.Services.FinalEvaluation.FirebaseFinalEvaluationRepository -ArgumentList $client,$provider,$configuration
    $stage = 'authenticated repository connectivity'
    $probe = $repository.GetPolicyAsync('dev-connectivity-' + $testId).GetAwaiter().GetResult()
    if ($null -ne $probe) { throw 'Unexpected probe collision.' }
    Write-Output 'Authenticated repository connectivity succeeded.'
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
    $payload = @{ testRunId = $testId; fictional = $true; purpose = 'Phase0Connectivity' } | ConvertTo-Json -Compress
    $null = Invoke-IsolatedRequest 'PUT' $payload 'null_etag'
    $stage = 'readback verification'
    $read = Invoke-IsolatedRequest 'GET' $null $null
    $record = $read.Body | ConvertFrom-Json
    if ($record.testRunId -ne $testId -or $record.fictional -ne $true -or $record.purpose -ne 'Phase0Connectivity' -or
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
```

### Security review and remaining blocker

Verification on 2026-10-21 found no tracked credential filename candidates and no private-key/access-token/refresh-token JSON markers in tracked text or scanned working source (excluding Git internals, package caches and build outputs). This is a scoped source inspection, not proof about historical Git revisions or every possible secret format. The configured default root matches the supplied HTTPS URL and no conflicting process override was present. Public repository methods accept identifiers, not arbitrary paths. Conditional draft updates, Submitted-state protection and fail-closed service authorization remain intact and are covered by the passing tests. The privileged repository is not a user-facing authorization layer; real authentication and placement adapters are still required before page integration.

The final verification changed this README, the existing FirebaseFinalEvaluationRepository raw ETag parsing, and regression assertions within the existing Program.cs test case (the test count remains 16). Models, interfaces, service authorization, frontend and shared authentication were preserved. No Phase 0 connectivity blockers remain. No Firebase rules or authentication settings were changed. Dependencies on other team members remain as listed below.

## Files and next-phase dependencies

Completed the existing empty `Models/FinalEvaluation/CompanyFinalEvaluation.cs`; added AcademicFinalEvaluation, CompletionReview, CompletionDocument and EvaluationPolicy model files. Added this README, Contracts, FirebaseConfiguration, FirebaseFinalEvaluationRepository, InMemoryFinalEvaluationRepository, FinalEvaluationValidation and FinalEvaluationServices under `Services/FinalEvaluation`. Added the separate console test project and Program.cs. Updated the application's csproj and packages.config for compilation/dependencies, and the solution-folder .gitignore for local credential filenames. No commits, branch switches or pushes were made; branch is Roaa.

Phase 1 requires real authenticated-user and role/assignment adapters from the authentication/placements team, stable internship/student/supervisor IDs, coordinator scope rules, officially approved and immutable policies, and server ADC credentials/permissions. Hours/tasks teams must provide authoritative completion inputs before approval is implemented. Then connect the existing pages to these authorized services, implement and test conditional final submission/approval, audit requirements, and later certificate/PDF generation. Page demo identities and example UI grading settings must not become backend configuration or authorization.

References: [Firebase REST authentication and scopes](https://firebase.google.com/docs/database/rest/auth), [Firebase conditional requests and ETags](https://firebase.google.com/docs/reference/rest/database/), [Google.Apis.Auth package compatibility](https://www.nuget.org/packages/Google.Apis.Auth/1.77.0).
