<div align="center">

# 🎓 University Internship Management System
### UIMS · Jordan University of Science and Technology (JUST)
**Faculty of Computer and Information Technology**

*A connected internship journey — from opportunity discovery to final approval.*

![ASP.NET Web Forms](https://img.shields.io/badge/ASP.NET-Web%20Forms-512BD4?style=flat-square) ![C%23](https://img.shields.io/badge/C%23-.NET%20Framework%204.7.2-239120?style=flat-square) ![Firebase](https://img.shields.io/badge/Firebase-Auth%20%26%20Realtime%20DB-FFCA28?style=flat-square) ![Bootstrap](https://img.shields.io/badge/Bootstrap-5-7952B3?style=flat-square) ![Supabase](https://img.shields.io/badge/Supabase-Storage-3ECF8E?style=flat-square)

[Features](#-features) · [Technology](#-technology-stack) · [Getting Started](#-getting-started) · [Team](#-development-team)

</div>

---

## 📖 Overview

**University Internship Management System (UIMS)** is a web application designed to coordinate university internship programs among **students**, **company supervisors**, **academic supervisors**, and **university internship coordinators**. It brings opportunity applications, placements, assigned work, attendance, progress tracking, evaluations, and completion reviews into a role-based platform.

The project is tailored to **Jordan University of Science and Technology (JUST)** and its **Faculty of Computer and Information Technology**.

## ✨ Features

| Area | Capabilities represented in the project |
| --- | --- |
| 🔐 Accounts & access | Firebase Authentication, role-specific dashboards and access flows |
| 🎯 Internship opportunities | Explore openings, submit applications, review decisions and track statuses |
| 👨‍🎓 Student workspace | Profile, applications, tasks, progress, exams, attendance and weekly reports |
| 🏢 Company workspace | Manage opportunities and trainees, review applications, assign/review tasks, attendance verification and evaluations |
| 🧑‍🏫 Academic workspace | Monitor assigned students, review progress and prepare academic evaluations |
| 🏛️ Coordinator workspace | Manage accounts, supervisor assignments, placements and final completion reviews |
| 📱 QR attendance | Student attendance and company-side QR verification pages |
| 📝 Exams & reporting | Task-related exams, weekly reports and final evaluation workflows |
| 📄 Completion | Final review and report-related pages |
| 📁 File uploads | Supabase Storage integration for task resources, submissions, exam images and weekly-report files |

> **Implementation note:** These areas are evidenced by the included pages and project documentation. Availability of particular operations depends on environment configuration, feature gates, credentials, and permissions; this README does not claim every workflow has been verified end-to-end.

## 👥 User Roles

**Student:** Discover suitable internships, apply, follow application decisions, complete assigned tasks, review progress, and access attendance and reporting tools.

**Company Supervisor:** Publish opportunities, decide on applications, supervise trainees, review submissions, verify attendance, and contribute evaluations.

**Academic Supervisor:** Follow assigned students' training progress and provide academic evaluation.

**University Internship Coordinator:** Manage student and supervisor records, monitor placements and evaluations, and perform final completion review.

## 🔄 Internship Journey

```text
Explore opportunities → Apply → Company reviews application
        → Internship placement → Tasks / exams / reports / attendance
        → Company evaluation → Academic evaluation
        → Coordinator final review → Internship completion
```

## 🛠️ Technology Stack

| Layer | Technology |
| --- | --- |
| Frontend | HTML, CSS, JavaScript, Bootstrap 5 |
| Backend | C#, ASP.NET Web Forms, .NET Framework 4.7.2 |
| Authentication | Firebase Authentication |
| Application data | Firebase Realtime Database |
| File storage | Supabase Storage (private bucket integration) |
| Design | Figma |
| Team coordination | Trello |
| Version control | Git & GitHub |

## 🗂️ Project Structure

```text
University-Internship-Management-System-/ 
├── University-Internship-Management-System/   # ASP.NET web application
│   ├── Academic/                             # Academic supervisor pages
│   ├── Auth/                                 # Authentication pages
│   ├── Company/                              # Company supervisor pages
│   ├── Coordinator/                          # Coordinator pages
│   ├── Public/                               # Public-facing pages
│   ├── Student/                              # Student pages
│   ├── Services/                             # Application services and schemas
│   ├── Web.config                            # Application configuration
│   └── University-Internship-Management-System.csproj
├── Tests/                                    # Test projects and verification notes
├── Tools/                                    # Local configuration helpers
├── TEAM-FIREBASE-SETUP.md                    # Shared Firebase setup guidance
├── SUPABASE-STORAGE-SETUP.md                 # File storage configuration
└── University-Internship-Management-System.slnx
```

## 🚀 Getting Started

### Prerequisites

- Windows and **Visual Studio** with ASP.NET / .NET Framework web development support.
- **.NET Framework 4.7.2** targeting pack.
- IIS Express for local development.
- Authorized access to the team's existing Firebase project and, where needed, Supabase Storage.

### Local setup

1. Clone the repository:
   ```bash
   git clone https://github.com/Uni-Project-Group-3/University-Internship-Management-System-.git
   ```
2. Open the solution in Visual Studio (use the included `.slnx` solution supported by your Visual Studio version).
3. Restore required NuGet packages and build the web project.
4. Follow **`TEAM-FIREBASE-SETUP.md`** for the correct Firebase credentials and per-device setup. The application uses a local `Firebase.Local.config` and authorized server credentials; **do not commit secrets**.
5. For file uploads, follow **`SUPABASE-STORAGE-SETUP.md`**. The integration uses a private storage bucket and a server-side `UIMS_SUPABASE_SECRET_KEY` environment variable.
6. If testing QR attendance across devices, follow the shared QR signing-key guidance in `TEAM-FIREBASE-SETUP.md`; devices must use the authorized shared key and trusted local HTTPS.
7. Run the ASP.NET project through **IIS Express** in Visual Studio.

> ⚠️ **Safety:** Never publish Firebase service-account files, QR signing keys, Supabase secrets, or local configuration containing credentials. Do not import sample database JSON into a live Firebase database; a root import can overwrite existing data. Review the repository's Firebase setup and rules documentation before changing live configuration.

## 📸 Screenshots

Screenshots of the home page, dashboards, application workflow, QR attendance and evaluation screens can be added here as the team selects approved images.

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

## 📚 Additional Documentation

- `TEAM-FIREBASE-SETUP.md` — team development environment and shared Firebase configuration.
- `SUPABASE-STORAGE-SETUP.md` — storage integration and private-bucket setup.
- `University-Internship-Management-System/FIREBASE_SETUP_GUIDE.md` — Firebase setup considerations and demo-data precautions.
- `Tests/` — feature-specific verification notes and tests.

---

<div align="center">

**Built as a collaborative university internship management project.**

*University Internship Management System · UIMS*

</div>
