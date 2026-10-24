# UIMS final task fixes — 23 October 2026

## Delivered

- Exactly four fixed white workflow lanes: New / Pending, In Progress, Review, Complete. Every lane has the same viewport-dependent height (`clamp(440px, 68dvh, 680px)`), independent of its card count. Only the card area scrolls vertically. Headers/counts remain outside the scrolling area. Narrow viewports scroll the board horizontally without widening the page.
- Compact 12px-padded cards, 14px titles, small status badges, left-aligned readable text, consistent 36px action buttons centered in Board and List views. Long instructions and titles wrap rather than being silently truncated. Official JUST variables, Manrope/Inter declarations, and existing navy styling are retained.
- Company native drag/drop now scrolls a lane near its top/bottom edges and the board near its horizontal edges. Drag/drop, keyboard Move up/down controls, and Firebase board ordering retain the existing company-only permission. Cross-stage dragging remains rejected. Student boards remain read-only under the current authorization model.
- Board refreshes retain each lane's vertical scroll position. Company task details identify exam-only tasks even before their exam draft exists, preventing standard-task grading/submission sections from appearing for an incomplete exam assignment.
- Multi-student creation displays each student's task ID, resource status, and exam publication status. A failed individual operation no longer silently aborts the rest of the acknowledged assignments. The wizard stays open with actionable failure information and only reports success when every selected assignment, required resource, and examination has acknowledged completion.
- The first submitted creation plan is locked during recovery. Retry retains the same batch, placement IDs, resource references, and examination creation keys. The user can pause and reopen the dialog in the same page to resume the plan. Successfully saved resources and published examinations are reused. Resource and question-image uploads have explicit timeouts.
- Exam creation retries are bound to the original normalized exam definition, including its answer keys. A changed request using the same creation key is rejected. Its fingerprint is stored privately inside the existing `taskExamAnswerKeys/{examId}/creationRequest` subtree, never in public exam records. Existing task/exam/database paths have not been renamed or moved; no configuration or connection changes were made.

Existing acceptance/submission/review/approval, graded-edit locks, independent student instances, revision history, extension decisions, server-side exam timing, private grading, explicit result release, and weekly-report snapshots/evaluations remain in place and are covered by the regression/browser suites.

## Exact source/test files modified in this pass

1. `University-Internship-Management-System/Assets/tasks-premium.css`
2. `University-Internship-Management-System/Js_project/Student/MyTasks.js`
3. `University-Internship-Management-System/Js_project/Company/ManageTasks.js`
4. `University-Internship-Management-System/Js_project/Company/TaskComposer.js`
5. `University-Internship-Management-System/Services/TaskExamService.cs`
6. `Tests/FinalEvaluation/TaskExamTests.cs`
7. `Tests/FinalEvaluation/TaskWorkspace.browser.cjs`
8. `Tests/FinalEvaluation/TaskBoardLayout.browser.cjs` — new
9. `TASKS-FINAL-FIXES-2026-10-23.md` — this report

Browser-generated evidence in `Tests/FinalEvaluation` includes the refreshed existing task-workspace screenshots plus new `student-fixed-board-mobile.png` and `company-fixed-board-mobile.png`. Existing screenshot filenames refreshed: `company-task-review-desktop.png`, `company-task-list-desktop.png`, `task-modal-mobile.png`, `task-modal-desktop.png`, `task-exam-builder-mobile.png`, `exam-locked-mobile.png`, `student-task-exam-popup-mobile.png`, `company-board-unavailable.png`, `student-board-unavailable.png`.

Pre-existing uncommitted changes were preserved. No shared sidebar or navigation source, unrelated page, Firebase configuration, authentication/Storage setting, database URL, or Rules file was edited. Before/after SHA-256 comparison confirmed **18 protected sidebar/navigation/Firebase source/configuration files unchanged**. Nothing was committed, pushed, deployed, or written to production Firebase.

## Actual checks

| Check | Result |
|---|---|
| Complete `.slnx` MSBuild solution build | PASS |
| ASP.NET Web Forms precompilation | PASS; five pre-existing CS0108 warnings |
| Full offline C# regression executable | **239 passed, 0 failed** |
| TaskWorkspace browser suite | PASS |
| ExamPopup browser suite | PASS |
| WeeklyPopup browser suite | PASS |
| New TaskBoardLayout browser suite | PASS |
| Six production task/exam scripts and four browser scripts, Node syntax | PASS |
| Changed tracked sources, `git diff --check` | PASS |
| Protected file SHA-256 comparison | PASS: 18 unchanged |

The new layout suite verifies both roles with **0, 5, 20, and 50 cards per lane**, equal white fixed heights regardless of card count, independent lane scrolling, stationary headers, desktop/mobile overflow, Board/List text/button alignment and sizing, the Manrope heading declaration, drag-edge autoscroll, and a same-stage reorder request with the correct placement/task/target IDs. Backend board regressions independently verify persistence and ETag restrictions through isolated Firebase HTTP doubles.

The extended task-workspace test injects a partial task creation failure, a successful publication whose response is lost, and a separate publication failure. It verifies the locked plan, pause/reopen, same batch across three attempts, independent exam IDs, reused uploads, and final completion without extra exam instances. Backend exam tests reject changed titles or correct answers under the same creation key, including after publication.

Browser suites use actual ASPX markup and production JavaScript with isolated fixture responses. External traffic is blocked. They do not render the server-side shared sidebar control and are not evidence of live Firebase persistence or deployed Rules. Screenshots were visually inspected.

### Pre-existing compiler warnings

Generated ASP.NET pages for Company/ApplicationsReview, Company/ManageOpportunities, Student/ApplicationForm, Student/MyApplications, and Student/ExploreOpportunities report CS0108: their generated `Profile` member hides `InternshipPage.Profile`. No unrelated source was changed to suppress these warnings. There were no compilation errors. Disposable precompiled verification output was removed after the check.

## Unresolved/external acceptance requirements

- No isolated Firebase Emulator/development project was configured. Real Firebase/Storage persistence, deployed Rules, direct-client grade secrecy, and real authenticated IIS sessions still need an approved isolated environment. Tests did not access production Firebase and did not disable its enabled configuration.
- Multi-student task/exam creation spans several existing backend operations; it is not an atomic transaction. Partial records are preserved and explicitly reported, then reconciled using the same IDs. Keep the page open while retrying: unsaved question-image bytes and the pending browser plan are not a persistent business database and do not survive page reload/browser closure. Already saved task IDs are shown in the progress panel; saved tasks/exams remain accessible through the existing management pages. No existing record is deleted as rollback.
- Font downloads are blocked in the isolated browser tests; the official font declarations are checked, but the screenshots use available fallback glyphs.

## Exact Visual Studio verification steps

1. Open `C:\Users\user\Desktop\UIMS-Integration-Latest\University-Internship-Management-System.slnx` in the installed Visual Studio. Use the existing application configuration. Select **Build > Rebuild Solution**.
2. In **Tools > Command Line > Developer PowerShell**, change to the workspace root and run the offline checks below. These test fixtures do not connect to the configured live Firebase project.

```powershell
Set-Location C:\Users\user\Desktop\UIMS-Integration-Latest
MSBuild.exe University-Internship-Management-System.slnx /p:UseSharedCompilation=false /verbosity:minimal
MSBuild.exe Tests\FinalEvaluation\FinalEvaluation.Tests.csproj /p:UseSharedCompilation=false /verbosity:minimal
& .\Tests\FinalEvaluation\bin\FinalEvaluation.Tests.exe
node Tests/FinalEvaluation/TaskWorkspace.browser.cjs
node Tests/FinalEvaluation/ExamPopup.browser.cjs
node Tests/FinalEvaluation/WeeklyPopup.browser.cjs
node Tests/FinalEvaluation/TaskBoardLayout.browser.cjs
```

The browser runners use the existing bundled Playwright runtime and installed Chrome. They install no dependencies.

3. To repeat Web Forms precompilation, choose a new disposable target directory each time:

```powershell
$taskCompileTarget = Join-Path (Get-Location) ('Tests\task-precompile-' + [Guid]::NewGuid().ToString('N'))
& C:\Windows\Microsoft.NET\Framework64\v4.0.30319\aspnet_compiler.exe -v / -p C:\Users\user\Desktop\UIMS-Integration-Latest\University-Internship-Management-System $taskCompileTarget
```

4. Set the existing Web Forms project as Startup Project and use **Ctrl+F5**. With existing authorized accounts, view `/Student/MyTasks.aspx` and `/Company/ManageTasks.aspx`. Inspect the four lanes, Board/List filters, card alignment, and existing task/exam details. At mobile width, scroll the board horizontally; with enough existing tasks, scroll the cards inside each lane.
5. Creation, acceptance, submission, grading, extensions, and result release mutate business data. Verify those real-account actions only in an explicitly authorized isolated environment. Do not create test records against the enabled production configuration.
