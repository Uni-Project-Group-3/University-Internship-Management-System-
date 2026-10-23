# Task workspace connection repair

The company and student ASPX pages were missing `task-dialogs.js`, `firebase-client.js`, the role-specific exam integration and `task-workspace.css`, while their workspace JavaScript already depended on these modules. Initialization could throw before loading/rendering the board, presenting a frontend error as a Firebase outage. Dependencies now load before the task workspace script.

The company script also referenced edit/archive/extension controls whose definitions were missing after integration. The existing implementations were restored. Two stale student lines referenced undefined variables while opening an overdue task; these were removed while the existing deadline blocking remains.

Company task loading no longer waits for the optional exam list. That request has its own timeout and refreshes badges in the background. An exam service delay does not hold the task data request open.

Verification: real read-only Firebase service checks returned four columns and five cards for an authorized placement in each of two existing company accounts. No production cards were moved and no tasks were created for this diagnostic. Isolated persistence/authorization tests and Web Forms compilation were run separately. Browser test output is in `FinalEvaluation/task-workspace-browser-results.log` locally; tests intercept all Firebase requests and never seed the application database.

Run `node Tests/FinalEvaluation/TaskWorkspace.browser.cjs` for the full isolated interface cycle. Run the existing PowerShell runner with `-ReadOnlyFirebase` for authorized real reads. Private Firebase Rules, credentials, project configuration and Storage were not changed by this repair.
