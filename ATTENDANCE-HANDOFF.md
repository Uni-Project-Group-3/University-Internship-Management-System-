# UIMS QR Attendance and Weekly Reports — Final Handoff

Status date: 23 October 2026, Asia/Amman.
Project: `just-project-4af15`. Keep Firebase on **Spark**. No billing upgrade, paid service, Firebase Storage, Rules, or IAM change is authorized by this handoff.

## Verified status

| Area | Evidence and limit |
| --- | --- |
| Real attendance cycle | Student `2023105`, existing placement `PLC-APP-TEST-2023105`, assignment `TA-2023105`, assigned supervisor `company@example.invalid`: real login, QR issuance/display, physical camera check-in, actual elapsed time, physical checkout, and supervisor hours approval completed. |
| Real attendance result | 23 October: check-in 20:49, checkout 20:54; 5 whole elapsed minutes approved. Both QR states consumed. Total 2405 minutes = 40.08 h; remaining 259.92 h; required 300 h. Student page, Firebase, and report calculation agree. |
| Weekly report calculation | Current C# report calculation read real Firebase and returned 2405 minutes for this placement. This does not prove a saved report or DOCX download. |
| Private DOCX alternative | Implemented and locally configured on disk outside the website; no Base64 in RTDB. Root and restrictive Windows ACL verified. Current real DOCX count: **zero**. |
| Report UI text | User's actual reports tab refreshed; UTF-8 confirmed, broken characters absent, old Firebase Storage wording absent. |
| Automated tests | Build passed; 206 existing/new C# suite tests, 55 attendance integration tests, and 39 JavaScript tests passed. Tests used isolated data, including temporary private disk files; they did not create production reports. |
| Pending | Real DOCX upload/download, saved report details as assigned academic supervisor, real early checkout request, real expiration/replay/concurrency rejection cases, network/phone HTTPS deployment, and complete eligibility for all students. |

No report is to be fabricated. Submission remains pending because no genuine Word report exists yet. Historical attendance was not deleted/reset to rerun the cycle. The previous day's pending approval was not approved.

## Changed files

Inventory of changes made during this implementation, based on the session's work and integration log; no Git command or Git diff was used.

Paths below are relative to:
`C:\Users\USER\Desktop\University-Internship-Management-System-`.

| File(s) | Purpose |
| --- | --- |
| `University-Internship-Management-System/Services/Attendance/AttendanceIntegration.cs` (new) | Academic supervisor resolution, durable/idempotent placement provisioning, verified write scope and HTTPS host validation. |
| `University-Internship-Management-System/Services/Attendance/AttendanceFirebase.cs` | Persisted Firebase mapping and eligibility access. |
| `University-Internship-Management-System/Services/Attendance/AttendanceService.cs` | Binding/placement validation, roster failure isolation, actual elapsed minutes and attendance authorization integration. |
| `University-Internship-Management-System/Services/ApplicationWorkflowService.cs` | Validated confirmation/retry provisioning, compatible existing assignment reuse. |
| `University-Internship-Management-System/Services/WeeklyReportService.cs` | Placement-scoped approved minutes, academic consistency, roster isolation, private disk provider selection and per-revision provider compatibility. |
| `University-Internship-Management-System/Services/PrivateReportFileStore.cs` (new) | Private DOCX persistence, restricted paths/ACL, create-only writes, quota and cross-process upload lock. |
| `University-Internship-Management-System/Student/MyAttendance.aspx`, `MyAttendance.aspx.cs`, `Js_project/Student/my-attendance.js` | Student attendance page, backend integration and authenticated session handling. |
| `University-Internship-Management-System/Company/VerifyAttendance.aspx`, `VerifyAttendance.aspx.cs`, `Js_project/Company/verify-attendance.js` | Supervisor attendance page and authorized operations. |
| `University-Internship-Management-System/Company/WeeklyReports.aspx` | Explicitly authorized text/UTF-8 fix only; report submission logic unchanged. |
| `University-Internship-Management-System/University-Internship-Management-System.csproj` | Compile includes for added services. |
| `University-Internship-Management-System/Firebase.Local.config` | Local attendance/report configuration and private report folder; not a portable production configuration. |
| `Tests/AttendanceIntegration/AttendanceIntegration.Tests.csproj`, `AttendanceCycleTests.cs`, `IntegrationTests.cs`, `student-session.cjs`, `company-session.cjs` | Attendance/security/integration tests, including explicit file-provider fixture settings. |
| `Tests/FinalEvaluation/Program.cs`, `WeeklyReportTests.cs`, `PrivateReportFileTests.cs` (new), `FinalEvaluation.Tests.csproj` | Test registration, validated report fixtures, private disk/auth/integrity/quota tests. |
| `ATTENDANCE-INTEGRATION.md`, `ATTENDANCE-HANDOFF.md` | Detailed work log and this final handoff. |

Login, shared Sidebar/Dashboard pages, Firebase Rules and IAM were not modified. No Git operations, Commit or Push were performed. User-performed password recovery for the same student 2023105 account preserved its UID; no password is included in this handoff or its backups.

## Firebase changes and backups

Earlier verified mapping additions:

- `uimsAttendance/students/2023001/studentNumber` and `uimsAttendance/students/2023105/studentNumber`.
- `uimsAttendance/provisioningClaims/2023001` and `uimsAttendance/provisioningClaims/2023105`.
- Real test writes, including QR lifecycle and approval, were confined to `uimsAttendance/attendance/TA-2023105/2026-10-23`.

IDs, Auth UIDs, existing assignments, schedules, and earlier approved hours were preserved. The authorized real test added 5 approved minutes; it is retained, not silently cleaned up.

Backup/artifact base:
`C:\Users\USER\.codex\visualizations\2026\10\20\01a159aa-008d-7033-90a8-04e8e45a70df`.

| Location under that base | Contents |
| --- | --- |
| `uims-firebase-backups-20261023/firebase-before-repair.json` | Exact affected mapping paths, old values and ETags before additions. |
| `uims-firebase-backups-20261023/firebase-before-qr-test.json` | Test attendance path before earlier QR issuance. |
| `uims-e2e-20261023/before-checkin-20261023-174317.json` | Fresh pre-check-in path snapshots. |
| `uims-e2e-20261023/before-checkout-20261023-175104.json` | Fresh pre-checkout path snapshots. |
| `uims-e2e-20261023/before-approval-20261023-175537.json` | Fresh pre-approval path snapshots. |
| `uims-e2e-20261023/Firebase.Local.before-private-reports.config` | Local configuration before enabling private report writes. |
| `uims-e2e-20261023/checkin-confirmed.png`, `checkout-approved.png`, `user-report-tab-refreshed.png` | Actual UI proof. |
| `uims-recovery-20261023/before-password-metadata.json`, `result.json` | Safe account-recovery metadata/result, no password or access token. Standalone recovery utility in that folder is an administrative tool, not part of the web deployment. |

These are scoped path backups, **not a complete database backup**. Protect backups containing student metadata. Do not publish them with the website or add private data/credentials to source control. Restore must be reviewed and conditional on current data; never import an entire old snapshot over newer attendance.

## Unresolved student mappings

Two tested/verified attendance identities: `2023001` and `2023105`. Only 2023105 completed the new physical end-to-end cycle described above.

| Student(s) | Confirmed blocker / required resolution |
| --- | --- |
| `2023301` | The uniquely verified existing Auth UID was repaired in placement/student/application UID references, with conditional writes and backups; all other IDs and historical attendance stayed unchanged. Academic mapping still conflicts: current coordinator assignment names Dr. Rania Haddad, while legacy assignment and placement name Dr. Dana Al-Qudah. Coordinator must decide the authoritative academic supervisor; no supervisor was guessed. This student is still unfinished. |
| `2023005`, `2023006`, `2023007`, `2023101`, `2023104`, `2023106`, `2023107`, `2023201`, `2023204`, `2023205`, `2023206`, `2023207`, `2023304`, `2023305`, `2023307` | No enabled Auth account/profile binding matching their referenced attendance identities was established from the existing records inspected. Team lead must supply or reconcile the approved existing identity and actual placement/academic relationships; do not guess UIDs or create duplicate accounts. |

Review exact paths before changing anything: `users/{verifiedUid}`, `placements/{placementId}/studentUid`, `uimsAttendance/students/{studentId}/firebaseUid`, current coordinator assignment and legacy academic assignment. Check the original application relationship as well. Roster visibility alone does not establish full student login/attendance eligibility.

The fresh complete audit covers **27** existing student IDs. The single current per-student conflict/decision list is [STUDENT-READINESS.md](C:/Users/USER/Desktop/University-Internship-Management-System-/STUDENT-READINESS.md), including unplaced students beyond the original 16. Six have a unique enabled identity; two currently pass complete attendance setup. Student 2023002 has an accepted offer starting 1 December 2026 but no approved calendar; the actual confirmation code correctly rejects the real-data read-only preflight before writes. A genuinely newly confirmed placement and its physical QR cycle remain unverified. Do not call the overall integration complete.

Verified UID repair backup: `C:\Users\USER\UIMS-Data\Backups\before-2023301-uid-repair-20261023-184816.json`; result: `2023301-uid-repair-result.json` in the same folder. Fresh sanitized audit snapshot: `student-readiness-after-uid-repair-20261023.json` in that folder. This audit/repair did not create Authentication accounts or alter approved hours.

## Private file persistence and free deployment requirements

**What is confirmed:** the current folder exists outside the project and website, with inheritance disabled and access restricted to the current Windows user, SYSTEM and Administrators. A normal project build/publish does not contain or copy it. The application does not automatically erase existing reports on restart.

Current folder:
`C:\Users\USER\UIMS-Data\PrivateReports`.

**Permanent folder migration completed:** the former Codex visualization folder was moved to the path above. ACL now explicitly permits the verified IIS Express Windows account (USER), SYSTEM and Administrators only, with inheritance disabled. The old folder no longer hosts report storage. A previous ACL was tied to the sandbox inspection account; this was corrected for the real IIS identity during migration.

**Actual restart persistence verified:** IIS Express for port 55448 was stopped and relaunched (PID 17848 → 2700), Login returned HTTP 200, and a random non-report probe file retained its exact SHA-256. The probe was then deleted. No DOCX or Firebase report was created. Evidence: `C:\Users\USER\UIMS-Data\storage-restart-verification.json`; pre-migration configuration: `C:\Users\USER\UIMS-Data\Firebase.Local.before-permanent-storage.config`. The configured root was separately validated under the actual IIS Windows identity.

**What cannot be guaranteed yet:** automatic persistence on a different deployment. This folder is now operator-managed outside Codex/project directories, but is still on this PC's disk. No remote production hosting or restore test exists. Publishing the website to another machine will **not** transfer the folder or its files. The migration and backup gates below still apply to that deployment.

Use the following deployment gates before claiming production readiness:

1. Existing Windows server/PC with IIS and ASP.NET/.NET Framework 4.7.2 support; static hosting or a Linux-only host cannot directly run this Web Forms application. No free cloud host has been selected or verified. Using existing hardware/private disk avoids Firebase Storage charges; it does not promise free third-party hosting.
2. A permanent, operator-managed NTFS folder outside the webroot/deployment directory (not temp or an ephemeral/free-host filesystem). Set `WeeklyReportPrivateRoot` to its absolute path.
3. Give only the actual IIS application-pool Windows identity, SYSTEM and administrators the required folder access. Keep inherited/broad ACLs disabled. Copy all existing report files to the new private folder, preserving content. Current implementation expects one server and a persistent local disk, not multiple independent instances or a UNC share.
4. Back up private files **together with RTDB report/revision metadata** to a separately retained private location; configure retention, disk monitoring and a tested restore procedure. The application does not provide an automatic off-machine backup. Default quota is 100 MiB total, 10 MiB per DOCX; failing quota/ACL/path checks must leave uploads unavailable, never fall back to public storage.
5. Configure local deployment settings: `WeeklyReportFileProvider=PrivateDisk`, `WeeklyReportPrivateDiskWritesEnabled=true`, `WeeklyReportsLiveWritesEnabled=true`, a valid persistent root and quota. Firebase Storage flags remain false. Do not overwrite the deployment-specific configuration with this PC's local path.
6. The IIS identity must have existing authorized Firebase credentials and access to the external QR signing key, provided securely outside the site. User ADC on this PC does not automatically transfer to a different IIS identity. Do not deploy the recovery tool, keys or ADC files as web content.
7. Trusted HTTPS hostname/certificate reachable from phones, an IIS HTTPS binding and authorized network access. Configure `AttendanceAllowedHttpsHosts` to exact approved hostname(s); it is currently empty. Do not bypass certificate validation, trust arbitrary forwarded headers, or expose HTTP login. Configure secure session/auth cookies for the HTTPS deployment. Local `https://localhost:44372` succeeded on this PC, but remote phone access remains unverified.
8. Post-deployment: verify the private folder is inaccessible by any static/public URL, restart/redeploy without deleting it, then confirm an authorized real report download still matches the stored hash. Until this passes, folder persistence after deployment is a requirement, not a verified result.

## Resume the genuine report test

Do not submit until a real DOCX exists with the correct training week and dates.

1. User supplies the file path, week number, week start and end (no invented content/dates).
2. Sign in as `company@example.invalid` and open `http://localhost:55448/Company/WeeklyReports`; choose `PLC-APP-TEST-2023105` for Zaid Mahmoud Al-Sharif, student 2023105.
3. Back up the exact placement-week report path if it exists; do not overwrite an existing report or revision. Submit the real Word file via the form. Expected new paths are `weeklyReports/{stablePlacementWeekId}`, `fileAttachments/{newFileId}` and a new private disk object. No attendance hours should change.
4. Verify upload confirmation, private file integrity and matching RTDB metadata. Download using the assigned company supervisor.
5. In a separate session, user signs in as the verified assigned academic supervisor `academic.cyber.1@example.invalid`, opens `/Academic/WeeklyReports`, and opens the new report details. Verify 2405 approved minutes / 40.08 h and download the actual DOCX. If new attendance has been approved meanwhile, reconcile the new expected total instead of forcing 40.08.
6. Test rejection of an unrelated supervisor without bypassing authorization. Leave genuine submitted reports/history intact.

Final state: real attendance cycle passed; genuine report submission/download remains **pending by user instruction**. Spark and no paid services remain unchanged.

## Final independent code pass — separated from data and deployment

**Code:** no known unresolved attendance/report backend defect remains in the reviewed/tested scope. This is not a claim that every account or production deployment is ready. A confirmed placement with verified unique identity, active matching company/supervisors, approved dates/hours and a valid pre-published calendar is automatically provisioned by `ConfirmOfferAsync`; actor/assignment/claim mappings persist, compatible IDs are reused, and retries recover partial writes. The code intentionally does not invent or automatically approve calendars, accounts or supervisor relationships.

One final independently reproducible defect was corrected: already-complete actor and assignment data could cause an early return even when the durable provisioning claim was missing. `AttendanceIntegration.cs` now includes the claim in its completeness check, restoring only the validated missing claim through conditional writes when confirmation runs. No live claim repair or other Firebase write was executed for this final code change.

The isolated full-cycle test now starts from an **accepted application without a placement**, rather than only retrying an already confirmed placement. It confirms the offer, persists the mapping, checks supervisor visibility, issues/scans QR, waits an actual minute, checks out, approves minutes and verifies report calculation. Added tests cover a missing calendar before any write, recovery of interrupted new confirmation stages without duplicate IDs/history changes, and restoration of a missing claim. The fake database now uses per-path content ETags rather than a global revision, matching the conditional-write behavior relevant to this flow.

Final checks passed: Build, **58 attendance integration tests**, **206 application/placement/report/storage/regression C# tests**, and **39 JavaScript tests**. These are isolated tests, not a new live placement. Read-only Firebase recheck: 2023105 remains 2405 min / 40.08 h; 2023001 remains 2402 min / 40.03 h; report count remains zero. Existing MyApplications code/JS bridge was inspected read-only and calls `ApplicationWorkflowService`; no unrelated application page/style was changed.

**Data decisions:** the current exact per-student list is `STUDENT-READINESS.md`. Short blockers: 2023301 needs the authoritative academic supervisor selected (Rania vs Dana); remaining unmatched Auth/profile relationships need approved identity evidence; new placements need their actual approved calendar. The December offer is not used or modified for today's live test. The prior genuine 2023105 cycle is the live current-placement evidence; it was not reset/repeated to manufacture another attendance day.

**Deployment/report tests:** phone HTTPS is unfinished; cross-machine persistent-folder deployment/restore is unfinished; genuine DOCX upload/download and academic report display remain pending. Local process-restart persistence has passed separately. Spark stays unchanged with Firebase Storage, Rules, IAM and paid services untouched.

This final code pass changed only `Services/Attendance/AttendanceIntegration.cs`, `Tests/AttendanceIntegration/IntegrationTests.cs`, and handoff/readiness documentation. The earlier three-field verified Firebase correction and its exact backup remain documented above; no additional Firebase writes were made by the tests or this code pass.

## Latest recheck and free HTTPS preparation

The real 27-student audit was repeated after the previous code pass. Results are unchanged: 21 without a proven enabled Auth mapping, one academic conflict (2023301), one accepted future offer without an approved calendar (2023002), two enabled but unplaced/unassigned students (2023003, 2023306), and two complete existing setups (2023001, 2023105). Exact student IDs, source paths and decisions are consolidated under **Current grouped blockers** in `STUDENT-READINESS.md`. No additional correction was supported by records, so this recheck made no Firebase writes and requires no new before-write backup.

Automatic provisioning is verified for attendance actor/assignment/claim creation on accepted-offer confirmation, with existing approved calendar as a prerequisite. It does not require manual edits of those generated mappings. It does **not** create or approve an unspecified schedule, and no schedule-management UI was added. Thus entirely automatic onboarding without an approved calendar input/publication step is not verified or claimed. The December dates remain untouched, and its offer is not used for today's live attendance.

Added deployment-only files, without editing live shared settings/pages:

- `Deployment/Attendance/Test-Readiness.ps1`: read-only configuration/ACL/IIS/HTTPS preflight, optional normal-certificate-validated Login GET, no installs or Firebase calls.
- `Deployment/Attendance/Https-settings.example.xml`: review-only fragment for a **deployment copy**, never blindly replace Web.config or apply Secure-cookie settings to the current HTTP localhost site.
- `Deployment/Attendance/README.md`: no-paid-service operator runbook and precise access requirements.

PowerShell AST and XML parsing passed. Running the checker as the actual IIS Express Windows user passed project identity, disabled Firebase Storage, private-disk provider and protected permanent-folder ACL checks. It correctly reports blocked prerequisites: full IIS is absent, no approved DNS hostname/HTTPS binding is configured, localhost cookies are not configured as HTTPS-only, and the remote attendance hostname allowlist is empty. No ports, URLACLs, certificates, firewall, tunnels, Rules, IAM or paid services were changed. No production hostname or free external Windows hosting is assumed.

To finish phone HTTPS, the team must provide its approved Windows host/operator access and DNS hostname/control, plus authorization for the intended network reachability. A free ACME certificate still needs domain validation and renewal setup; backend must actually see HTTPS. Current prepared files do not establish working phone access. No private keys, DNS passwords or credentials should be sent in chat.

Only deployment preparation and documentation changed in this recheck; the previously passed 58+206 C# and 39 JavaScript checks remain the latest backend regression evidence. Genuine report testing stays pending until a real Word document is supplied. **Overall integration remains unfinished.**

## Coordinator hold and account/calendar checklist

User has deferred the academic decision for 2023301: leave it unchanged from this point. The single actionable team checklist is now at the end of `STUDENT-READINESS.md` under **One team checklist**. Further independent C# calendar validation against the latest real metadata snapshot found valid existing calendars for all 15 other already-placed students with unresolved identity bindings (20/52 entries each, zero structural errors, and a 2026-10-23 entry). These calendars must be preserved, not recreated or copied; human approval and future-day completeness remain distinct checks. New calendars are needed for the accepted 2023002 offer and, after official placement setup, the eight other unplaced students. No Firebase, Auth, schedule, date or approved-hours changes were made while preparing this checklist. Spark and no-paid-service restrictions remain in force.
