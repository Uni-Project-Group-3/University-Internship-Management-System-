<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="MyTasks.aspx.cs" Inherits="University_Internship_Management_System.Student.MyTasks" %>

<!DOCTYPE html>
<html lang="en" dir="ltr">
    <head runat="server">
        <meta charset="utf-8" />
        <meta name="viewport" content="width=device-width, initial-scale=1" />
        <title>My Tasks | JUST Internships</title>
        <link href="../Content/bootstrap.min.css" rel="stylesheet" />
        <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />

        <link href="../Assets/shared/colors.css" rel="stylesheet" />
        <link href="../Assets/student/MyTasks.css?v=20261021-studio2" rel="stylesheet" />
    </head>
    <body data-portal="student">
        <form id="form1" runat="server">
            <div class="app">

                <!-- ============ SIDEBAR (Student) ============ -->
                <aside class="sidebar" id="sidebar" aria-label="Student navigation">
                    <button type="button" class="sidebar-close" aria-label="Close navigation">
                        <i class="bi bi-x-lg" aria-hidden="true">
                        </i>
                    </button>
                    <a href="StudentDashboard.aspx" class="brand">
                        <img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="48" height="48" />
                        <div class="brand-text">
                            <strong>JUST</strong>
                            <span>Internships</span>
                        </div>
                    </a>

                    <ul class="side-nav">
                        <li>
                            <a href="StudentDashboard.aspx">
                                <i class="bi bi-house-door" aria-hidden="true">
                                </i>
                                <span>Dashboard</span>
                            </a>
                        </li>
                        <li>
                            <a href="ExploreOpportunities.aspx">
                                <i class="bi bi-compass" aria-hidden="true">
                                </i>
                                <span>Explore Opportunities</span>
                            </a>
                        </li>
                        <li>
                            <a href="MyApplications.aspx">
                                <i class="bi bi-file-earmark-text" aria-hidden="true">
                                </i>
                                <span>My Applications</span>
                            </a>
                        </li>
                        <li>
                            <a href="ApplicationForm.aspx">
                                <i class="bi bi-file-earmark-plus" aria-hidden="true">
                                </i>
                                <span>Application Form</span>
                            </a>
                        </li>
                        <li>
                            <a href="MyTasks.aspx" class="active" aria-current="page">
                                <i class="bi bi-list-check" aria-hidden="true">
                                </i>
                                <span>My Tasks</span>
                            </a>
                        </li>
                        <li>
                            <a href="MyProgressEvaluation.aspx">
                                <i class="bi bi-graph-up-arrow" aria-hidden="true">
                                </i>
                                <span>Progress &amp; Evaluation</span>
                            </a>
                        </li>
                        <li>
                            <a href="Profile.aspx">
                                <i class="bi bi-person" aria-hidden="true">
                                </i>
                                <span>My Profile</span>
                            </a>
                        </li>
                    </ul>

                    <div class="side-divider">
                    </div>

                    <ul class="side-nav">
                        <li>
                            <a aria-disabled="true" title="Page not available yet">
                                <i class="bi bi-gear" aria-hidden="true">
                                </i>Settings</a>
                        </li>
                        <li>
                            <a href="../Contact.aspx">
                                <i class="bi bi-question-circle" aria-hidden="true">
                                </i>Help</a>
                        </li>
                        <li>
                            <asp:LinkButton ID="btnLogout" runat="server" OnClick="btnLogout_Click" CssClass="text-danger">
                                <i class="bi bi-box-arrow-right" aria-hidden="true">
                                </i>Logout</asp:LinkButton>
                        </li>
                    </ul>

                    <div class="side-promo">
                        <i class="bi bi-mortarboard-fill" aria-hidden="true">
                        </i>
                        <p>Build your future<br />with real experience.</p>
                        <a href="ExploreOpportunities.aspx">Explore opportunities &rarr;</a>
                    </div>
                </aside>
                <div class="sidebar-backdrop" aria-hidden="true">
                </div>

                <div class="main">
                    <!-- TOPBAR -->
                    <header class="topbar">
                        <button type="button" class="menu-btn d-lg-none" aria-label="Open navigation" aria-controls="sidebar" aria-expanded="false">
                            <i class="bi bi-list" aria-hidden="true">
                            </i>
                        </button>
                        <div class="topbar-actions ms-auto">
                            <div class="user-chip">
                                <span class="avatar">ST</span>
                                <span class="name">Student Portal<span class="role">Student</span>
                                </span>
                            </div>
                        </div>
                    </header>

                    <!-- CONTENT -->
                    <div class="content c3-workspace" id="cycle3Workspace">
    <p class="c3-demo" role="note">Your current-placement tasks are loaded from Firebase. Cycle 3 exams require a verified identity mapping.</p>
    <div id="pageNotice" role="status" aria-live="polite" hidden></div>
    <div class="c3-heading"><div><span class="c3-eyebrow">UIMS / TRAINING WORKSPACE</span><h1 id="c3Heading">Training Tasks</h1><p id="c3Subtitle">Plan focused work. Review each student's progress independently.</p></div><div class="c3-actions"><button type="button" id="c3Create" class="c3-primary" data-t="create">+ Create Task</button></div></div>
    <div id="c3Stats" class="c3-stats" aria-live="polite"></div>
    <div class="c3-toolbar"><input type="search" id="taskSearch" aria-label="Search tasks" placeholder="Search tasks…" /><select id="c3Student" aria-label="Student"></select><select id="c3Week" aria-label="Week"></select><select id="c3Priority" aria-label="Priority"></select><select id="c3Status" aria-label="Status"></select><select id="c3Sort" aria-label="Sort"><option value="asc">Deadline ↑</option><option value="desc">Deadline ↓</option></select><button type="button" id="c3View" data-t="list">List view</button><label id="c3GroupLabel"><input type="checkbox" id="c3Group" /> <span data-t="group">Group tasks</span></label><button type="button" id="c3Drafts" data-t="drafts">Drafts</button><button type="button" id="c3Reports" data-t="reports">Weekly reports</button><button type="button" id="c3Clear" data-t="clear">Clear filters</button></div>
    <div id="taskList" class="c3-board" aria-label="Training task board"></div><p id="noTasksMsg" class="c3-empty" hidden>No matching tasks for this placement. Adjust the filters.</p>
</div>

</div></div>

<dialog id="c3Dialog" class="c3-dialog" aria-labelledby="c3DialogTitle"><header><div><small id="c3DialogHint">TRAINING WORKSPACE</small><h2 id="c3DialogTitle"></h2></div><button type="button" id="c3Close" aria-label="Close dialog">×</button></header><p id="c3Error" role="alert" hidden></p><div id="c3DialogBody" class="c3-dialog-body"></div><footer id="c3DialogActions"></footer></dialog>
<dialog id="c3Confirm" class="c3-confirm" aria-labelledby="c3ConfirmTitle"><h2 id="c3ConfirmTitle">Confirm action</h2><p id="c3ConfirmText"></p><div class="c3-actions"><button type="button" id="c3ConfirmCancel">Cancel</button><button type="button" id="c3ConfirmYes" class="c3-primary">Confirm</button></div></dialog>

<dialog id="c3ImageViewer" class="c3-image-viewer" aria-label="Question image"><button type="button" id="c3ImageClose" aria-label="Close image">×</button><img alt="Question illustration" /></dialog>
        </form>
        <script src="../Scripts/bootstrap.bundle.min.js">
        </script>
        <script src="../Js_project/Student/MyTasks.js?v=20261021-studio2">
        </script>
    <script src="../Js_project/Public/tab-session.js" defer="defer"></script>
</body>
</html>
