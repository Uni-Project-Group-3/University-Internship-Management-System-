Training period and flexible attendance

- Creating or editing an opportunity automatically persists `attendanceMode: "flexible"`, `timeZone: "Asia/Amman"`, and `trainingStartBasis: "companyAcceptance"` alongside its duration and required hours. No manual database fields are needed. Acceptance-specific dates are written when acceptance actually happens.
- Company acceptance snapshots `acceptedAt`, `durationWeeks`, `startDate`, `endDate`, and `attendanceMode: "flexible"` on `applications/{id}`.
- The first day is the acceptance date in Asia/Amman. The last day is first day + (weeks * 7) - 1, inclusive. Supported duration: 1–52 weeks.
- Student confirmation copies the period to `placements/{id}` and `uimsAttendance/trainingAssignments/{id}`. Later edits to the opportunity duration do not change an accepted student's period.
- Attendance check-in and check-out have no shift hours or excluded weekdays within that period. QR ownership, supervisor verification, expiry and check-in-before-check-out checks still apply.
- For flexible assignments, the attendance history derives absence for each elapsed calendar date with no check-in. Today and future dates are not absent. These derived rows are display data; they are not stored attendance events or approved hours.
- Existing placements retain their saved period and attendance history. This change does not migrate historical Firebase records or deploy the application.

Validation: build `Tests/FinalEvaluation/FinalEvaluation.Tests.csproj`, then run `Tests/FinalEvaluation/bin/FinalEvaluation.Tests.exe` without arguments. The default suite uses isolated databases and performs no network requests.

Existing confirmed placements can be linked from the company attendance dialog with Enable attendance. The server verifies the assigned supervisor, the student's active Authentication account and matching profile, the existing placement dates and academic supervisor, and the attendance company/supervisor before creating missing assignment/student/claim nodes. Writes use the enabled attendance gate, exact per-child ETags and a request-local list of server-approved records; no general Firebase write flags are changed. Existing history, company identities and calendars are preserved. Retrying completes the same links.

The company workspace updates and highlights the server-returned record immediately after a successful scan, clears filters that could hide it, and requests confirmation before approving hours. Issuing a QR alone never records attendance.
