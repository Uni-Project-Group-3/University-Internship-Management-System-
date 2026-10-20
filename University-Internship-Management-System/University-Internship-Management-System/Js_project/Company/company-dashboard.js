/* =========================================================
   company-dashboard.js  (COMPANY SUPERVISOR)

   How it works:
   - Every sidebar button has  onclick="loadSomething()".
   - Each load function builds the page HTML and puts it inside
     <main id="mainContent"> using innerHTML.
   - The sidebar and navbar never change, only the main area.
   ========================================================= */


/* ================= 1) SAMPLE DATA (replace with Firebase later) ================= */
const supervisor = {
    fullName: "Rania Haddad",
    shortName: "Rania H.",
    firstName: "Rania",
    company: "Orange Jordan"
};

const counts = { activeTrainees: 6, toReview: 9 };

// The "opportunities" list is in manage-opportunities.js
// The "applications" list is in applications-review.js

const submissions = [
    { task: "Build the login page UI", student: "Ahmad Saleh", date: "Oct 20", time: "10:15 AM" },
    { task: "Sales data cleaning", student: "Omar Shdeifat", date: "Oct 19", time: "04:40 PM" },
    { task: "Weekly progress report #4", student: "Hala Rawashdeh", date: "Oct 19", time: "01:05 PM" }
];

const hoursRequests = [
    { student: "Ahmad Saleh", hours: 8, week: "Week of Oct 18" },
    { student: "Hala Rawashdeh", hours: 6, week: "Week of Oct 18" }
];


/* ================= 2) HELPERS ================= */
const mainContent = document.getElementById("mainContent");

// Highlight the clicked sidebar button
function setActive(buttonId) {
    document.querySelectorAll(".side-link").forEach(btn => btn.classList.remove("active"));
    document.getElementById(buttonId).classList.add("active");
    closeSidebar();
    window.scrollTo(0, 0);
}

function getGreeting() {
    const hour = new Date().getHours();
    if (hour < 12) return "Good morning";
    if (hour < 18) return "Good afternoon";
    return "Good evening";
}

// "TUE, OCT 20, 03:30 PM"
function getDateLine() {
    const d = new Date();
    return d.toLocaleDateString("en-US", { weekday: "short", month: "short", day: "numeric" }) + ", " +
        d.toLocaleTimeString("en-US", { hour: "2-digit", minute: "2-digit" });
}

function getInitials(name) {
    return name.replace("Dr. ", "").split(" ").slice(0, 2).map(w => w[0]).join("").toUpperCase();
}

// Status text → colour class
function statusClass(status) {
    const green = ["Accepted", "Approved", "Completed", "On Track", "Ready"];
    const red = ["Rejected", "Delayed"];
    const orange = ["Pending", "Needs Revision", "Awaiting Academic", "Awaiting Company"];

    if (green.includes(status)) return "tone-success";
    if (red.includes(status)) return "tone-danger";
    if (orange.includes(status)) return "tone-warning";
    return "tone-primary";   // Under Review, Submitted, New ...
}

// Simple page for buttons you haven't built yet
function comingSoon(title, text, icon) {
    return `
    <div class="page">
        <header class="dash-head">
            <div class="dash-date">${getDateLine()}</div>
            <h1 class="dash-title">${title}</h1>
            <p class="dash-sub">${text}</p>
        </header>
        <section class="panel empty-state">
            <span class="stat-icon tone-primary"><i class="bi ${icon}"></i></span>
            <h3>${title} — coming soon</h3>
            <p>Write this page's HTML inside its load function.</p>
        </section>
    </div>`;
}



/* ================= 3) PAGES — one function per sidebar button ================= */

function loadDashboard() {
    setActive("btnDashboard");

    // Calculated from the opportunities list
    const openCount = opportunities.filter(o => o.status === "Open").length;
    const closingSoon = opportunities.filter(o => o.status === "Open" && daysUntil(o.deadline) >= 0 && daysUntil(o.deadline) <= 7).length;

    // Calculated from the applications list
    const pendingCount = applications.filter(a => a.status === "Pending").length;
    const thisWeek = applications.filter(a => daysUntil(a.appliedOn) >= -7).length;

    // 4 most recent applications
    const recent = [...applications].sort((a, b) => b.appliedOn.localeCompare(a.appliedOn)).slice(0, 4);

    let applicationsHtml = "";
    recent.forEach(app => {
        applicationsHtml += `
        <div class="item">
            <span class="item-logo">${getInitials(app.studentName)}</span>
            <div class="item-body">
                <div class="item-title">${esc(app.studentName)}</div>
                <div class="item-sub">${esc(getOpportunity(app.opportunityId).title)}</div>
            </div>
            <div class="item-meta">
                <span class="pill ${statusClass(app.status)}">${app.status}</span>
                <span>${formatDate(app.appliedOn)}</span>
            </div>
        </div>`;
    });

    let submissionsHtml = "";
    submissions.forEach(s => {
        submissionsHtml += `
        <div class="item">
            <span class="item-icon tone-primary"><i class="bi bi-file-earmark-arrow-up"></i></span>
            <div class="item-body">
                <div class="item-title">${s.task}</div>
                <div class="item-sub">${s.student}</div>
            </div>
            <div class="item-meta">
                <strong>${s.date}</strong>
                <span><i class="bi bi-clock"></i> ${s.time}</span>
            </div>
            <i class="bi bi-chevron-right chev"></i>
        </div>`;
    });

    let hoursHtml = "";
    hoursRequests.forEach(h => {
        hoursHtml += `
        <div class="item">
            <span class="item-icon tone-warning"><i class="bi bi-clock-history"></i></span>
            <div class="item-body">
                <div class="item-title">${h.student}</div>
                <div class="item-sub">${h.hours} hrs · ${h.week}</div>
            </div>
            <div class="item-meta"><span class="pill tone-warning">Pending</span></div>
        </div>`;
    });

    mainContent.innerHTML = `
    <div class="dash">

        <header class="dash-head">
            <div class="dash-date">${getDateLine()}</div>
            <h1 class="dash-title">${getGreeting()}, ${supervisor.firstName}.</h1>
            <p class="dash-sub">${supervisor.company} internship programme at a glance.</p>
        </header>

        <section class="stats">
            <article class="stat soft">
                <div class="stat-label">Open Opportunities</div>
                <div class="stat-value">${String(openCount).padStart(2, "0")}</div>
                <div class="stat-note">${closingSoon} closing this week</div>
                <span class="stat-icon tone-primary"><i class="bi bi-briefcase"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">Pending Applications</div>
                <div class="stat-value">${String(pendingCount).padStart(2, "0")}</div>
                <div class="stat-note">${thisWeek} received this week</div>
                <span class="stat-icon tone-primary"><i class="bi bi-inbox"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">Active Trainees</div>
                <div class="stat-value">${String(counts.activeTrainees).padStart(2, "0")}</div>
                <div class="stat-note">Across 3 programmes</div>
                <span class="stat-icon tone-success"><i class="bi bi-people"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">To Review</div>
                <div class="stat-value">${String(counts.toReview).padStart(2, "0")}</div>
                <div class="stat-note">Submissions waiting</div>
                <span class="stat-icon tone-warning"><i class="bi bi-hourglass-split"></i></span>
            </article>
        </section>

        <section class="hero">
            <div class="hero-content">
                <div class="hero-kicker">Hiring season <i class="bi bi-arrow-up-right"></i></div>
                <h2>Find your next great intern.</h2>
                <p>Publish an opportunity and reach students by college and major.</p>
                <button type="button" class="hero-btn" onclick="loadOpportunities()">Manage opportunities <i class="bi bi-arrow-right"></i></button>
            </div>
        </section>

        <section class="panel area-list">
            <div class="panel-head">
                <h2 class="panel-title">Recent Applications</h2>
                <button type="button" class="link-btn" onclick="loadApplications()">View all</button>
            </div>
            ${applicationsHtml}
        </section>

        <section class="panel area-side1">
            <div class="panel-head">
                <h2 class="panel-title">Submissions to Review</h2>
                <button type="button" class="link-btn" onclick="loadTasks()">View all</button>
            </div>
            ${submissionsHtml}
        </section>

        <section class="panel area-side2">
            <div class="panel-head">
                <h2 class="panel-title">Hours Awaiting Approval</h2>
                <button type="button" class="link-btn" onclick="loadTrainees()">View all</button>
            </div>
            ${hoursHtml}
        </section>

    </div>`;
}

function loadTrainees() {
    setActive("btnTrainees");
    mainContent.innerHTML = comingSoon("My Trainees", "Accepted students, their training information, hours and progress.", "bi-people");
}

function loadTasks() {
    setActive("btnTasks");
    mainContent.innerHTML = comingSoon("Manage & Review Tasks", "Create tasks with deadlines and criteria, evaluate submissions or request revisions.", "bi-list-check");
}

function loadEvaluation() {
    setActive("btnEvaluation");
    mainContent.innerHTML = comingSoon("Reports & Final Evaluation", "Complete the final company evaluation and send it to the academic supervisor.", "bi-clipboard-check");
}

function loadAccount() {
    setActive("btnAccount");
    mainContent.innerHTML = comingSoon("Supervisor Account", "Manage your account information.", "bi-person");
}


/* ================= 4) MOBILE SIDEBAR ================= */
function openSidebar() {
    document.getElementById("sidebar").classList.add("open");
    document.getElementById("sidebarBackdrop").classList.add("show");
}

function closeSidebar() {
    document.getElementById("sidebar").classList.remove("open");
    document.getElementById("sidebarBackdrop").classList.remove("show");
}



/* ================= 5) FIRST LOAD ================= */
document.getElementById("userName").textContent = supervisor.shortName;
document.getElementById("userInitials").textContent = getInitials(supervisor.fullName);
loadDashboard();   // the user lands on Dashboard directly