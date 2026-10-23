<%@ Page Language="C#" AutoEventWireup="true" Async="true" AsyncTimeout="120" CodeBehind="MyAttendance.aspx.cs" Inherits="University_Internship_Management_System.Student.MyAttendance" %>
<%@ Register Src="~/Shared/StudentSidebar.ascx" TagPrefix="uims" TagName="RoleSidebar" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>My Attendance | JUST Internships</title>
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="../Assets/shared/colors.css" rel="stylesheet" />
    <link href="../Assets/student/my-attendance.css?v=ui-simple1" rel="stylesheet" />
    <link href="../Assets/shared/attendance-workspace.css?v=20261024-design2" rel="stylesheet" />
</head>
<body class="attendance-workspace">
    <form id="attendanceForm" runat="server">
        <script type="application/json" id="attendanceBootstrap">{"csrf":"<%= AttendanceCsrfToken %>","localDemo":<%= LocalDemoAllowed ? "true" : "false" %>}</script>
        <div class="app">
            <uims:RoleSidebar runat="server" />

            <div class="main">
                <!-- ============ NAVBAR ============ -->
                <header class="topbar">
                    <button type="button" class="menu-btn" onclick="toggleSidebar()" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input id="attendanceSearch" type="search" aria-label="Search attendance history" placeholder="Search attendance history…" />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" id="notificationsButton" aria-label="Attendance status"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown" aria-expanded="false">
                                <span class="avatar"><span>PS</span></span>
                                <span class="name"><span id="navName">Student</span><span class="role">Student</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="Profile.aspx"><i class="bi bi-person me-2"></i>My Profile</a></li>
                                <li><a class="dropdown-item" aria-disabled="true" title="This page is not available yet"><i class="bi bi-key me-2"></i>Change Password</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><a id="attendanceLogout" class="dropdown-item" href="../Auth/Login.aspx">Log out</a></li>
                            </ul>
                        </div>
                    </div>
                </header>


<main class="content attendance-content">
    <div class="page-heading"><div><p class="eyebrow">TRAINING ATTENDANCE</p><h1>My Attendance</h1><p>Your attendance, approved hours and training details in one place.</p></div><span class="date-chip" id="connectionStatus">Connecting…</span></div>
    <ol class="attendance-guide" aria-label="Attendance steps"><li><span class="guide-number" aria-hidden="true">1</span><div><strong>Receive your QR</strong><p>Your supervisor issues a code for your attendance action.</p></div></li><li><span class="guide-number" aria-hidden="true">2</span><div><strong>Show & scan</strong><p>Open your QR and show it to your assigned supervisor.</p></div></li><li><span class="guide-number" aria-hidden="true">3</span><div><strong>Review your hours</strong><p>Check out, then view your hours after supervisor approval.</p></div></li></ol>
    <span id="modeMessage" class="visually-hidden"></span>
    <button type="button" id="refreshAttendance" class="secondary-action refresh-action">Refresh attendance</button>
    <div id="pageNotice" class="page-notice" role="status" hidden></div>
    <section class="row g-3 metric-row" aria-label="Training hours">
        <div class="col-sm-4"><div class="metric-card blue"><span>Approved hours</span><strong id="approvedHours">—</strong><small>Verified and approved only</small></div></div>
        <div class="col-sm-4"><div class="metric-card ice"><span>Remaining hours</span><strong id="remainingHours">—</strong><small>Until your training requirement is met</small></div></div>
        <div class="col-sm-4"><div class="metric-card"><span>Required hours</span><strong id="requiredHours">—</strong><small>Defined by your training assignment</small></div></div>
    </section>
    <div class="row g-4 align-items-start">
        <div class="col-xl-8">
            <section class="attendance-panel" aria-labelledby="todayHeading"><div class="panel-heading"><h2 id="todayHeading">Today's attendance</h2><span id="todayStatus" class="attendance-badge">—</span></div>
                <p class="section-note" id="scheduleDate"></p>
                <div class="schedule-grid"><div><span>Scheduled start</span><strong id="scheduledStart">—</strong></div><div><span>Scheduled end</span><strong id="scheduledEnd">—</strong></div><div><span>Actual check-in</span><strong id="actualCheckIn">—</strong></div><div><span>Actual check-out</span><strong id="actualCheckOut">—</strong></div></div>
                <div class="timeline"><span id="scheduleHelp"></span></div>
                <div class="student-qr-actions"><p class="action-heading"><i class="bi bi-qr-code-scan" aria-hidden="true"></i> Your attendance QR</p><p class="section-note">When your supervisor issues a code, the matching button becomes available. If needed, select Refresh attendance.</p><div class="attendance-actions"><button id="checkInButton" type="button" disabled class="primary-action">Show check-in QR</button><button id="checkOutButton" type="button" disabled class="primary-action">Show check-out QR</button></div></div>
                <button id="earlyButton" type="button" disabled class="text-action">Request early check-out</button><p id="earlyStatus" class="section-note" aria-live="polite"></p>
            </section>
            <section class="attendance-panel" aria-labelledby="historyHeading"><div class="panel-heading"><h2 id="historyHeading">Attendance history</h2><label class="filter-label" for="historyFilter">Status<select id="historyFilter" class="form-select"><option value="all">All records</option><option value="approved">Approved hours</option><option value="pending">Pending hours</option><option value="absent">Absent</option></select></label></div>
                <div class="table-responsive"><table class="table attendance-table"><caption class="visually-hidden">Attendance records; hours count only when approved</caption><thead><tr><th scope="col">Day</th><th scope="col">Check-in</th><th scope="col">Check-out</th><th scope="col">Worked</th><th scope="col">Attendance</th><th scope="col">Hours</th></tr></thead><tbody id="historyRows"></tbody></table></div><p id="historyEmpty" class="empty-message" hidden>No matching attendance records.</p>
            </section>
        </div>
        <aside class="col-xl-4">
            <section class="attendance-panel assignment-panel"><p class="eyebrow">YOUR TRAINING</p><h2>Assignment details</h2><dl class="assignment-list"><div><dt>Student</dt><dd id="studentName">—</dd></div><div><dt>Company</dt><dd id="companyName">—</dd></div><div><dt>Company supervisor</dt><dd id="supervisorName">—</dd></div><div><dt>Academic supervisor</dt><dd id="academicSupervisorName">—</dd></div><div><dt>Training progress</dt><dd id="progressText">—</dd></div></dl><progress id="trainingProgress" max="1" value="0" aria-label="Approved training progress"></progress></section>
            <details class="attendance-panel quick-help"><summary>How attendance works</summary><ol class="steps-list"><li>Display your temporary QR when the action is available.</li><li>Your assigned company supervisor checks your identity and scans it.</li><li>A valid scan records attendance automatically. Hours are approved separately.</li></ol><p class="section-note">A missing check-out does not receive full-day hours. Day-off records are not absences.</p></details>
        </aside>
    </div>
    <dialog id="qrDialog" class="attendance-dialog qr-display-dialog" aria-labelledby="qrTitle" aria-describedby="qrDisplayHelp qrDescription">
        <div class="qr-dialog-header"><div><p class="eyebrow">YOUR ATTENDANCE PASS</p><h2 id="qrTitle">Attendance QR</h2></div><button type="button" class="dialog-close" data-close-dialog="qrDialog" aria-label="Close QR">&times;</button></div>
        <p id="qrDisplayHelp" class="qr-display-help">Hold this code steady in front of your supervisor’s camera.</p>
        <div class="qr-placeholder"><div id="qrCode" aria-label="Temporary attendance QR code"></div></div>
        <div class="qr-validity"><i class="bi bi-clock" aria-hidden="true"></i><strong id="qrCountdown" role="status" aria-live="polite" aria-atomic="true"></strong></div>
        <p id="qrDescription" class="qr-identity"></p>
        <div class="qr-scan-tip"><i class="bi bi-brightness-high" aria-hidden="true"></i><p>Keep the whole code visible. Increase screen brightness if scanning is difficult.</p></div>
        <div class="qr-dialog-footer"><p>The code expires automatically. If it expires, ask your supervisor for a new one.</p><button type="button" class="secondary-action" data-close-dialog="qrDialog">Close QR</button></div>
    </dialog>
    <dialog id="earlyDialog" class="attendance-dialog" aria-labelledby="earlyTitle"><div class="panel-heading"><h2 id="earlyTitle">Request early check-out</h2><button type="button" class="dialog-close" data-close-dialog="earlyDialog" aria-label="Close early check-out request">&times;</button></div><p class="section-note">Your supervisor must approve the request before an early check-out QR becomes available.</p><label for="earlyReason" class="form-label">Reason *</label><textarea id="earlyReason" class="form-control" rows="4" maxlength="500" placeholder="Explain why you need to leave early."></textarea><p id="reasonError" class="error-message" role="alert" hidden>Please enter a reason.</p><div class="attendance-actions"><button type="button" class="secondary-action" data-close-dialog="earlyDialog">Cancel</button><button type="button" class="primary-action" id="sendEarlyRequest">Send request</button></div></dialog>
    <noscript><p>Enable JavaScript to load attendance and display QR codes.</p></noscript>
</main>

            </div>
        </div>
    </form>
    <script src="../Js_project/firebase-client.js"></script>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Student/attendance-qrcode.min.js"></script>
    <script src="../Js_project/Student/my-attendance.js?v=20261023-placements"></script>
</body>
</html>
