# Firebase Auth and student integration — 24 October 2026

## Verified live evidence and the 403

The final read-only run used the existing Application Default Credentials and existing Firebase Admin integration against `just-project-4af15`, database `https://just-project-4af15-default-rtdb.firebaseio.com`. Final observation completed at 2026-10-24 01:27:58 Asia/Amman (2026-10-23 22:27:58 UTC). Reads are sequential, not an atomic database snapshot. No Firebase/Auth/Supabase data, Rules, IAM or credentials were modified.

Exact metadata request: `POST https://identitytoolkit.googleapis.com/v1/projects/just-project-4af15/accounts:lookup` (Firebase Admin `GetUserAsync`). The Admin path succeeded. A controlled comparison using the existing database-scoped token against that same read-only endpoint returned HTTP 403 with `ACCESS_TOKEN_SCOPE_INSUFFICIENT`. Database OAuth scopes must not be reused for Auth administration. The audit now uses Admin Auth, without expanding the database credential scopes or changing credentials. This reproduces a concrete failure mode; the original external audit's request log was not available, so its historical cause is not asserted as proven.

This was not evidence of disabled students, a broken database connection, or an application-role denial. The current Admin credential has working Auth metadata read permission. **No IAM grant is needed for the successfully tested process.** If a different IIS worker still receives permission denial, the administrator should verify its actual existing ADC identity and project. Only for a confirmed missing IAM permission, approve `firebaseauth.users.get` through the existing principal's minimal approved policy; `roles/firebaseauth.viewer` includes that read-only permission. Do not grant Auth Admin, change database Rules, remove API restrictions, or rotate credentials to address a scope-only denial. See [lookup request/permission](https://docs.cloud.google.com/identity-platform/docs/reference/rest/v1/projects.accounts/lookup) and [Firebase Auth Viewer](https://docs.cloud.google.com/iam/docs/roles-permissions/firebaseauth). An API-disabled/restricted error is now classified separately; neither was observed in the successful Admin run.

No password hashes, passwords, tokens, service-account keys or provider error bodies were exported. The metadata reports contain UID/email and must remain private and outside source control. The read-only audit has no production repair mode.

## Actual counts, with definitions

- **27** known business students in current profiles/attendance/placements.
- **6** current profile UIDs verified present and enabled in Auth: 2023001, 2023002, 2023003, 2023105, 2023301, 2023306.
- **20** UIDs currently used as student-profile keys return Auth UserNotFound. This does **not** prove that a different genuine account for the student does not exist. The existing approved binding or official provisioning decision is required; none was guessed.
- **1** student, 2023101, has no unique matching users profile and therefore no proven canonical Auth mapping from that relationship.
- Thus **21 pending identity mappings** remain, consistent with the earlier decision list; earlier “403 unverified” counts could not distinguish nonexistent candidate UIDs from permission denial.
- **2 existing-setup candidates** pass structural checks with enabled matching Auth: 2023001 and 2023105. The audit does not equate this with approved calendar publication, live QR/phone success, or production write certification. The strict fully certified `ready` count is **0** for that reason. These are deliberately separate metrics, not inconsistent counts.
- **5 academic conflicts** remain: 2023203, 2023204, 2023205, 2023301, 2023305.
- **9 missing placements**: the eight previously unplaced students plus accepted-but-not-confirmed 2023002. The latter's accepted offer remains 2026-12-01, 16 weeks, 300 hours.
- 2023305's application profile remains inactive; it was not reactivated. No student was classified Auth-disabled merely because a request failed.
- 2023105 still has **2405 approved minutes = 40.08 hours**. No hours/history were changed. 2023301 remains frozen.

Detailed per-student blockers, record paths and proposed decisions: `Artifacts/Student-Integration-Reconciliation-2026-10-24.md`. Final metadata evidence: `Artifacts/Student-Integration-Verified-2026-10-24.json`; earlier read-only observations are retained separately.

## Code changes

1. `Shared/StudentFirebaseGateway.cs`: read-only Auth metadata projection using existing Admin credentials; separate UserNotFound/disabled/unverified outcomes; allowlisted error diagnostics; reject an already-initialized Admin instance targeting another project.
2. `Services/StudentAuthAuditFailure.cs`: distinct OAuth-scope, API service/restriction, credential and unconfirmed IAM-denial diagnostics; no provider body/token exposure.
3. `Services/ApplicationWorkflowService.cs`: reconcile active links from both existing academic assignment collections and verified UID/business-ID aliases; reject conflicts instead of picking the first row. Validate source offer/placement/calendar before reserving new claims. Confirmed-offer retries can complete missing attendance setup and missing durable claims. Complete no-op retries remain read-only even when attendance writes are off.
4. `Services/Attendance/AttendanceOnboarding.cs`: preflight existing approved-calendar prerequisites, actor UID, assignment/placement/company/supervisor/academic relations and dates/hours. Only after all checks and the existing write gate pass, create/reuse the student's actor, assignment and deterministic provisioning claim. No account, supervisor, company or calendar is created. New assignment end date may be calculated from an explicitly stored approved start and duration; existing dates/hours/calendars are never replaced. The existing attendance write setting remains unchanged.
5. Application `.csproj`: include the two new services.
6. `Tests/FinalEvaluation/StudentIntegrationAudit.cs`, `Program.cs`, and test `.csproj`: explicit read-only CLI audit plus offline permission/reconciliation/onboarding tests and failure injection.

Registration/account provisioning was inspected: the existing official service uses deterministic managed UIDs, lease/recovery operations, duplicate checks and staged profile activation. It was not run against production, and no account was adopted, deleted or re-created during this task. Academic assignment already refuses conflicting professors; the confirmation path now independently enforces consistency as well.

### Persistence boundary and recovery

The onboarding write uses one exact-ETag conditional PUT of the existing `uimsAttendance` subtree, preserving every unrelated field, schedule and history entry from that version. ETag conflicts reload and retry up to three times. Actor, assignment and provisioning claim are consistent within that write. The larger application/placement/attendance operation is **resumable, not one global transaction**: a lost acknowledgement can leave a saved placement/setup and an accepted application. Retry the same confirmation to finish; never delete records or claim success early. The offline tests reproduce this, including a stale ETag, lost acknowledgement, idempotent retry and missing-claim restoration. Large-subtree contention can require retry; no blind wildcard/root write is used.

`2023301` is explicitly refused by the new onboarding/confirmation path pending the coordinator decision. Existing disabled or conflicting actors/assignments are refused rather than repaired implicitly. These code changes do not apply a retrospective student-data migration.

## Proposed repairs — approval still required

- Approve the real existing UID/email/profile mapping for each of the 21 unresolved identities, or explicitly approve official provisioning where no real account exists. For 2023101 additionally supply the missing profile relationship. Do not infer the binding from names or copy another student's UID.
- Coordinator decides the authoritative academic assignments. Review only the affected profile, assignment, placement and attendance-assignment academic fields. No decision for frozen 2023301 was supplied, so no repair for it is authorized.
- Decide whether to reactivate 2023305; no automatic enablement.
- Supply/approve the exact persisted calendar for 2023002, preserving its December offer and hours. Complete approved offer/placement/academic setup for the remaining unplaced students before provisioning. Existing calendars are retained, not cloned.
- Any future approved change requires a fresh authoritative backup, reviewed exact paths, expected old values/ETags and post-write verification. **No production repair has been applied by this work.** Do not import the earlier cleaned full-export artifacts as an integration fix.

## Validation and limitations

- Complete solution build: passed.
- Web Forms precompilation: passed; five existing CS0108 Profile warnings on unrelated opportunity/application pages.
- Full current C# suite: **249 passed, 0 failed**, including Auth mapping/failure classification, authorization/cross-student denial, placement confirmation and attendance onboarding/recovery.
- AuthBackoff isolated browser suite: all four scenarios passed. No real credentials/logins were used by that browser test.
- Live evidence was obtained only by the explicit administrative read-only CLI mode. Normal regressions use offline mocks and report no network requests or writes.
- No live attendance scan/check-in/check-out, approved production write test, phone HTTPS, new calendar approval, genuine DOCX cycle or all-student physical end-to-end run was performed. The earlier external 58-test/206-test/39-JS claims were not substituted for this workspace's actual results. Separate attendance deployment/onboarding files from that older checklist were absent here.
- Shared Sidebars/navigation, Tasks/Exams/UI, Firebase/Supabase connections, flags, Rules, IAM, credentials and historical records were not modified in this task. No commit, push, deployment or paid service was performed.

Repeat an authorized read-only audit (output must be a new private local file):

```powershell
Tests/FinalEvaluation/bin/FinalEvaluation.Tests.exe --student-integration-readonly University-Internship-Management-System/Web.config C:/secure/new-student-audit.json
```

The process must inherit the same approved server ADC as the application. Do not paste credentials into the command or repository. The CLI reads this config's explicit appSettings; it is not proof of a differently configured/deployed worker's effective environment.
