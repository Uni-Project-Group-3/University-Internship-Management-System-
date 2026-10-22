# Local fictional Firebase demo — 22 October 2026

This is the current testing guide. Older milestone reports are historical. Real authentication and team integrations remain unavailable; official UI operations remain disabled. No university passing threshold, hours requirement or rejection rule is inferred: the demo explicitly uses a fictional policy (70%, 10 approved hours, rejection enabled).

## Open it now

The agent launched a loopback IIS Express instance at **http://localhost:61877/Company/ReportsFinalEvaluation**. Open this in your own browser. A new browser session receives its own fictional dataset. Keep all four role pages in the same browser session; do not use a different private window for each role.

The page must display **LOCAL-ONLY Firebase DEMO** and `_devTests/roaaFinalEvaluationDemo/v1/sessions/{id}`. If it says official operations disabled, follow the launch instructions below. Never enable real production factories with a demo identity.

## Start through Visual Studio

1. Close previous Visual Studio and IIS Express instances for this solution so the new process inherits its environment.
2. From the repository root, run Windows PowerShell:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\University-Internship-Management-System\Tests\FinalEvaluation\StartFinalEvaluationDemo.ps1 -HostMode VisualStudio
```

3. Select **Debug** and **IIS Express**. Right-click `Company/ReportsFinalEvaluation.aspx`, choose **Set As Start Page**, and press F5. Use the port Visual Studio opens.
4. The script only changes the launched process environment. It checks credential-file accessibility without printing contents. Server composition additionally checks the exact external credential/project and development Firebase host. Windows User/Machine settings and Web.config authentication are unchanged.

Alternatively, after a Debug solution build, start hidden loopback IIS Express:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\University-Internship-Management-System\Tests\FinalEvaluation\StartFinalEvaluationDemo.ps1 -Port 61877
```

Use a different free port if already running. The script prints the exact PID to stop. It does not kill other Visual Studio processes. To disable, stop that printed IIS process or close the demo Visual Studio instance, then launch normally without `UIMS_FINAL_EVALUATION_FIREBASE_DEMO=1` or `UIMS_FINAL_EVALUATION_LOCAL=1`. Release/non-loopback requests cannot activate this composition.

## Test each page

Paths below use the same host and port as Company. The role-specific demo composition is server controlled, scoped to one owned fictional dataset; it is not production login.

### Company — `/Company/ReportsFinalEvaluation`

1. Initial new session: Lina has a partial Draft, Omar is Submitted, Noor has an Academic approval. All are fictional.
2. Search `Omar`; only Omar remains. Clear filters. Select Draft; only Lina remains. Clear filters.
3. Lina → Actions → View Reports. Check the fictional report, close with X. View Details shows 10 approved / 10 required fixture hours, not 60 (50 pending hours are excluded).
4. Continue Draft. Enter a percentage and comments; Save Draft. Wait for **Saved and confirmed by the repository**. Refresh, reopen, verify both values. Save again to update.
5. Try 101 or nonnumeric input. Browser validation/server validation must prevent saving; server errors preserve posted values. Partial Drafts are allowed; final submission requires a saved valid percentage and comments.
6. Save valid values, check confirmation, Submit Evaluation. Submitted reloads read-only. Repeated/stale submissions cannot create a second archived version.
7. Omar → Actions → View Evaluation: submitted, locked. Do not expect Continue Draft for a Submitted record.
8. After Academic Request Revision, Lina's action becomes Revise Evaluation. Save creates a new Draft version, preserving prior submission/review. Save before confirming Submit again.

### Academic — `/Academic/FinalAcademicEvaluation`

1. Search/filter/clear work like Company. Actions → View Company Evaluation and Review Evaluation load the selected submitted Company percentage/comments. View Student Details is read-only.
2. There is no independent Academic percentage. The displayed Company percentage is read-only; forged posted values cannot change it.
3. Request Revision with confirmation but no reason: error. Enter a fictional reason, confirm, Request Revision: success only after Firebase acknowledgment. Close/reopen to view the recorded decision.
4. Return to Company, revise/save/submit. Return to Academic and approve this **new version** with confirmation. A prior review cannot approve a later version. Already-reviewed decisions are locked.
5. Close buttons cancel the modal without saving. Declining confirmation prevents a decision. Shared account/notification controls are not authorization integrations.

### Coordinator — `/Coordinator/FinalCompletionReview`

1. Select each student, use navbar search to narrow authorized options, clear the search, and Refresh readiness. Check the correct student ID/internship and read-only Company and Academic information.
2. Lina before Academic approval is blocked. Noor initially is ready. Approved hours are 10; pending 50 are not counted. Missing requirements/policy/eligibility block readiness.
3. Select Noor. Check acknowledgment → Approve completion → Keep reviewing: no decision. Reopen → Confirm approval. Wait for the persisted Approved status and audit history.
4. Generate / Verify Completion Report PDF is a separate action after Approved. It creates a real private PDF and scoped Firebase metadata. Repeated generation reuses verified existing bytes. No certificate/public URL is created.
5. Use Omar to test Request Changes: reason and acknowledgment are mandatory; open confirmation, Cancel, then confirm. Refresh verifies persisted history.
6. The explicitly fictional demo policy permits Reject. Enter a reason, acknowledge, open Reject confirmation and confirm. Rejected is terminal; its earlier history remains. Production rejection is blocked unless the actual approved policy explicitly permits it.
7. Request Changes does not unlock a Company evaluation: only the Academic version-specific revision request does.

### Student — `/Student/MyProgressEvaluation`

1. LOCAL DEMO Student selector A/B/C switches only among this dataset's fictional owners. Choose the student approved above.
2. Confirm approved hours, completion date/status and policy-permitted Pass/Fail. Company comments, percentages, Academic reasons and Coordinator decision reasons must never appear.
3. All tasks shows two visible tasks. Evaluated shows the published 4/7 task; Submitted shows a task with no published grade. Other filters show empty states as appropriate. Hidden tasks/private feedback remain absent. Expand/collapse Submission & evaluation history: missing trusted history is stated honestly.
4. Generate the report on Coordinator first, then return/refresh. Download Completion Report returns a PDF attachment only for its authorized owner after Approved and integrity verification. Before approval, without a report, or for a Rejected student it is unavailable.
5. Print is intentionally disabled; download and print in your PDF reader. Exam results and detailed task history remain blocked pending trusted adapters; no exam/demo grades are invented.
6. Dashboard/My Tasks/sidebar links belong to other team modules. Their authentication/data behavior is outside this isolated test; do not use them as evidence that this module is authenticated.

## Persistence and reset

Every demo read/write uses only `_devTests/roaaFinalEvaluationDemo/v1/sessions/{id}` in `uims-roa-a-development`. The ID is generated server-side and protected with MachineKey plus the ASP.NET session cookie. It is not an arbitrary URL selector. Seed is idempotent and never overwrites an existing owned dataset. Refresh and navigation preserve saved records. A separate browser session receives a separate dataset. On application/session loss, protected-cookie recovery depends on stable MachineKey and session ID; a new session safely creates a new dataset rather than impersonating an old one.

Firebase persists evaluation versions, reviews, completion decisions/audit and issued PDF metadata. PDF bytes are private session memory, so after a restart explicitly Generate / Verify again; completion remains Approved and prior metadata is preserved. Production needs persistent private storage and trusted team adapters.

Inspect the exact GUID displayed on your page:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\University-Internship-Management-System\Tests\FinalEvaluation\FirebaseDemoDataset.ps1 -Mode Inspect -DatasetId YOUR_32_HEX_GUID -EnableDevelopmentDemoOperations
```

For a deliberate reset, use the same command with `-Mode Reset`. It requires typing **DELETE {exact GUID}**, validates the owner/schema and exact ETag, deletes only that dataset, verifies deletion, and reseeds it. No parent delete or official record operation exists. Do not reset while other tabs are writing; refresh afterward. **The agent did not execute any live Reset.**

## Verification limits

See `CONTROL-AUDIT.md` for browser versus HTTP/unit coverage and outstanding controls. No mobile viewport/emulation capability was exposed by the connected browser; mobile menu/layout/touch checks remain manual. To check manually, use browser DevTools at 390×844 and 768×1024: open/close Sidebar/backdrop/Escape; scroll both modals and tables; reopen dropdowns; confirm all dialog footer buttons remain reachable. No mobile PASS is claimed.
