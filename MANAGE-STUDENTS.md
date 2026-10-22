# Manage Students implementation and handoff

Work in `C:\Projects\UIMS-Repo`, branch `yasmeen`. The solution is at
`University-Internship-Management-System\University-Internship-Management-System.slnx`.
Reload externally changed files in Visual Studio after preserving any unsaved edits.

## Implemented

- Coordinator-only university lookup and revalidation on Confirm, including eligibility and required academic information.
- A single cryptographically generated temporary password, held in server session for a 15-minute lookup; never stored in Realtime Database, client storage, or ViewState.
- Active academic-supervisor selection at setup, matching university, college and department.
- Authentication provisioning, matching `users/{uid}` profile, temporary credential email, activation and `mustChangePassword` flag.
- Durable setup status and conditional operation leases to recover common partial failures after retry or session loss. Existing unrelated Authentication accounts are never adopted.
- Real account list; department, supervisor, name/student-ID/email search, and completion filters; result count, clear and refresh.
- View details; admin-requested reset-link emails; confirmed login/profile deletion while retaining university records, placements and evaluations. No Edit action.
- Current database role/activity checks, ViewState user binding, explicit CSRF token checks, output encoding and request-safe errors.

## Required external configuration

The implementation fails closed when login or mail configuration is missing. It does not create a demo authentication bypass.

### Login-owner integration

The current `Auth/Login.aspx.cs` still contains a sign-in placeholder. That is separate from Manage Students and was not replaced.
The login owner must authenticate against Firebase and establish an ASP.NET authenticated principal/session securely.
`User.Identity.Name` must be the verified Firebase UID or verified account email. This page resolves the Authentication account,
checks it is not disabled, and checks fresh `users/{uid}` fields: `role: "coordinator"`, `isActive: true`.
An arbitrary UID or role supplied by the browser is insufficient.

The login owner must enforce `mustChangePassword` before student dashboard access and check `isActive`/account setup state.
This page creates the flag; it does not replace the first-login/change-password flow owned by the login developer.

### Firebase

- Existing `FirebaseDatabaseUrl` in Web.config must be the HTTPS Realtime Database URL.
- Existing `GOOGLE_APPLICATION_CREDENTIALS` / Application Default Credentials must grant appropriate Admin Authentication and database access.
- Enable Email/Password authentication in Firebase.
- Protect all server-only operation records and user profiles using database rules. Admin service-account calls bypass client rules;
  the server performs coordinator authorization independently. Never grant public access to make a test pass.

### Welcome and reset email

Configure these environment variables on the IIS Express / IIS process, then restart the host:

| Variable | Meaning |
| --- | --- |
| `UIMS_SMTP_HOST` | Provider's SMTP hostname supporting authenticated STARTTLS |
| `UIMS_SMTP_PORT` | STARTTLS submission port, defaults to `587` |
| `UIMS_SMTP_FROM` | Provider-approved sender address |
| `UIMS_SMTP_USERNAME` | SMTP username |
| `UIMS_SMTP_PASSWORD` | SMTP/app-specific password; never commit it |
| `UIMS_LOGIN_URL` | Actual HTTPS login-page URL; HTTP localhost is permitted for development |

Use a mail provider that supports this SMTP flow. The sender is not invented or automatically configured.
Welcome mail contains the login email, student number and temporary password, with first-login change instructions.
Reset mail contains a Firebase-generated reset link, not a new password. SMTP acceptance does not guarantee inbox delivery.
When email is unconfigured, Confirm creates the Firebase login and student profile without sending welcome email. The coordinator can copy the temporary password from the success dialog; password change remains mandatory. Reset email controls stay disabled.

## Data contract

Existing university records are read, never modified:

```
universityRecords/students/{studentNumber}
  studentId: "2023001"
  universityId: "just"
  collegeId: "cit"
  majorId: "softwareEngineering"
  fullName, email, requiredTrainingHours, isEligible
```

New student accounts keep those IDs separate and use a stable Authentication UID derived from university + student number.
`users/{uid}` contains `role: "student"`, `academicSupervisorId`, `isActive`, `mustChangePassword`,
`accountSetupStatus`, name, email and academic fields. No password is stored there.

Supervisor accounts must already exist under `users/{supervisorUid}`, with `isActive: true`, `fullName`, `universityId`,
`collegeId` and `majorId` (or `departmentId`). Canonical role is `academicSupervisor`; existing `academic` and
`academic_supervisor` values are recognized. A university supervisor record alone is not an enabled login account.
New students can be created without an initial supervisor and display Unassigned. Supervisor assignment is handled separately.

The completion/placement integration reads this documented contract; it does not invent or write completion results:

```
placements/{placementId}
  studentUid: "<Authentication UID>"  # legacy studentId is accepted only if its value is that UID
  completionStatus: "InTraining" | "PendingStudentConfirmation" | "Completed"
  studentConfirmedAt: "<ISO UTC timestamp>"
  accountDisableAt: "<ISO UTC timestamp>"
  certificateId: "<report/certificate reference>"
```

No placement displays `Not placed`. Multiple placements for the same user display `Status unavailable` rather than choosing arbitrarily.
Unknown completion values are not mislabeled as In Training. The completion owner must provide these records, generate reports and
implement any scheduled deactivation policy; a filter does not certify that those external workflows are implemented.

`studentAccountOperations/{uid}` is a server-only recovery marker (no password), including provisioning/deletion state,
email acceptance, creator, timestamp and a 10-minute operation lease. Firebase ETag conditional writes prevent simultaneous
setup requests from both acquiring it. A crash can require waiting for lease expiry. Recovery is not a distributed atomic transaction;
SMTP may accept a message before a timeout is reported, so a retry can send a newer welcome message. Use the latest message.
Deletion does not delete university records, placements or evaluations. Consumers of old academic references must handle a missing login/profile.

## Validation performed

- C# project build.
- ASP.NET markup precompilation, including server event/control bindings.
- JavaScript syntax validation.
- Offline workflow tests using fake Authentication, database and mail adapters: coordinator access, invalid/ineligible records,
  supervisor matching, provisioning, separate IDs, password consistency/no database password, duplicate prevention,
  failed mail recovery, lost Authentication response, activation retry, unrelated-account protection, leases,
  reset email target, deletion retry/history preservation, completion mapping, and random password generation.

No real users were created/deleted, no live emails were sent, and no live database mutations were performed during validation.
Before calling the page production-ready, complete a controlled integration test with an explicitly disposable student account
after the login principal and sender configuration are in place. Confirm the emitted welcome/reset messages and all four filters.

No-email creation is explicitly selected by the page only when email configuration is absent. The service defaults to requiring email. Recovery markers record credentialDelivery=coordinator without claiming mailAccepted. Passwords are not stored in the database.
