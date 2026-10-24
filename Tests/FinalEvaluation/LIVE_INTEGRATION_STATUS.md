# Real Firebase evaluation integration

Implemented on `roaa-v2`. No live database writes were executed.

`Application_Start` now registers the Company/Academic, Coordinator and Student page factories using `FinalEvaluationFirebaseIntegration`. Existing explicitly registered factories retain precedence. All sources are request scoped. Production never selects an in-memory fixture.

## Read boundary

- Signed, unexpired Forms Authentication ticket required before privileged I/O.
- Authentication UID must remain active; `users/{uid}` must be active, setup complete, have the matching role, and not require password change.
- Company placements are queried by the verified supervisor mapping and checked against company ID.
- Academic placements are queried by supervisor UID and active supervisor assignments. Conflicting/revoked assignments are denied.
- Student placements are queried by the verified profile's student ID and UID. Conflicting UIDs are excluded.
- Exact placement is reread before access, including reassignment checks.
- Coordinator role alone never grants global placement access. A trusted `ICoordinatorPlacementScope` adapter is required.

Existing paths reused: `users`, `placements`, `supervisorAssignments`, `weeklyReports`, `finalEvaluationReviewsV2`, repository policy/completion paths, `uimsAttendance/students`, `uimsAttendance/trainingAssignments`, `uimsAttendance/attendance`, and `completionDocuments`.

No schema migration or new database path is introduced. Existing placement keys are used as internship IDs only when no explicit internship ID exists. Policies must be explicitly referenced by the placement or its existing matching evaluation. No default policy is selected.

Approved attendance requires matching assignment/student/company/supervisor, valid elapsed minutes, completed checkout and approval by the assigned supervisor. Pending and rejected attendance are excluded from approved hours. Required hours must agree across placement and attendance assignment when both are supplied.

## Remaining integration blockers

1. The current environment can obtain an OAuth token and perform authorized read-only Firebase requests. The latest read-only collection probe returned HTTP 200. Do not send or commit the credential file.
2. Coordinator scope requires a trusted server adapter implementing authorized placement enumeration and per-internship checks. No global access fallback exists.
3. Required task/report, eligibility and published-task contracts require authoritative team adapters. A completed/graded task is not assumed to be published or mandatory.
4. Student placement must establish a policy reference. Missing references are reported rather than invented.
5. Private report storage adapter is required for authorized PDF retrieval. No public storage fallback exists.
6. Production evaluation mutations remain disabled: no live-write approval or authoritative cross-workflow concurrency guard is connected. Existing isolated workflows retain their contracts.

Extension points are server-side factories on `FinalEvaluationFirebaseIntegration`, never browser parameters. Missing adapters remain explicit blockers.

## Verification executed

- Full solution build: PASS.
- Test project build: PASS.
- Automated offline suite: **228 PASS**; no network requests or Firebase writes.
- Actual IIS Express HTTP access checks: **20 PASS**. Anonymous GET and forged browser identity POST are denied on all four pages. Postbacks reuse their session cookies and hidden Web Forms fields.
- Command: `node Tests/FinalEvaluation/LiveIntegrationAccess.smoke.cjs http://localhost:61879` against a running local IIS instance.
- Authenticated browser interactions: **BLOCKED / NOT VERIFIED**. The current IIS Express endpoint was not listening, and no authenticated browser session was available. The separate authenticated live Firebase read-only probe is recorded below.

## 2026-10-23 integration follow-up

- `PercentagePageIntegration.Resolve`, `CoordinatorReviewPageIntegration.ResolveAsync` and `StudentCompletionPageIntegration.ResolveAsync` now idempotently invoke the existing trusted Firebase factory registration if their factory has not been initialized. This recovers from an app-domain that reaches a page before `Application_Start`; explicit trusted factories retain precedence, and each default factory still requires its signed Forms Authentication identity before Firebase I/O.
- Credential presence and project identity were checked without displaying credential data. Token refresh was blocked by the execution sandbox's outbound socket restriction; no Firebase endpoint was reached and no database read or write occurred.
- Rebuilt the Web Forms project and test project in Debug. **225 automated tests passed**, including a regression check for missing startup registration. **20 actual IIS Express anonymous/forged-identity access checks passed** on all four pages. Authenticated Firebase reads and role-based live page flows remain unverified.
- Firebase writes remain disabled (`FirebaseLiveWritesEnabled=false`, schema validation flags false).

No Navbar, Sidebar, page design, Firebase configuration, rules, credentials or database records changed.

## 2026-10-23 live read-only follow-up

- Authenticated Firebase reads succeeded for `users`, `placements`, `supervisorAssignments`, `weeklyReports`, `finalEvaluationReviewsV2`, `evaluationPolicies`, `completionReviews`, `completionDocuments`, `uimsAttendance/students`, `uimsAttendance/trainingAssignments`, `uimsAttendance/attendance`, `tasks`, and `taskExams`. Only aggregate counts, field names and status summaries were emitted; no names, IDs, emails, credentials or tokens were displayed. No writes were sent.
- Current collection summary: 46 user profiles, 18 active placements, 25 academic assignments, 10 versioned evaluation records, 1 approved policy marked `isTestPolicy`, 10 completion review records (5 Approved, 5 Pending), 18 attendance students/assignments, and 406 nested attendance records (389 explicitly approved, 9 pending, 8 without an approval status). `weeklyReports` and `completionDocuments` are empty.
- Placements reference the configured policy ID/version, but the only policy is explicitly marked as a test policy. The model now reads that marker; official evaluation, completion and student outcome checks treat it as non-production-approved. No university result is inferred from it.
- Firebase returned HTTP 400 “Index not defined” for `placements.academicSupervisorId`, `supervisorAssignments.academicSupervisorId`, and `weeklyReports.placementId`; the Company supervisor and Student ID queries tested returned HTTP 200. The server integration now detects only Firebase's explicit missing-index response and falls back to a bounded server-side collection read followed by exact field/value filtering. It does not change Security Rules.
- Academic/Company final evaluation reads can use the existing `finalEvaluationReviewsV2` records. Coordinator pages remain blocked because no trusted Coordinator placement-scope fields/adapter exist. Task/report requirements, explicit task-grade publication, and private PDF storage remain unconnected; empty report/document collections are not treated as successful availability.
- Debug solution build and test-project build passed. **228 automated tests passed**, including missing-index fallback and explicit test-policy handling. Firebase live writes remain disabled; schema validation remains false.

## 2026-10-23 Admin Auth timeout follow-up

- The page error originates at `StudentFirebaseGateway.IsActiveAuthenticationUidAsync` while Firebase Admin SDK calls `GetUserAsync`, before the page reads its role profile or placements. The previous gateway had an 8-second cancellation limit and initialized Admin SDK credentials synchronously with `GoogleCredential.GetApplicationDefault()`, outside the cancellable async path. The original Visual Studio/IIS process environment was not available to inspect, so a stale process environment cannot be ruled out.
- The gateway now initializes ADC asynchronously with the request cancellation token, logs safe elapsed-time/result diagnostics for credential initialization and `GetUserAsync`, and gives the Auth lookup a bounded 15-second operation timeout (the ASP.NET page budget remains bounded). No UID, credential, token, or Firebase response body is logged.
- After the fix, a live read-only Firebase Admin Auth probe completed and correctly reported a random nonexistent UID as not found; the existing database gateway also read `evaluationPolicies` successfully. A read-only aggregate probe returned: `users` 46, `placements` 18, `supervisorAssignments` 25, `weeklyReports` 0, `finalEvaluationReviewsV2` 10, `evaluationPolicies` 1, `completionReviews` 10, `completionDocuments` 0, `uimsAttendance/students` 18, `uimsAttendance/trainingAssignments` 18, `uimsAttendance/attendance` 18 top-level assignment buckets, `tasks` 90, and `taskExams` 19. Counts only were emitted.
- Debug solution and test-project builds passed; **228 automated tests passed**. IIS Express ran the existing HTTP authorization smoke test: **20 checks passed** across all four pages for anonymous GET and forged-identity POST denial.
- These backend probes establish credentialed service connectivity, not an authenticated browser role session. No real Company, Academic, Coordinator, or Student page request was verified against a user's signed-in identity. Coordinator scope, trusted required-task/report/eligibility adapters, production-approved policy, published-task projection, and private report storage remain missing; weekly reports and completion documents currently have no records. Official UI writes remain disabled, and no Firebase records or rules were changed.

## 2026-10-23 Academic page integration follow-up

- The Academic page's generic access-denied banner previously conflated an invalid/expired signed role, inactive or incomplete Firebase profile, Firebase Auth UID validation failure, and a stale/out-of-scope internship selection. The server now reports safe categories for session/profile failures without emitting UID, email, token, or record payloads. A stale internship selection returns 403 while preserving the current user's separately authorized list; it does not broaden scope.
- Academic placements continue to require a signed `academicSupervisor` identity, active `users/{uid}` profile, current active `supervisorAssignments/{studentId}` mapping to the exact Firebase UID, and a matching confirmed placement. Conflicting or revoked assignments remain excluded. No alternate supervisor ID, browser role, or all-placement fallback was added.
- The Academic table now renders student name (or “Name unavailable”), student ID, linked company name when its existing `companies/{companyId}` record provides one, and separate Company evaluation and Academic review statuses. The details modal's existing JavaScript target is now present. Search/filter/clear and the existing server postback actions remain in place.
- Read-only test coverage now includes linked company-name resolution, missing student-name fallback, mismatched/revoked assignment exclusion, and sanitized authorization reasons. Approve/Request Revision remain disabled on the live page with an explicit blocker until a production-approved policy, schema validation and trusted cross-workflow guard are available. No Firebase records/rules were changed and no Firebase requests or writes were executed during this follow-up.
- Full Debug solution build and test-project build passed; **234 automated tests passed**. An authenticated live Academic browser session was unavailable, so display of real assigned records and actual user-specific denial cause remain unverified.

## 2026-10-24 Coordinator scope adapter preparation

- Added a read-only `FirebaseCoordinatorPlacementScope` for the proposed `/coordinatorAssignments/{coordinatorUid}/{placementId}` mapping. It derives UID only from the signed Coordinator Forms ticket, revalidates Firebase Auth/profile role and active state, reads only that user's assignment node, verifies audit fields and each exact active/confirmed placement, and rechecks the assignment and placement for each review. It rejects malformed/inactive/missing/conflicting records and never infers scope from university or college values.
- `CoordinatorScopeFactory` now has a trusted server composition signature that receives the request's existing Firebase database adapter. The built-in factory is guarded by `CoordinatorAssignmentScopeEnabled=false`; the new path is not active until the schema and assignments are separately approved. Evaluation/completion live writes remain disabled.
- Added the proposal and deployment/rollback checklist at `Tests/FinalEvaluation/COORDINATOR-ASSIGNMENT-SCHEMA-PROPOSAL.md`. No real Coordinator UID, placement IDs, live records, credentials, or rules were changed. Exact live assignment references remain pending authorized scope confirmation.
- Isolated coverage: assigned placement, empty scope, inactive/malformed assignment, missing/inactive/conflicting placement, inactive Coordinator profile, duplicate internship mapping, cross-Coordinator and wrong-role denial, and Firebase assignment-read failure. **243 automated tests passed; no Firebase network requests or writes were made.** Debug solution and test-project builds passed with Visual Studio MSBuild 18.10.1.
- Real Firebase Coordinator assignments and authenticated page display remain unverified. Keep `CoordinatorAssignmentScopeEnabled=false` until the authoritative scope, exact records, provisioner and deployment are approved.
