<%@ Page Language="C#" AutoEventWireup="true" Async="true" AsyncTimeout="120" CodeBehind="VerifyAttendance.aspx.cs" Inherits="University_Internship_Management_System.Company.VerifyAttendance" %>
<%@ Register Src="~/Shared/CompanySidebar.ascx" TagPrefix="uims" TagName="RoleSidebar" %>
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
    <link href="../Assets/shared/attendance-workspace.css?v=20261024-supervisor3" rel="stylesheet" />
</head>
<body class="attendance-workspace supervisor-workspace">
    <form id="attendanceForm" runat="server">
        <script type="application/json" id="attendanceBootstrap">{"csrf":"<%= AttendanceCsrfToken %>","localDemo":<%= LocalDemoAllowed ? "true" : "false" %>}</script>
        <div class="app">
            <uims:RoleSidebar runat="server" />

            <div class="main">
                <!-- ============ NAVBAR ============ -->
                <header class="topbar">
                    <button type="button" class="menu-btn" onclick="toggleSidebar()" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
                    <div class="workspace-context"><i class="bi bi-building" aria-hidden="true"></i><span>Company workspace <small>Attendance management</small></span></div>
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
    <div class="page-heading"><div><p class="eyebrow">COMPANY WORKSPACE</p><h1>Verify Attendance</h1><p>Issue QR codes, scan attendance and approve your trainees’ hours.</p></div><span class="date-chip" id="connectionStatus">Connecting…</span></div>
    <nav class="supervisor-section-nav" aria-label="Attendance sections"><button type="button" onclick="document.getElementById('prepareAttendance').focus({preventScroll: true}); document.getElementById('prepareAttendance').scrollIntoView({block: 'start'});"><span>1</span> Issue QR</button><button type="button" onclick="document.getElementById('scanAttendance').focus({preventScroll: true}); document.getElementById('scanAttendance').scrollIntoView({block: 'start'});"><span>2</span> Scan &amp; verify</button><button type="button" onclick="document.getElementById('reviewAttendance').focus({preventScroll: true}); document.getElementById('reviewAttendance').scrollIntoView({block: 'start'});"><span>3</span> Review requests</button><button type="button" onclick="document.getElementById('attendanceRecords').focus({preventScroll: true}); document.getElementById('attendanceRecords').scrollIntoView({block: 'start'});"><span>4</span> Approve hours</button></nav>
    <span id="modeMessage" class="visually-hidden"></span>
    <section id="prepareAttendance" tabindex="-1" class="attendance-panel prepare-attendance" aria-labelledby="prepareHeading">
        <div class="prepare-copy"><p class="eyebrow">STEP 1 · PREPARE</p><h2 id="prepareHeading">Your trainees &amp; QR codes</h2><p class="section-note">Choose your assigned students and issue a check-in or check-out code. Issuing a code does not record attendance.</p><p id="assignedStudents" class="assigned-trainees section-note" aria-live="polite">Loading assigned students…</p></div>
        <div class="prepare-actions"><button id="issueQrButton" type="button" class="primary-action" disabled><i class="bi bi-qr-code" aria-hidden="true"></i> Issue student QR codes</button><button id="refreshAttendance" type="button" class="secondary-action refresh-action"><i class="bi bi-arrow-clockwise" aria-hidden="true"></i> Refresh records</button></div>
    </section>
    <div id="pageNotice" class="page-notice" role="status" hidden></div>
    <div id="scanAttendance" tabindex="-1" class="row g-4 align-items-stretch supervisor-scan-row">
        <div class="col-xl-6"><section class="attendance-panel" aria-labelledby="scannerHeading"><div class="panel-heading"><div><p class="eyebrow">STEP 2 · RECORD ATTENDANCE</p><h2 id="scannerHeading">Scan the student’s QR</h2></div><span class="attendance-badge">Live QR scanner</span></div>
            <div class="scanner-frame"><video id="cameraVideo" autoplay playsinline muted hidden></video><div id="cameraPlaceholder"><i class="bi bi-qr-code-scan" aria-hidden="true"></i><strong>Ready to scan</strong><p>Scan a student QR to record attendance.</p></div><span class="scanner-corner" aria-hidden="true"></span></div>
            <p id="cameraMessage" class="section-note" role="status">Open the camera, allow access, then place the student’s QR inside the frame.</p><div class="attendance-actions"><button id="openCamera" type="button" class="primary-action">Open camera</button><button id="stopCamera" type="button" class="secondary-action" disabled hidden>Stop camera</button></div>

        </section></div>
        <div class="col-xl-6"><section class="attendance-panel" aria-labelledby="verificationHeading"><div class="panel-heading"><div><p class="eyebrow">SCAN DETAILS</p><h2 id="verificationHeading">Student &amp; result</h2></div><span id="scanStatus" class="attendance-badge">Waiting for scan</span></div>
            <div id="verificationEmpty" class="verification-empty"><i class="bi bi-person-badge" aria-hidden="true"></i><h3>Waiting for a student QR</h3><p>Student details appear after a successful scan.</p></div>
            <div id="verificationDetails" hidden><div class="identity-heading"><img id="studentPhoto" class="student-avatar" alt="Student profile photo" hidden /><span id="studentInitials" class="student-avatar"></span><div><h3 id="verifiedStudentName"></h3><p id="verifiedIdentity"></p></div></div><dl class="assignment-list"><div><dt>Student ID</dt><dd id="verifiedStudentNumber"></dd></div><div><dt>Assigned company</dt><dd id="verifiedCompany"></dd></div><div><dt>Assigned supervisor</dt><dd id="verifiedSupervisor"></dd></div><div><dt>Attendance action</dt><dd id="scannedAction"></dd></div><div><dt>Recorded time</dt><dd id="recordedTime"></dd></div></dl><p id="verificationExplanation" class="section-note"></p><p class="identity-warning">Confirm the student’s identity.</p></div>
            <p id="scanError" class="error-message" role="alert" hidden></p>
        </section></div>
    </div>
    <section id="reviewAttendance" tabindex="-1" class="attendance-panel supervisor-review" aria-labelledby="requestsHeading"><div class="panel-heading"><div><p class="eyebrow">STEP 3 · REVIEW REQUESTS</p><h2 id="requestsHeading">Early check-out requests</h2><p class="section-note">Review any early check-out requests here. Training hours are approved separately in the attendance records below.</p></div><span id="requestStatus" class="attendance-badge badge-warning">Loading</span></div><div id="requestList"></div></section>
    <section id="attendanceRecords" tabindex="-1" class="attendance-panel supervisor-records" aria-labelledby="recordsHeading"><div class="panel-heading"><div><p class="eyebrow">STEP 4 · APPROVE HOURS</p><h2 id="recordsHeading">Attendance records &amp; hours approval</h2></div><span class="section-note"><span id="recordsDate"></span></span></div><div class="records-toolbar"><label class="visually-hidden" for="attendanceSearch">Search attendance records</label>                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input id="attendanceSearch" type="search" aria-label="Search attendance history" placeholder="Search attendance by student or date…" />
                    </div><p class="section-note">Only completed, approved hours count toward training.</p></div><div class="table-responsive"><table class="table attendance-table"><caption class="visually-hidden">Attendance records and separate hours approval</caption><thead><tr><th scope="col">Date</th><th scope="col">Student</th><th scope="col">Check-in</th><th scope="col">Check-out</th><th scope="col">Worked</th><th scope="col">Attendance</th><th scope="col">Hours approval</th></tr></thead><tbody id="recordRows"></tbody></table></div><p class="section-note">Hours count after check-out and your approval.</p></section>
    <dialog id="issueQrDialog" class="attendance-dialog qr-issue-dialog" aria-labelledby="issueQrHeading">
        <div class="panel-heading"><div><p class="eyebrow">PREPARE ATTENDANCE</p><h2 id="issueQrHeading">Issue student QR codes</h2></div><button type="button" class="dialog-close" data-close-dialog="issueQrDialog" aria-label="Close">&times;</button></div>
        <p class="section-note">Choose an action, select your students, then issue their codes. Each student opens their own QR from My Attendance. Codes are valid for 120 seconds; issuing a code does not record attendance.</p>
        <label for="issuedQrAction" class="form-label">1. Choose the attendance action</label>
        <select id="issuedQrAction" class="form-select"><option value="checkIn">Check-in</option><option value="checkOut">Check-out</option></select>
        <div class="qr-student-heading"><strong>2. Select students</strong><label><input id="selectAllQrStudents" type="checkbox" /> Select all</label></div>
        <div id="qrStudentList"></div>
        <p id="issueQrStatus" class="section-note" role="status"></p>
        <div class="attendance-actions issue-dialog-actions"><button id="confirmIssueQr" type="button" class="primary-action">Issue selected QR codes</button><button type="button" class="secondary-action" data-close-dialog="issueQrDialog">Close</button></div>
    </dialog>
    <noscript><p>Enable JavaScript to scan QR codes and load attendance records.</p></noscript>
</main>

            </div>
        </div>
    </form>
    <script src="../Js_project/firebase-client.js"></script>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Company/attendance-jsqr.js"></script>
    <script src="../Js_project/Company/verify-attendance.js?v=20261023-placements"></script>
</body>
</html>
