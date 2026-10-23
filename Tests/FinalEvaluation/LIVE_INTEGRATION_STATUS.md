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

1. This runtime has no configured `GOOGLE_APPLICATION_CREDENTIALS` at Process/User/Machine scope, nor a default local gcloud ADC file. Authenticated live reads were not verified. Configure authorized credentials for `just-project-4af15` outside the repository, restart Visual Studio/IIS Express, then log in normally. Do not send or commit the credential file.
2. Coordinator scope requires a trusted server adapter implementing authorized placement enumeration and per-internship checks. No global access fallback exists.
3. Required task/report, eligibility and published-task contracts require authoritative team adapters. A completed/graded task is not assumed to be published or mandatory.
4. Student placement must establish a policy reference. Missing references are reported rather than invented.
5. Private report storage adapter is required for authorized PDF retrieval. No public storage fallback exists.
6. Production evaluation mutations remain disabled: no live-write approval or authoritative cross-workflow concurrency guard is connected. Existing isolated workflows retain their contracts.

Extension points are server-side factories on `FinalEvaluationFirebaseIntegration`, never browser parameters. Missing adapters remain explicit blockers.

## Verification executed

- Full solution build: PASS.
- Test project build: PASS.
- Automated offline suite: **224 PASS**, including 11 new integration tests; no network requests or Firebase writes.
- Actual IIS Express HTTP access checks: **20 PASS**. Anonymous GET and forged browser identity POST are denied on all four pages. Postbacks reuse their session cookies and hidden Web Forms fields.
- Command: `node Tests/FinalEvaluation/LiveIntegrationAccess.smoke.cjs http://localhost:61879` against a running local IIS instance.
- Authenticated browser interactions / live Firebase records: **BLOCKED / NOT VERIFIED**. No claim of full end-to-end browser success.

No Navbar, Sidebar, page design, Firebase configuration, rules, credentials or database records changed.
