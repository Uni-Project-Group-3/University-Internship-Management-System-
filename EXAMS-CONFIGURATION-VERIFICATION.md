# Exams configuration verification

The Create Task failure is a readiness gate, not evidence that Realtime Database is disconnected. Module enablement, schema/security approval, database writes, and Storage availability are separate checks. Exam flags remain unchanged; local tests cannot certify deployed Rules or live permissions.

## Implemented changes

- `Services/TaskExamsApiHandler.cs`: structured configuration errors, authenticated company-only configuration inspection and read-only environment verification, and write-readiness preflight.
- `Services/TaskExamService.cs`: configuration diagnostics, existing-schema/private-key compatibility inspection, legacy-record findings, and reuse of the existing shared server credential provider. No automatic migration or flag changes.
- `Services/FirebaseFileStorageService.cs`: authenticated, read-only bucket permission inspection. A 404 now identifies the configured bucket and Console verification steps without claiming that Firebase is disconnected.
- `Services/TaskService.cs`: replace the unconditional deferred-upload error with the existing Storage service's availability check.
- `Js_project/Company/TaskComposer.js`: check exam write readiness before creating assignments, including image Storage requirements.
- `Company/ManageTasks.aspx`: update the composer script cache version only.
- `Tests/FinalEvaluation/TaskExamTests.cs`, `FirebaseFileStorageTests.cs`, and `TaskWorkspace.browser.cjs`: configuration, compatibility, privacy, permission, and pre-creation failure coverage.

Application paths above are relative to `University-Internship-Management-System/`; test paths are relative to the repository root.

## Actual local results

- Complete solution build and regression-project build: passed. Latest regression run: **243 passed, 0 failed**, no network requests or Firebase writes.
- Web Forms precompilation: passed during this pass. Five existing CS0108 generated Profile warnings on unrelated opportunity/application pages.
- TaskWorkspace, ExamPopup, WeeklyPopup, and TaskBoardLayout browser suites: passed with mocked responses. Board tests cover both roles and 0/5/20/50 cards per column.
- JavaScript syntax checks: passed. These browser/syntax checks preceded the final change to the Storage 404 message; the solution and all regressions were rerun after that change.
- Local exported schema inspection: compatible supported legacy exam structure; legacy raw grade fields require deployed Rules review. This is not live database verification.

## Current screenshot: Storage 404

The configured bucket is `just-project-4af15.firebasestorage.app`. The screenshot reports a 404 during read-only permission verification. Its live provisioning/access has not been independently verified here. Do not guess an alternative bucket name or bypass this check.

1. Open the existing project in Firebase Console > Storage. Confirm that the exact configured bucket exists and Storage setup has completed. If provisioning or a billing change is required, the project administrator must approve it.
2. In Google Cloud Console > Cloud Storage, inspect that same bucket and the identity used by the existing server credentials. Verify the required object create, get, and delete permissions. Check credentials locally without sharing private key files.
3. If the Console shows a different bucket, resolve the discrepancy with the administrator; this pass does not change the configured bucket.

The permission check uses Google's [read-only testIamPermissions API](https://docs.cloud.google.com/storage/docs/json_api/v1/buckets/testIamPermissions). It does not upload or delete an object and does not certify Firebase client Rules.

## Safe live verification still required

Run the existing application in Visual Studio with IIS Express and sign in as a genuine company supervisor. From Company/ManageTasks, the authenticated inspection endpoints are:

- GET `../TaskExamsApi.ashx?action=configuration&images=true`
- POST `../TaskExamsApi.ashx?action=verifyConfiguration`, same-origin JSON body `{"images":true}` and `Content-Type: application/json`.

The second endpoint reads existing authorized exam records and checks Storage permissions; it changes no records or flags. Inspect compatibility issues and security findings. An empty collection is not proof that historical schemas or live writes work.

Manually verify deployed Realtime Database and Storage Rules: students must not directly read private answer keys, calculated results, private creation operations, or unreleased legacy task/attempt grades, nor write workflow/grade state. Successful server reads do not prove client Rules are secure or that database writes are permitted. No Rules were deployed here.

Only after these checks pass should an administrator approve the schema flag and enable the exam module/live-write flags, then restart IIS Express. Certify image upload, publication, submission, timing, and supervisor release in an approved isolated Firebase environment. No production test records, deployment, commits, or pushes were performed. Shared sidebars, navigation, Firebase connection settings, and unrelated pages were not edited in this pass.
