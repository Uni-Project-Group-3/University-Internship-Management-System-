# Manage Companies

The coordinator enters all company and supervisor information manually. There is no official-record import or prerequisite university company dataset.

## Data and account model

- `companies/{companyId}` stores company name, industry, contact details, website, address, description, active status, revision and setup metadata.
- `users/{firebaseAuthenticationUid}` stores a company supervisor's name, email, phone, `role: companySupervisor`, `companyId`, `isActive`, `mustChangePassword` and setup status. Multiple supervisors can belong to one company.
- Firebase Authentication stores each supervisor's email/password login. Profile keys exactly match Authentication UIDs.
- `companyAccountOperations/{companyId}` stores a short operation lease and a recovery form for interrupted setup. It never stores passwords.
- Passwords are generated on the server, returned only in the current response, displayed in a temporary dialog, and cleared when it closes. No passwords are stored in Realtime Database, localStorage, sessionStorage or ViewState. Email is skipped in this version.

## Functions

Create/edit company and supervisor, add another supervisor, view details, activate/deactivate a company, enable/disable a single supervisor, and delete a supervisor login/profile. New passwords require a first-login change. Password reset controls and the company reset API action were removed at the user's request.

The company directory has real counts, search, independent company/account filters, sorting, pagination and refresh. Account deletion preserves the company, other supervisors and placements/history. Archive disables all linked supervisor logins and keeps records/history; restore returns the company as inactive without enabling logins. The default directory hides archived records; select Archived companies to view or restore them. Company status and supervisor login status are otherwise independent.

The existing company supervisor portal pages are outside this coordinator-page implementation; several still contain demo workflows. Do not represent the whole company portal as complete.

## Request security and recovery

Every page/API request resolves the Forms Authentication identity, checks the Authentication account is enabled, and reads the current coordinator database role. API mutations require a per-session CSRF header. Submitted company and user IDs are validated and linked against fresh database profiles; client-side roles are never trusted. Input lengths, email addresses and website schemes are validated on the server. API responses do not expose unrelated users or credentials.

All company mutations obtain an ETag-protected operation lease. Editing checks a revision to reject stale forms. Creating a new supervisor starts with disabled Authentication and a pending database profile. Interrupted provisioning can be resumed with the directory's recovery form; retries use the same request identity and rotate the temporary password safely. Completed duplicate requests do not create another account. A lost credential response requires administrator review; the original password cannot be retrieved from storage.

Disable and deletion make the database profile inactive before changing Authentication. Deletion is retryable when the Authentication account is already absent. Exceptions produce safe messages, never remote bodies/tokens.

## Validation

Project build and JavaScript syntax checks pass. Thirteen offline company workflow checks cover create/linkage, duplicate requests/emails, validation, lost Authentication response, resume, edits, stale revisions, independent statuses, rejection of the removed reset action, deletion/history preservation, archive/restore, cross-company protection and concurrent leases. Nineteen existing workflow checks also pass: 32 total.

An unauthenticated live API POST returned HTTP 403 with a coordinator-sign-in error. With explicit user approval, a disposable UIMS Company Test / uims-company-test-20261022@example.com supervisor was created and edited. Supervisor disable/re-enable, company deactivation, details, filtering/search/sorting/page-size controls, permanent supervisor deletion and company archive were verified live. A fresh directory response showed zero linked supervisors after deletion. The test company is archived and can be found with the Archived companies filter. No email was sent. Multi-page navigation and multiple-supervisor behavior were not exercised against live data; the backend multiple-supervisor and history-preservation paths passed offline tests.
