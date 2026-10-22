<%@ Page Language="C#" AutoEventWireup="true" Async="true" AsyncTimeout="120" CodeBehind="VerifyAttendance.aspx.cs" Inherits="University_Internship_Management_System.Company.VerifyAttendance" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Verify Attendance | JUST Internships</title>
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="../Assets/shared/colors.css" rel="stylesheet" />
    <link href="../Assets/company/verify-attendance.css?v=ui-simple1" rel="stylesheet" />
</head>
<body>
    <form id="attendanceForm" runat="server">
        <script type="application/json" id="attendanceBootstrap">{"csrf":"<%= AttendanceCsrfToken %>","localDemo":<%= LocalDemoAllowed ? "true" : "false" %>}</script>
        <div class="app">

            <!-- ============ SIDEBAR (Company Supervisor) ============ -->
            <aside class="sidebar" id="sidebar">
<button type="button" class="legacy-sidebar-close" aria-label="Close navigation">&times;</button>
                <a href="CompanyDashboard.aspx" class="brand">
                    <div class="brand-logo">J</div>
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav" aria-label="Company pages">
<li><a href="CompanyDashboard.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="ManageOpportunities.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Manage Opportunities</span></a></li>
<li><a href="ApplicationsReview.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Applications Review</span></a></li>
<li><a href="MyTrainees.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="7" r="3"/><path d="M5 21v-3a7 7 0 0 1 14 0v3"/></svg><span>My Trainees</span></a></li>

<li><a href="ManageTasks.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Manage &amp; Review Tasks</span></a></li>

<li><a href="ReportsFinalEvaluation.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Reports &amp; Final Evaluation</span></a></li>

<li><a href="CompanySupervisorAccount.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>Supervisor Account</span></a></li>
<li><a href="VerifyAttendance.aspx" class="active" aria-current="page"><i class="bi bi-qr-code-scan" aria-hidden="true"></i><span>Verify Attendance</span></a></li>
</ul>

                <div class="side-divider"></div>

                <ul class="side-nav"><li><a href="../Contact.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Help &amp; contact</span></a></li>
</ul>

                <div class="side-promo">
                    <i class="bi bi-building-add"></i>
                    <p>Have a new role?<br />Reach JUST students today.</p>
                    <a href="ManageOpportunities.aspx">Post opportunity &rarr;</a>
                </div>
            </aside>
            <div class="sidebar-backdrop" onclick="toggleSidebar()"></div>

            <div class="main">
                <!-- ============ NAVBAR ============ -->
                <header class="topbar">
                    <button type="button" class="menu-btn" onclick="toggleSidebar()" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input id="attendanceSearch" type="search" aria-label="Search attendance history" placeholder="Search for trainees, tasks, or applications..." />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" id="notificationsButton" aria-label="Attendance status"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown" aria-expanded="false">
                                <span class="avatar"><span>PC</span></span>
                                <span class="name"><span id="navName">Company supervisor</span><span class="role">Company Supervisor</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a href="CompanySupervisorAccount.aspx" class="dropdown-item"><i class="bi bi-person me-2"></i>Account</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><button type="button" class="dropdown-item" disabled title="Authentication is not connected on this preview page">Log out (not connected)</button></li>
                            </ul>
                        </div>
                    </div>
                </header>


<main class="content attendance-content">
    <div class="page-heading"><div><p class="eyebrow">COMPANY WORKSPACE</p><h1>Verify Attendance</h1><p>Scan attendance and review your trainees.</p></div><span class="date-chip" id="connectionStatus">Connecting…</span></div>
    <span id="modeMessage" class="visually-hidden"></span>
    <div class="panel-heading"><span id="assignedStudents" class="section-note">Loading assigned students…</span><div class="attendance-actions"><button id="issueQrButton" type="button" class="primary-action" disabled>Issue student QR codes</button><% if (LocalDemoAllowed) { %><button id="newLocalTest" type="button" class="text-action">New test session</button><% } %><button id="refreshAttendance" type="button" class="text-action">Refresh records</button></div></div>
    <div id="pageNotice" class="page-notice" role="status" hidden></div>
    <div class="row g-4 align-items-start">
        <div class="col-xl-6"><section class="attendance-panel" aria-labelledby="scannerHeading"><div class="panel-heading"><h2 id="scannerHeading">Scan QR</h2><span class="attendance-badge">Live QR scanner</span></div>
            <div class="scanner-frame"><video id="cameraVideo" autoplay playsinline muted hidden></video><div id="cameraPlaceholder"><i class="bi bi-qr-code-scan" aria-hidden="true"></i><strong>Ready to scan</strong><p>Scan a student QR to record attendance.</p></div><span class="scanner-corner" aria-hidden="true"></span></div>
            <p id="cameraMessage" class="section-note" role="status">Allow camera access to begin.</p><div class="attendance-actions"><button id="openCamera" type="button" class="primary-action">Open camera</button><button id="stopCamera" type="button" class="secondary-action" disabled hidden>Stop camera</button></div>

        </section></div>
        <div class="col-xl-6"><section class="attendance-panel" aria-labelledby="verificationHeading"><div class="panel-heading"><h2 id="verificationHeading">Scan result</h2><span id="scanStatus" class="attendance-badge">Waiting for scan</span></div>
            <div id="verificationEmpty" class="verification-empty"><i class="bi bi-person-badge" aria-hidden="true"></i><h3>Waiting for a student QR</h3><p>Student details appear after a successful scan.</p></div>
            <div id="verificationDetails" hidden><div class="identity-heading"><img id="studentPhoto" class="student-avatar" alt="Student profile photo" hidden /><span id="studentInitials" class="student-avatar"></span><div><h3 id="verifiedStudentName"></h3><p id="verifiedIdentity"></p></div></div><dl class="assignment-list"><div><dt>Student ID</dt><dd id="verifiedStudentNumber"></dd></div><div><dt>Assigned company</dt><dd id="verifiedCompany"></dd></div><div><dt>Assigned supervisor</dt><dd id="verifiedSupervisor"></dd></div><div><dt>Attendance action</dt><dd id="scannedAction"></dd></div><div><dt>Recorded time</dt><dd id="recordedTime"></dd></div></dl><p id="verificationExplanation" class="section-note"></p><p class="identity-warning">Confirm the student’s identity.</p></div>
            <p id="scanError" class="error-message" role="alert" hidden></p>
        </section></div>
    </div>
    <section class="attendance-panel" aria-labelledby="requestsHeading"><div class="panel-heading"><div><h2 id="requestsHeading">Early check-out requests</h2><p class="section-note">Approve the request before the student scans out.</p></div><span id="requestStatus" class="attendance-badge badge-warning">Loading</span></div><div id="requestList"></div></section>
    <section class="attendance-panel" aria-labelledby="recordsHeading"><div class="panel-heading"><h2 id="recordsHeading">Attendance records &amp; hours approval</h2><span class="section-note"><span id="recordsDate"></span></span></div><div class="table-responsive"><table class="table attendance-table"><caption class="visually-hidden">Attendance records and separate hours approval</caption><thead><tr><th scope="col">Date</th><th scope="col">Student</th><th scope="col">Check-in</th><th scope="col">Check-out</th><th scope="col">Worked</th><th scope="col">Attendance</th><th scope="col">Hours approval</th></tr></thead><tbody id="recordRows"></tbody></table></div><p class="section-note">Hours count after check-out and your approval.</p></section>
    <dialog id="issueQrDialog" class="attendance-dialog" aria-labelledby="issueQrHeading">
        <div class="panel-heading"><h2 id="issueQrHeading">Issue student QR codes</h2><button type="button" class="dialog-close" data-close-dialog="issueQrDialog" aria-label="Close">&times;</button></div>
        <p class="section-note">Each selected student receives a different QR. Students use Refresh attendance, then Show QR. Valid for 120 seconds.</p>
        <label for="issuedQrAction" class="form-label">Attendance action</label>
        <select id="issuedQrAction" class="form-select"><option value="checkIn">Check-in</option><option value="checkOut">Check-out</option></select>
        <p><label><input id="selectAllQrStudents" type="checkbox" /> Select all students</label></p>
        <div id="qrStudentList"></div>
        <p id="issueQrStatus" class="section-note" role="status"></p>
        <div class="attendance-actions"><button id="confirmIssueQr" type="button" class="primary-action">Issue selected QR codes</button><button type="button" class="secondary-action" data-close-dialog="issueQrDialog">Close</button></div>
    </dialog>
    <noscript><p>Enable JavaScript to scan QR codes and load attendance records.</p></noscript>
</main>

            </div>
        </div>
    </form>
    <script src="../Js_project/firebase-client.js"></script>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Company/attendance-jsqr.js"></script>
    <script src="../Js_project/Company/verify-attendance.js?v=ui-simple2"></script>
</body>
</html>
