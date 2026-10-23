# PowerShell site cycle checks

From the existing repository directory:

```powershell
.\Tests\Run-SiteCycles.ps1
```

Full available checks, with the existing local application running:

```powershell
.\Tests\Run-SiteCycles.ps1 -Browser -Http -ReadOnlyFirebase
```

If local script policy blocks execution, run this file for this process only:

```powershell
powershell.exe -NoProfile -ExecutionPolicy Bypass -File .\Tests\Run-SiteCycles.ps1 -Browser -Http -ReadOnlyFirebase
```

The runner discovers MSBuild using Visual Studio's vswhere. You can supply `-MSBuildPath` and `-BaseUrl https://localhost:44372`. It produces `Tests/Reports/SiteCycles-<timestamp>/report.html`, `summary.csv`, `summary.json`, per-check logs and an optional per-page HTTP inventory. Exit code 1 means a failed or blocked check. Optional checks not requested appear as SKIP.

The C# suite now continues after individual test failures and names each failing cycle, rather than stopping at the first failure. Build failures block running stale test executables. Process timeouts prevent a hanging check from blocking the entire report. Inspect cycles.log for PASS/FAIL test names and exceptions; inspect pages.json for individual broken URLs. Do not publish diagnostic logs without reviewing them.

Coverage includes the existing isolated account/student management, application/offer/placement, tasks/board, exam, weekly report and final evaluation test suites; Web Forms precompilation; every application JS file's syntax; optional isolated popup/mobile browser flows; optional signed-out HTTP checks for local ASPX pages. Authenticated pages returning 401/403 or redirecting to login are marked AUTH_REQUIRED, not claimed as verified business cycles. Reset/logout/demo/seed/delete-named pages are excluded from the HTTP scan. Browser tests use intercepted test-only responses and never import fixtures into Firebase.

`-ReadOnlyFirebase` uses the existing external server credentials and verifies real authorized reads only. It does not create accounts, submit reports, approve grades, generate attendance or upload files. The runner never runs legacy demo-seeding/deployment scripts. Storage and authenticated live write cycles remain outside this diagnostic. Modules without existing automated coverage require additional tests; a green report does not prove every possible site action works.
