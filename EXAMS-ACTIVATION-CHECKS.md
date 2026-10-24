# Exams activation checks — 23 October 2026

## Completed locally

The three exam flags remain false. These are deliberate activation gates, not a Firebase connectivity failure. No live-write authorization or deployed security verification was established during this pass, so changing them would falsely approve unverified requirements.

Modified files:

- `University-Internship-Management-System/Services/TaskExamService.cs`: validate Supabase URL, bucket, provider, and image-write configuration instead of silently skipping provider configuration checks; normalize provider selection consistently with the storage service; inspect private answer keys per authorized exam instead of reading the entire collection; report malformed records/results without blindly approving them.
- `Tests/FinalEvaluation/TaskExamTests.cs`: extend readiness validation and add a complete mocked Supabase-image exam workflow, including publication, active-attempt image authorization, private automatic grading, and explicit supervisor release.
- This report. Existing browser suites also regenerate their screenshot outputs.

No Firebase/Supabase setting, shared sidebar, navigation, Tasks UI, Rules file, historical record, or authentication/connection service was edited in this pass.

## Actual verification

- Complete solution build and regression-project build: passed.
- ASP.NET Web Forms precompilation: passed. Five existing CS0108 generated `Profile` warnings on Company ApplicationsReview/ManageOpportunities and Student ApplicationForm/ExploreOpportunities/MyApplications.
- **245 regression tests passed; 0 failed.** All HTTP/storage persistence tests use offline doubles and make no live service requests.
- TaskWorkspace and ExamPopup browser suites: passed with mocked APIs and production traffic blocked. Includes question creation, images, publication, timing/attempt restrictions, answering/submission, hidden grades, supervisor release, and creation recovery.
- TaskComposer/TaskExams JavaScript syntax and changed-source whitespace checks: passed.
- Existing exported Firebase snapshot remains compatible with supported legacy published exams. It contains legacy unreleased raw grades requiring direct-client Rules protection. No snapshot or live record was migrated.
- Repository `firebase-final-rules.json` denies client reads/writes at the root and adds query indexes. This is repository evidence only; it does not certify deployed Rules.

## Administrator verification before activation

1. Start the existing application in Visual Studio/IIS Express and sign in with a genuine company-supervisor account. Supply the server-only `UIMS_SUPABASE_SECRET_KEY` to that worker process as described in `SUPABASE-STORAGE-SETUP.md`; restart Visual Studio/IIS Express after supplying it. Do not share or commit the secret.
2. Inspect effective settings with authenticated GET `../TaskExamsApi.ashx?action=configuration&images=true` from the Company page. Check effective values, including any overrides in the existing local appSettings file; do not change Firebase connections or Storage provider settings to bypass failures.
3. Invoke authenticated same-origin POST `../TaskExamsApi.ashx?action=verifyConfiguration`, with `Content-Type: application/json` and body `{"images":true}`. This endpoint is available while the exam flags are false. It reads existing records and private-bucket metadata without creating test records, changing flags, or uploading files. Resolve every compatibility issue and review security findings. Repeat for the relevant supervisors; one supervisor's records do not certify the whole database. Empty collections do not prove historical compatibility.
4. In Firebase Console, inspect deployed Realtime Database Rules (or use Rules Playground without deploying). Ordinary student Firebase tokens must be denied direct access to `taskExamAnswerKeys` and its draft/version keys, immutable calculated results, and private creation operations. They must not directly read raw unreleased grades in legacy `taskExamAttempts`/`tasks`, nor write exam definitions, attempts belonging to others, grades, release status, or workflow state. Root/ancestor allows override child denials. Repository Rules and successful administrator reads do not prove this protection.
5. Confirm the existing server identity has the required database read/write authority and indexes for the current exam/task paths, without granting client access. Review that the existing C# endpoints retain company/placement authorization, student assignment/attempt ownership, same-origin updates, server deadlines, and release-gated response projections. Do not test production with fabricated records.
6. Verify `internship-files` is private in the existing Supabase project and that anonymous/public clients cannot access its objects. The read-only bucket check confirms bucket metadata, not successful uploads/deletions or deployed policy security. Verify image uploads/downloads/cleanup and Firebase metadata persistence in an explicitly authorized isolated environment. The server secret is privileged, so backend authorization must remain mandatory.
7. Record administrative approval for enabling **live exam writes** on the target environment. Once schema compatibility, deployed security, server permissions, and isolated workflow checks have passed, set `TaskExamsSchemaValidated=true`, `TaskExamsEnabled=true`, and `TaskExamsLiveWritesEnabled=true` in the effective server configuration. Restart the application and recheck authenticated readiness. Do not mark schema/security validated merely because local tests pass.

Live activation remains externally blocked until those checks and approval are completed. No production Firebase/Supabase data was accessed or written, no Rules/policies were deployed, and no commit or push was performed.
