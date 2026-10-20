<%@ Page Language="C#" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Explore Opportunities | JUST Internships</title>
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="../Assets/shared/colors.css" rel="stylesheet" />
    <link href="../Assets/student/explore-opportunities.css" rel="stylesheet" />
</head>
<body>
    <form id="exploreForm" runat="server">
        <!-- المحتوى الرئيسي مستقل حتى يسهل دمجه مع القائمة الجانبية لاحقاً -->
        <main class="opportunities-page">
            <header class="page-heading">
                <p class="eyebrow">JUST / STUDENT WORKSPACE</p>
                <h1>Explore Opportunities</h1>
                <p class="page-description">Find a place to learn, contribute, and take your next step.</p>
                <div class="college-label">Jordan University of Science and Technology <span aria-hidden="true">&middot;</span> Information Technology</div>
            </header>

            <section class="search-panel" aria-label="Filter opportunities">
                <div class="row g-3 align-items-end">
                    <div class="col-lg-6">
                        <label for="searchInput" class="form-label">Search opportunities</label>
                        <input type="search" id="searchInput" class="form-control" placeholder="Opportunity title or company name" />
                    </div>
                    <div class="col-sm-6 col-lg-3">
                        <label for="majorFilter" class="form-label">Specialization</label>
                        <select id="majorFilter" class="form-select">
                            <option value="">All specializations</option>
                            <option>Computer Science</option>
                            <option>Software Engineering</option>
                            <option>Cybersecurity</option>
                        </select>
                    </div>
                    <div class="col-sm-6 col-lg-3">
                        <label for="locationFilter" class="form-label">Location</label>
                        <select id="locationFilter" class="form-select">
                            <option value="">All locations</option>
                            <option>Amman</option>
                            <option>Irbid</option>
                            <option>Remote</option>
                        </select>
                    </div>
                </div>
            </section>

            <div class="results-heading">
                <h2>Available opportunities <span id="resultCount" aria-live="polite">(3)</span></h2>
                <button type="button" id="resetFilters" class="reset-button">Reset filters</button>
            </div>
            <p class="sample-note">Sample opportunities for preview. Availability will be provided by the system.</p>

            <!-- بيانات تجريبية للواجهة، وتستبدل لاحقاً بفرص مناسبة لتخصص الطالب من قاعدة البيانات -->
            <section id="opportunitiesList" class="row g-4" aria-label="Available internship opportunities">
                <div class="col-md-6 col-xl-4 opportunity-item" data-major="Software Engineering" data-location="Amman" data-id="sample-web">
                    <article class="opportunity-card">
                        <div class="company-heading"><span class="company-mark" aria-hidden="true">NT</span><div><p class="company-name">North Tech</p><p class="company-location">Amman &middot; On-site</p></div></div>
                        <span class="specialization">Software Engineering</span>
                        <h3>Web Development Intern</h3>
                        <p class="opportunity-description">Build practical web development skills while working with a team on useful digital products.</p>
                        <dl class="opportunity-facts"><div><dt>Duration</dt><dd>8 weeks</dd></div><div><dt>Available seats</dt><dd>3 students</dd></div></dl>
                        <div class="opportunity-details" hidden><p>Assist with responsive interfaces, test web pages, and collaborate on small development tasks under a company supervisor.</p><h4>Requirements</h4><ul><li>Basic HTML, CSS, and JavaScript.</li><li>Familiarity with C# and Git.</li><li>Interest in teamwork and learning.</li></ul></div>
                        <button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#opportunityModal">View details <span aria-hidden="true">&rarr;</span></button>
                    </article>
                </div>
                <div class="col-md-6 col-xl-4 opportunity-item" data-major="Computer Science" data-location="Irbid" data-id="sample-data">
                    <article class="opportunity-card">
                        <div class="company-heading"><span class="company-mark" aria-hidden="true">DL</span><div><p class="company-name">Data Lab</p><p class="company-location">Irbid &middot; On-site</p></div></div>
                        <span class="specialization">Computer Science</span>
                        <h3>Data Analysis Intern</h3>
                        <p class="opportunity-description">Explore real-world datasets and learn how to turn information into clear, useful insights.</p>
                        <dl class="opportunity-facts"><div><dt>Duration</dt><dd>10 weeks</dd></div><div><dt>Available seats</dt><dd>2 students</dd></div></dl>
                        <div class="opportunity-details" hidden><p>Help prepare datasets, create basic reports, and explain findings with guidance from a data analysis team.</p><h4>Requirements</h4><ul><li>Basic Python and SQL.</li><li>Understanding of data structures.</li><li>Attention to detail.</li></ul></div>
                        <button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#opportunityModal">View details <span aria-hidden="true">&rarr;</span></button>
                    </article>
                </div>
                <div class="col-md-6 col-xl-4 opportunity-item" data-major="Cybersecurity" data-location="Remote" data-id="sample-security">
                    <article class="opportunity-card">
                        <div class="company-heading"><span class="company-mark" aria-hidden="true">SP</span><div><p class="company-name">Secure Path</p><p class="company-location">Remote</p></div></div>
                        <span class="specialization">Cybersecurity</span>
                        <h3>Security Operations Intern</h3>
                        <p class="opportunity-description">Develop security fundamentals through guided activities, monitoring, and practical team exercises.</p>
                        <dl class="opportunity-facts"><div><dt>Duration</dt><dd>8 weeks</dd></div><div><dt>Available seats</dt><dd>2 students</dd></div></dl>
                        <div class="opportunity-details" hidden><p>Practice reviewing sample security alerts, documenting observations, and applying basic security procedures in a supervised environment.</p><h4>Requirements</h4><ul><li>Basic networking knowledge.</li><li>Familiarity with operating systems.</li><li>Interest in information security.</li></ul></div>
                        <button type="button" class="details-button" data-bs-toggle="modal" data-bs-target="#opportunityModal">View details <span aria-hidden="true">&rarr;</span></button>
                    </article>
                </div>
            </section>

            <div id="emptyState" class="empty-state" hidden>
                <h3>No matching opportunities</h3>
                <p>Try a different search or reset your filters.</p>
            </div>
            <noscript><p class="sample-note">Enable JavaScript to filter opportunities and view their full details.</p></noscript>

            <div class="modal fade" id="opportunityModal" tabindex="-1" aria-labelledby="modalTitle" aria-hidden="true">
                <div class="modal-dialog modal-dialog-centered modal-dialog-scrollable">
                    <div class="modal-content">
                        <div class="modal-header">
                            <h2 class="modal-title fs-5" id="modalTitle">Opportunity details</h2>
                            <button type="button" class="btn-close" data-bs-dismiss="modal" aria-label="Close"></button>
                        </div>
                        <div class="modal-body">
                            <p id="modalCompany" class="modal-company"></p>
                            <p id="modalMajor" class="specialization"></p>
                            <p id="modalLocation"></p>
                            <div id="modalFacts"></div>
                            <div id="modalDetails"></div>
                            <p class="sample-note">Sample opportunity. You can preview the application form without submitting a real application.</p>
                        </div>
                        <div class="modal-footer">
                            <button type="button" class="secondary-button" data-bs-dismiss="modal">Close</button>
                            <a id="applyLink" class="apply-button" href="ApplicationForm.aspx">Apply &rarr;</a>
                        </div>
                    </div>
                </div>
            </div>
        </main>
    </form>
    <script src="../Scripts/bootstrap.bundle.min.js"></script>
    <script src="../Js_project/Student/explore-opportunities.js"></script>
</body>
</html>
