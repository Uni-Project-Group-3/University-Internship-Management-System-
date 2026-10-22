<%@ Page Language="C#" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" /><meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>My Exams | JUST Internships</title><link href="../Assets/student/TaskExams.css" rel="stylesheet" />
</head>
<body>
    <main class="exam-shell">
        <header class="exam-header"><div><a href="MyTasks.aspx">← My Tasks</a><h1>My Exams</h1><p>Published exams for your verified internship placement.</p></div></header>
        <div id="notice" class="notice" role="status">Loading Firebase exam records…</div>
        <section id="examList" class="exam-list" aria-live="polite"></section>
        <section id="attemptPanel" class="panel hidden" aria-live="polite"></section>
    </main>
    <script src="../Js_project/Student/TaskExams.js"></script>
</body>
</html>
