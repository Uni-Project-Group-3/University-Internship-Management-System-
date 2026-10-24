# Tasks and exams verification — 23 October 2026

## Scope and implementation

The workspace already contained the Tasks/Exams implementation and extensive uncommitted changes before this request. Those changes were retained. This pass repaired and verified the existing implementation rather than replacing the Firebase architecture or shared navigation.

Changes made in this pass:

- Student Board/List now use the same exact-title, status, archive and placement filtering. Company Board/List use the same authorized task records, with Firebase board order applied separately. Archived instances cannot be dragged.
- All four fixed workflow columns remain visible and white, including empty/error states. Dragging is restricted to ordering within the stored workflow stage; student boards retain their existing read-only permission.
- Student details display the backend's actual assessment instructions (`criteria`).
- Task list cards use consistent title/body sizing; task modals are vertically centered with reduced-motion support. Styling changes are confined to task content and dialogs.
- Student task-list and board responses suppress exam scores from legacy task records; task-list responses also suppress legacy exam feedback. Released results continue through the existing release-gated exam receipt API.
- Private Firebase Storage attachment submissions can be submitted without a fabricated work URL or mandatory notes. A filename alone cannot satisfy this requirement: the backend requires the attachment IDs produced by its trusted upload flow. Existing link-based submissions remain supported.
- Offline regression coverage includes legacy exam score/feedback privacy, file-only submission, forged filename rejection, and exact title/status filter parity.
- Updated the older standalone exam/weekly browser fixtures for the current eligibility API, confirmation dialogs, and independent academic score/rating requirements.

## Files edited in this pass

Application:

1. `University-Internship-Management-System/Student/MyTasks.aspx`
2. `University-Internship-Management-System/Js_project/Student/MyTasks.js`
3. `University-Internship-Management-System/Js_project/Company/ManageTasks.js`
4. `University-Internship-Management-System/Services/TaskService.cs`
5. `University-Internship-Management-System/Assets/tasks-premium.css`

Tests:

6. `Tests/FinalEvaluation/TaskBoardTests.cs`
7. `Tests/FinalEvaluation/FirebaseFileStorageTests.cs`
8. `Tests/FinalEvaluation/TaskWorkspace.browser.cjs`
9. `Tests/FinalEvaluation/ExamPopup.browser.cjs`
10. `Tests/FinalEvaluation/WeeklyPopup.browser.cjs`

This report is new. The browser suite also regenerated its nine screenshot outputs in `Tests/FinalEvaluation`: `company-task-review-desktop.png`, `company-task-list-desktop.png`, `task-modal-mobile.png`, `task-modal-desktop.png`, `task-exam-builder-mobile.png`, `exam-locked-mobile.png`, `student-task-exam-popup-mobile.png`, `company-board-unavailable.png`, and `student-board-unavailable.png`.

No shared sidebar, navigation, Firebase configuration, connection, database path, authentication setting, Storage setting, Rules file, or unrelated business page was edited in this pass. Pre-existing changes in those areas were left alone. Nothing was committed, pushed, or deployed.

## Actual verification

- Visual Studio MSBuild solution build: PASS.
- Regression project build and executable: **239 passed, 0 failed**. The suite uses offline HTTP/Storage doubles and reports no network requests or Firebase writes.
- ASP.NET Web Forms precompilation: PASS. Five existing CS0108 generated-page warnings concern Profile members on unrelated application/opportunity pages. No compilation errors. Generated verification directories were removed after verification.
- JavaScript syntax: six production task/exam scripts plus the three browser test scripts passed Node syntax checks.
- TaskWorkspace browser suite: PASS. Exercises actual ASPX markup and production JavaScript using isolated fixture responses, blocking external traffic. Covers independent student assignments, restricted drag/drop, common filters, validation, wizard/exam preview, image/resource upload requests, mobile dialogs, server countdown/eligibility, closing restrictions, answering/submission confirmation, pending score privacy, supervisor release/hide/revision, standard submission, company/student refresh, four white columns, and unavailable-data retry.
- Standalone ExamPopup browser suite: PASS after updating its obsolete fixture/selectors. Covers eligibility, start/cancel, question rendering and unanswered/submission confirmation.
- WeeklyPopup browser suite: PASS after supplying the currently required independent academic score/rating. Covers approval confirmation, student report popup and mobile sizing.
- Desktop task list/review screenshots were inspected. Browser screenshots use offline fixture identities; they are not live Firebase evidence and do not render the server-side sidebar control.

## Existing workflows retained and exercised

Task acceptance/submission/company grading, independent bulk assignments, versioned edits and graded locks, revision deadlines, final extension rejection, task resources, and audit history remain in the existing task services. Examinations retain MCQ/true-false/image questions, server-enforced windows/duration/attempts, private calculated grades, and explicit company release. Weekly reports retain immutable task snapshots and independent academic evaluation with student-safe projections. The full offline regression suite covers these service workflows.

## External acceptance requirements and limitations

No Firebase Emulator environment was configured. Live Firebase/Storage persistence, real authenticated IIS sessions, restart persistence, deployed Rules and direct-client read restrictions have not been certified. In particular, deployed Rules must protect private answer keys/calculated results and any legacy exam scores stored directly on task records. Hiding those values in the C# API cannot secure a permissive direct database rule. No live data migration or Rules deployment was attempted.

The existing task wizard persists independent task assignments before publishing their exams. Failed exam publication leaves those saved assignments available for a safe retry; task creation and exam publication are not one atomic transaction. Notifications remain task-scoped persisted notifications rather than a rebuilt global push system.

An approved isolated Firebase test environment and real role accounts are required to certify full end-to-end persistence/security. Production was not accessed or modified by these tests.
