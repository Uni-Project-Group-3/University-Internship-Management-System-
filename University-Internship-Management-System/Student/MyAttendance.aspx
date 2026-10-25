<%@ Page Language="C#" AutoEventWireup="true" Async="true" AsyncTimeout="120" CodeBehind="MyAttendance.aspx.cs" Inherits="University_Internship_Management_System.Student.MyAttendance" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>My Attendance | JUST Internships</title>
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="../Assets/shared/colors.css" rel="stylesheet" />
    <link href="../Assets/student/my-attendance.css?v=20261025-student-equal-panels-v3" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
</head>
<body class="coordinator-banner">
    <form id="attendanceForm" runat="server">
        <script type="application/json" id="attendanceBootstrap">{"csrf":"<%= AttendanceCsrfToken %>","localDemo":<%= LocalDemoAllowed ? "true" : "false" %>}</script>
        <div class="app">

            <!-- ============ SIDEBAR (Student) ============ -->
            <aside class="sidebar workspace-canonical-sidebar" id="sidebar"><a class="brand workspace-nav-brand" href="StudentDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Student workspace</p><nav class="workspace-nav-primary" aria-label="Student workspace pages"><a class="workspace-nav-link" href="StudentDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="ExploreOpportunities.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Explore Opportunities</span></a><a class="workspace-nav-link" href="MyApplications.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>My Applications</span></a><a class="workspace-nav-link" href="MyTasks.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>My Tasks</span></a><a class="workspace-nav-link active" href="MyAttendance.aspx" aria-current="page"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>My Attendance</span></a><a class="workspace-nav-link" href="WeeklyReports.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Weekly Report Status</span></a><a class="workspace-nav-link" href="MyProgressEvaluation.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>My Progress &amp; Evaluation</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="Profile.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Profile</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
            <div class="sidebar-backdrop" onclick="toggleSidebar()"></div>

            <div class="main">
                <!-- ============ NAVBAR ============ -->
                <header class="topbar">
                    <button type="button" class="menu-btn" onclick="toggleSidebar()" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input id="attendanceSearch" type="search" aria-label="Search attendance history" placeholder="Search attendance by date or status..." />
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
    <div class="page-heading"><div><p class="eyebrow">TRAINING ATTENDANCE</p><h1>My Attendance</h1><p>Your training, attendance and hours.</p></div><div class="attendance-heading-actions"><span class="date-chip" id="connectionStatus">Connecting&#8230;</span><button type="button" id="refreshAttendance" class="secondary-action">Refresh attendance</button></div></div>
    <span id="modeMessage" class="visually-hidden"></span>
    <div id="pageNotice" class="page-notice" role="status" hidden></div>
    <section class="row g-3 metric-row" aria-label="Training hours">
        <div class="col-sm-4"><div class="metric-card blue"><span>Approved hours</span><strong id="approvedHours">&#8212;</strong><small>Verified and approved only</small></div></div>
        <div class="col-sm-4"><div class="metric-card ice"><span>Remaining hours</span><strong id="remainingHours">&#8212;</strong><small>Until your training requirement is met</small></div></div>
        <div class="col-sm-4"><div class="metric-card"><span>Required hours</span><strong id="requiredHours">&#8212;</strong><small>Defined by your training assignment</small></div></div>
    </section>
    <div class="row g-4 align-items-stretch student-attendance-grid">
        <div class="col-xl-7">
            <section class="attendance-panel" aria-labelledby="todayHeading"><div class="panel-heading"><h2 id="todayHeading">Today's attendance</h2><span id="todayStatus" class="attendance-badge">&#8212;</span></div>
                <p class="section-note" id="scheduleDate"></p>
                <div class="schedule-grid"><div><span>Training starts</span><strong id="scheduledStart">&#8212;</strong></div><div><span>Training ends</span><strong id="scheduledEnd">&#8212;</strong></div><div><span>Actual check-in</span><strong id="actualCheckIn">&#8212;</strong></div><div><span>Actual check-out</span><strong id="actualCheckOut">&#8212;</strong></div></div>
                <div class="timeline"><i class="bi bi-info-circle" aria-hidden="true"></i><span id="scheduleHelp"></span></div>
                <div class="attendance-actions student-qr-actions"><button id="checkInButton" type="button" disabled class="primary-action">Show check-in QR</button><button id="checkOutButton" type="button" disabled class="primary-action">Show check-out QR</button><span id="qrAvailability" class="qr-availability" role="status"></span></div>
                <div id="resetTodayTools" class="attendance-reset-tools" hidden><button id="resetTodayButton" type="button" class="secondary-action" disabled>Reset today’s attendance</button><p class="section-note">Local testing only &middot; previous attempts are preserved.</p></div>
                <button id="earlyButton" type="button" disabled class="text-action">Request early check-out</button><p id="earlyStatus" class="section-note" aria-live="polite"></p>
            </section>

        </div>
        <aside class="col-xl-5">
            <section class="attendance-panel assignment-panel"><p class="eyebrow">YOUR TRAINING</p><h2>Assignment details</h2><dl class="assignment-list"><div><dt>Student</dt><dd id="studentName">&#8212;</dd></div><div><dt>Company</dt><dd id="companyName">&#8212;</dd></div><div><dt>Company supervisor</dt><dd id="supervisorName">&#8212;</dd></div><div><dt>Training period</dt><dd id="trainingPeriodSummary">&#8212;</dd></div><div><dt>Training progress</dt><dd id="progressText">&#8212;</dd></div></dl><progress id="trainingProgress" max="1" value="0" aria-label="Approved training progress"></progress></section>

        </aside>
    </div>
            <section class="attendance-panel student-history-panel" aria-labelledby="historyHeading"><div class="panel-heading"><h2 id="historyHeading">Attendance history</h2><label class="filter-label" for="historyFilter">Status<select id="historyFilter" class="form-select"><option value="all">All records</option><option value="approved">Approved hours</option><option value="pending">Pending hours</option><option value="absent">Absent</option></select></label></div>
                <div class="table-responsive"><table class="table attendance-table"><caption class="visually-hidden">Attendance records; hours count only when approved</caption><thead><tr><th scope="col">Day</th><th scope="col">Check-in</th><th scope="col">Check-out</th><th scope="col">Worked</th><th scope="col">Attendance</th><th scope="col">Hours</th></tr></thead><tbody id="historyRows"></tbody></table></div><p id="historyEmpty" class="empty-message" hidden>No matching attendance records.</p>
            </section>
            <details class="attendance-panel quick-help"><summary>How attendance works</summary><ol class="steps-list"><li>Display your temporary QR when the action is available.</li><li>Your assigned company supervisor checks your identity and scans it.</li><li>A valid scan records attendance automatically. Hours are approved separately.</li></ol><p class="section-note">Check-in and check-out are available at any hour within your training period. Worked hours count after your supervisor approves them.</p></details>
    <dialog id="qrDialog" class="attendance-dialog" aria-labelledby="qrTitle" aria-describedby="qrDescription">
        <div class="panel-heading"><div><p class="eyebrow">TRAINING ATTENDANCE</p><h2 id="qrTitle">Attendance QR</h2></div><button type="button" class="dialog-close" data-close-dialog="qrDialog" aria-label="Close QR">&times;</button></div>
        <p id="qrDescription" class="section-note">Show this code to your assigned company supervisor.</p>
        <div class="qr-placeholder"><div id="qrCode" aria-label="Temporary attendance QR code"></div><strong id="qrCountdown" role="status" aria-live="off"></strong></div>
        <p id="qrInstruction" class="qr-instruction"><i class="bi bi-shield-check" aria-hidden="true"></i><span>Your assigned supervisor scans this QR to record attendance. The code expires after 2 minutes.</span></p>
        <div class="attendance-actions dialog-footer"><button type="button" class="secondary-action" data-close-dialog="qrDialog">Close</button></div>
    </dialog>
    <dialog id="resetTodayDialog" class="attendance-dialog" aria-labelledby="resetTodayHeading" aria-describedby="resetTodayExplanation">
        <div class="panel-heading"><h2 id="resetTodayHeading">Reset today’s attendance?</h2><button type="button" class="dialog-close" data-close-dialog="resetTodayDialog" aria-label="Cancel reset">&times;</button></div>
        <p id="resetTodayExplanation">This resets your check-in, check-out and QR codes for today. A copy of this attempt is preserved. Your schedule and other days stay unchanged. Saved reports and files are not edited; live hours displayed in reports may change.</p>
        <p id="resetTodayImpact" class="page-notice"></p>
        <p class="section-note">After resetting, ask your company supervisor to issue a new check-in QR.</p>
        <p id="resetTodayError" class="error-message" role="alert" hidden></p>
        <div class="attendance-actions"><button type="button" class="secondary-action" data-close-dialog="resetTodayDialog">Cancel</button><button type="button" class="primary-action" id="confirmResetToday">Reset attendance</button></div>
    </dialog>
    <dialog id="earlyDialog" class="attendance-dialog" aria-labelledby="earlyTitle"><div class="panel-heading"><h2 id="earlyTitle">Request early check-out</h2><button type="button" class="dialog-close" data-close-dialog="earlyDialog" aria-label="Close early check-out request">&times;</button></div><p class="section-note">Your supervisor must approve the request before an early check-out QR becomes available.</p><label for="earlyReason" class="form-label">Reason *</label><textarea id="earlyReason" class="form-control" rows="4" maxlength="500" placeholder="Explain why you need to leave early."></textarea><p id="reasonError" class="error-message" role="alert" hidden>Please enter a reason.</p><div class="attendance-actions"><button type="button" class="secondary-action" data-close-dialog="earlyDialog">Cancel</button><button type="button" class="primary-action" id="sendEarlyRequest">Send request</button></div></dialog>
    <noscript><p>Enable JavaScript to load attendance and display QR codes.</p></noscript>
</main>

            </div>
        </div>
    </form>
    <script src="../Js_project/firebase-client.js?v=20261024-attendance-session"></script>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Student/attendance-qrcodegen.js?v=nayuki-local-20261024"></script>
    <script src="../Js_project/Student/my-attendance.js?v=20261025-student-equal-panels-v3"></script>
</body>
</html>
