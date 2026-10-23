<%@ Page Language="C#" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Task Exams | JUST Internships</title>
    <link href="../Assets/company/TaskExams.css" rel="stylesheet" />
    <link href="../Assets/shared/workflow-ui.css?v=20261023" rel="stylesheet" />
</head>
<body>
    <main class="exam-shell">
        <header class="exam-header"><div><a href="ManageTasks.aspx">← Manage Tasks</a><h1>Task Exams</h1><p>Create and publish versioned exams linked to existing placement tasks.</p></div><button id="newExam" class="primary" type="button">New exam</button></header>
        <div id="notice" class="notice" role="status">Loading Firebase exam records…</div>
        <div class="exam-layout">
            <section class="panel exam-list"><h2>Your exams</h2><div id="examList" class="stack"><p class="muted">Loading…</p></div></section>
            <section class="panel editor" aria-labelledby="editorTitle">
                <h2 id="editorTitle">Create exam draft</h2>
                <form id="examForm">
                    <input id="examId" type="hidden" /><input id="draftRevision" type="hidden" value="0" />
                    <div class="grid two"><label>Placement ID<input id="placementId" required maxlength="160" autocomplete="off" /></label><label>Task ID<input id="taskId" required maxlength="160" autocomplete="off" /></label></div>
                    <label>Exam title<input id="title" required maxlength="180" /></label>
                    <label>Instructions<textarea id="instructions" rows="2" maxlength="4000"></textarea></label>
                    <div class="grid two"><label>Opens at<input id="opensAt" type="datetime-local" required /></label><label>Closes at<input id="closesAt" type="datetime-local" required /></label></div>
                    <div class="grid two"><label>Duration (minutes)<input id="duration" type="number" min="1" max="600" value="30" required /></label><label>Attempt limit<input id="attemptLimit" type="number" min="1" max="10" value="1" required /></label></div>
                    <div class="question-head"><h3>Questions</h3><button type="button" id="addQuestion" class="secondary">Add question</button></div>
                    <div id="questions" class="stack"></div>
                    <div class="actions"><button id="saveDraft" type="button" class="secondary">Save draft</button><button id="publishExam" type="button" class="primary">Publish exam</button></div>
                </form>
            </section>
        </div>
    </main>
    <script src="../Js_project/Company/TaskExams.js"></script>
</body>
</html>
