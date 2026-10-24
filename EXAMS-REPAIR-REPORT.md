# Exams repair dry run — 23 October 2026

## Diagnosis

The two schema errors can be caused by an inspector defect rather than lost answers. Firebase REST can represent maps with numeric revision names as arrays. This service writes published key revisions at `/taskExamAnswerKeys/{examId}/{version}` and draft keys at `/draftVersions/{revision}`. The inspector previously required both containers to be JSON objects. An array could therefore produce both “private answer-key record must be an object” and “missing private grading key revision” even when the original answers existed.

The read adapter now accepts those two known numeric containers, keeps every original numeric index, skips null placeholders, and preserves every answer value. It does not rename paths, guess answers, write migrations, or alter history. This behavior is documented by [Firebase](https://firebase.googleblog.com/2014/04/best-practices-arrays-in-firebase.html). The cause is reproduced offline; it remains a hypothesis for the nine live records until their observed types are checked.

A genuinely missing revision or scalar root still blocks compatibility. Historical creator identity and creation cause cannot be established from the screenshot. Trusted audit logs/exports are required; no attribution is invented.

## Actual local dry run

See `EXAMS-REPAIR-DRY-RUN.json`, generated read-only from the existing local `firebase-final.json` export:

- Exam `EXAM-DEMO-001`: grading keys compatible, zero repair patches.
- Legacy unreleased raw grade at `/taskExamAttempts/EXAM-DEMO-001/2023001/ATT-DEMO-001`: direct-client Rules protection is required. No grade value is included in this report or changed.
- The local export contains one exam, whereas the user's authenticated live verifier inspected nine. It cannot identify the remaining live record IDs or their creation history.

## Get exact live IDs without changing production

Restart the locally built application, sign in as the real company supervisor, and run this from Company/ManageTasks. This invokes the existing authenticated, read-only verifier; it does not create test data or reveal keys/grades:

```javascript
fetch('../TaskExamsApi.ashx?action=verifyConfiguration', {
  method: 'POST', credentials: 'same-origin',
  headers: { 'Content-Type': 'application/json' },
  body: JSON.stringify({ images: true })
}).then(r => r.json()).then(r => console.log(JSON.stringify({
  compatible: r.compatible,
  recordReports: r.recordReports,
  securityFindings: r.securityFindings,
  storage: r.storage
}, null, 2)));
```

`recordReports` now identifies the exam ID, exact private-key root/revision paths, observed root type, numeric-array compatibility handling, per-exam issues, and attempt paths requiring Rules review. It reports zero planned writes. This output is needed to distinguish scalar/missing data from Firebase's numeric-map encoding. No live authenticated verifier was run by this agent during this pass.

## Safe offline repair tooling

`Tools/ExamRepair.py` has no networking, credentials, or live-apply mode. Default behavior is dry-run only:

```powershell
python Tools/ExamRepair.py "C:\secure\existing-export.json"
```

The planner can normalize numeric maps in an offline copy. Such normalization is unnecessary on production now that reads support Firebase's representation; Firebase may return arrays again regardless of how equivalent numeric keys were written.

Genuinely absent key revisions require administrator-supplied **original verified keys** in a separate secure file. The shape is exam ID -> revision path (`1` or `draftVersions/2`) -> the original keyed answers. No answer values are supplied by this tool. Supplied keys must exactly match question IDs, valid answer types/choices, and points. Existing invalid keys and scalar roots are never automatically overwritten. An exam with unresolved issues gets no partial repair patch.

After reviewing a dry run, an administrator may explicitly request a **local private package**, which contains originals, conditional patches, and a repaired export copy:

```powershell
python Tools/ExamRepair.py "C:\secure\existing-export.json" --trusted-keys "C:\secure\verified-original-keys.json" --approve-offline-copy --private-package "C:\secure\exam-repair-package.json"
```

Keep exports, verified answer files and the private package outside the repository and do not share them in chat. The original input is not modified; existing output files are refused. Patches include original private records and SHA-256 source preconditions. Applying the same patches twice to an offline copy is idempotent; a changed source is rejected. Tasks, definitions, historical attempts, grades, and submissions are untouched. The planner's compatibility check is limited to key restoration, not complete exam activation certification.

**No production repair has been applied.** Any future live repair needs explicit approval of the exact reviewed paths and conditional patch, a fresh authoritative backup, and server-side authorization/concurrency checks. Do not import the entire repaired export over production. No automatic production-apply endpoint was added.

## Security and HTTP 503

`action=list` is blocked by the three false exam flags and returns the existing structured configuration error (`EXAMS_DISABLED`, `configurationBlocked: true`). This does not establish that Firebase is disconnected. The authenticated inspection endpoint intentionally works before activation.

The repository Rules deny client access at the root. Deployed Rules remain unverified. An administrator must inspect the active Rules for the actual database and ensure ordinary Firebase clients cannot read `/taskExamAnswerKeys`, legacy unreleased grades in `/taskExamAttempts` or task records, or write grades/workflow state. Check for permissive root/ancestor grants and test relevant concrete descendant paths with student/unauthenticated identities. If permissive Rules were previously deployed, restoring denial does not undo prior exposure. No Rules were changed or deployed here.

The supplied live screenshot confirms Supabase's read-only private-bucket readiness. `writesVerified: false` is accurate: no object upload/deletion was tested. Authorized isolated persistence/security testing and live-write approval are still required. All three exam flags remain unchanged until actual security and compatibility checks pass.

## Files and verification

Changed: `Services/TaskExamService.cs` in the application, `Tests/FinalEvaluation/TaskExamTests.cs`, new `Tools/ExamRepair.py`, `Tools/test_exam_repair.py`, `EXAMS-REPAIR-DRY-RUN.json`, and this report. Existing browser suites regenerate their screenshot outputs. No configuration, Sidebar, navigation, Tasks UI, or unrelated business module was changed.

- Solution build and regression-project build: passed.
- Full C# regressions: **246 passed, 0 failed**. New coverage reproduces numeric array roots, preserves original answers/indexes, verifies idempotent normalization, and checks exact per-exam diagnostic paths without writes.
- Offline repair tests: **4 passed**, covering preserved originals/history, idempotency, stale-source rejection, verified-source restoration, missing/invalid keys, and scalar refusal.
- ASP.NET precompilation: passed; five pre-existing CS0108 generated Profile warnings on unrelated opportunity/application pages.
- ExamPopup and TaskWorkspace browser suites: passed against mocked APIs with production traffic blocked.
- No production service requests, records, migrations, policy deployment, commits, or pushes were performed.
