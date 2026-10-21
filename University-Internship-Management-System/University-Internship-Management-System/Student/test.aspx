<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="test.aspx.cs" Inherits="University_Internship_Management_System.Student.test" %>
<%@ Register Src="~/Shared/Sidebar.ascx" TagPrefix="shared" TagName="Sidebar" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <title>Student Dashboard | UIMS</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/sidebar.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/student/student.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/completion.css" rel="stylesheet" />
</head>
<body class="app-page completion-page">
    <form id="form1" runat="server">
        <div class="app-layout">
            <shared:Sidebar runat="server" HomeUrl="~/Student/test.aspx" UserRole="Student" />
            <main class="app-content">
                <p class="completion-eyebrow">YOUR TRAINING JOURNEY</p><h1>Student Dashboard</h1><p class="completion-subtitle">Your internship milestones and completion documents.</p>
                <p class="completion-notice">Interface preview only. No approved internship is loaded. Firebase, certificates, and automatic deactivation are not connected.</p>
                <section class="completion-card" aria-labelledby="completionCardTitle"><span class="completion-symbol" aria-hidden="true">&#10003;</span><div><p class="completion-eyebrow">COMPLETION &amp; CERTIFICATE</p><h2 id="completionCardTitle">A clear next step for your internship</h2><p>When the Coordinator approves completion, your confirmation notification will remain available here until acknowledged.</p><div class="completion-actions"><button type="button" class="completion-button" data-completion-open="pending">Preview completion notification</button><button type="button" class="completion-button completion-button--secondary" data-completion-open="grace">Preview certificate access</button></div></div></section>
                <section class="completion-card completion-card--compact"><div><h2>Your completion document</h2><p>The certificate becomes available after confirmation and successful document preparation. No certificate is loaded in this preview.</p><div class="completion-actions"><button type="button" class="completion-button completion-button--secondary" disabled>Download Certificate</button><button type="button" class="completion-button completion-button--secondary" disabled>Print Certificate</button></div></div><span class="completion-badge">Not available</span></section>
            </main>
        </div>
<dialog id="completionDialog" class="completion-dialog" aria-labelledby="completionDialogTitle" aria-describedby="completionDialogNote">
<header class="completion-dialog-header"><div><p class="completion-eyebrow">INTERNSHIP COMPLETION</p><h2 id="completionDialogTitle">Confirm your completion</h2></div><button type="button" class="completion-close" data-completion-close aria-label="Close completion popup">&times;</button></header>
<div class="completion-dialog-body"><p id="completionDialogNote" class="completion-notice">Layout preview only. No student confirmation, timestamp, certificate, or account change is recorded.</p>
<div data-completion-view="pending"><span class="completion-symbol" aria-hidden="true">&#10003;</span><h3>Your next milestone</h3><p class="completion-subtitle">After the Coordinator approves your internship, review the approved details and acknowledge completion here.</p><dl class="completion-info"><div><dt>Training company</dt><dd>&mdash;</dd></div><div><dt>Approved training hours</dt><dd>&mdash;</dd></div><div><dt>Completion approval date</dt><dd>&mdash;</dd></div><div><dt>Internship</dt><dd>No approved record loaded</dd></div></dl><div class="completion-warning"><strong>One hour to access your certificate</strong><p>Confirming completion will start a 60-minute grace period. Your account will be disabled after that period. Review Later does not start it.</p></div><p class="completion-help">Your certificate will become available after confirmation and successful preparation. If preparation fails, a recovery action will be provided.</p><div class="completion-actions"><button type="button" class="completion-button completion-button--secondary" data-completion-close>Review Later</button><button type="button" class="completion-button" id="previewConfirmCompletion">Confirm Completion</button></div><p id="completionConfirmMessage" role="status" class="completion-feedback"></p></div>
<div data-completion-view="grace" hidden><span class="completion-badge">Grace-period layout preview</span><h3>Your certificate access window</h3><p class="completion-subtitle">Download and print your certificate while your account remains enabled during the grace period.</p><dl class="completion-info"><div><dt>Student confirmed at (UTC)</dt><dd>&mdash;</dd></div><div><dt>Account disable deadline (UTC)</dt><dd>&mdash;</dd></div><div><dt>Certificate ID</dt><dd>&mdash;</dd></div><div><dt>Certificate status</dt><dd>Not connected</dd></div></dl><div class="completion-document"><strong>Completion certificate</strong><p>No official PDF is available. Document generation and secure access will be connected later.</p><div class="completion-actions"><button type="button" class="completion-button" disabled>Download Certificate</button><button type="button" class="completion-button completion-button--secondary" disabled>Print Certificate</button></div></div><p class="completion-help">If certificate preparation fails, this area will show a retry or assistance option. No countdown is started in this preview.</p><button type="button" class="completion-button completion-button--secondary" data-completion-close>Close</button></div></div></dialog>
    </form>
    <script src="<%= ResolveUrl("~/Js_project/Public/completion.js") %>"></script>
<script src="../Js_project/Public/tab-session.js" defer="defer"></script>
</body>
</html>
