# Restored original management pages

Original reference (read only): C:\Users\user\Videos\UIMS-Repo\University-Internship-Management-System\University-Internship-Management-System
Target: C:\Projects\UIMS-Repo\University-Internship-Management-System

Restored original ASPX, code-behind, designer, JavaScript, and CSS for ManageStudents, ManageCompanies, and Manage_Academic_Supervisor. Only the per-page Firebase gateway configuration was adapted to use the shared Web.config target with enabled page-local reads/writes; global Firebase switches are unchanged. Bare data-* attributes in original markup were quoted for ASP.NET parsing compatibility. Existing shared company/academic services and credential mailer match the originals. Shared student service retains merged role normalization and recovery for historical completed markers left without an account.

Run tests\ManageStudents\Run.ps1. Seventeen isolated checks cover the original student account workflow, including repeated create/delete/create. No test Firebase account or record is created. The tests no longer depend on the removed experimental page adapters.

Read-only live checks succeeded: coordinator Admin authorization, student directory, company directory, academic supervisor directory, all against just-project-4af15. Signed-out browser requests render the student sign-in message or redirect to login for the other two pages. Full authenticated UI mutation testing was not performed.