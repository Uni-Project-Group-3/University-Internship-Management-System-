/* =========================================================
   student-dashboard.js  (STUDENT)

   How it works:
   - Every sidebar button has  onclick="loadSomething()".
   - Each load function builds the page HTML and puts it inside
     <main id="mainContent"> using innerHTML.
   - The sidebar and navbar never change, only the main area.
   ========================================================= */


/* ================= 1) SAMPLE DATA (replace with Firebase later) ================= */
const student = {
    fullName: "Ahmad Saleh",
    shortName: "Ahmad S.",
    firstName: "Ahmad",
    company: "Orange Jordan",
    approvedHours: 120,
    requiredHours: 240
};

const applications = [
    { title: "Frontend Developer Intern", company: "Orange Jordan", status: "Accepted", date: "Oct 12, 2026" },
    { title: "Software Engineering Intern", company: "Zain Jordan", status: "Under Review", date: "Oct 8, 2026" },
    { title: "Mobile App Intern", company: "Umniah", status: "Pending", date: "Oct 6, 2026" },
    { title: "IT Support Intern", company: "Aramex", status: "Rejected", date: "Oct 5, 2026" }
];

const tasks = [
    { title: "Build the login page UI", due: "Thu, Oct 22", time: "11:59 PM", status: "Pending" },
    { title: "Write API documentation", due: "Sun, Oct 25", time: "05:00 PM", status: "Needs Revision" },
    { title: "Weekly progress report #4", due: "Tue, Oct 27", time: "02:00 PM", status: "Pending" }
];

const feedback = [
    { task: "Database schema design", by: "Rania Haddad", grade: "9 / 10", note: "Clear and well normalised." },
    { task: "Weekly progress report #3", by: "Rania Haddad", grade: "Excellent", note: "Good detail on blockers." }
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

    const hoursPercent = Math.round((student.approvedHours / student.requiredHours) * 100);
    const revisions = tasks.filter(t => t.status === "Needs Revision").length;

    // Build the list rows
    let applicationsHtml = "";
    applications.forEach(app => {
        applicationsHtml += `
        <div class="item">
            <span class="item-logo">${getInitials(app.company)}</span>
            <div class="item-body">
                <div class="item-title">${app.title}</div>
                <div class="item-sub">${app.company}</div>
            </div>
            <div class="item-meta">
                <span class="pill ${statusClass(app.status)}">${app.status}</span>
                <span>${app.date}</span>
            </div>
        </div>`;
    });

    let tasksHtml = "";
    tasks.forEach(task => {
        tasksHtml += `
        <div class="item">
            <span class="item-icon tone-primary"><i class="bi bi-calendar-event"></i></span>
            <div class="item-body">
                <div class="item-title">${task.title}</div>
                <div class="item-sub"><span class="pill ${statusClass(task.status)}">${task.status}</span></div>
            </div>
            <div class="item-meta">
                <strong>${task.due}</strong>
                <span><i class="bi bi-clock"></i> ${task.time}</span>
            </div>
            <i class="bi bi-chevron-right chev"></i>
        </div>`;
    });

    let feedbackHtml = "";
    feedback.forEach(f => {
        feedbackHtml += `
        <div class="item">
            <span class="item-icon tone-success"><i class="bi bi-chat-left-text"></i></span>
            <div class="item-body">
                <div class="item-title">${f.task}</div>
                <div class="item-sub">${f.by} · ${f.note}</div>
            </div>
            <div class="item-meta"><span class="pill tone-success">${f.grade}</span></div>
        </div>`;
    });

    // Put the whole page inside <main>
    mainContent.innerHTML = `
    <div class="dash">

        <header class="dash-head">
            <div class="dash-date">${getDateLine()}</div>
            <h1 class="dash-title">${getGreeting()}, ${student.firstName}.</h1>
            <p class="dash-sub">Your internship journey, all in one place.</p>
        </header>

        <section class="stats">
            <article class="stat soft">
                <div class="stat-label">Applied</div>
                <div class="stat-value">${String(applications.length).padStart(2, "0")}</div>
                <div class="stat-note">1 accepted &amp; confirmed</div>
                <span class="stat-icon tone-primary"><i class="bi bi-file-earmark-text"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">Open Tasks</div>
                <div class="stat-value">${String(tasks.length).padStart(2, "0")}</div>
                <div class="stat-note">${revisions} needs revision</div>
                <span class="stat-icon tone-warning"><i class="bi bi-list-check"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">Approved Hours</div>
                <div class="stat-value">${student.approvedHours}<small> / ${student.requiredHours}</small></div>
                <div class="stat-note">${hoursPercent}% of required hours</div>
                <span class="stat-icon tone-success"><i class="bi bi-clock-history"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">Evaluated Tasks</div>
                <div class="stat-value">06</div>
                <div class="stat-note">View your feedback</div>
                <span class="stat-icon tone-primary"><i class="bi bi-award"></i></span>
            </article>
        </section>

        <section class="hero">
            <div class="hero-content">
                <div class="hero-kicker">Current internship · ${student.company} <i class="bi bi-arrow-up-right"></i></div>
                <h2>Keep building real experience.</h2>
                <p>You have ${tasks.length} open tasks and ${student.approvedHours} approved training hours.</p>
                <button type="button" class="hero-btn" onclick="loadTasks()">Open my tasks <i class="bi bi-arrow-right"></i></button>
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
                <h2 class="panel-title">Upcoming Deadlines</h2>
                <button type="button" class="link-btn" onclick="loadTasks()">View all</button>
            </div>
            ${tasksHtml}
        </section>

        <section class="panel area-side2">
            <div class="panel-head">
                <h2 class="panel-title">Latest Feedback</h2>
                <button type="button" class="link-btn" onclick="loadProgress()">View all</button>
            </div>
            ${feedbackHtml}
            <div class="item">
                <div class="item-body">
                    <div class="item-sub">Training hours progress</div>
                    <div class="progress-line success"><span style="width:${hoursPercent}%"></span></div>
                </div>
            </div>
        </section>

    </div>`;
}

function loadOpportunities() {
    setActive("btnOpportunities");
    mainContent.innerHTML = comingSoon("Explore Opportunities", "Internships that match your college and major — open one to see full details and apply.", "bi-search");
}

function loadApplications() {
    setActive("btnApplications");
    mainContent.innerHTML = comingSoon("My Applications", "Track every application: Pending, Accepted or Rejected. You can confirm only one offer.", "bi-file-earmark-text");
}

function loadTasks() {
    setActive("btnTasks");
    mainContent.innerHTML = comingSoon("My Tasks", "Tasks from your company supervisor — submit work and resubmit when revisions are requested.", "bi-list-check");
}

function loadProgress() {
    setActive("btnProgress");
    mainContent.innerHTML = comingSoon("My Progress & Evaluation", "Feedback, task grades, approved training hours and your Internship Completion Report.", "bi-graph-up");
}

function loadProfile() {
    setActive("btnProfile");
    mainContent.innerHTML = comingSoon("My Profile", "Your academic information, personal details and CV / portfolio link.", "bi-person");
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
document.getElementById("userName").textContent = student.shortName;
document.getElementById("userInitials").textContent = getInitials(student.fullName);
loadDashboard();   // the user lands on Dashboard directly
