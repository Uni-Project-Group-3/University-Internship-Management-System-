# Supabase file storage integration

New task resources, student submissions, exam question images, and company weekly-report Word files use Supabase Storage. Firebase Authentication and Realtime Database remain unchanged. Ownership, task/exam authorization, file metadata, report revisions, and grades remain in Firebase. No Supabase database or Supabase Authentication integration was added.

## Local setup in Visual Studio

1. In the existing Supabase project, verify the `internship-files` bucket is **private**. No bucket or policy is automatically created or modified by this integration.
2. Obtain a server secret from the Supabase Dashboard API Keys settings. Prefer a `sb_secret_...` key; a legacy `service_role` JWT is also supported. Public/publishable/anon keys are not accepted.
3. Open Windows **Edit environment variables for your account**. Add `UIMS_SUPABASE_SECRET_KEY` with that secret as its value. Do not paste it into source files, JavaScript, a terminal command/history, chat, or logs. This local-development environment is for your trusted Windows account; production hosting should supply it through its protected secret configuration to the application worker identity.
4. Completely close Visual Studio and IIS Express, then reopen the existing solution and run it. For full IIS, supply the variable to its worker process and recycle the application pool.
5. Sign in using the existing Firebase-backed company account. The task wizard uses the existing Storage readiness endpoint; exams additionally retain their existing schema, authorization, and enablement gates. Missing secret, unavailable bucket, public bucket, and permission errors block the operation with an actionable message.

The nonsecret settings added to Web.config are:

| Setting | Value |
| --- | --- |
| FileStorageProvider | supabase |
| SupabaseStorageUrl | https://qtawrtwwhkvgcmcpwfeu.supabase.co |
| SupabaseStorageBucket | internship-files |
| SupabaseStorageLiveWritesEnabled | true |

No existing Firebase setting was changed. Supabase configuration values except the provider can also be supplied by `UIMS_` plus the exact setting name, for example `UIMS_SupabaseStorageUrl`. Provider selection uses the application setting first. The secret is accepted only through `UIMS_SUPABASE_SECRET_KEY`, never appSettings.

## Behavior and safety

- The existing C# file-storage facade selects the upload provider. Domain services continue to verify the authenticated actor, placement, task ownership, report assignment, and exam attempt/image eligibility before accessing bytes.
- New Firebase `fileAttachments` records include `storageProvider: supabase`; weekly-report immutable revision metadata also includes the provider. Existing `storagePath`, hashes, ownership, sizes, and history are retained. Missing provider means Firebase for historical downloads; unknown providers fail closed.
- Downloads are streamed through the existing authenticated backend endpoints, without public URLs or signed links. Exam images still require the appropriate company authority or an authorized active student attempt.
- File names, signatures/MIME, extension, and the existing 10 MiB size limit are validated. Object paths reject traversal, backslashes, encoded traversal, and empty segments. Downloads enforce the same size cap.
- Uploads are create-only (`x-upsert: false`). A failed or ambiguous upload does not trigger speculative deletion of a possibly existing object. Existing backend cleanup invokes the selected provider after its domain authorization; confirmed unlinked objects may be removed when metadata persistence fails. Historical or linked files are not migrated or bulk-deleted. No general client-controlled raw-path deletion endpoint is exposed.
- Readiness reads bucket metadata and requires `public: false`. It does not upload a probe and cannot certify write permissions without an authorized write test. Administrative changes to bucket privacy/policies must be controlled separately. Backend secrets bypass user RLS, so existing C# authorization is essential; do not expose the secret or permit anonymous bucket access.
- Completion-report storage has a one-line explicit Firebase provider selection so this unrelated historical consumer retains its existing transport and records.
- Exam flags and schema/security validation were not bypassed. Existing deployed Firebase Rules still require verification for private keys and unreleased grades. No live data, policies, or Rules were changed.

Supabase documents [server-only secret keys and headers](https://supabase.com/docs/guides/getting-started/api-keys) and [private downloads](https://supabase.com/docs/guides/storage/serving/downloads). The transport uses the [Storage REST API](https://supabase.github.io/storage/).

## Modified files

Under `University-Internship-Management-System/`:

- `Services/SupabaseFileStorage.cs` (new provider implementation)
- `Services/FirebaseFileStorageService.cs` (existing facade, provider overloads, StoredFile provider)
- `Services/TaskService.cs` (provider metadata and downloads)
- `Services/TaskExamService.cs` (provider metadata, downloads, provider-aware image readiness)
- `Services/WeeklyReportService.cs` (provider metadata and revision downloads)
- `Services/FinalEvaluation/LiveFirebaseIntegration.cs` (preserve Firebase routing for completion reports)
- `University-Internship-Management-System.csproj` (include provider source)
- `Web.config` (four additive nonsecret Storage settings only)

Also: `Tests/FinalEvaluation/FirebaseFileStorageTests.cs` and this document. Browser suites regenerate their existing screenshot outputs. No UI, shared sidebar, navigation, Firebase authentication, or database connection source was edited for this integration.

## Actual verification

- Solution build: passed.
- Regression project build: passed; **244 tests passed, 0 failed**, using offline HTTP/storage doubles. Added coverage for Supabase private-bucket verification, secret header handling, create-only uploads/conflicts, downloads/deletion, legacy Firebase routing, path rejection, missing secret, Firebase provider metadata, and cross-student denial.
- ASP.NET precompilation: passed. Five pre-existing CS0108 generated `Profile` warnings on Company ApplicationsReview/ManageOpportunities and Student ApplicationForm/ExploreOpportunities/MyApplications.
- TaskWorkspace, ExamPopup, and WeeklyPopup browser suites: passed, isolated API responses with production traffic blocked.
- TaskComposer/MyTasks JavaScript syntax checks and changed-source whitespace checks: passed.

No live Supabase or Firebase test requests were made. A real secret, private-bucket provisioning/access check, and approved isolated upload/download/deletion tests are still required to certify live integration. No secret was supplied in this session, so live persistence is not claimed. Do not use production test records without authorization.
