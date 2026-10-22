# Firebase Storage security proposal

The application uploads and downloads through `FirebaseFileStorageService` and the authenticated ASP.NET handlers. The bucket remains unset and Storage is disabled in `Web.config`; no Storage request was made.

`storage.rules.proposed` denies direct client access. The current server transport obtains Google credentials through Application Default Credentials. A trusted service identity with Storage IAM permissions can access objects independently of Firebase Storage Rules, so the application handler's verified UID, role, placement, file purpose, and ownership checks remain mandatory. Keep the service identity private and grant only the bucket-level permissions the deployment requires. Do not publish tokenized public download URLs.

Realtime Database stores metadata at `/fileAttachments/{fileId}` only. File bytes stay in Storage. The task, exam question image, weekly report, and completion-document flows each need an object path and matching purpose-specific authorization. Upload metadata may be marked `ready` only after Storage confirms the object; DOCX report revisions and completion-document issuance must not reference missing objects. The development seed therefore leaves `/fileAttachments` and `/completionDocuments` empty and `/weeklyReports` empty.

Before enabling Storage, configure the exact bucket belonging to the intended Firebase project, validate service-account/ADC project binding and IAM, deploy the deny-by-default Rules only after review, and run upload, interrupted-upload cleanup, private download, cross-student, cross-company, and unauthorized-role tests in an isolated project or Emulator. No Rules were deployed by this work.
