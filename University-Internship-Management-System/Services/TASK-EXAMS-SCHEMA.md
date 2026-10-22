# Task Exams: implemented Firebase contract

This document describes the new nodes used by `TaskExamService`. The service is disabled by default. It does not import or migrate existing data. The existing `/tasks`, `/taskBoards`, `/placements`, `/users`, and `/fileAttachments` records remain in place.

## `/taskExams/{examId}`

| Field | Type | Required | Purpose / relationship | Access |
|---|---|---:|---|---|
| `id` | string | yes | Same generated key as `{examId}` (`EX-` plus random GUID). | Company owner and server; student API projection only. |
| `taskId` | string | yes | Existing `/tasks/{taskId}` key. Task must already belong to `placementId`. | Authorized company/student API projection. |
| `placementId` | string | yes | Existing `/placements/{placementId}` key. This is the student visibility and company authorization boundary. | Authorized company/student API projection. |
| `companyId` | string | optional | Existing company profile ID from verified supervisor profile. | Company API only. |
| `createdBy` | string | yes | Existing company supervisor profile ID, never accepted as verified from browser. | Company API only. |
| `status` | string | yes | `draft` or `published`. Published exams are locked; create a new exam for changed content. | Server controlled. |
| `publishedVersion` | integer | yes | `0` until published, then points at `/versions/{version}`. | Server controlled. |
| `draftRevision` | integer | yes | Monotonic compare-and-swap revision for draft edits. | Server controlled. |
| `draft` | object | draft only | Current editable definition. Never contains correct answers. | Company owner only; excluded from student projection. |
| `draft.title` | string | yes | 3–180 character exam title. | Company owner. |
| `draft.instructions` | string | optional | Student instructions. | Company owner; student receives published value. |
| `draft.opensAt`, `draft.closesAt` | ISO-8601 UTC string | yes | Availability range, closing must follow opening. | Company owner; student receives published values. |
| `draft.durationMinutes` | integer | yes | Server-measured attempt duration, 1–600. | Company owner; student read-only. |
| `draft.attemptLimit` | integer | yes | Maximum attempts per student and exam, 1–10. | Company owner; server enforced. |
| `draft.totalPoints` | number | yes | Derived sum of question points. Client value is ignored and recomputed. | Server controlled. |
| `draft.questionOrder` | array of strings | yes | Ordered question IDs for display. | Company owner; student receives published value. |
| `draft.questions` | object keyed by question ID | yes | Draft question definitions. IDs are preserved in published snapshots. | Company owner; student receives published snapshot. |
| `draft.questions/{questionId}.id` | string | yes | Must equal the question map key. | Company owner/student read projection. |
| `.type` | string | yes | `multipleChoice` or `trueFalse`. | Company owner/student read projection. |
| `.prompt` | string | yes | 1–4000 character prompt. | Company owner/student read projection. |
| `.points` | number | yes | Positive points, maximum 1000 per item. | Company owner; grading service reads the matching answer key. |
| `.order` | integer | yes | Display order derived from `questionOrder`. | Company owner/student read projection. |
| `.options` | array of `{id:string,text:string}` | MC only | 2–8 stable option IDs and non-empty labels. | Company owner/student read projection. |
| `.imageFileId` | string | optional | References `/fileAttachments/{fileId}` where `purpose` is `examQuestionImage`. | Company owner; students get only the published image endpoint. |
| `createdAt`, `updatedAt`, `publishedAt` | ISO-8601 UTC strings | as applicable | Server timestamps. | Server controlled. |
| `versions/{version}` | object | published | Immutable published, student-safe exam snapshot. Contains no answer fields. | Student can read through authorized API only; company can read through API. |

Questions and options are objects/arrays as listed; no binary content is stored here. Dates are written as ISO-8601 strings. Existing IDs in `/tasks` and `/placements` are referenced verbatim.

## `/taskExamAnswerKeys/{examId}`

This subtree is server-only. It must never be returned by the student exam, attempt, or image endpoints.

* `draftVersions/{draftRevision}/{questionId}`: `{ "correctOptionId": "option-id", "points": 2 }` for multiple choice, or `{ "correctAnswer": true, "points": 2 }` for true/false.
* `{publishedVersion}/{questionId}`: same fields, copied from the matching draft revision during publish. Published keys are immutable.

The company draft API joins only that draft's matching key revision after checking the verified company relationship. The public definition stored under `/taskExams` never includes correct answers. Server grading reads only the key version attached to the attempt.

## `/taskExamAttempts/{examId}/{studentId}/{attemptId}`

`attemptId` is random (`AT-` plus GUID); the path includes the verified profile student ID so start/retry operations can use scoped ETag compare-and-swap.

| Field | Type | Required | Purpose |
|---|---|---:|---|
| `id`, `examId`, `taskId`, `placementId`, `studentId` | string | yes | IDs bind the attempt to an exam, existing task/placement, and verified student. |
| `version` | integer | yes | Immutable `/taskExams/{examId}/versions/{version}` and `/taskExamAnswerKeys/{examId}/{version}` used for this attempt. |
| `status` | string | yes | `inProgress`, `submitted`, or `expired`; only server transitions it. |
| `startedAt`, `deadlineAt` | ISO-8601 UTC strings | yes | Both assigned by server; deadline is the earlier of duration expiry and exam close; retry never resets it. |
| `answers` | object keyed by question ID | yes | MC value is option ID string; true/false value is boolean. Validated against the immutable published version. |
| `submittedAt`, `gradedAt` | ISO-8601 UTC strings | on finalization | Server assigned. |
| `score`, `maximumScore` | number | on finalization | Computed by C# from versioned answer keys and point values. Never accepted from a request. |

## `/fileAttachments/{fileId}` for exam question images

The existing attachment node is reused. Exam images use `purpose: "examQuestionImage"`, `status: "ready"`, and the existing metadata pattern: `id`, `examId`, `questionId`, `taskId`, `placementId`, `ownerUid`, `ownerProfileId`, `fileName`, `contentType`, `sizeBytes`, `sha256`, `storagePath`, `storageGeneration`, and `createdAt`. `storagePath` is `uims/exam-question-images/{placementId}/{examId}/{fileId}`. Only PNG and JPEG signatures are supported, maximum 10 MiB. Metadata is written after Storage succeeds; a metadata failure attempts exact-object cleanup.

## Authorization and Rules requirements

The handler resolves the signed-in Firebase UID through the existing trusted UIMS sign-in ticket and `/users/{uid}` profile; request body IDs and role are not treated as identity. A company operation additionally checks verified company supervisor/company ownership of the existing placement and linked task. A student operation checks both placement and task ownership against the verified student profile ID. Student read projections include published exam definitions and their own attempt only. Student writes are limited to validated answer values through the server API; the server owns deadlines, status, keys, score and timestamps. Company supervisors can manage their own drafts, publish, and view attempts only for their authorized exam. No Coordinator/Academic exam APIs are implemented in this phase.

Because trusted server OAuth credentials can have privileges that bypass RTDB Rules, endpoint authorization remains mandatory. Rules must still deny direct student reads of `/taskExamAnswerKeys`, direct client writes of exam definitions/attempt score fields, cross-student attempts, cross-company drafts, and all public access. If server credentials have Admin privileges, Rules cannot constrain that service credential; restrict who can call the server and keep the C# ownership checks authoritative. Storage Rules must deny public object access and allow access only through the same authorized backend path. Do not use an API key as authorization.

The list endpoint uses scoped Realtime Database equality queries rather than reading the complete roots. Rules therefore need `.indexOn` entries at `/placements` for `studentId` and `studentProfileId`, and at `/taskExams` for `companyId`, `createdBy`, and `placementId`. The service still rechecks every result against the verified actor and task/placement records. No live Rules validation was performed; missing indexes/permissions will surface as Firebase errors and require deploying matching least-privilege Rules before enabling the integration.
