# University Internship Management System

## Current integration status (21 October 2026)

Completed portal pages keep sidebar/navbar markup in their ASPX files. New deayy and roaa pages keep their layout CSS in their own stylesheets; colors.css remains the token source. Older role test pages and public template pages still have legacy shared-layout dependencies and are not evidence of a completed standalone migration.

The checkout contains 38 ASPX files. Local route/asset and JavaScript syntax audits pass, but this is not an end-to-end authenticated system. Firebase sign-in, trusted roles, database CRUD and rules, cross-role synchronization, official grading, PDF certificates and durable deactivation scheduling remain unavailable. The owner confirmed there is currently no Firebase database or schema. Password-recovery dispatch requires configuration; it does not provide login authorization. Do not deploy the preview portals as protected production pages.

The final QA pass could build the solution and statically inspect routes, IDs and handlers. Live browser verification was unavailable because local browser use was disabled in Work settings. Prior per-page UI results do not establish cross-role or Firebase functionality.


ASP.NET Web Forms application targeting .NET Framework 4.7.2. Open
`University-Internship-Management-System/University-Internship-Management-System.slnx`
in Visual Studio with the ASP.NET and web development workload installed.
Restore the existing NuGet packages before building.

## Structure

The web project is inside
`University-Internship-Management-System/University-Internship-Management-System/`.

```text
Academic/                 Academic Web Forms
Company/                  Company Web Forms
Coordinator/              Coordinator Web Forms
Student/                  Student Web Forms
Public/                   Public Web Forms
Shared/
  Sidebar.ascx            Shared role sidebar
  Sidebar.ascx.cs          Sidebar presentation logic
  Sidebar.ascx.designer.cs Server-control declarations
  HomeNavbar.ascx         Home navigation markup
Assets/
  academic/academic.css   Role-specific styles
  company/company.css
  coordinator/coordinator.css
  student/student.css
  public/public.css
  shared/
    colors.css            Single shared colour palette
    sidebar.css           Workspace defaults and sidebar layout
    navbar.css            Home navigation styles
    images/               Shared images
Js_project/               Existing per-role JavaScript placeholders
App_Start/                Routes and bundle registration
Content/                  Existing Bootstrap and template styles
Scripts/                  Existing framework scripts
App_Data/                 Existing application data folder
Properties/               Assembly metadata
Default.aspx              Home page
About.aspx                Existing About page
Contact.aspx              Existing Contact page
Site.Master               Main template; shows navigation only on Home
Site.Mobile.Master        Existing mobile template
ViewSwitcher.ascx         Existing desktop/mobile switch
Global.asax               Application startup
Web.config                Framework and runtime settings
Bundle.config             Existing CSS bundles
packages.config           Existing package dependencies
```

Every ASPX page retains its code-behind, designer, class name and server-control
IDs. Pages with no presentation logic have an empty partial class rather than an
unused `Page_Load`. Keep implemented code in this order: imports, class, private
fields, `Page_Load`, event handlers, main methods, helper methods. Omit empty
sections. Generated designer declarations remain separate.

## Shared UI

Role pages load `colors.css`, `sidebar.css`, then their own stylesheet. Use the
canonical variables such as `var(--primary)` and `var(--background)`; colour
values belong only in `colors.css`. Shared workspace defaults use `.app-page`.

`Sidebar.ascx` accepts `HomeUrl` and `UserRole`. It displays the role as encoded
text and derives the active dashboard state from the actual page virtual path,
including when Friendly URLs are used. New role navigation should target real
pages. The sidebar is presentation only; it does not grant access to a role.

`Site.Master.cs` controls Home navigation visibility on every request. All
existing URLs and page content are retained.

## Firebase status

Password reset uses the single shared `Shared/FirebaseAuthService.cs` service.
It calls Firebase Authentication from the server with a reused `HttpClient`.
Sign-in, role authorization and Realtime Database integration are still not wired.
No database records were contacted or modified.

Before implementing a data-backed feature, supply the Realtime Database URL,
existing record paths and the intended Firebase authentication flow. Implement
only the operations that feature needs in one shared service using a reused
`HttpClient`. Keep credentials outside source control, use the authenticated
user's token and honour Firebase security rules. Do not make the database public
to enable access.

Data-backed pages should use `Async="true"` and register a `PageAsyncTask` for
asynchronous work. Validate input before writing, prevent duplicate submissions
where applicable and show a safe error message without exposing credentials.
The password reset handler uses asynchronous page operations and server input
validation; database submission handlers have not been implemented.

## Refactor scope

- Removed unused imports and no-op handlers from the eight ASPX code-behind
  files and the mobile master. Kept all page/control identities.
- Moved sidebar logic into its compiled code-behind; moved home navigation
  visibility into the existing master code-behind.
- Simplified application startup, route/bundle registration and ViewSwitcher
  without changing the configured routes or bundle names.
- Replaced repeated role body styles with one shared rule and retained one
  stylesheet per role. Expanded compressed shared CSS for readability.
- Removed the six empty `Assets/{academic,company,coordinator,public,shared,student}/test.css`
  placeholders. Student now uses `Assets/student/student.css`.
- Updated project content/compile entries and code-behind filename casing.
- Added no packages, repositories, interfaces, database frameworks or services.

## Validation

Build from a Visual Studio Developer PowerShell:

```powershell
msbuild .\University-Internship-Management-System\University-Internship-Management-System\University-Internship-Management-System.csproj /t:Build /p:Configuration=Debug
```

The refactor was checked with MSBuild, ASP.NET markup precompilation, project/file
reference checks and local IIS Express HTTP smoke tests. There is no automated
unit test suite or Firebase integration test in this checkout. Live Firebase
email delivery requires Firebase configuration; sign-in integration is still missing.


## Login page implementation status

`Auth/Login.aspx` is a standalone, responsive Web Forms login page with its
code-behind and designer. It loads the existing shared colour tokens, then
`Assets/auth/auth.css`; interaction code is in `Js_project/Auth/auth.js`.
The central `colors.css` is unchanged. No other page was redesigned.

The page includes separate visibility controls for all three password fields,
client/server input checks, a CSRF nonce, cache prevention, duplicate-submit
protection in the browser, and an accessible native first-login dialog. Password
feedback checks a 12-character minimum with uppercase/lowercase letters, a number
and a symbol; Firebase project policy still needs to be reconciled during wiring.

**Sign-in integration is blocked:** the checkout has no sign-in authorization
service, role records or first-login enforcement architecture. Login and first-login
handlers do not create authenticated sessions, grant dashboard access or claim
a password was changed. Password recovery is implemented separately below. The modal can read a server-owned pending-change flag,
but there is currently no authentication service that sets it. Existing dashboards
remain as they were; they are not secured by this page alone.

To finish the requested working authentication flow, provide the Firebase project
ID, Web API key through local configuration, Realtime Database URL, existing user
record paths and field names, role/dashboard mapping, password policy and the
trusted server operation that clears the required-change state. Never place a
service-account private key or an account password in source control or chat.
Authentication and first-login enforcement must be implemented and integration
tested before deployment.

### Login image credits

- `Assets/auth/just-campus.jpg`: **Jordan University of Science and Technology's
  Library**, Fawwaz Dawod, 2013. Source:
  https://commons.wikimedia.org/wiki/File:Jordan_University_of_Science_and_Technology%27s_Library.jpg
  License: **CC BY-SA 3.0**, https://creativecommons.org/licenses/by-sa/3.0/.
  Original file retained without pixel edits; the page displays it with a
  responsive crop and CSS overlay. Credit and license links appear on the page.
  Any adaptations of this photograph must retain the applicable share-alike terms.
- `Assets/shared/images/just-logo.png`: official JUST emblem downloaded from
  https://www.just.edu.jo/assets/common/images/just-logo-mob.png?rev=44
  for identification of the university in this requested university application.
  The emblem remains the university's property; the photograph's Creative Commons
  license does not cover the emblem or imply university endorsement.

Login validation completed: Debug build and ASP.NET markup precompilation passed;
JavaScript syntax and behaviour checks passed for independent eye toggles,
strength/matching feedback, loading and duplicate-submit prevention, and dialog
Escape prevention. Browser checks at 1440, 900 and 390 pixels showed no horizontal
overflow. Local HTTP checks passed for server email validation, CSRF rejection,
configuration-error handling, all existing pages and new assets. Firebase login, password changes and role authorization remain unimplemented.
Password reset dispatch is implemented; live delivery needs server configuration.


## Forgot Password popup

The existing Login page now has a native, accessible reset dialog. It validates
email on both client and server, submits asynchronously to the same page, disables
duplicate submissions and restores focus after Close, Back to Login, Escape or a
backdrop click. First-login dialog behaviour and login handlers are preserved.

Set **FIREBASE_API_KEY** in the web server process environment to the intended
Firebase project's Web API key, then restart IIS Express/the app pool. No key is
stored in the repository. Enable and configure the project's Firebase password
reset email template and hosted email action handler. The service calls only
`accounts:sendOobCode` with `PASSWORD_RESET`; it does not create/enable accounts,
modify roles, clear first-login flags or issue application sessions.

Accepted requests, unknown/deleted/disabled accounts and provider account-specific
throttling all produce the same neutral confirmation. A 60-second server-session
cooldown applies to all valid requests regardless of email; network, configuration
and server failures display a generic recovery error. Raw Firebase errors are
never returned to the browser. Existing application authorization checks must
remain in place; password recovery alone does not grant dashboard access.

Validation: local popup interaction and endpoint checks, JavaScript regression
checks and 11 isolated Firebase-response cases passed. Service tests used an
in-memory HTTP handler and sent no real email. Live delivery and disabled-account
login restrictions cannot be verified without configured Firebase authentication
and the application's missing authorization integration.


## First-login popup design preview

Open `Auth/Login?preview=first-login` after starting the project to preview the
password-change popup without signing in. The preview is explicitly labelled;
its loading/feedback interaction never saves a password or grants access.
The normal Login page and existing backend handlers are unchanged.

The popup uses the existing theme, independent password visibility controls,
live password-strength and match feedback, and a Save button disabled until
all displayed requirements pass. Escape is prevented; the preview-only Back to
Login link provides an exit. This is presentation work only. Firebase password
updates, profile flags, provisioning and route authorization remain future work.


### Connected local frontend demo (Firebase deferred)

Open `Auth/Login.aspx` and select an identity under **Local frontend demo**. Browser `localStorage` key `uims.demo.v1` is the authoritative demo state; sessionStorage selects the current identity. No password is stored and this is not secure authentication. Connected workspaces on 27 role pages persist cross-role applications, tasks, evaluations and completion simulations. Original independent layout previews are retained outside demo mode. Public contact can save a local demo message; no mail is sent.

The solution builds and isolated data-provider tests pass. Live browser interactions, responsive layouts and the full original UI migration remain unverified/incomplete. Certificate output is demonstration HTML, not an official PDF; file selections retain names only. Account expiry is a persisted test-clock simulation evaluated on access, not a background worker. Firebase, official grading, secure authorization and durable account deactivation remain intentionally pending.
