# Account setup and password recovery

The coordinator creates student and academic supervisor accounts from university records. Company supervisor details are entered in Manage Companies. Confirm & Send creates the Firebase Authentication login and application profile, then asks Firebase to email a password setup link to that account's own email address.

The coordinator does not select or receive the user's password. Account provisioning generates an internal random password; the user chooses their own password through the Firebase email link. Temporary passwords are not returned in company form responses or shown in management forms. After setting a password, the user signs in with the same email and reaches their role's dashboard without a second password-change dialog.

Failed email delivery does not remove the saved account. Company forms provide Retry Send, which retries only the email. Supervisor account actions also allow resending the setup link. Students and academic supervisors can retry within their current creation session; Forgot Password remains available afterward.

Email must belong to the account being reset. Firebase Authentication requires a unique email per login. Emailing an unrelated test mailbox cannot reset a different student's or supervisor's password. Reserved example domains are rejected for setup email delivery. Configure a real university-record email for testing.

Company and company-supervisor phone fields are optional. When supplied, they accept ASCII digits 0–9 only, up to 40 characters. Leading zeros are preserved. Letters, punctuation, spaces within the number, and plus signs are rejected by both browser validation and the backend.

Firebase sends the password emails using the existing Firebase Web API configuration. Gmail SMTP credentials or App Passwords are not required. Firebase's configured password policy is authoritative; login does not impose a separate complexity regex.

Run `tests/AccountSetup/Run.ps1` for phone, link-only sign-in, and mocked Firebase email/reset checks. These tests do not send real email. Existing account lifecycle checks remain in `tests/ManageStudents`.
