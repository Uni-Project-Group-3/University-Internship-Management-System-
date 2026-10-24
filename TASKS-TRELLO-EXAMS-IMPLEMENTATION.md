# Tasks, board, examinations and weekly task context

Implemented in the existing ASP.NET Web Forms application using the existing C# services, verified Firebase actors, Firebase REST/ETag transport and Firebase Storage service. JavaScript and CSS provide the interface. No Python application files or dependencies were added. Firebase settings, credentials, database URLs and deployed Rules were not changed. No live records were imported, migrated or written during verification. Nothing was committed or pushed.

## Implemented behavior

- Centered responsive task popup with searchable placement checkboxes, select all, clear, removable selection chips, inline validation, retained invalid input, first-invalid focus, publication confirmation and unsaved-discard protection.
- Bulk creation produces an independent `/tasks/{taskId}` per placement. Stable batch/placement IDs make retries reuse existing instances; a creation signature rejects reuse with changed content. Partial results identify failed placements. Resources use the existing private Storage upload and metadata flow; identical attachment retries are deduplicated by content hash and filename.
- Combined authorized student board and individual placement boards; separate cards, workflow statuses, student initials, due dates, overdue/resource/exam indicators, extension indicators, search, student/status/deadline/batch filters, drag/drop, keyboard-accessible card movement and persisted column ordering. Positions remain in `/taskBoards/{placementId}`. Reordering columns materializes legacy card positions before changing column order.
- Individual or batch editing preserves submissions, instruction versions, original deadlines, resource references, independent board positions and grades. Graded instances are skipped. Confirmation identifies affected and excluded students. Submitted work can remain under review or require resubmission with a new default/individual deadline. Partial edit failures are reported individually. Successful grading appends grade history.
- Overdue submissions are rejected on the server, including attachment submissions. Extension requests retain reason, proposed/old/final deadlines, timestamps, decisions and history. Pending requests cannot be duplicated; rejection is final. Approval changes only the requesting task. Task activity and recipient-scoped notifications are persisted within the existing task record and displayed in task details.
- Archive confirmation soft-archives a task without deleting its submissions, grades, resources or board positions. Archived task history remains accessible through the Archived list filter. Task editing, submission, resource changes and review are blocked for archived instances. Published examinations retain their own availability/attempt lifecycle.
- The optional exam builder opens inside the creation popup **after the independent task assignments have been saved**, one builder per student task. Assignment creation and exam publication are separate explicit operations; an exam-validation failure does not roll back a successfully saved assignment. The existing standalone examination editor remains available.
- Examination builder supports MCQ and True/False, adding/removing/reordering questions and answer options, correct-answer selection, points, PNG/JPEG uploads with progress and previews, image replacement/removal, timezone display, field validation and publication confirmation. Publication verifies that every referenced image is ready and belongs to that exact exam/question. Immutable publication writes can be retried after an interrupted publish.
- Student examination details and taking experience use centered dialogs. Details expose availability and question counts without questions/answer keys. A server-synchronized digital countdown and eligibility request control the locked Start button. Starting always rechecks the trusted backend. Questions and images require an active authorized attempt. The effective attempt deadline is bounded by examination closure and duration.
- Attempts resume from saved Firebase answers, autosave answers, show question navigation/progress, require final submission confirmation and lock after submission/expiry. Expiry grades the last saved answers and ignores late changes. Expired student attempts are reconciled when attempts are read.
- Newly calculated grades are written under the existing private answer-key collection, with an immutable calculation ID referenced by the finalized attempt. Pending student responses contain neither score nor maximumScore. Authorized company review can see answers, answer keys and original calculated results. Explicit release writes reviewer identity, release time and feedback; release does not change grading rules. Repeated release preserves the first released feedback/result. Existing legacy scores are hidden by student API projections until explicitly released; existing live legacy data is not migrated.
- Weekly report task suggestions come from the authorized student's task activity. The company can include/exclude suggested tasks. Every report revision captures a new immutable task snapshot. Academic evaluation requires a separately entered score out of 100 and separately selected rating. Approved evaluation is bound to its exact revision; the student projection exposes only the latest approved evaluation and never supervisor discussions. Authorized company/academic supervisors can discuss a report or a task in its immutable snapshot. Academic progress includes only explicitly released examination results.
- Existing coordinator screens and their authorization model were preserved. No permission to edit company grades or student submissions was added for academic supervisors or coordinators.

## Verification results

- Visual Studio solution build succeeded; the final normal-permission rebuild completed without compiler errors. An earlier sandbox rebuild reported access-denied cleanup warnings for generated Roslyn files; rerunning outside the sandbox succeeded.
- ASP.NET Web Forms precompilation succeeded with .NET Framework's `aspnet_compiler.exe`. The initial sandbox attempt was denied access to CodeDom compiler assemblies; the normal-permission run succeeded. Generated precompiled copies were removed after verification.
- Regression executable: **205 tests passed**, using isolated HTTP/Storage doubles, with no live Firebase requests or writes. The full regression suite covers the existing application workflows as well as the added checks below.
- New/extended C# checks cover bulk retry reuse, changed-content conflict, partial creation, independent student records, graded exclusions, instruction/submission preservation, approval/rejection of extensions, final rejection, archive preservation, card and column persistence, attempt privacy/release authorization, server windows/expiry, invalid exam definitions, missing images, weekly snapshots, private discussions and independent numeric/descriptive evaluation.
- Browser tests run the actual ASPX markup and production JavaScript through a local fixture server. External/Firebase traffic is blocked. They verify independent card dragging with the correct placement, task popup validation, searchable selection/chips, bulk confirmation, 390px mobile layout, locked exam countdown, answering/submission confirmation, pending result privacy, explicit supervisor release, feedback and released score display. These are UI tests with test fixtures, **not live persistence tests**.
- Changed JavaScript files passed Node syntax checks. Mobile screenshots were visually inspected: `Tests/FinalEvaluation/task-modal-mobile.png` and `Tests/FinalEvaluation/exam-locked-mobile.png`.

## Remaining acceptance checks

No configured Firebase Emulator integration suite was found. Live Firebase/Storage writes, real-account browser sessions, service/browser restart persistence against the deployed database, and deployed Firebase Rules/direct-client-read security have **not** been tested. The local deny-by-default Rules proposal is not evidence of the currently deployed policy. In particular, deployed Rules must protect answer keys/calculated results, gate raw examination data appropriately, and prevent unrestricted student reads of legacy stored scores. Backend projection tests cannot certify those deployed Rules. No Rules deployment or legacy-data migration was performed.

The complete 36-scenario request has therefore not been certified end-to-end against production. The automated evidence is the offline C# regression suite and the isolated browser suite described above. Global notification-bell/push delivery was not rebuilt; this change persists task-scoped notifications and surfaces them in task details/cards.

## Run the automated checks

From the existing workspace in a Visual Studio Developer Command Prompt:

```bat
MSBuild.exe University-Internship-Management-System.slnx /p:UseSharedCompilation=false
MSBuild.exe Tests\FinalEvaluation\FinalEvaluation.Tests.csproj /p:UseSharedCompilation=false
Tests\FinalEvaluation\bin\FinalEvaluation.Tests.exe
node Tests\FinalEvaluation\TaskWorkspace.browser.cjs
```

The browser runner uses the bundled Playwright installation under the current user's Codex runtime and installed Chrome/Edge. It does not install packages or connect to Firebase. Precompilation can be repeated into a new disposable directory:

```bat
C:\Windows\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe -v / -p C:\Users\user\Desktop\UIMS-Integration-Latest\University-Internship-Management-System C:\Users\user\Desktop\UIMS-Integration-Latest\Tests\tasks-precompiled-check
```

## Manual verification with existing application accounts

These steps create/change real business records when run against the configured database. They were not executed against live data during this implementation. Use only the existing authorized accounts/placements and explicitly approved test records/environment.

1. Open the existing solution in Visual Studio and launch the existing application through its established IIS/IIS Express profile. Keep its Firebase configuration unchanged.
2. Sign in as an existing company supervisor. Open `Company/ManageTasks.aspx`, open Create New Task, test blank-field validation, then select one student, several students and all eligible students. Create each approved test assignment. Confirm distinct task IDs/placements and independent cards.
3. Move one card in the combined board; verify the other student's card stays independent. Choose a placement to reorder its columns. Refresh/restart the application and verify persisted positions. Test search and student/status/deadline/batch filters.
4. Open a card and edit just that student, then edit its ungraded batch. Verify the affected/excluded confirmation. Have a student submit, keep that submission under review during one edit, then request resubmission with a distinct future deadline during another edit. Inspect previous instruction versions/submission history. Grade an instance and verify instruction/resource locks and bulk exclusion.
5. With an explicitly approved overdue test task, verify submission denial, pending extension prevention, approval with a final deadline and successful subsequent submission. On a separate approved test task, reject the extension and verify another request is denied.
6. During task creation select optional examination configuration. After assignments save, open the embedded builder for each intended task. Add MCQ, True/False and image questions; reorder questions/options; test duplicate/empty options and incomplete uploads. Save and explicitly publish. Use the displayed timezone.
7. Sign in as the assigned student and open `Student/MyExams.aspx`. Verify View Exam reveals details but no questions before starting. Before opening, inspect the disabled Start button and countdown. At opening, wait for server eligibility confirmation. Start/resume, answer, save, refresh and resume. Verify effective expiry and submission confirmation.
8. Before release, inspect student attempt/list/save/submit API payloads and verify no score is returned. Using the authorized company account, open `Company/ManageExams.aspx`, choose Review and release results, inspect answers/keys/calculation, enter optional feedback and explicitly approve/release. Verify the student can then see the score, feedback and release time. Attempt unauthorized actions with another existing account and expect denial.
9. Open `Company/WeeklyReports.aspx`; choose a placement and week, include/exclude suggested tasks and submit the approved test DOCX. Inspect the snapshot through report history. As the assigned academic supervisor, open `Academic/WeeklyReports.aspx`, comment privately on a snapshot task, independently enter the numeric score and rating, and approve or request revision. A new revision must capture a new snapshot and evaluation. Verify `Student/WeeklyReports.aspx` shows only the latest approved academic evaluation and never private discussion text.
10. Verify historical legacy tasks/submissions and the existing academic/coordinator progress screens still load without new write permissions. Archive an approved test task only after checking the confirmation; verify its historical submissions/files/grades remain available through the Archived filter.

## Exact source-file inventory

Paths below are relative to this existing workspace. Build outputs and the regression `.log` are generated/ignored outputs.

Modified C# / project files:

- `University-Internship-Management-System/Services/TaskService.cs`
- `University-Internship-Management-System/Services/TaskExamService.cs`
- `University-Internship-Management-System/Services/TaskExamsApiHandler.cs`
- `University-Internship-Management-System/Services/WeeklyReportService.cs`
- `University-Internship-Management-System/Services/WeeklyReportsApiHandler.cs`
- `University-Internship-Management-System/University-Internship-Management-System.csproj`

Modified ASPX files:

- `University-Internship-Management-System/Company/ManageTasks.aspx`
- `University-Internship-Management-System/Company/ManageExams.aspx`
- `University-Internship-Management-System/Company/WeeklyReports.aspx`
- `University-Internship-Management-System/Student/MyTasks.aspx`
- `University-Internship-Management-System/Student/MyExams.aspx`
- `University-Internship-Management-System/Academic/WeeklyReports.aspx`

Modified interface scripts:

- `University-Internship-Management-System/Js_project/Company/ManageTasks.js`
- `University-Internship-Management-System/Js_project/Company/TaskExams.js`
- `University-Internship-Management-System/Js_project/Company/WeeklyReports.js`
- `University-Internship-Management-System/Js_project/Student/MyTasks.js`
- `University-Internship-Management-System/Js_project/Student/TaskExams.js`
- `University-Internship-Management-System/Js_project/Student/WeeklyReports.js`
- `University-Internship-Management-System/Js_project/Academic/WeeklyReports.js`

Added interface files:

- `University-Internship-Management-System/Assets/task-workspace.css`
- `University-Internship-Management-System/Js_project/task-dialogs.js`
- `University-Internship-Management-System/Js_project/weekly-task-context.js`

Modified C# tests:

- `Tests/FinalEvaluation/Program.cs`
- `Tests/FinalEvaluation/TaskBoardTests.cs`
- `Tests/FinalEvaluation/TaskCompatibilityTests.cs`
- `Tests/FinalEvaluation/TaskExamTests.cs`
- `Tests/FinalEvaluation/WeeklyReportTests.cs`

Added verification/documentation artifacts:

- `Tests/FinalEvaluation/TaskWorkspace.browser.cjs`
- `Tests/FinalEvaluation/task-modal-mobile.png`
- `Tests/FinalEvaluation/exam-locked-mobile.png`
- `TASKS-TRELLO-EXAMS-IMPLEMENTATION.md`
# Task write activation and creation dialog refresh — 2026-10-23

Enabled the existing `FirebaseLiveWritesEnabled`, `FirebaseSchemaValidated`, and `TasksLiveWritesEnabled` gates required by `TaskService.Ensure(write: true)`. Project identity, database URLs, credentials, Firebase Rules, Storage flags, exam flags and other module-specific flags remain unchanged. No live business records were created, reordered or deleted during this change. Existing ETag, idempotency and role/placement checks remain in effect.

The creation dialog now separates task details, eligible students, resources and optional examination into responsive panels. Description and instructions are separate controls; instructions use the existing `criteria` request and `evaluationCriteria` database contract. Maximum grade and deadline appear together. Labels, field limits and cache versions were updated without changing stored task contracts.

Verification: solution build, ASP.NET Web Forms precompilation, 205 offline regression tests, JavaScript syntax and isolated browser tests passed. Browser tests assert separate description/instructions/maximum payloads, bulk assignment confirmation, independent drag/drop and keyboard moves, validation, unsaved-work protection and 390px responsive layout. Firebase requests are blocked in the browser fixture. Live write/restart persistence was not exercised because this would alter existing business data. Storage and exams remain disabled by their existing independent configuration flags.
