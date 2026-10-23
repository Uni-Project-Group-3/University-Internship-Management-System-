<%@ Page Language="C#" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>My Exams | JUST Internships</title><link href="../Assets/student/TaskExams.css" rel="stylesheet" />
    <link href="../Assets/shared/workflow-ui.css?v=20261023" rel="stylesheet" />
</head>
<body>
    <main class="exam-shell">
        <header class="exam-header"><div><a href="MyTasks.aspx">← My Tasks</a><h1>My Exams</h1><p>Published exams for your verified internship placement.</p></div></header>
        <div id="notice" class="notice" role="status">Loading Firebase exam records…</div>
        <section id="examList" class="exam-list" aria-live="polite"></section>
    </main>
    <dialog id="attemptDialog" class="exam-dialog" aria-labelledby="attemptTitle">
        <button type="button" class="exam-close" id="closeAttempt" aria-label="Close exam">×</button>
        <h2 id="attemptTitle">Your examination</h2>
        <div id="attemptNotice" class="notice" role="status" aria-live="polite" hidden></div>
        <section id="attemptPanel" class="panel hidden" aria-live="polite"></section>
    </dialog>
    <dialog id="examConfirmation" class="exam-dialog exam-confirm" aria-labelledby="examConfirmationTitle" aria-describedby="examConfirmationText">
        <h2 id="examConfirmationTitle">Confirm action</h2>
        <p id="examConfirmationText"></p>
        <div class="actions"><button type="button" class="secondary" id="examConfirmCancel">Cancel</button><button type="button" class="primary" id="examConfirmAccept">Confirm</button></div>
    </dialog>
    <script src="../Js_project/Student/TaskExams.js"></script>
</body>
</html>
