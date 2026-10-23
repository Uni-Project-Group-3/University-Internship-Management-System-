# Company and Academic — approved local fixes (2026-10-23)

## Scope and limitations

Only local UI/orchestration fixes were approved. No production Factory was registered, no credentials or write flags changed, and no Firebase request/write was made. Existing unrelated working-tree changes (Student, Coordinator, Login/configuration) predate this change and are separate.

## Changes

- Company cards now bind through the shared page's virtual statistics callback to snapshots loaded by the authorized workflow. Unknown integrations/read failures retain em dashes; a verified empty set yields zero. Invalid/migration records do not produce misleading partial statistics.
- Company card labels and filter options match Not Started, Draft, Submitted, Revision Requested and Approved. Table heading is Actions.
- Integration unavailable, no authorized records, invalid records, denied access and read failure are distinguished. Messages do not assert that Firebase is empty or disconnected merely because a Factory is missing.
- Search has a result count and No Matches state for loaded rows. Clear resets/focuses search. Enter in search does not trigger a default evaluation postback. Loading is announced during form submission and reset on pageshow.
- Access denial clears every Academic percentage, timestamp, reason, editor, ticket and confirmation; all mutation controls are disabled. A failed read disables actions and does not display stale success/counts.
- Navbar and Sidebar markup/handlers are unchanged. Academic markup changed only the version of the existing interaction script.
- No grading policy, domain workflow, repository schema, identity mapping or Firebase Rules changed.

## Verification

- Full solution Debug build PASS.
- Automated suite: 213 PASS (210 existing plus authorized statistics, Company binding/unknown state, and complete Academic denial cleanup). Offline fixtures only in the existing test executable; no runtime fake records.
- New default-disabled Company/Academic postback suite: 17 PASS.
- Existing Company Actions default-disabled checks: 2 PASS.
- Existing v2 default-disabled checks: 3 PASS.
- Total targeted Web Forms checks executed: 22 PASS.
- Browser: both actual pages loaded on temporary IIS Express; search input, status selection and Clear were executed. Academic search Enter retained its value without a postback. No unauthorized rows were displayed. No populated modal, real persistence or No Matches with real rows was browser-tested because authorized data integration remains blocked.
- JS syntax and git diff whitespace checks PASS. Navbar/Sidebar markup and shared-script navigation handler segment compared with HEAD and unchanged.

## Still BLOCKED

Real Company/Academic reads and writes require the trusted PercentagePageIntegration.Factory composition, verified Firebase UID-to-business-ID/placement/assignment mapping, approved policy and authoritative concurrency guard. ADC is unavailable in the audited environment and live/schema write flags remain false. Reports/details callbacks are not connected. No production authorization can be inferred from a browser role or seed JSON. Save/Submit/Approve/Request Revision and refresh persistence on real Firebase have not passed a live test. Any proposed live write needs exact-path review and separate user approval.

## Manual checks

Open Company/ReportsFinalEvaluation.aspx and Academic/FinalAcademicEvaluation.aspx from the actual Visual Studio HTTPS origin and hard-refresh. Expect Supervisor access integration is unavailable and em-dash counts until a trusted adapter is connected. Confirm Company filter matches workflow statuses; input a search, change filter and Clear. Do not enable write flags or demo fixtures to make the empty page appear successful.
