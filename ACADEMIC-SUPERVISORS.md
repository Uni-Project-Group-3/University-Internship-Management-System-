# Academic supervisor management

All work is on `yasmeen` in the active UIMS-Repo checkout.

## Official record contract

The existing live database schema was inspected through a temporary, coordinator-authorized read-only page, which was removed immediately afterward:

```
universityRecords/academicSupervisors/{staffId}
  staffId: "A1001"
  fullName: "Dr. Sara Hassan"
  email: "sara.academic@example.com"
  universityId: "just"
  collegeId: "cit"
  departmentId: "softwareEngineering"
```

The staff ID field must exactly match its key. Missing or invalid name/email/university/college/department is rejected. The current records do not contain eligibility flags; if `isEligible` or `isActive` is supplied, it must be boolean true. No official records are manufactured, edited or deleted by this page.

## Account workflow

The coordinator looks up a staff ID. The official fields are read-only and a temporary password is generated on the server. Confirm re-reads the official source and rejects stale or altered lookup data. It creates a disabled Firebase Authentication account, saves `users/{authenticationUid}` with `role: academicSupervisor`, staff/university/college/department IDs, activates the account, and requires a first-login password change. `majorId` mirrors `departmentId` for compatibility with existing student supervisor matching.

Staff IDs and Authentication UIDs are separate. Authentication UIDs are deterministic hashes of university and staff ID. Existing unrelated logins and duplicate official emails cannot be adopted. Pending setup can be retried after a lookup using the same staff ID; no extra account is created.

Email delivery and password-reset controls are intentionally absent. The current temporary password remains only in the short-lived server candidate session and current UI; it is never written to Realtime Database or ViewState/browser storage. Copy it before closing the creation form.

## Directory and actions

The page has live totals, active counts, departments, text search, department filtering, refresh, details, official-record sync, activate/deactivate and deletion. Sync accepts official changes rather than manually replacing staff details. Active assignments prevent moving a supervisor's college/department or deleting the account until reassignment is done. Deactivation leaves assignments and history unchanged.

Assignment counts read `users/{studentUid}.academicSupervisorId` and active `groups/{id}` linked through `supervisorId`, `academicSupervisorId` or `supervisorUid`. `isActive: false` marks historical/inactive assignments for these checks. No counts or groups are fabricated. Account deletion preserves official records and academic history and can retry when Authentication was already deleted.

Every page/API request validates the real Forms Authentication identity and the current enabled coordinator account/database role. API POSTs require a per-session CSRF header. Server checks never trust client roles, staff details or arbitrary target UIDs. ETag-protected account operations and leases prevent concurrent changes. Safe errors do not include credentials or raw Firebase responses.

The separate assignment and academic-supervisor portal pages retain their pre-existing workflows; this implementation covers the coordinator's Manage Academic Supervisors page and its compatible account records.

## Validation

Project build, live ASPX compilation and JavaScript syntax checks passed. Thirteen new offline academic checks cover official lookup/eligibility/schema, stale/tampered records, Auth/profile mapping, duplicate protection, lost-response recovery, student matching, status changes, official sync, assignment restrictions, deletion retry/history preservation, real assignment counts and concurrency/role checks. Together with the existing 32 student/company checks, 45 checks passed.

With explicit approval, A1001 was created from the existing official record and retained for use. Its live directory entry, deactivate/reactivate flow, official-record sync, details, staff-ID search and department filtering were checked. An anonymous API POST returned HTTP 403. The original creation tab was kept open so the user can copy its temporary password. Live deletion of this retained account was not performed; deletion/recovery/history checks passed offline.
