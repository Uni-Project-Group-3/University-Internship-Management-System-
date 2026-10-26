# University Internship Management System

ASP.NET Web Forms application for students, company supervisors, academic supervisors and internship coordinators at Jordan University of Science and Technology.

The application manages opportunities, applications, training placements, tasks, exams, QR attendance, weekly reports and final approvals. Firebase provides Authentication and Realtime Database; Supabase provides private file storage.

## Project files

- `University-Internship-Management-System.slnx`: solution to open in Visual Studio.
- `University-Internship-Management-System/`: application source, pages, assets and configuration.
- `Tools/Configure-Shared-Firebase.ps1`: per-device Firebase configuration helper.
- `firebase-final-rules.json` and application rule files: database and storage deployment configuration.

## Run the application

1. Open the solution in Visual Studio with ASP.NET web development support and .NET Framework 4.7.2.
2. Restore NuGet packages and build the web project.
3. Keep the existing local `Firebase.Local.config`, Firebase server credentials and QR signing configuration on each development machine. The shared configuration helper supports `-ValidateOnly`.
4. Set `UIMS_SUPABASE_SECRET_KEY` in the server environment and use the configured private Supabase bucket.
5. Start the application with IIS Express. Use the team's existing accounts and shared Firebase project.

Local configuration, credentials, build output and verification artifacts are excluded from Git. Database and Storage contents are managed separately from this repository.

## 👨‍💻 Development Team

| # | Team Member |
| --- | --- |
| 1 | **Mohammad Mustafa** |
| 2 | **Tariq Batayneh** |
| 3 | **Eyad Mansour** |
| 4 | **Yasmin Talafha** |
| 5 | **Anas Ghawanimah** |
| 6 | **Diaa Al-Wardat** |
| 7 | **Roa Al-Ajlouni** |
