<%@ Page Language="C#" %>

<script runat="server">
    protected void btnLogout_Click(object sender, System.EventArgs e)
    {
        Session.Clear();
        Session.Abandon();
        Response.Redirect("~/Auth/Login.aspx");
    }
</script>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Final Completion Review | JUST Internships</title>
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="../Assets/shared/colors.css" rel="stylesheet" />
    <link href="../Assets/coordinator/final-completion-review.css?v=20261021-original-shell" rel="stylesheet" />
</head>
<body>
    <form id="reviewForm" runat="server">
        <div class="app">
            <!-- سايد بار المنسق داخل الصفحة مباشرة حسب التنظيم المتفق عليه -->
            <aside class="sidebar" id="coordinatorSidebar" aria-label="Coordinator navigation">
                <button type="button" id="closeSidebar" class="sidebar-close" aria-label="Close navigation">&times;</button>
                <a href="CoordinatorDashboard.aspx" class="brand">
                    <img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="Jordan University of Science and Technology logo" width="52" height="52" />
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav">
                    <li><a href="CoordinatorDashboard.aspx"><i class="bi bi-house-door"></i>Dashboard</a></li>
                    <li><a href="ManageStudents.aspx"><i class="bi bi-mortarboard"></i>Manage Students</a></li>
                    <li><a href="ManageCompanies.aspx"><i class="bi bi-building"></i>Manage Companies</a></li>
                    <li><a href="ManageSupervisors.aspx"><i class="bi bi-person-badge"></i>Academic Supervisors</a></li>
                    <li><a href="AssignSupervisors.aspx"><i class="bi bi-diagram-3"></i>Assign Supervisors</a></li>
                    <li><a href="MonitorPlacements.aspx"><i class="bi bi-activity"></i>Placements &amp; Progress</a></li>
                    <li><a href="FinalCompletionReview.aspx" class="active" aria-current="page"><i class="bi bi-patch-check"></i>Final Completion Review<span class="count">4</span></a></li>
                    <li><a href="ContactMessages.aspx"><i class="bi bi-envelope"></i>Contact Messages</a></li>
                </ul>

                <div class="side-divider"></div>

                <ul class="side-nav">
                    <li><a href="Account.aspx"><i class="bi bi-gear"></i>Coordinator Account</a></li>
                </ul>

                <div class="side-promo">
                    <i class="bi bi-person-plus"></i>
                    <p>New semester?<br />Add students and assign groups.</p>
                    <a href="ManageStudents.aspx">Add students &rarr;</a>
                </div>
            </aside>
            <div id="sidebarBackdrop" class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main" id="coordinatorMain">
                <header class="topbar">
                    <button type="button" class="menu-btn" id="openSidebar" aria-label="Open navigation" aria-controls="coordinatorSidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input id="navbarSearch" type="search" aria-label="Workspace search" placeholder="Search for students, companies, or supervisors..." />
                    </div>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" id="navbarNotifications" aria-label="Notifications preview"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown" aria-expanded="false" aria-label="Account menu">
                                <span class="avatar"><asp:Label ID="lblInitials" runat="server" Text="CO" /></span>
                                <span class="name"><asp:Label ID="lblUserName" runat="server" Text="Coordinator" /><span class="role">Internship Coordinator</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="Account.aspx"><i class="bi bi-person me-2"></i>Account</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><asp:LinkButton ID="btnLogout" runat="server" CssClass="dropdown-item text-danger" OnClick="btnLogout_Click" CausesValidation="false"><i class="bi bi-box-arrow-right me-2"></i>Log out</asp:LinkButton></li>
                            </ul>
                        </div>
                    </div>
                </header>
                <p id="navbarNotice" class="navbar-notice" role="status" hidden></p>
        <main class="review-page">
            <header class="page-heading">
                <p class="eyebrow">JUST / COORDINATOR WORKSPACE</p>
                <h1>Final Completion Review</h1>
                <p>Review the evaluations, verify requirements, and approve internship completion.</p>
            </header>

            <p class="preview-note">Frontend preview — students, evaluations, hours, and completion criteria are hypothetical examples, not official JUST requirements. Approval is not saved. After preview approval, you can print a sample report or save it as PDF.</p>

            <section class="selection-panel" aria-label="Select student">
                <div><label for="studentSelect" class="form-label">Student to review</label><select id="studentSelect" class="form-select"><option value="sample-ready">Ahmad Ali · 20230001</option><option value="sample-incomplete">Sara Omar · 20230002</option></select></div>
                <span id="reviewStatus" class="status-badge status-pending" role="status">Awaiting review</span>
            </section>

            <div class="row g-4 align-items-start">
                <div class="col-lg-8">
                    <section class="review-card" aria-labelledby="studentHeading">
                        <div class="section-heading"><span class="section-number" aria-hidden="true">01</span><h2 id="studentHeading">Student &amp; placement</h2></div>
                        <dl class="information-grid">
                            <div><dt>Student</dt><dd id="studentName"></dd></div>
                            <div><dt>Student ID</dt><dd id="studentNumber"></dd></div>
                            <div><dt>University / College</dt><dd>JUST / Information Technology</dd></div>
                            <div><dt>Specialization</dt><dd id="studentMajor"></dd></div>
                            <div><dt>Company</dt><dd id="companyName"></dd></div>
                            <div><dt>Internship</dt><dd id="internshipTitle"></dd></div>
                            <div><dt>Company supervisor</dt><dd id="companySupervisor"></dd></div>
                            <div><dt>Academic supervisor</dt><dd id="academicSupervisor"></dd></div>
                        </dl>
                    </section>

                    <!-- التقييمان مخصصان للمنسق، ولا يعرضان للطالب -->
                    <section class="review-card" aria-labelledby="companyHeading">
                        <div class="evaluation-heading"><h2 id="companyHeading">Company final evaluation</h2><span id="companyStatus" class="status-badge"></span></div>
                        <p class="section-description">Submitted by the company supervisor and reviewed by the academic supervisor.</p>
                        <dl class="evaluation-facts"><div><dt>Submitted on</dt><dd id="companyDate"></dd></div><div><dt>Recommendation</dt><dd id="companyRecommendation"></dd></div></dl>
                        <h3>Supervisor feedback</h3><p id="companyFeedback" class="feedback-text"></p>
                    </section>

                    <section class="review-card" aria-labelledby="academicHeading">
                        <div class="evaluation-heading"><h2 id="academicHeading">Final academic evaluation</h2><span id="academicStatus" class="status-badge"></span></div>
                        <p class="section-description">Sent to the coordinator by the assigned academic supervisor.</p>
                        <dl class="evaluation-facts"><div><dt>Submitted on</dt><dd id="academicDate"></dd></div><div><dt>Recommendation</dt><dd id="academicRecommendation"></dd></div></dl>
                        <h3>Academic feedback</h3><p id="academicFeedback" class="feedback-text"></p>
                        <p class="preview-note">Evaluation scores and the calculated final result will follow the agreed university evaluation method after integration. No grading scale is assumed in this preview.</p>
                    </section>

                    <section class="review-card" aria-labelledby="requirementsHeading">
                        <div class="section-heading"><span class="section-number" aria-hidden="true">02</span><h2 id="requirementsHeading">Completion requirements</h2></div>
                        <div class="hours-heading"><h3>Approved training hours</h3><span id="hoursText"></span></div>
                        <progress id="hoursProgress" aria-label="Approved training hours"></progress>
                        <p class="section-description">Only approved hours count. Completed tasks do not automatically approve hours.</p>
                        <ul id="requirementsList" class="requirements-list"></ul>
                        <p class="preview-note">The example uses a hypothetical 240-hour requirement. Replace it with the student's configured requirements.</p>
                    </section>
                </div>

                <aside class="col-lg-4" aria-labelledby="decisionHeading">
                    <section class="review-card decision-panel">
                        <p class="eyebrow">FINAL STEP</p>
                        <h2 id="decisionHeading">Completion decision</h2>
                        <p id="eligibilityMessage" class="section-description" aria-live="polite"></p>
                        <label for="reviewNotes" class="form-label">Coordinator notes <span>(optional)</span></label>
                        <textarea id="reviewNotes" class="form-control" rows="4" maxlength="1000" placeholder="Add notes about your review."></textarea>
                        <div class="form-check review-check"><input id="reviewAcknowledgment" class="form-check-input" type="checkbox" /><label for="reviewAcknowledgment" class="form-check-label">I reviewed both final evaluations and verified the completion requirements.</label></div>
                        <button type="button" id="approveButton" class="primary-button" data-bs-toggle="modal" data-bs-target="#approvalModal" disabled>Approve completion <span aria-hidden="true">&rarr;</span></button>
                        <div class="report-section">
                            <h3>Official completion report</h3>
                            <p class="section-description">After actual approval, generate the PDF and send it to the student's My Progress &amp; Evaluation page for download and printing.</p>
                            <button type="button" id="generateReport" class="secondary-button" disabled>Preview completion report</button>
                            <p class="preview-note">After preview approval, print the sample report or choose Save as PDF. Official delivery requires backend integration.</p>
                        </div>
                    </section>
                </aside>
            </div>

            <div id="approvalNotice" class="approval-notice" role="status" tabindex="-1" hidden></div>

            <!-- هذا القسم وحده يظهر عند الطباعة، دون التقييمات أو ملاحظات المنسق -->
            <section id="completionReport" class="completion-report" aria-labelledby="reportTitle" tabindex="-1" hidden>
                <div class="report-toolbar">
                    <p>Report preview — choose Save as PDF in the print dialog.</p>
                    <div><button type="button" id="closeReport" class="secondary-button">Close preview</button><button type="button" id="printReport" class="primary-button">Print / Save as PDF</button></div>
                </div>
                <article class="report-paper">
                    <header class="report-header">
                        <p class="eyebrow">JORDAN UNIVERSITY OF SCIENCE AND TECHNOLOGY</p>
                        <p>Information Technology · University Internship Management System</p>
                        <h2 id="reportTitle">Internship Completion Report</h2>
                        <p class="report-disclaimer">SAMPLE — FOR DEMONSTRATION ONLY · NOT AN OFFICIAL DOCUMENT</p>
                    </header>
                    <p class="report-introduction">This sample report records the preview approval of internship completion for the student below, based on hypothetical training requirements.</p>
                    <dl class="information-grid report-information">
                        <div><dt>Student name</dt><dd id="reportName"></dd></div>
                        <div><dt>Student ID</dt><dd id="reportNumber"></dd></div>
                        <div><dt>Specialization</dt><dd id="reportMajor"></dd></div>
                        <div><dt>Company</dt><dd id="reportCompany"></dd></div>
                        <div><dt>Internship</dt><dd id="reportInternship"></dd></div>
                        <div><dt>Training period (sample)</dt><dd id="reportPeriod"></dd></div>
                        <div><dt>Approved training hours</dt><dd id="reportHours"></dd></div>
                        <div><dt>Required training hours (hypothetical)</dt><dd id="reportRequiredHours"></dd></div>
                        <div><dt>Completion status</dt><dd>Approved in frontend preview</dd></div>
                        <div><dt>Preview approval date (Amman)</dt><dd id="reportApprovalDate"></dd></div>
                    </dl>
                    <footer class="report-footer"><p>This report contains fictional data and does not certify actual internship completion. No official approval, signature, or delivery has been recorded.</p></footer>
                </article>
            </section>

            <div class="modal fade" id="approvalModal" tabindex="-1" aria-labelledby="approvalTitle" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered"><div class="modal-content">
                    <div class="modal-header"><h2 id="approvalTitle" class="modal-title fs-5">Approve internship completion?</h2><button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button></div>
                    <div class="modal-body"><p id="approvalSummary"></p><p>You are confirming that both evaluations and the completion requirements have been reviewed.</p><p class="preview-note">This is a preview only. No approval, notes, or report will be saved or sent.</p></div>
                    <div class="modal-footer"><button type="button" class="secondary-button" data-bs-dismiss="modal">Keep reviewing</button><button type="button" id="confirmApproval" class="primary-button">Confirm approval</button></div>
                </div></div>
            </div>
            <noscript><p>Enable JavaScript to load the sample student review and preview completion approval.</p></noscript>
        </main>
            </div>
        </div>
    </form>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Coordinator/final-completion-review.js?v=20261021-original-shell"></script>
</body>
</html>
