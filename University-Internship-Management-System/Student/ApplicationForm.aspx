<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ApplicationForm.aspx.cs" Inherits="University_Internship_Management_System.Student.ApplicationForm" %>

<script src="../Js_project/Demo/demo-store.js?v=20261023-hide-unavailable-notice"></script>
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
    <title>Application Form | JUST Internships</title>
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="../Assets/shared/colors.css" rel="stylesheet" />
    <link href="../Assets/student/application-form.css?v=20261025-profile-cv" rel="stylesheet" />
<link href="../Assets/shared/workspace-identity.css?v=20261025-progress-stat" rel="stylesheet" />
<script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script>
</head>
<body class="coordinator-banner standalone-shell" data-demo-role="student">
    <form id="applicationForm" runat="server">
        <div class="app">
            <!-- نفس السايد بار المرسل، مكرر داخل الصفحة مع ضبط الروابط المتوفرة -->
            <aside class="sidebar workspace-canonical-sidebar" id="studentSidebar" aria-label="Student navigation"><a class="brand workspace-nav-brand" href="StudentDashboard.aspx"><img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="50" height="50"/><span class="brand-text"><strong>JUST</strong><small>Internships</small></span></a><button type="button" class="workspace-nav-close" data-workspace-close aria-label="Close navigation">&times;</button><p class="workspace-nav-role">Student workspace</p><nav class="workspace-nav-primary" aria-label="Student workspace pages"><a class="workspace-nav-link" href="StudentDashboard.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a><a class="workspace-nav-link" href="ExploreOpportunities.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 7h18v14H3V7Zm5 0V3h8v4M3 12h18"/></svg><span>Explore Opportunities</span></a><a class="workspace-nav-link" href="MyApplications.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>My Applications</span></a><a class="workspace-nav-link" href="MyTasks.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>My Tasks</span></a><a class="workspace-nav-link" href="MyAttendance.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>My Attendance</span></a><a class="workspace-nav-link" href="WeeklyReports.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M6 3h9l4 4v14H6V3Zm9 0v5h4M9 12h7M9 16h7"/></svg><span>Weekly Report Status</span></a><a class="workspace-nav-link" href="MyProgressEvaluation.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 20h18M5 16v-5m6 5V4m6 12V8"/></svg><span>My Progress &amp; Evaluation</span></a></nav><div class="workspace-nav-account"><a class="workspace-nav-link" href="Profile.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M4 21v-3a8 8 0 0 1 16 0v3M16 7a4 4 0 1 1-8 0 4 4 0 0 1 8 0Z"/></svg><span>My Profile</span></a><a class="workspace-nav-link" href="../Contact.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M12 17v1M9 9a3 3 0 1 1 5 2c-2 1-2 2-2 3M21 12a9 9 0 1 1-18 0 9 9 0 0 1 18 0Z"/></svg><span>Help &amp; contact</span></a><a class="workspace-nav-link" data-workspace-logout href="../Auth/Login.aspx?account=new"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M10 3H4v18h6m4-15 6 6-6 6M9 12h11"/></svg><span>Log out</span></a></div><div class="workspace-nav-public"><a class="workspace-nav-link workspace-home-link" href="../Public/home.aspx"><svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M3 10 12 3l9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Home</span></a></div></aside>
            <div id="sidebarBackdrop" class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main" id="studentMain">
                <header class="topbar">
                    <button type="button" class="menu-btn" id="openSidebar" aria-label="Open navigation" aria-controls="studentSidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
                    <div class="topbar-actions">
                        <button type="button" class="icon-btn" id="navbarNotifications" aria-label="Notifications preview"><i class="bi bi-bell"></i><span class="dot"></span></button>
                        <div class="dropdown">
                            <button type="button" class="user-chip" data-bs-toggle="dropdown" aria-expanded="false" aria-label="Account menu">
                                <span class="avatar"><asp:Label ID="lblInitials" runat="server" Text="ST" /></span>
                                <span class="name"><asp:Label ID="lblUserName" runat="server" Text="Student" /><span class="role">Student</span></span>
                                <i class="bi bi-chevron-down small"></i>
                            </button>
                            <ul class="dropdown-menu dropdown-menu-end">
                                <li><a class="dropdown-item" href="Profile.aspx"><i class="bi bi-person me-2"></i>My Profile</a></li>
                                <li><a class="dropdown-item" aria-disabled="true" title="This page is not available yet"><i class="bi bi-key me-2"></i>Change Password</a></li>
                                <li><hr class="dropdown-divider" /></li>
                                <li><asp:LinkButton ID="btnLogout" runat="server" CssClass="dropdown-item text-danger" OnClick="btnLogout_Click" CausesValidation="false"><i class="bi bi-box-arrow-right me-2"></i>Log out</asp:LinkButton></li>
                            </ul>
                        </div>
                    </div>
                </header>
                <p id="navbarNotice" class="navbar-notice" role="status" hidden></p>
        <main class="application-page">
            <a class="back-link" href="ExploreOpportunities.aspx"><i class="bi bi-arrow-left"></i> Back to opportunities</a>

            <header class="page-heading">
                <div class="heading-text">
                    <p class="eyebrow"><i class="bi bi-send"></i> JUST / STUDENT WORKSPACE</p>
                    <h1>Application Form</h1>
                    <p>Introduce yourself and take the next step in your internship journey.</p>
                </div>
                <div class="heading-progress" aria-live="polite">
                    <div class="progress-ring" id="progressRing" style="--value: 0">
                        <span id="progressPercent">0%</span>
                    </div>
                    <div>
                        <strong id="progressTitle">Let's get started</strong>
                        <small id="progressText">0 of 3 steps complete</small>
                    </div>
                </div>
            </header>

            <div class="preview-notice" id="previewNotice"><i class="bi bi-info-circle"></i> Frontend preview — your information will not be saved or sent.</div>
            <div id="invalidOpportunity" class="invalid-opportunity" role="alert" hidden>
                <span class="alert-icon"><i class="bi bi-exclamation-triangle"></i></span>
                <div>
                    <strong>No opportunity selected</strong>
                    <p>Please select a valid opportunity from <a href="ExploreOpportunities.aspx">Explore Opportunities</a> before continuing.</p>
                </div>
            </div>
            <div id="alreadyApplied" class="already-applied" role="status" hidden>
                <span class="alert-icon"><i class="bi bi-check2-circle"></i></span>
                <div>
                    <strong>You already applied to this opportunity</strong>
                    <p>Follow its status in <a href="MyApplications.aspx">My Applications</a>.</p>
                </div>
            </div>

            <div class="row g-4 align-items-start">
                <div class="col-lg-8">
                    <fieldset id="applicationFields" disabled>
                        <section class="form-section" data-step="personal" aria-labelledby="personalHeading">
                            <div class="section-heading">
                                <span class="section-number" aria-hidden="true">01</span>
                                <div><h2 id="personalHeading">Personal information</h2><p>Use your university contact details. Fields marked * are required.</p></div>
                            </div>
                            <!-- بيانات الطالب ستعبأ من حسابه عند الربط بالخلفية -->
                            <div class="row g-3">
                                <div class="col-md-6">
                                    <label for="fullName" class="form-label">Full name *</label>
                                    <div class="input-icon">
                                        <i class="bi bi-person"></i>
                                        <input id="fullName" name="fullName" class="form-control" type="text" autocomplete="name" maxlength="100" required />
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <label for="studentNumber" class="form-label">Student ID *</label>
                                    <div class="input-icon">
                                        <i class="bi bi-person-badge"></i>
                                        <input id="studentNumber" name="studentNumber" class="form-control" type="text" maxlength="30" required />
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <label for="email" class="form-label">University email *</label>
                                    <div class="input-icon">
                                        <i class="bi bi-envelope"></i>
                                        <input id="email" name="email" class="form-control" type="email" autocomplete="email" maxlength="150" required />
                                    </div>
                                </div>
                                <div class="col-md-6">
                                    <label for="phone" class="form-label">Phone number *</label>
                                    <div class="input-icon">
                                        <i class="bi bi-telephone"></i>
                                        <input id="phone" name="phone" class="form-control" type="tel" autocomplete="tel" maxlength="25" placeholder="07X XXX XXXX" required />
                                    </div>
                                </div>
                                <div class="col-12">
                                    <label for="major" class="form-label">Specialization *</label>
                                    <div class="input-icon">
                                        <i class="bi bi-mortarboard"></i>
                                        <select id="major" name="major" class="form-select" required>
                                            <option value="">Select your specialization</option>
                                            <option>Computer Science</option>
                                            <option>Software Engineering</option>
                                            <option>Cybersecurity</option>
                                        </select>
                                    </div>
                                    <p class="field-help"><i class="bi bi-bank"></i> University and college: JUST &middot; Information Technology</p>
                                </div>
                            </div>
                        </section>

                        <section class="form-section" data-step="supporting" aria-labelledby="supportHeading">
                            <div class="section-heading">
                                <span class="section-number" aria-hidden="true">02</span>
                                <div><h2 id="supportHeading">Supporting information</h2><p>Share a little about your skills and interests.</p></div>
                            </div>
                            <label for="cvAttachment" class="form-label">CV attachment *</label>
                            <div class="input-icon">
                                <i class="bi bi-file-earmark-person"></i>
                                <input id="cvAttachment" name="cvAttachment" class="form-control" type="file" accept=".pdf,.doc,.docx" aria-describedby="cvHelp" required /><input id="cvLink" type="hidden" /><input id="cvAttachmentId" type="hidden" />
                            </div>
                            <p id="cvHelp" class="field-help"><i class="bi bi-link-45deg"></i> Attach a PDF, DOC or DOCX file up to 10 MiB. Only you and the receiving company can download it.</p>

                            <label for="portfolioLink" class="form-label">Portfolio <span>(optional)</span></label>
                            <div class="input-icon">
                                <i class="bi bi-github"></i>
                                <input id="portfolioLink" name="portfolioLink" class="form-control" type="url" placeholder="https://" maxlength="500" />
                            </div>

                            <div class="message-label">
                                <label for="motivation" class="form-label">Why are you interested? <span>(optional)</span></label>
                                <span id="characterCount">0 / 1000</span>
                            </div>
                            <textarea id="motivation" name="motivation" class="form-control" rows="5" maxlength="1000" placeholder="Tell the team about your interests, relevant skills, and what you hope to learn."></textarea>
                        </section>

                        <section class="form-section review-section" data-step="review" aria-label="Review application">
                            <div class="section-heading">
                                <span class="section-number" aria-hidden="true">03</span>
                                <div><h2>Review &amp; submit</h2><p>Check your details, then confirm they are accurate.</p></div>
                            </div>
                            <label class="confirm-box" for="confirmInformation">
                                <input id="confirmInformation" name="confirmInformation" class="form-check-input" type="checkbox" required />
                                <span>I confirm that the information I provided is accurate. *</span>
                            </label>
                            <div class="form-actions">
                                <a href="ExploreOpportunities.aspx" class="cancel-link">Cancel</a>
                                <button type="submit" class="primary-button"><span id="submitLabel">Preview application</span> <i class="bi bi-arrow-right"></i></button>
                            </div>
                        </section>
                    </fieldset>
                    <div id="previewResult" class="preview-result" role="status" tabindex="-1" hidden>
                        <div class="result-icon"><i class="bi bi-check-lg"></i></div>
                        <h2>Your application is ready for review</h2>
                        <p id="previewSummary"></p>
                        <p>This was a preview only. No application has been submitted or saved.</p>
                        <button type="button" id="editApplication" class="edit-button">Continue editing</button>
                    </div>
                </div>

                <aside class="col-lg-4" aria-labelledby="opportunityHeading">
                    <div class="summary-sticky">
                        <div class="opportunity-summary">
                            <div class="summary-head">
                                <p class="eyebrow">YOUR SELECTED OPPORTUNITY</p>
                                <div class="summary-title">
                                    <span class="company-mark" id="opportunityMark" aria-hidden="true"><i class="bi bi-briefcase"></i></span>
                                    <div class="min-w-0">
                                        <h2 id="opportunityHeading">Select an opportunity</h2>
                                        <p id="companyName" class="company-name"></p>
                                    </div>
                                </div>
                            </div>
                            <dl>
                                <div><dt><i class="bi bi-geo-alt"></i> Location</dt><dd id="opportunityLocation">—</dd></div>
                                <div><dt><i class="bi bi-mortarboard"></i> Specialization</dt><dd id="opportunityMajor">—</dd></div>
                                <div><dt><i class="bi bi-hourglass-split"></i> Duration</dt><dd id="opportunityDuration">—</dd></div>
                                <div><dt><i class="bi bi-calendar-event"></i> Deadline</dt><dd id="opportunityDeadline">—</dd></div>
                            </dl>
                            <p class="summary-note"><i class="bi bi-info-circle"></i> The company supervisor reviews your application. Applying does not confirm an internship offer.</p>
                        </div>

                        <div class="checklist-card">
                            <h3>Application checklist</h3>
                            <ul class="checklist" id="checklist">
                                <li data-check="personal"><span class="check-dot"><i class="bi bi-check"></i></span> Personal information</li>
                                <li data-check="supporting"><span class="check-dot"><i class="bi bi-check"></i></span> CV attached</li>
                                <li data-check="review"><span class="check-dot"><i class="bi bi-check"></i></span> Information confirmed</li>
                            </ul>
                        </div>
                    </div>
                </aside>
            </div>
            <noscript><p>Please enable JavaScript to select an opportunity and preview your application.</p></noscript>
        </main>
            </div>
        </div>
    </form>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Student/application-form.js?v=20261025-profile-cv"></script>
</body>
</html>
