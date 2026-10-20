/* =========================================================
   academic-dashboard.js  (ACADEMIC SUPERVISOR)

   How it works:
   - Every sidebar button has  onclick="loadSomething()".
   - Each load function builds the page HTML and puts it inside
     <main id="mainContent"> using innerHTML.
   - The sidebar and navbar never change, only the main area.
   ========================================================= */


/* ================= 1) SAMPLE DATA (replace with Firebase later) ================= */
const supervisor = {
    fullName: "Dr. Omar Khasawneh",
    shortName: "Dr. Omar K.",
    firstName: "Dr. Omar"
};

const counts = { assigned: 15, delayed: 3, evaluationsSent: 4 };

const students = [
    { name: "Ahmad Saleh", company: "Orange Jordan", hours: 120, required: 240, status: "On Track" },
    { name: "Sara Al-Zoubi", company: "Zain Jordan", hours: 180, required: 240, status: "On Track" },
    { name: "Yousef Obeidat", company: "Umniah", hours: 60, required: 240, status: "Delayed" },
    { name: "Hala Rawashdeh", company: "Orange Jordan", hours: 232, required: 240, status: "On Track" }
];

const companyEvaluations = [
    { student: "Hala Rawashdeh", company: "Orange Jordan", date: "Oct 19" },
    { student: "Sara Al-Zoubi", company: "Zain Jordan", date: "Oct 17" }
];

const alerts = [
    { text: "Yousef Obeidat missed 2 task deadlines", tone: "tone-danger", icon: "bi-exclamation-triangle" },
    { text: "Ahmad Saleh resubmitted “API documentation”", tone: "tone-primary", icon: "bi-arrow-repeat" },
    { text: "3 students have hours awaiting company approval", tone: "tone-warning", icon: "bi-clock-history" }
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

    let studentsHtml = "";
    students.forEach(s => {
        const percent = Math.round((s.hours / s.required) * 100);
        const barColor = s.status === "Delayed" ? "danger" : "";
        studentsHtml += `
        <div class="item">
            <span class="item-logo">${getInitials(s.name)}</span>
            <div class="item-body">
                <div class="item-title">${s.name}</div>
                <div class="item-sub">${s.company} · ${s.hours}/${s.required} hrs</div>
                <div class="progress-line ${barColor}"><span style="width:${percent}%"></span></div>
            </div>
            <div class="item-meta"><span class="pill ${statusClass(s.status)}">${s.status}</span></div>
        </div>`;
    });

    let evaluationsHtml = "";
    companyEvaluations.forEach(e => {
        evaluationsHtml += `
        <div class="item">
            <span class="item-icon tone-success"><i class="bi bi-clipboard-check"></i></span>
            <div class="item-body">
                <div class="item-title">${e.student}</div>
                <div class="item-sub">${e.company}</div>
            </div>
            <div class="item-meta">
                <span class="pill tone-success">Ready</span>
                <span>${e.date}</span>
            </div>
            <i class="bi bi-chevron-right chev"></i>
        </div>`;
    });

    let alertsHtml = "";
    alerts.forEach(a => {
        alertsHtml += `
        <div class="item">
            <span class="item-icon ${a.tone}"><i class="bi ${a.icon}"></i></span>
            <div class="item-body"><div class="item-title">${a.text}</div></div>
        </div>`;
    });

    mainContent.innerHTML = `
    <div class="dash">

        <header class="dash-head">
            <div class="dash-date">${getDateLine()}</div>
            <h1 class="dash-title">${getGreeting()}, ${supervisor.firstName}.</h1>
            <p class="dash-sub">Follow your students' training progress in one place.</p>
        </header>

        <section class="stats">
            <article class="stat soft">
                <div class="stat-label">Assigned Students</div>
                <div class="stat-value">${counts.assigned}</div>
                <div class="stat-note">Group A · Software Eng.</div>
                <span class="stat-icon tone-primary"><i class="bi bi-people"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">On Track</div>
                <div class="stat-value">${counts.assigned - counts.delayed}</div>
                <div class="stat-note">Progressing normally</div>
                <span class="stat-icon tone-success"><i class="bi bi-check-lg"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">Delayed</div>
                <div class="stat-value">${String(counts.delayed).padStart(2, "0")}</div>
                <div class="stat-note">Need follow-up</div>
                <span class="stat-icon tone-danger"><i class="bi bi-exclamation-triangle"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">Evaluations Sent</div>
                <div class="stat-value">${String(counts.evaluationsSent).padStart(2, "0")}</div>
                <div class="stat-note">of ${counts.assigned} to the coordinator</div>
                <span class="stat-icon tone-primary"><i class="bi bi-send"></i></span>
            </article>
        </section>

        <section class="hero">
            <div class="hero-content">
                <div class="hero-kicker">Final academic evaluation <i class="bi bi-arrow-up-right"></i></div>
                <h2>${companyEvaluations.length} students are ready for evaluation.</h2>
                <p>Company evaluations are in — prepare the academic evaluation and send it to the coordinator.</p>
                <button type="button" class="hero-btn" onclick="loadEvaluation()">Start evaluating <i class="bi bi-arrow-right"></i></button>
            </div>
        </section>

        <section class="panel area-list">
            <div class="panel-head">
                <h2 class="panel-title">My Students</h2>
                <button type="button" class="link-btn" onclick="loadProgress()">View all</button>
            </div>
            ${studentsHtml}
        </section>

        <section class="panel area-side1">
            <div class="panel-head">
                <h2 class="panel-title">Company Evaluations Received</h2>
                <button type="button" class="link-btn" onclick="loadEvaluation()">View all</button>
            </div>
            ${evaluationsHtml}
        </section>

        <section class="panel area-side2">
            <div class="panel-head">
                <h2 class="panel-title">Alerts</h2>
                <button type="button" class="link-btn" onclick="loadProgress()">View all</button>
            </div>
            ${alertsHtml}
        </section>

    </div>`;
}

function loadProgress() {
    setActive("btnProgress");
    mainContent.innerHTML = comingSoon("Student Progress Monitor", "Tasks, submissions, company evaluations, training hours, delays and feedback for each student.", "bi-graph-up");
}

function loadEvaluation() {
    setActive("btnEvaluation");
    mainContent.innerHTML = comingSoon("Final Academic Evaluation", "Prepare each student's final academic evaluation, add comments and send it to the coordinator.", "bi-clipboard-check");
}

function loadAccount() {
    setActive("btnAccount");
    mainContent.innerHTML = comingSoon("Academic Supervisor Account", "View and update your permitted account information.", "bi-person");
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
