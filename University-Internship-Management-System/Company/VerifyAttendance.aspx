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
    <link href="../Assets/company/verify-attendance.css?v=20261024-scanner-flash" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261024-refine" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
<link href="../Assets/company/company-hero.css?v=20261024-blue" rel="stylesheet" />
</head>
<body>
    <form id="attendanceForm" runat="server">
        <script type="application/json" id="attendanceBootstrap">{"csrf":"<%= AttendanceCsrfToken %>","localDemo":<%= LocalDemoAllowed ? "true" : "false" %>}</script>
        <div class="app">

            <!-- ============ SIDEBAR (Company Supervisor) ============ -->
            <aside class="sidebar workspace-canonical-sidebar" id="sidebar"><a class="brand workspace-nav-brand" href="CompanyDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Company Supervisor</p><nav class="workspace-nav-primary" aria-label="Company Supervisor pages"><a class="workspace-nav-link" href="CompanyDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="ManageOpportunities.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Manage Opportunities</span></a><a class="workspace-nav-link" href="ApplicationsReview.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Applications Review</span></a><a class="workspace-nav-link" href="MyTrainees.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Trainees</span></a><a class="workspace-nav-link" href="ManageTasks.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Manage Tasks</span></a><a class="workspace-nav-link" href="ManageExams.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Task Exams</span></a><a class="workspace-nav-link active" aria-current="page" href="VerifyAttendance.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 3h6v6H3V3Zm12 0h6v6h-6V3ZM3 15h6v6H3v-6Zm12 0h2v2h-2v-2Zm4 0h2v6h-6v-2h4v-4Z"/></svg><span>Verify Attendance</span></a><a class="workspace-nav-link" href="WeeklyReports.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Weekly Reports</span></a><a class="workspace-nav-link" href="ReportsFinalEvaluation.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Final Evaluation</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="CompanySupervisorAccount.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Account</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
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
                                <li><a id="attendanceLogout" class="dropdown-item" href="../Auth/Login.aspx">Log out</a></li>
                            </ul>
                        </div>
                    </div>
                </header>


<main class="content attendance-content">
    <div class="page-heading company-reference-hero"><div><p class="eyebrow">COMPANY WORKSPACE</p><h1>Verify Attendance</h1><p>Scan attendance and review your trainees.</p></div><span class="date-chip" id="connectionStatus">Connecting&#8230;</span></div>
    <span id="modeMessage" class="visually-hidden"></span>
    <div class="panel-heading"><span id="assignedStudents" class="section-note">Loading assigned students&#8230;</span><div class="attendance-actions"><button id="issueQrButton" type="button" class="primary-action" disabled>Issue student QR codes</button><button id="refreshAttendance" type="button" class="text-action">Refresh records</button></div></div>
    <div id="pageNotice" class="page-notice" role="status" hidden></div>
    <div class="row g-4 align-items-start">
        <div class="col-xl-6"><section class="attendance-panel" aria-labelledby="scannerHeading"><div class="panel-heading"><h2 id="scannerHeading">Scan QR</h2><span class="attendance-badge">Live QR scanner</span></div>
            <div class="scanner-frame" id="attendanceScannerFrame"><video id="cameraVideo" autoplay playsinline muted hidden></video><canvas id="scanSnapshot" aria-hidden="true" hidden></canvas><span class="scanner-sweep" aria-hidden="true"></span><div id="cameraPlaceholder"><i class="bi bi-qr-code-scan" aria-hidden="true"></i><strong>Ready to scan</strong><p>Scan a student QR to record attendance.</p></div><span class="scanner-corner" aria-hidden="true"></span><div id="scanFeedback" class="scan-feedback" role="status" aria-live="polite" aria-atomic="true" hidden><span class="scan-feedback-icon" aria-hidden="true"><svg class="feedback-check" viewBox="0 0 32 32" fill="none"><path d="m8 16 5 5 11-11" /></svg><svg class="feedback-warning" viewBox="0 0 32 32" fill="none"><path d="m10 10 12 12m0-12L10 22" /></svg><span class="feedback-spinner"></span></span><strong id="scanFeedbackTitle"></strong><p id="scanFeedbackDescription"></p></div></div>
            <p id="cameraMessage" class="section-note" role="status">Allow camera access to begin.</p><div class="attendance-actions"><button id="openCamera" type="button" class="primary-action">Open camera</button><button id="stopCamera" type="button" class="secondary-action" disabled hidden>Stop camera</button></div>

        </section></div>
        <div class="col-xl-6"><section class="attendance-panel scanner-details" id="scanResultPanel" aria-labelledby="verificationHeading"><div class="panel-heading"><h2 id="verificationHeading">Last scan details</h2><span id="scanStatus" class="attendance-badge">Waiting for scan</span></div>
            <div id="verificationEmpty" class="verification-empty"><i class="bi bi-person-badge" aria-hidden="true"></i><h3>Waiting for a student QR</h3><p>Student details appear after a successful scan.</p></div>
            <div id="verificationDetails" hidden><div class="identity-heading"><img id="studentPhoto" class="student-avatar" alt="Student profile photo" hidden /><span id="studentInitials" class="student-avatar"></span><div><h3 id="verifiedStudentName"></h3><p id="verifiedIdentity"></p></div></div><dl class="assignment-list"><div><dt>Student ID</dt><dd id="verifiedStudentNumber"></dd></div><div><dt>Assigned company</dt><dd id="verifiedCompany"></dd></div><div><dt>Assigned supervisor</dt><dd id="verifiedSupervisor"></dd></div><div><dt>Attendance action</dt><dd id="scannedAction"></dd></div><div><dt>Recorded time</dt><dd id="recordedTime"></dd></div></dl><p id="verificationExplanation" class="section-note"></p><p class="identity-warning">Confirm the student’s identity.</p></div>
            <p id="scanError" class="error-message" role="alert" hidden></p>
        </section></div>
    </div>
    <section class="attendance-panel" aria-labelledby="requestsHeading"><div class="panel-heading"><div><h2 id="requestsHeading">Early check-out requests</h2><p class="section-note">Approve the request before the student scans out.</p></div><span id="requestStatus" class="attendance-badge badge-warning">Loading</span></div><div id="requestList"></div></section>
    <section class="attendance-panel" aria-labelledby="recordsHeading"><div class="panel-heading"><h2 id="recordsHeading">Attendance records &amp; hours approval</h2><span class="section-note"><span id="recordsDate"></span></span></div><div class="panel-heading"><p id="pendingApprovalSummary" class="section-note" role="status">Loading approval status…</p><label class="filter-label" for="approvalFilter">Show<select id="approvalFilter" class="form-select"><option value="all">All records</option><option value="pending">Pending approval</option></select></label></div><div class="table-responsive"><table class="table attendance-table"><caption class="visually-hidden">Attendance records and separate hours approval</caption><thead><tr><th scope="col">Date</th><th scope="col">Student</th><th scope="col">Check-in</th><th scope="col">Check-out</th><th scope="col">Worked</th><th scope="col">Attendance</th><th scope="col">Hours approval</th></tr></thead><tbody id="recordRows"></tbody></table></div><p class="section-note">Hours count after check-out and your approval.</p></section>
    <dialog id="issueQrDialog" class="attendance-dialog" aria-labelledby="issueQrHeading">
        <div class="panel-heading"><h2 id="issueQrHeading">Issue student QR codes</h2><button type="button" class="dialog-close" data-close-dialog="issueQrDialog" aria-label="Close">&times;</button></div>
        <p class="section-note">Each selected student receives a different QR. Students use Refresh attendance, then Show QR. Valid for 120 seconds.</p>
        <p class="section-note">Choose check-in or check-out whenever needed on an authorized training day, including outside the scheduled shift hours. Check-out requires an existing check-in. Issuing a QR does not record attendance; the supervisor must scan it.</p>
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
    <script src="../Js_project/firebase-client.js?v=20261024-attendance-session"></script>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Company/attendance-jsqr.js"></script>
    <script src="../Js_project/Company/verify-attendance.js?v=20261024-scanner-flash"></script>
</body>
</html>
