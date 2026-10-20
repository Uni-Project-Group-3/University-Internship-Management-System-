<%@ Page Language="C#" %>
<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Application Form | JUST Internships</title>
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="../Assets/shared/colors.css" rel="stylesheet" />
    <link href="../Assets/student/application-form.css" rel="stylesheet" />
</head>
<body>
    <form id="applicationForm" runat="server">
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
    </form>
    <script src="../Js_project/Student/application-form.js"></script>
</body>
</html>
