# Company weekly reports — Firebase contract

This feature uses the configured Realtime Database root and the existing private `FirebaseFileStorageService`. It does not create another database and does not change existing `/tasks`, `/taskBoards`, `/taskExams`, `/placements`, `/assignments`, `/users`, `/fileAttachments`, or `/uimsAttendance` records. All live flags remain disabled in `Web.config`.

## Realtime Database paths

### `/weeklyReports/{reportId}`

`reportId` is a deterministic `WR-` plus lowercase SHA-256 of `placementId + LF + weekNumber`. This supplies a create-only placement/week uniqueness key without introducing an index collection. A retry or concurrent submit uses RTDB `If-Match: null_etag`, so only one create succeeds. Existing IDs are never replaced.

| Field | Type | Required | Meaning |
|---|---|---:|---|
| `reportId` | string | yes | Same as the RTDB child key. |
| `placementId` | string | yes | Existing active placement ID. |
| `studentId` | string | yes | Existing student business ID from that placement. |
| `companyId` | string | conditional | Existing company ID carried by the placement when present. |
| `companySupervisorId` | string | yes | Verified company supervisor business ID from `/users/{uid}`. |
| `academicSupervisorId` | string | yes | Current assignment ID resolved from `/assignments` or the student `/users` profile; updated when a later revision is routed after reassignment. |
| `weekNumber` | integer | yes | Training week, 1–60. |
| `weekStart` | string (`yyyy-MM-dd`) | yes | Training week start date. |
| `weekEnd` | string (`yyyy-MM-dd`) | yes | Inclusive end date, no more than six days after start. |
| `status` | string | yes | `submitted`, `revisionRequested`, `resubmitted`, or `approved`. |
| `uploadedByUid` | string | yes | Verified Firebase Authentication UID of the company uploader. |
| `currentRevisionId` | string | yes | Key of the latest immutable child under `revisions`. |
| `revisionNumber` | integer | yes | Current revision sequence, starting at 1. |
| `createdAt` | string (UTC ISO 8601) | yes | First successful report creation. |
| `updatedAt` | string (UTC ISO 8601) | yes | Latest state update. |
| `submittedAt` | string (UTC ISO 8601) | yes | Latest company submission time. |
| `companyComments` | string or null | yes | Current company performance comments, max 3,000 characters. Revision-specific copies remain in the revision records. |
| `lastReviewId` | string | after review | Idempotency key of the review applied to the current report state. |
| `lastReviewedAt` | string (UTC ISO 8601) | after review | Latest academic review time. |
| `revisions` | object keyed by revision ID | yes | Append-only revision history. |

Each `/weeklyReports/{reportId}/revisions/{revisionId}` object contains `revisionId` (string), `revisionNumber` (integer), `fileId` (string), `fileName` (string), `contentType` (the DOCX MIME string), `sizeBytes` (integer, at most 10,485,760), `sha256` (lowercase hex string), `storagePath` (private object path), `storageGeneration` (string), `companyComments` (string or null), `submittedAt` (UTC ISO string), and `uploadedByUid` (verified Auth UID). Revision nodes are created with `If-Match: null_etag`; later resubmission reads the whole report, adds a new revision, and conditionally updates that one report node with its ETag. It never replaces or deletes old revisions.

### `/weeklyReportReviews/{reportId}/{reviewId}`

`reviewId` is a client-generated UUID used only as an idempotency key. It is not an identity or permission claim. Each review contains `reviewId`, `reportId`, `revisionId`, `academicSupervisorId`, `reviewerUid`, `decision` (`approved` or `revisionRequested`), `feedback` (required, max 3,000 characters), `reviewedAt` (UTC ISO string), and `status` (`pendingCommit`, `committed`, or `stale`). The decision, feedback, reviewer, and revision are retained. The status marker distinguishes a review whose report ETag compare-and-swap committed from a stale/concurrent attempt. Retries with the same key return the prior result; reusing the key with another payload is rejected.

### `/fileAttachments/{fileId}`

Reuses the existing metadata collection. Weekly report file entries use `purpose: "weeklyReport"`, `status: "ready"`, `id`, `reportId`, `revisionId`, `placementId`, `studentId`, `ownerUid`, `ownerProfileId`, `fileName`, `contentType`, `sizeBytes`, `sha256`, `storagePath`, `storageGeneration`, and `createdAt`. No bytes or base64 are written to RTDB. Metadata is written only after the Storage service confirms the exact object and size. If later RTDB persistence fails, the service attempts to remove only that new unique object and metadata; if the outcome of the report write cannot be read back, it leaves the file private rather than risking deletion of a committed report.

Storage object paths use `uims/weekly-reports/{reportId}/{revisionId}/{fileId}.docx`. Each ID is unique; uploads use create-only generation preconditions. The download route returns bytes only after role, placement/assignment, revision, file type, size, and SHA-256 checks. Public download tokens and browser Storage URLs are not used.

## Identity and authorization

The existing `Firebase Authentication UID → /users/{uid}` contract remains authoritative. The company supervisor business ID and company ID come from the fresh verified profile. Student ID is read from the placement and cross-checked against the report. Academic supervisor ID comes from a current active `/assignments` record for the student; the existing student profile `academicSupervisorId` is a fallback only when no active assignment entry exists. Company placement access requires the explicit placement supervisor ID to match; when the placement has no supervisor field, the verified company ID must match.

The academic report list also returns an `assignedStudents` projection built from active `/assignments` rows, matching `/users` records queried by `studentId`, and `/placements` records queried by `studentId` or `studentProfileId`. It contains only the assigned student's business ID, profile display fields, and existing placement/company IDs and display fields; it does not expose Auth UIDs. Each report/detail/download/review operation re-checks the student's current assignment. If a supervisor assignment changes, the old supervisor loses access; the new supervisor can see the report through that student's current assignment while the original report revision/review history stays intact. A new company revision is routed to the then-current supervisor. Student APIs return only the authenticated student's status, and only show company comments when approved; students cannot upload, resubmit, download, edit, or review reports.

The route uses the server's existing trusted Firebase OAuth/ADC transport. A privileged service identity can bypass RTDB Rules; therefore the C# checks are mandatory and Rules alone do not constrain that credential. Keep the service identity private, restrict its deployment/IAM exposure, and use closed client Rules. Do not grant public reads/writes or expose bucket tokens. This phase does not publish Rules or validate production permissions.

## Query indexes to add when schema/rules are reviewed

At the relevant node, index: `placements.studentId`, `placements.studentProfileId`, `placements.companyId`, `placements.companySupervisorId`, `placements.supervisorId`; `users.studentId`; `assignments.studentId`, `assignments.academicSupervisorId`; `weeklyReports.companyId`, `weeklyReports.companySupervisorId`, `weeklyReports.academicSupervisorId`, `weeklyReports.studentId`; `tasks.placementId`; and `taskExams.placementId`. Attendance uses the existing exact `uimsAttendance/students/{studentId}.activeAssignmentId` pointer and reads the matching `trainingAssignments/{assignmentId}` and `attendance/{assignmentId}` records; it does not query or sum historical assignments. Reads are scoped equality queries or exact record paths; this feature does not fetch the database root. No index has been deployed.

## Read-only progress projection

Academic report detail queries existing `/tasks` for the report placement, `/taskExams` for the placement, each `/taskExamAttempts/{examId}/{studentId}`, `/uimsAttendance/trainingAssignments` by student, and `/uimsAttendance/attendance/{assignmentId}`. It returns task status and task grade, submitted exam scores without answers, and sums only approved attendance `workedMinutes` (falling back to validated check-in/out timestamps for legacy approved rows). It does not copy these values into the report. Live schema/rules and exact existing data still require integration testing.
