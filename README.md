# University Internship Management System

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

This checkout contains no Firebase connection, HTTP requests, authentication,
data models, CRUD handlers or database configuration. No database was contacted
or modified during the refactor. No unused Firebase client or speculative CRUD
methods were added.

Before implementing a data-backed feature, supply the Realtime Database URL,
existing record paths and the intended Firebase authentication flow. Implement
only the operations that feature needs in one shared service using a reused
`HttpClient`. Keep credentials outside source control, use the authenticated
user's token and honour Firebase security rules. Do not make the database public
to enable access.

Data-backed pages should use `Async="true"` and register a `PageAsyncTask` for
asynchronous work. Validate input before writing, prevent duplicate submissions
where applicable and show a safe error message without exposing credentials.
There are currently no data submission handlers requiring these additions.

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
behaviour cannot be verified until an integration exists.


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

**Authentication integration is blocked:** the checkout has no Firebase client,
authorization service, role records or first-login enforcement architecture.
The current handlers explicitly report unavailable configuration. They do not
create authenticated sessions, grant dashboard access, send reset emails or claim
a password was changed. The modal can read a server-owned pending-change flag,
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
configuration-error handling, all existing pages and new assets. Firebase login,
password changes, reset delivery and role authorization remain unimplemented and
untested until the missing integration details are supplied.
