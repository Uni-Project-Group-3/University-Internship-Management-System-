# UIMS merge recovery and Login verification — 22 October 2026

## Git preservation

Branch: tariq-final-merge. HEAD: 591b25f6015335f45a451415854856c7325d457f. Saved commit 47e4adbb7efde5a7ecfff4142cf556edcfad8df6 exists and is an ancestor of HEAD. MERGE_HEAD and origin/main both cfc1192ae843d769da4db9654c4cc472b0a86572. No reset, clean, checkout, branch switch, abort, commit or push was performed.

Full conflicted working files and Git stages 1/2/3 were backed up outside the repository at:
C:\Users\user\AppData\Local\Packages\sandbox.{96acec1b-7018-493e-a7cb-901a7249ffe1}\AC\Temp/uims-recovery-lEG8i2
Earlier backup: C:/Users/user/AppData/Local/Packages/sandbox.{96acec1b-7018-493e-a7cb-901a7249ffe1}/AC/Temp/uims-merge-XxyRZ1.

All ten content conflicts are resolved. Git index staging was blocked in agent sandbox; the user ran Stage-ResolvedMerge.ps1 successfully. Read-only git status confirms no unmerged entries and git diff --cached --check passes. The merge remains open awaiting review/commit. .vs/VSWorkspaceState.json is an unrelated user modification, left unstaged. Stage-ResolvedMerge.ps1 remains untracked intentionally.

## Exact conflict decisions

1. Assets/coordinator/contact-messages.css: incoming modern contact-message styles retained; existing scoped live-detail styles appended without a second palette.
2. Company/ManageOpportunities.aspx.cs: retained existing authenticated application-compatible code and current live Gateway bindings. Incoming unauthenticated whole-database REST and fallback sup-orange identity rejected.
3. Contact.aspx: retained incoming compatible style/version changes; no demo-store runtime injection.
4. Coordinator/ContactMessages.aspx: incoming redesigned inbox markup preserved; trusted tab-session script retained; demo-store and old JavaScript navigation that drops POST tickets excluded.
5. Js_project/Coordinator/contact-messages.js: incoming sender/avatar/status/row presentation adapted to the current roleData projection and original messageDialog. Current server-authorized scoped reads, escaping, Jordan dates, filters/paging retained. Mark/read/reply writes remain explicitly unavailable because no approved write scope exists.
6. Js_project/Public/contact-demo.js: keeps historical filename but no fake persistence or success. Public contact submission remains unavailable until a safe anti-abuse/authorized write path exists.
7–9. Student/ApplicationForm.aspx.cs, ExploreOpportunities.aspx.cs, MyApplications.aspx.cs: retained verified Context account, first-login restriction, tab logout and existing live Firebase projection. Incoming stu-sara fallback, raw root reads, forced mustChangePassword=false and mismatched database schema were not activated.
10. csproj: preserves Firebase service compile entries including RolePortalService, one Newtonsoft.Json 13.0.4 reference, current framework/project identity and NuGet import guard. No missing included source/content paths; packages.config unchanged.

Also reviewed automatically merged Company/ApplicationsReview.aspx.cs, Contact.aspx.cs, Coordinator/ContactMessages.aspx.cs: removed incoming unauthenticated alternate-database adapters by retaining their current trusted/live-adapter-compatible code. Updated designer whitespace only where needed. Original files and unsafe incoming implementations remain recoverable from Git/MERGE_HEAD and the stage backups.

## Security findings and feature limits

Incoming backend code targeted just-41ad1-default-rtdb rather than configured intership-managment-system, read the entire database without an auth token, used Session fallback IDs, injected window.UimsDemo and set mustChangePassword=false in client projections. These implementations cannot safely be enabled under existing Firebase Rules/schema. The merge preserves compatible presentation and current functionality, but **new apply/confirm-offer/company decision/opportunity write/contact submission/reply operations from that adapter are not operational**. Secure schema-mapped versions and tested narrowly scoped write permissions remain separate required work; no live Rules/data/accounts were changed.

Auth/Login.aspx, Login.aspx.cs, Global.asax.cs, FirebaseConfiguration, Firebase REST auth/token verification, UserAccountService, SessionService, password completion and tab-session logic are unchanged from HEAD. No service-account credentials or local auth fallback added.

Homepage files have no differences from HEAD; the incoming merge only added contact-page changes in its two commits, so existing main homepage improvements are retained. No unrelated page layout, language or direction logic was changed. The incoming Contact Messages design is retained; no universal UI redesign was attempted.

## Verification executed

PASS Debug MSBuild after resolving the compile conflicts. NuGet restoration unnecessary: references are installed and build succeeded. PASS source marker scan; PASS git diff --check and staged diff --check after staging. PASS JavaScript syntax for the resolved scripts. PASS all included source/content paths; one Newtonsoft reference.

PASS 104 isolated C# scenarios: REST auth 15, security 27, password completion 8, diagnostics 4, role routing 28, portal ownership/projection 22. Existing tests were discovered at C:/Users/user/Desktop/Tests, outside the relocated repository. Compiled into a temporary test directory using current application DLL and Web.config binding redirects. No live writes.
PASS 7 password popup and 5 tab-session JS tests. Their old relative paths were adapted only in temporary copies to the current checkout; original tests unchanged.

Chrome Student login: initial attempt used a stale pre-rebuild form and was rejected by CSRF protection. After loading the current form, actual Firebase login completed, trusted ticket created and StudentDashboard displayed 1 application, 240 approved hours, owned placement/task records. This is a live read success, not a new password test. No password or token captured. Chrome also verified Company Dashboard (2 published opportunities, 1 new application, 1 active trainee), Academic Dashboard (10 assigned students, 6 active placements), and Coordinator Dashboard (40 students, 8 companies, 14 opportunities). Four different accounts were simultaneously authorized in separate tabs. Company logout returned to Login while Academic navigation to StudentProgressMonitor remained authorized and displayed its assigned roster. Student navigation to MyApplications passed. Coordinator ContactMessages displayed its live record in the incoming inbox layout; opening and closing the original messageDialog passed. Refresh, all-role logout, fresh first-login password changes and a separate Admin account were NOT RUN in this recovery pass. Some legacy UI notices/disabled shortcuts remain; this is not a claim that all portal interactions are complete.

## Finalization

Review staged diff first. Run git diff --name-only --diff-filter=U (must be empty), git diff --cached --check, then git status. Do not commit until remaining Login/browser tests and feature-limit review are accepted. When approved, git commit completes this existing merge; no pull/merge/rebase/reset is needed. Do not use git add . because .vs and the local helper/report are intentionally not included.

## Follow-up: tab lifecycle and opportunity integration

Actual Chrome: Refresh succeeded on Student/MyApplications, Academic/StudentProgressMonitor, Coordinator/ContactMessages and Company/CompanyDashboard. Navigation succeeded for all four previously authenticated roles. Logout returned Student, Academic and Coordinator to Login without ending Company access. Company ManageOpportunities had an inactive logout link; tab-session now recognizes explicit logout controls/text and invokes server ticket revocation. Re-tested its actual Logout successfully; direct protected URL afterward was denied. Back showed Chrome form-resubmission protection, not protected content. In-process tickets intentionally expire on application rebuild/restart. A separate Admin browser account remains untested.

Company opportunity create/edit is now bound to the original Bootstrap modal, with existing programs loaded from Firebase. Popup open, required-input rejection and Cancel were browser-tested without writes. Save uses Gateway verified account, current account checks, immutable server-resolved CompanyId, typed allowlisted input, catalog checks, Jordan deadline checks, conditional ETags, a stable creation request ID, and preservation of existing policy fields. No caller UID, role, company or approval flag is accepted. Actual live save is pending Rules publication and browser test. The stored workMode convention is onsite/hybrid/remote; UI labels remain unchanged.

Latest Debug build passed before the final workMode compatibility adjustment; final rebuild is recorded separately below. Existing 104 C# checks and 12 JS checks passed again, plus 22 new isolated workflow security tests. User-supplied Emulator output confirms the initial 35 opportunity permission tests passed. Final review added audit-field deletion assertions and workMode schema compatibility; the final 38-check Rules version must run before any publication. Tests/Publish-WorkflowRules.ps1 defaults to verification only; -Publish reruns tests, backs up current live Rules, rejects unexpected changes, hash-checks the tested snapshot, deploys only that instance, and compares readback. User approved this specific addition after passing tests. Nothing has been published by the agent.

Changed in this follow-up: Auth/Gateway.ashx.cs; Services/FirebaseRealtimeDatabaseRepository.cs; Services/RolePortalService.cs; Js_project/Company/manage-opportunities.js; Js_project/Public/tab-session.js; root firebase.workflow.rules.json, firebase.workflow.baseline.rules.json, firebase.workflows.emulator.json; Tests/WorkflowSecurityTests.cs, Tests/workflow-rules.test.cjs, Tests/Publish-WorkflowRules.ps1. No ASPX/CSS design changed in this follow-up.

Remaining: live opportunity writes not yet verified; close/reopen, student application submission/placement confirmation, company application decisions, public contact submission/reply, and task/exam/report writes are still incomplete. Existing Cycle 3 STU/CMP IDs have not been guessed or relinked to DEMO IDs. The project must not be called fully ready for final merge until these operations are implemented and tested. Merge remains uncommitted; new follow-up files are unstaged for review.

Final follow-up rebuild: PASS. Recompiled final WorkflowSecurityTests: 22/22 PASS. Final two JS syntax checks and staged/unstaged diff checks PASS; no unmerged Git entries.
