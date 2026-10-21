/* =========================================================
   coordinator-dashboard.js  (UNIVERSITY INTERNSHIP COORDINATOR)

   How it works:
   - Every sidebar button has  onclick="loadSomething()".
   - Each load function builds the page HTML and puts it inside
     <main id="mainContent"> using innerHTML.
   - The sidebar and navbar never change, only the main area.
   ========================================================= */


/* ================= 1) SAMPLE DATA (replace with Firebase later) ================= */
const coordinator = {
    fullName: "Dr. Lina Momani",
    shortName: "Dr. Lina M.",
    firstName: "Dr. Lina"
};

const counts = { students: 240, placed: 212, companies: 38, activeCompanies: 35, opportunities: 57, unassigned: 6 };

// Both evaluations must arrive before the coordinator can approve
const reviews = [
    { student: "Hala Rawashdeh", company: "Orange Jordan", supervisor: "Dr. Omar Khasawneh", status: "Ready", grade: "88.5" },
    { student: "Sara Al-Zoubi", company: "Zain Jordan", supervisor: "Dr. Omar Khasawneh", status: "Ready", grade: "91" },
    { student: "Rami Haddadin", company: "Aramex", supervisor: "Dr. Noor Al-Qudah", status: "Awaiting Academic", grade: "—" },
    { student: "Dana Tarawneh", company: "Umniah", supervisor: "Dr. Noor Al-Qudah", status: "Awaiting Company", grade: "—" }
];

const groups = [
    { supervisor: "Dr. Omar Khasawneh", group: "Group A · Software Eng.", count: 15 },
    { supervisor: "Dr. Noor Al-Qudah", group: "Group B · Computer Science", count: 18 }
];

const messages = [
    { name: "Khaled Nasser", subject: "Partnership request", date: "Oct 20" },
    { name: "Mai Saleh", subject: "Login problem", date: "Oct 19" }
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

    const readyCount = reviews.filter(r => r.status === "Ready").length;

    let reviewsHtml = "";
    reviews.forEach(r => {
        reviewsHtml += `
        <div class="item">
            <span class="item-logo">${getInitials(r.student)}</span>
            <div class="item-body">
                <div class="item-title">${r.student}</div>
                <div class="item-sub">${r.company} · ${r.supervisor}</div>
            </div>
            <div class="item-meta">
                <span class="pill ${statusClass(r.status)}">${r.status}</span>
                <span>Final: <strong>${r.grade}</strong></span>
            </div>
        </div>`;
    });

    let groupsHtml = "";
    groups.forEach(g => {
        groupsHtml += `
        <div class="item">
            <span class="item-icon tone-primary"><i class="bi bi-people"></i></span>
            <div class="item-body">
                <div class="item-title">${g.supervisor}</div>
                <div class="item-sub">${g.group}</div>
            </div>
            <div class="item-meta"><span class="pill tone-primary">${g.count} students</span></div>
            <i class="bi bi-chevron-right chev"></i>
        </div>`;
    });
    groupsHtml += `
        <div class="item">
            <span class="item-icon tone-danger"><i class="bi bi-person-exclamation"></i></span>
            <div class="item-body">
                <div class="item-title">${counts.unassigned} students not assigned yet</div>
                <div class="item-sub">Link them to an academic supervisor</div>
            </div>
            <i class="bi bi-chevron-right chev"></i>
        </div>`;

    let messagesHtml = "";
    messages.forEach(m => {
        messagesHtml += `
        <div class="item">
            <span class="item-icon tone-primary"><i class="bi bi-envelope"></i></span>
            <div class="item-body">
                <div class="item-title">${m.subject}</div>
                <div class="item-sub">${m.name}</div>
            </div>
            <div class="item-meta">
                <span class="pill tone-primary">New</span>
                <span>${m.date}</span>
            </div>
        </div>`;
    });

    mainContent.innerHTML = `
    <div class="dash">

        <header class="dash-head">
            <div class="dash-date">${getDateLine()}</div>
            <h1 class="dash-title">${getGreeting()}, ${coordinator.firstName}.</h1>
            <p class="dash-sub">The whole internship programme, all in one place.</p>
        </header>

        <section class="stats">
            <article class="stat soft">
                <div class="stat-label">Students</div>
                <div class="stat-value">${counts.students}</div>
                <div class="stat-note">${counts.placed} placed in companies</div>
                <span class="stat-icon tone-primary"><i class="bi bi-mortarboard"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">Companies</div>
                <div class="stat-value">${counts.companies}</div>
                <div class="stat-note">${counts.activeCompanies} active accounts</div>
                <span class="stat-icon tone-primary"><i class="bi bi-building"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">Opportunities</div>
                <div class="stat-value">${counts.opportunities}</div>
                <div class="stat-note">Open this semester</div>
                <span class="stat-icon tone-success"><i class="bi bi-briefcase"></i></span>
            </article>
            <article class="stat">
                <div class="stat-label">Completion Reviews</div>
                <div class="stat-value">${String(reviews.length).padStart(2, "0")}</div>
                <div class="stat-note">${readyCount} ready for approval</div>
                <span class="stat-icon tone-warning"><i class="bi bi-patch-check"></i></span>
            </article>
        </section>

        <section class="hero">
            <div class="hero-content">
                <div class="hero-kicker">Final completion review <i class="bi bi-arrow-up-right"></i></div>
                <h2>${readyCount} internships are waiting for your approval.</h2>
                <p>Company and academic evaluations are both submitted — verify the requirements and approve.</p>
                <button type="button" class="hero-btn" onclick="loadCompletion()">Review now <i class="bi bi-arrow-right"></i></button>
            </div>
        </section>

        <section class="panel area-list">
            <div class="panel-head">
                <h2 class="panel-title">Completion Review Queue</h2>
                <button type="button" class="link-btn" onclick="loadCompletion()">View all</button>
            </div>
            ${reviewsHtml}
        </section>

        <section class="panel area-side1">
            <div class="panel-head">
                <h2 class="panel-title">Academic Supervisor Groups</h2>
                <button type="button" class="link-btn" onclick="loadAssign()">View all</button>
            </div>
            ${groupsHtml}
        </section>

        <section class="panel area-side2">
            <div class="panel-head">
                <h2 class="panel-title">Contact Messages</h2>
                <button type="button" class="link-btn" onclick="loadMessages()">View all</button>
            </div>
            ${messagesHtml}
        </section>

    </div>`;
}

function loadStudents() {
    setActive("btnStudents");
    mainContent.innerHTML = comingSoon("Manage Students", "Create student accounts and manage university, college, major and required training hours.", "bi-mortarboard");
}

function loadCompanies() {
    setActive("btnCompanies");
    mainContent.innerHTML = comingSoon("Manage Companies", "Create company supervisor accounts and activate or deactivate them.", "bi-building");
}

function loadSupervisors() {
    setActive("btnSupervisors");
    mainContent.innerHTML = comingSoon("Manage Academic Supervisors", "Create and manage academic supervisor accounts.", "bi-person-badge");
}

function loadAssign() {
    setActive("btnAssign");
    mainContent.innerHTML = comingSoon("Assign Academic Supervisors", "Organise students into groups and link each group to an academic supervisor.", "bi-diagram-3");
}

function loadMonitor() {
    setActive("btnMonitor");
    mainContent.innerHTML = comingSoon("Monitor Placements & Progress", "Students, companies, tasks, evaluations, training hours and progress.", "bi-graph-up");
}

function loadCompletion() {
    setActive("btnCompletion");
    mainContent.innerHTML = comingSoon("Final Completion Review", "Review both final evaluations, verify requirements, approve and generate the PDF report.", "bi-patch-check");
}

function loadMessages() {
    setActive("btnMessages");
    mainContent.innerHTML = comingSoon("Contact Messages", "View and manage Contact Us messages.", "bi-envelope");
}

function loadAccount() {
    setActive("btnAccount");
    mainContent.innerHTML = comingSoon("Coordinator Account", "Manage your account information.", "bi-person");
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
document.getElementById("userName").textContent = coordinator.shortName;
document.getElementById("userInitials").textContent = getInitials(coordinator.fullName);
loadDashboard();   // the user lands on Dashboard directly
