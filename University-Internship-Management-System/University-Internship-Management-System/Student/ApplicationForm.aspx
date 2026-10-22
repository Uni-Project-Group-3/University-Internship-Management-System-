<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="ApplicationForm.aspx.cs" Inherits="University_Internship_Management_System.Student.ApplicationForm" %>

<script src="../Js_project/Demo/demo-store.js?v=20261021-original-ui"></script>
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
    <link href="../Assets/student/application-form.css?v=20261021-original-ui" rel="stylesheet" />
</head>
<body class="standalone-shell" data-demo-role="student">
    <form id="applicationForm" runat="server">
        <div class="app">
            <!-- نفس السايد بار المرسل، مكرر داخل الصفحة مع ضبط الروابط المتوفرة -->
            <aside class="sidebar" id="studentSidebar" aria-label="Student navigation">
                <button type="button" id="closeSidebar" class="sidebar-close" aria-label="Close navigation">&times;</button>
                <a href="StudentDashboard.aspx" class="brand">
                    <img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="Jordan University of Science and Technology logo" width="52" height="52" />
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>

                <ul class="side-nav" aria-label="Student pages">
<li><a href="StudentDashboard.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="m3 10 9-7 9 7v10h-6v-7H9v7H3V10Z"/></svg><span>Dashboard</span></a></li>
<li><a href="ExploreOpportunities.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Explore Opportunities</span></a></li>
<li><a href="MyApplications.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>My Applications</span></a></li>
<li><a href="ApplicationForm.aspx" class="active" aria-current="page"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Application Form</span></a></li>
<li><a href="Profile.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><circle cx="12" cy="7" r="3"/><path d="M5 21v-3a7 7 0 0 1 14 0v3"/></svg><span>My Profile</span></a></li>

<li><a href="MyTasks.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>My Tasks</span></a></li>

<li><a href="MyProgressEvaluation.aspx"><svg class="nav-icon" width="20" height="20" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.7" aria-hidden="true"><path d="M4 5h16v14H4zM8 9h8M8 13h6" /></svg><span>My Progress &amp; Evaluation</span></a></li>
</ul>

                <div class="side-divider"></div>

                <ul class="side-nav"><li><a href="../Contact.aspx"><svg class="nav-icon" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="1.6" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><rect x="5" y="3" width="14" height="18" rx="2"/><path d="m8 12 3 3 5-6"/></svg><span>Help &amp; contact</span></a></li>
</ul>

                <div class="side-promo">
                    <i class="bi bi-mortarboard-fill"></i>
                    <p>Build your future<br />with real experience.</p>
                    <a href="ExploreOpportunities.aspx">Explore opportunities &rarr;</a>
                </div>
            </aside>
            <div id="sidebarBackdrop" class="sidebar-backdrop" aria-hidden="true"></div>
            <div class="main" id="studentMain">
                <header class="topbar">
                    <button type="button" class="menu-btn" id="openSidebar" aria-label="Open navigation" aria-controls="studentSidebar" aria-expanded="false"><i class="bi bi-list"></i></button>
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input id="navbarSearch" type="search" aria-label="Workspace search" placeholder="Search for internships, companies, or skills..." />
                    </div>
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
            <a class="back-link" href="ExploreOpportunities.aspx">&larr; Back to opportunities</a>
            <header class="page-heading">
                <p class="eyebrow">JUST / STUDENT WORKSPACE</p>
                <h1>Application Form</h1>
                <p>Introduce yourself and take the next step in your internship journey.</p>
            </header>

            <div class="preview-notice">Frontend preview — your information will not be saved or sent.</div>
            <div id="invalidOpportunity" class="invalid-opportunity" role="alert" hidden>
                Please select a valid opportunity from <a href="ExploreOpportunities.aspx">Explore Opportunities</a> before continuing.
            </div>

            <div class="row g-4 align-items-start">
                <div class="col-lg-8">
                    <fieldset id="applicationFields" disabled>
                        <section class="form-section" aria-labelledby="personalHeading">
                            <div class="section-heading">
                                <span class="section-number" aria-hidden="true">01</span>
                                <div><h2 id="personalHeading">Personal information</h2><p>Use your university contact details. Fields marked * are required.</p></div>
                            </div>
                            <!-- بيانات الطالب ستعبأ من حسابه عند الربط بالخلفية -->
                            <div class="row g-3">
                                <div class="col-md-6">
                                    <label for="fullName" class="form-label">Full name *</label>
                                    <input id="fullName" name="fullName" class="form-control" type="text" autocomplete="name" maxlength="100" required />
                                </div>
                                <div class="col-md-6">
                                    <label for="studentNumber" class="form-label">Student ID *</label>
                                    <input id="studentNumber" name="studentNumber" class="form-control" type="text" maxlength="30" required />
                                </div>
                                <div class="col-md-6">
                                    <label for="email" class="form-label">University email *</label>
                                    <input id="email" name="email" class="form-control" type="email" autocomplete="email" maxlength="150" required />
                                </div>
                                <div class="col-md-6">
                                    <label for="phone" class="form-label">Phone number *</label>
                                    <input id="phone" name="phone" class="form-control" type="tel" autocomplete="tel" maxlength="25" required />
                                </div>
                                <div class="col-12">
                                    <label for="major" class="form-label">Specialization *</label>
                                    <select id="major" name="major" class="form-select" required>
                                        <option value="">Select your specialization</option>
                                        <option>Computer Science</option>
                                        <option>Software Engineering</option>
                                        <option>Cybersecurity</option>
                                    </select>
                                    <p class="field-help">University and college: JUST · Information Technology</p>
                                </div>
                            </div>
                        </section>

                        <section class="form-section" aria-labelledby="supportHeading">
                            <div class="section-heading">
                                <span class="section-number" aria-hidden="true">02</span>
                                <div><h2 id="supportHeading">Supporting information</h2><p>Share a little about your skills and interests.</p></div>
                            </div>
                            <label for="cvLink" class="form-label">CV link *</label>
                            <input id="cvLink" name="cvLink" class="form-control" type="url" placeholder="https://" maxlength="500" aria-describedby="cvHelp" required />
                            <p id="cvHelp" class="field-help">Use a shareable link that the company supervisor can access.</p>

                            <label for="portfolioLink" class="form-label">Portfolio or GitHub link <span>(optional)</span></label>
                            <input id="portfolioLink" name="portfolioLink" class="form-control" type="url" placeholder="https://" maxlength="500" />

                            <div class="message-label">
                                <label for="motivation" class="form-label">Why are you interested? <span>(optional)</span></label>
                                <span id="characterCount">0 / 1000</span>
                            </div>
                            <textarea id="motivation" name="motivation" class="form-control" rows="5" maxlength="1000" placeholder="Tell the team about your interests, relevant skills, and what you hope to learn."></textarea>
                        </section>

                        <section class="form-section" aria-label="Review application">
                            <div class="form-check">
                                <input id="confirmInformation" name="confirmInformation" class="form-check-input" type="checkbox" required />
                                <label for="confirmInformation" class="form-check-label">I confirm that the information I provided is accurate. *</label>
                            </div>
                            <div class="form-actions">
                                <a href="ExploreOpportunities.aspx" class="cancel-link">Cancel</a>
                                <button type="submit" class="primary-button">Preview application <span aria-hidden="true">&rarr;</span></button>
                            </div>
                        </section>
                    </fieldset>
                    <div id="previewResult" class="preview-result" role="status" tabindex="-1" hidden>
                        <h2>Your application is ready for review</h2>
                        <p id="previewSummary"></p>
                        <p>This was a preview only. No application has been submitted or saved.</p>
                        <button type="button" id="editApplication" class="edit-button">Continue editing</button>
                    </div>
                </div>

                <aside class="col-lg-4" aria-labelledby="opportunityHeading">
                    <div class="opportunity-summary">
                        <p class="eyebrow">YOUR SELECTED OPPORTUNITY</p>
                        <h2 id="opportunityHeading">Select an opportunity</h2>
                        <p id="companyName" class="company-name"></p>
                        <dl>
                            <div><dt>Location</dt><dd id="opportunityLocation">—</dd></div>
                            <div><dt>Specialization</dt><dd id="opportunityMajor">—</dd></div>
                            <div><dt>Duration</dt><dd id="opportunityDuration">—</dd></div>
                        </dl>
                        <p class="summary-note">The company supervisor reviews your application. Applying does not confirm an internship offer.</p>
                    </div>
                </aside>
            </div>
            <noscript><p>Please enable JavaScript to select an opportunity and preview your application.</p></noscript>
        </main>
            </div>
        </div>
    </form>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Student/application-form.js?v=20261021-original-ui"></script>
</body>
</html>
