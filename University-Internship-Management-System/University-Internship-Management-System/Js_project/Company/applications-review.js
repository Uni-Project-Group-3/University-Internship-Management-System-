// @ts-nocheck
/* =========================================================
   UIMS – Applications Review (Company Supervisor)
   Front-end only: uses the sample data below.
   Later, replace loadData() with a call to the server / Firebase,
   and send decisions from acceptApplication() / rejectApplication().

   Load AFTER bootstrap.bundle.min.js, at the end of <body>.
   ========================================================= */
(function() {
    'use strict';

    /* =====================================================
       Sample data
       (ids match the sample opportunities on Manage Opportunities)
       ===================================================== */
    var DAY = 24 * 60 * 60 * 1000;
    var today = new Date(); today.setHours(9, 0, 0, 0);
    function daysAgo(n, hour) {
        var d = new Date(today.getTime() - n * DAY);
        if (hour != null) d.setHours(hour, 15);
        return d;
    }
    function daysFromNow(n) { return new Date(today.getTime() + n * DAY); }

    function loadData() {
        var opportunities = [
            {
                id: 'opp1', title: 'Frontend Developer Intern', location: 'Amman', workMode: 'Hybrid', seats: 4, status: 'Open',
                majors: ['Computer Science', 'Software Engineering'], deadline: daysFromNow(9)
            },
            {
                id: 'opp2', title: 'Network Operations Intern', location: 'Amman', workMode: 'On-site', seats: 3, status: 'Open',
                majors: ['Network Engineering & Security', 'Computer Engineering'], deadline: daysFromNow(2)
            },
            {
                id: 'opp3', title: 'Data Analyst Intern', location: 'Irbid', workMode: 'Remote', seats: 2, status: 'Open',
                majors: ['Data Science & AI', 'Computer Information Systems'], deadline: daysFromNow(15)
            },
            {
                id: 'opp4', title: 'Cybersecurity Intern', location: 'Amman', workMode: 'On-site', seats: 2, status: 'Closed',
                majors: ['Cybersecurity'], deadline: daysFromNow(-30)
            }
        ];

        function app(id, oppId, name, studentId, major, year, hours, skills, status, applied, extra) {
            var first = name.split(' ')[0].toLowerCase();
            var a = {
                id: id, oppId: oppId, name: name, studentId: studentId, major: major,
                year: year, completedHours: hours, skills: skills, status: status,
                appliedAt: applied,
                email: first + '.' + studentId.slice(-4) + '@just.edu.jo',
                phone: '+962 7' + (7 + (parseInt(studentId.slice(-1), 10) % 3)) + ' ' + studentId.slice(2, 5) + ' ' + studentId.slice(-4),
                cvFile: name.replace(/\s+/g, '_') + '_CV.pdf',
                portfolio: 'portfolio/' + first,
                coverLetter: 'I am a ' + year.toLowerCase() + ' ' + major + ' student at JUST. ' +
                    'I enjoy building real projects and I would love to learn from your team, ' +
                    'contribute from the first week, and grow my skills in a professional environment.',
                confirmed: false, reason: '', message: '', decidedAt: null
            };
            for (var k in (extra || {})) a[k] = extra[k];
            return a;
        }

        var applications = [
            // Frontend Developer Intern — 4 seats
            app('a1', 'opp1', 'Layla Haddad', '20221045', 'Computer Science', '4th year', 112, ['HTML', 'CSS', 'JavaScript', 'React'], 'Pending', daysAgo(1, 14)),
            app('a2', 'opp1', 'Omar Khalil', '20211876', 'Software Engineering', '4th year', 120, ['JavaScript', 'Vue', 'Git'], 'Pending', daysAgo(2, 11)),
            app('a6', 'opp1', 'Dana Qasem', '20223310', 'Software Engineering', '3rd year', 96, ['HTML', 'CSS', 'Figma'], 'Pending', daysAgo(3, 16)),
            app('a3', 'opp1', 'Rana Saleh', '20210923', 'Computer Science', '4th year', 118, ['React', 'TypeScript', 'Tailwind'], 'Accepted', daysAgo(8, 10),
                { confirmed: true, decidedAt: daysAgo(6, 12) }),
            app('a4', 'opp1', 'Ahmad Saleem', '20212204', 'Software Engineering', '4th year', 115, ['JavaScript', 'ASP.NET', 'SQL'], 'Accepted', daysAgo(6, 9),
                { decidedAt: daysAgo(2, 13) }),
            app('a5', 'opp1', 'Tariq Mansour', '20224417', 'Computer Science', '3rd year', 90, ['Python', 'Django'], 'Rejected', daysAgo(9, 15),
                {
                    reason: "Skills don't match the requirements", decidedAt: daysAgo(7, 11),
                    message: 'Thank you for applying. This role needs hands-on front-end experience (HTML, CSS, JavaScript). We encourage you to apply again after building a small front-end project.'
                }),

            // Network Operations Intern — 3 seats (full)
            app('a7', 'opp2', 'Yazan Nasser', '20210511', 'Network Engineering & Security', '4th year', 121, ['CCNA', 'Linux', 'Wireshark'], 'Accepted', daysAgo(12, 10),
                { confirmed: true, decidedAt: daysAgo(10, 9) }),
            app('a8', 'opp2', 'Hala Mahmoud', '20211139', 'Computer Engineering', '4th year', 117, ['Networking', 'Python', 'Linux'], 'Accepted', daysAgo(11, 13),
                { confirmed: true, decidedAt: daysAgo(9, 15) }),
            app('a9', 'opp2', 'Sami Odeh', '20213382', 'Network Engineering & Security', '4th year', 114, ['CCNA', 'Firewalls'], 'Accepted', daysAgo(7, 9),
                { decidedAt: daysAgo(3, 10) }),
            app('a10', 'opp2', 'Noor Al-Khatib', '20222907', 'Computer Engineering', '3rd year', 99, ['Networking', 'C++'], 'Pending', daysAgo(1, 9)),

            // Data Analyst Intern — 2 seats
            app('a11', 'opp3', 'Mais Obeidat', '20221788', 'Data Science & AI', '4th year', 110, ['SQL', 'Python', 'Power BI'], 'Pending', daysAgo(0, 8)),
            app('a12', 'opp3', 'Khaled Jaradat', '20214450', 'Computer Information Systems', '4th year', 116, ['Excel', 'SQL', 'Tableau'], 'Pending', daysAgo(2, 17)),
            app('a13', 'opp3', 'Leen Haddad', '20225021', 'Data Science & AI', '3rd year', 88, ['Python', 'Pandas'], 'Rejected', daysAgo(5, 12),
                {
                    reason: 'Incomplete application', decidedAt: daysAgo(4, 10),
                    message: 'Your application was missing a CV. Please upload your CV and apply again before the deadline.'
                }),

            // Cybersecurity Intern — closed
            app('a14', 'opp4', 'Faris Bataineh', '20210307', 'Cybersecurity', '4th year', 122, ['SIEM', 'Linux', 'Python'], 'Accepted', daysAgo(50, 10),
                { confirmed: true, decidedAt: daysAgo(45, 9) }),
            app('a15', 'opp4', 'Jana Smadi', '20211620', 'Cybersecurity', '4th year', 119, ['Network security', 'Bash'], 'Accepted', daysAgo(48, 14),
                { confirmed: true, decidedAt: daysAgo(44, 11) }),
            app('a16', 'opp4', 'Bashar Rawashdeh', '20223954', 'Cybersecurity', '3rd year', 94, ['Linux', 'Python'], 'Rejected', daysAgo(46, 16),
                {
                    reason: 'All seats are filled', decidedAt: daysAgo(40, 10),
                    message: 'Thank you for your interest. All seats for this round are filled — please watch for our next opportunity.'
                })
        ];

        return { opportunities: opportunities, applications: applications };
    }

    var data = loadData();

    /* =====================================================
       State + element refs
       ===================================================== */
    var state = { opp: 'all', status: 'all', q: '', sort: 'newest', openId: null, actionId: null };

    var $ = function(id) { return document.getElementById(id); };
    var els = {
        rows: $('appRows'), empty: $('emptyState'), table: document.querySelector('.app-table'),
        oppFilter: $('oppFilter'), sortBy: $('sortBy'), search: $('searchBox'),
        tabs: $('statusTabs'), summary: $('oppSummary'),
        drawer: $('appDrawer'), drawerBody: $('drawerBody'), drawerFooter: $('drawerFooter'),
        acceptModal: $('acceptModal'), rejectModal: $('rejectModal'),
        rejectMessage: $('rejectMessage'), rejectCount: $('rejectCount'),
        toast: $('reviewToast')
    };

    var drawer = bootstrap.Offcanvas.getOrCreateInstance(els.drawer);
    var acceptModal = bootstrap.Modal.getOrCreateInstance(els.acceptModal);
    var rejectModal = bootstrap.Modal.getOrCreateInstance(els.rejectModal);
    var toast = bootstrap.Toast.getOrCreateInstance(els.toast, { delay: 4000 });

    /* =====================================================
       Helpers
       ===================================================== */
    function esc(text) {
        return String(text == null ? '' : text)
            .replace(/&/g, '&amp;').replace(/</g, '&lt;').replace(/>/g, '&gt;')
            .replace(/"/g, '&quot;').replace(/'/g, '&#39;');
    }
    function pad(n) { return n < 10 ? '0' + n : String(n); }
    function initials(name) {
        return name.split(/\s+/).slice(0, 2).map(function(p) { return p.charAt(0).toUpperCase(); }).join('');
    }
    function avatarTone(name) {
        var sum = 0;
        for (var i = 0; i < name.length; i++) sum += name.charCodeAt(i);
        return 'tone-' + (sum % 4);
    }
    function formatDate(d, withTime) {
        var opts = { month: 'short', day: 'numeric', year: 'numeric' };
        if (withTime) { opts.hour = 'numeric'; opts.minute = '2-digit'; }
        return d.toLocaleString('en-US', opts);
    }
    function relative(d) {
        var start = new Date(d); start.setHours(0, 0, 0, 0);
        var now = new Date(); now.setHours(0, 0, 0, 0);
        var diff = Math.round((now - start) / DAY);
        if (diff <= 0) return 'Today';
        if (diff === 1) return 'Yesterday';
        if (diff < 7) return diff + ' days ago';
        return formatDate(d);
    }

    function oppById(id) {
        for (var i = 0; i < data.opportunities.length; i++) if (data.opportunities[i].id === id) return data.opportunities[i];
        return null;
    }
    function appById(id) {
        for (var i = 0; i < data.applications.length; i++) if (data.applications[i].id === id) return data.applications[i];
        return null;
    }
    function acceptedCount(oppId) {
        return data.applications.filter(function(a) { return a.oppId === oppId && a.status === 'Accepted'; }).length;
    }
    function seatsLeft(opp) { return Math.max(0, opp.seats - acceptedCount(opp.id)); }

    // Why this application can't be accepted right now (or '' if it can)
    function acceptBlocker(a) {
        var opp = oppById(a.oppId);
        if (!opp) return 'Opportunity not found.';
        if (opp.status !== 'Open') return 'This opportunity is closed.';
        if (seatsLeft(opp) === 0) return 'All ' + opp.seats + ' seats are filled.';
        return '';
    }

    function statusBadge(a) {
        if (a.status === 'Pending') return '<span class="status warning">Pending</span>';
        if (a.status === 'Accepted') return '<span class="status success">Accepted</span>';
        return '<span class="status danger">Rejected</span>';
    }
    function statusNote(a) {
        if (a.status === 'Accepted') {
            return a.confirmed
                ? '<span class="status-note ok"><i class="bi bi-patch-check-fill"></i> Student confirmed</span>'
                : '<span class="status-note"><i class="bi bi-clock"></i> Awaiting student</span>';
        }
        if (a.status === 'Rejected') return '<span class="status-note">' + esc(a.reason) + '</span>';
        return '';
    }

    /* =====================================================
       Filtering + rendering
       ===================================================== */
    function inScope(a) { return state.opp === 'all' || a.oppId === state.opp; }

    function visibleApps() {
        var q = state.q.toLowerCase();
        var list = data.applications.filter(function(a) {
            if (!inScope(a)) return false;
            if (state.status !== 'all' && a.status !== state.status) return false;
            if (q) {
                var opp = oppById(a.oppId);
                var hay = (a.name + ' ' + a.studentId + ' ' + a.major + ' ' + (opp ? opp.title : '')).toLowerCase();
                if (hay.indexOf(q) === -1) return false;
            }
            return true;
        });

        list.sort(function(x, y) {
            if (state.sort === 'name') return x.name.localeCompare(y.name);
            if (state.sort === 'oldest') return x.appliedAt - y.appliedAt;
            // newest: pending first, then by date
            if (x.status === 'Pending' && y.status !== 'Pending') return -1;
            if (y.status === 'Pending' && x.status !== 'Pending') return 1;
            return y.appliedAt - x.appliedAt;
        });
        return list;
    }

    function renderRows() {
        var list = visibleApps();
        els.empty.hidden = list.length > 0;
        els.table.classList.toggle('is-empty', list.length === 0);

        els.rows.innerHTML = list.map(function(a) {
            var opp = oppById(a.oppId) || {};
            var blocker = acceptBlocker(a);
            var actions = '<button type="button" class="icon-action" data-action="view" data-id="' + a.id + '" title="View application" aria-label="View application"><i class="bi bi-eye"></i></button>';
            if (a.status === 'Pending') {
                actions +=
                    '<button type="button" class="btn-ghost danger sm" data-action="reject" data-id="' + a.id + '"><i class="bi bi-x-lg"></i><span>Reject</span></button>' +
                    '<button type="button" class="btn-soft success sm" data-action="accept" data-id="' + a.id + '"' +
                    (blocker ? ' disabled title="' + esc(blocker) + '"' : '') + '><i class="bi bi-check-lg"></i><span>Accept</span></button>';
            }

            return '' +
                '<tr data-id="' + a.id + '" class="' + (a.status === 'Pending' ? 'is-pending' : '') + '">' +
                '<td data-label="Student">' +
                '<div class="student-cell">' +
                '<span class="avatar ' + avatarTone(a.name) + '">' + initials(a.name) + '</span>' +
                '<div class="min-w-0">' +
                '<button type="button" class="student-name" data-action="view" data-id="' + a.id + '">' + esc(a.name) + '</button>' +
                '<div class="student-sub">' + esc(a.major) + ' · ' + esc(a.studentId) + '</div>' +
                '</div>' +
                '</div>' +
                '</td>' +
                '<td data-label="Opportunity"><div>' +
                '<div class="opp-cell">' + esc(opp.title) + '</div>' +
                '<div class="student-sub">' + esc(opp.workMode) + ' · ' + esc(opp.location) +
                (opp.status === 'Closed' ? ' · <span class="closed-flag">Closed</span>' : '') + '</div>' +
                '</div></td>' +
                '<td data-label="Applied"><span title="' + esc(formatDate(a.appliedAt, true)) + '">' + relative(a.appliedAt) + '</span></td>' +
                '<td data-label="Status"><div class="status-cell">' + statusBadge(a) + statusNote(a) + '</div></td>' +
                '<td class="actions-cell"><div class="row-actions">' + actions + '</div></td>' +
                '</tr>';
        }).join('');
    }

    function renderCounts() {
        var scoped = data.applications.filter(inScope);
        var count = { all: scoped.length, Pending: 0, Accepted: 0, Rejected: 0 };
        var confirmed = 0;
        scoped.forEach(function(a) {
            count[a.status]++;
            if (a.status === 'Accepted' && a.confirmed) confirmed++;
        });

        Array.prototype.forEach.call(els.tabs.querySelectorAll('[data-count]'), function(span) {
            span.textContent = count[span.getAttribute('data-count')];
        });

        $('statPending').textContent = pad(count.Pending);
        $('statAccepted').textContent = pad(count.Accepted);
        $('statAcceptedNote').textContent = confirmed + ' confirmed by student';
        $('statRejected').textContent = pad(count.Rejected);

        var opps = state.opp === 'all' ? data.opportunities : [oppById(state.opp)];
        var left = opps.filter(function(o) { return o && o.status === 'Open'; })
            .reduce(function(sum, o) { return sum + seatsLeft(o); }, 0);
        $('statSeats').textContent = pad(left);

        var allPending = data.applications.filter(function(a) { return a.status === 'Pending'; }).length;
        var nav = $('navPendingCount');
        if (nav) { nav.textContent = allPending; nav.hidden = allPending === 0; }
    }

    function renderOppFilter() {
        var current = els.oppFilter.value || state.opp;
        els.oppFilter.innerHTML = '<option value="all">All opportunities</option>' +
            data.opportunities.map(function(o) {
                var pending = data.applications.filter(function(a) { return a.oppId === o.id && a.status === 'Pending'; }).length;
                return '<option value="' + o.id + '">' + esc(o.title) +
                    (o.status === 'Closed' ? ' (closed)' : pending ? ' — ' + pending + ' pending' : '') + '</option>';
            }).join('');
        els.oppFilter.value = oppById(current) ? current : 'all';
    }

    function renderSummary() {
        var opp = state.opp === 'all' ? null : oppById(state.opp);
        els.summary.hidden = !opp;
        if (!opp) return;

        var used = acceptedCount(opp.id);
        var pct = opp.seats ? Math.min(100, Math.round(used * 100 / opp.seats)) : 0;
        var full = used >= opp.seats;

        els.summary.innerHTML =
            '<div class="summary-main">' +
            '<div class="summary-title">' +
            '<span class="status ' + (opp.status === 'Open' ? 'success' : 'neutral') + '">' + opp.status + '</span>' +
            '<h2>' + esc(opp.title) + '</h2>' +
            '</div>' +
            '<div class="summary-meta">' +
            '<span><i class="bi bi-geo-alt"></i>' + esc(opp.location) + ' · ' + esc(opp.workMode) + '</span>' +
            '<span><i class="bi bi-calendar-event"></i>Apply by ' + formatDate(opp.deadline) + '</span>' +
            '</div>' +
            '<div>' + opp.majors.map(function(m) { return '<span class="tag">' + esc(m) + '</span>'; }).join('') + '</div>' +
            '</div>' +
            '<div class="summary-seats">' +
            '<div class="d-flex justify-content-between"><span><strong>' + used + '</strong> of ' + opp.seats + ' seats filled</span>' +
            (full ? '<span class="full-flag">Full</span>' : '<span class="text-secondary">' + (opp.seats - used) + ' left</span>') + '</div>' +
            '<div class="progress-slim"><span class="' + (full ? 'success' : '') + '" style="width:' + pct + '%"></span></div>' +
            '<a href="ManageOpportunities.aspx" class="summary-link">Edit opportunity <i class="bi bi-arrow-right"></i></a>' +
            '</div>';
    }

    function renderDrawer() {
        var a = appById(state.openId);
        if (!a) return;
        var opp = oppById(a.oppId) || {};

        var avatar = $('drawerAvatar');
        avatar.textContent = initials(a.name);
        avatar.className = 'avatar lg ' + avatarTone(a.name);
        $('drawerName').textContent = a.name;
        $('drawerSub').innerHTML = esc(a.major) + ' · ' + esc(a.year);

        var decision = '';
        if (a.status === 'Accepted') {
            decision =
                '<div class="decision-box success">' +
                '<i class="bi bi-check2-circle"></i>' +
                '<div><strong>Accepted</strong> on ' + formatDate(a.decidedAt) + '<br />' +
                (a.confirmed ? 'The student confirmed this offer.' : 'Waiting for the student to confirm the offer.') + '</div>' +
                '</div>';
        } else if (a.status === 'Rejected') {
            decision =
                '<div class="decision-box danger">' +
                '<i class="bi bi-x-circle"></i>' +
                '<div><strong>Rejected</strong> on ' + formatDate(a.decidedAt) + ' — ' + esc(a.reason) +
                (a.message ? '<blockquote>' + esc(a.message) + '</blockquote>' : '') + '</div>' +
                '</div>';
        }

        els.drawerBody.innerHTML =
            decision +
            '<section class="drawer-section">' +
            '<h3>Application</h3>' +
            '<dl class="info-grid">' +
            '<dt>Opportunity</dt><dd>' + esc(opp.title) + '</dd>' +
            '<dt>Applied</dt><dd>' + formatDate(a.appliedAt, true) + '</dd>' +
            '<dt>Status</dt><dd>' + statusBadge(a) + '</dd>' +
            '</dl>' +
            '</section>' +
            '<section class="drawer-section">' +
            '<h3>Academic information</h3>' +
            '<dl class="info-grid">' +
            '<dt>Student ID</dt><dd>' + esc(a.studentId) + '</dd>' +
            '<dt>College</dt><dd>Computer &amp; Information Technology</dd>' +
            '<dt>Major</dt><dd>' + esc(a.major) + '</dd>' +
            '<dt>Year</dt><dd>' + esc(a.year) + ' · ' + a.completedHours + ' credit hours</dd>' +
            '</dl>' +
            '</section>' +
            '<section class="drawer-section">' +
            '<h3>Contact</h3>' +
            '<div class="contact-list">' +
            '<a href="mailto:' + esc(a.email) + '"><i class="bi bi-envelope"></i>' + esc(a.email) + '</a>' +
            '<span><i class="bi bi-telephone"></i>' + esc(a.phone) + '</span>' +
            '</div>' +
            '</section>' +
            '<section class="drawer-section">' +
            '<h3>Skills</h3>' +
            '<div>' + a.skills.map(function(s) { return '<span class="tag">' + esc(s) + '</span>'; }).join('') + '</div>' +
            '</section>' +
            '<section class="drawer-section">' +
            '<h3>Cover letter</h3>' +
            '<p class="cover-letter">' + esc(a.coverLetter) + '</p>' +
            '</section>' +
            '<section class="drawer-section">' +
            '<h3>Attachments</h3>' +
            '<div class="file-list">' +
            '<a href="#" class="file-item" data-action="file"><span class="file-icon pdf"><i class="bi bi-file-earmark-pdf"></i></span>' +
            '<span><strong>' + esc(a.cvFile) + '</strong><small>CV · PDF</small></span><i class="bi bi-box-arrow-up-right"></i></a>' +
            '<a href="#" class="file-item" data-action="file"><span class="file-icon"><i class="bi bi-link-45deg"></i></span>' +
            '<span><strong>' + esc(a.portfolio) + '</strong><small>Portfolio link</small></span><i class="bi bi-box-arrow-up-right"></i></a>' +
            '</div>' +
            '</section>';

        if (a.status === 'Pending') {
            var blocker = acceptBlocker(a);
            els.drawerFooter.hidden = false;
            els.drawerFooter.innerHTML =
                (blocker ? '<div class="footer-warning"><i class="bi bi-exclamation-triangle"></i>' + esc(blocker) + '</div>' : '') +
                '<div class="footer-buttons">' +
                '<button type="button" class="btn-ghost danger lg" data-action="reject" data-id="' + a.id + '"><i class="bi bi-x-lg"></i> Reject</button>' +
                '<button type="button" class="btn-primary-pill success" data-action="accept" data-id="' + a.id + '"' + (blocker ? ' disabled' : '') + '><i class="bi bi-check-lg"></i> Accept student</button>' +
                '</div>';
        } else {
            els.drawerFooter.hidden = true;
            els.drawerFooter.innerHTML = '';
        }
    }

    function render() {
        renderCounts();
        renderSummary();
        renderRows();
        if (state.openId) renderDrawer();
    }

    /* =====================================================
       Actions
       ===================================================== */
    function openDrawer(id) {
        state.openId = id;
        renderDrawer();
        drawer.show();
    }

    function openAccept(id) {
        var a = appById(id);
        if (!a) return;
        var blocker = acceptBlocker(a);
        if (blocker) { showToast(blocker, 'error'); return; }

        state.actionId = id;
        var opp = oppById(a.oppId);
        var used = acceptedCount(opp.id) + 1;
        $('acceptSub').innerHTML = esc(a.name) + ' → <strong>' + esc(opp.title) + '</strong>';
        $('acceptSeats').innerHTML =
            '<div class="d-flex justify-content-between"><span>After accepting</span><span><strong>' + used + '</strong> of ' + opp.seats + ' seats filled</span></div>' +
            '<div class="progress-slim"><span class="' + (used >= opp.seats ? 'success' : '') + '" style="width:' + Math.round(used * 100 / opp.seats) + '%"></span></div>' +
            (used >= opp.seats ? '<div class="seat-note">This fills the last seat.</div>' : '');
        acceptModal.show();
    }

    function acceptApplication() {
        var a = appById(state.actionId);
        if (!a) return;
        // TODO (back-end): send the decision to the server / Firebase here.
        a.status = 'Accepted';
        a.confirmed = false;
        a.decidedAt = new Date();
        acceptModal.hide();
        renderOppFilter();
        render();
        showToast(a.name + ' was accepted. Waiting for the student to confirm.', 'success');
    }

    function openReject(id) {
        var a = appById(id);
        if (!a) return;
        state.actionId = id;
        var opp = oppById(a.oppId);
        $('rejectSub').innerHTML = esc(a.name) + ' → <strong>' + esc(opp.title) + '</strong>';

        Array.prototype.forEach.call(document.querySelectorAll('input[name="rejectReason"]'), function(r) { r.checked = false; });
        els.rejectMessage.value = '';
        els.rejectMessage.dataset.template = '';
        updateRejectCount();
        $('reasonError').hidden = true;
        $('messageError').hidden = true;
        els.rejectMessage.classList.remove('is-invalid');
        $('reasonChips').classList.remove('is-invalid');
        rejectModal.show();
    }

    var reasonTemplates = {
        'All seats are filled': 'Thank you for applying. All seats for this opportunity are now filled. Please keep an eye on our future opportunities.',
        "Skills don't match the requirements": "Thank you for applying. For this role we need more experience with the listed requirements. We encourage you to build on these skills and apply again.",
        "Major doesn't fit the role": 'Thank you for applying. This role is aimed at a different specialization, so we can\'t move forward with your application this time.',
        'Incomplete application': 'Your application is missing some information (for example your CV). Please complete it and apply again before the deadline.',
        'Other': ''
    };

    function updateRejectCount() {
        els.rejectCount.textContent = els.rejectMessage.value.length + ' / 400';
    }

    function rejectApplication() {
        var a = appById(state.actionId);
        if (!a) return;
        var picked = document.querySelector('input[name="rejectReason"]:checked');
        var message = els.rejectMessage.value.trim();
        var ok = true;

        $('reasonError').hidden = !!picked;
        $('reasonChips').classList.toggle('is-invalid', !picked);
        if (!picked) ok = false;

        var shortMsg = message.length < 15;
        $('messageError').hidden = !shortMsg;
        els.rejectMessage.classList.toggle('is-invalid', shortMsg);
        if (shortMsg) ok = false;

        if (!ok) return;

        // TODO (back-end): send the decision + reason to the server / Firebase here.
        a.status = 'Rejected';
        a.reason = picked.value;
        a.message = message;
        a.decidedAt = new Date();
        rejectModal.hide();
        renderOppFilter();
        render();
        showToast(a.name + "'s application was rejected. The student can see your reason.", 'neutral');
    }

    function showToast(text, kind) {
        var icon = $('toastIcon');
        $('toastText').textContent = text;
        els.toast.classList.remove('is-success', 'is-error', 'is-neutral');
        els.toast.classList.add('is-' + (kind || 'success'));
        icon.className = kind === 'error' ? 'bi bi-exclamation-circle-fill'
            : kind === 'neutral' ? 'bi bi-info-circle-fill'
                : 'bi bi-check-circle-fill';
        toast.show();
    }

    /* =====================================================
       Events
       ===================================================== */
    // One click handler for every [data-action] button on the page
    document.addEventListener('click', function(e) {
        var btn = e.target.closest('[data-action]');
        if (!btn) {
            // Clicking anywhere on a row opens the details
            var row = e.target.closest('#appRows tr[data-id]');
            if (row) openDrawer(row.getAttribute('data-id'));
            return;
        }
        var id = btn.getAttribute('data-id');
        switch (btn.getAttribute('data-action')) {
            case 'toggle-sidebar': document.body.classList.toggle('sidebar-open'); break;
            case 'view': openDrawer(id); break;
            case 'accept': if (!btn.disabled) openAccept(id); break;
            case 'reject': openReject(id); break;
            case 'file':
                e.preventDefault();
                showToast('Sample data — files will open once the page is connected to Firebase.', 'neutral');
                break;
        }
    });

    els.oppFilter.addEventListener('change', function() {
        state.opp = this.value;
        var url = new URL(window.location.href);
        if (state.opp === 'all') url.searchParams.delete('opp'); else url.searchParams.set('opp', state.opp);
        window.history.replaceState(null, '', url);
        render();
    });

    els.sortBy.addEventListener('change', function() { state.sort = this.value; renderRows(); });

    els.search.addEventListener('input', function() { state.q = this.value.trim(); renderRows(); });
    els.search.addEventListener('keydown', function(e) { if (e.key === 'Enter') e.preventDefault(); });

    els.tabs.addEventListener('click', function(e) {
        var tab = e.target.closest('button[data-status]');
        if (!tab) return;
        state.status = tab.getAttribute('data-status');
        Array.prototype.forEach.call(els.tabs.querySelectorAll('button'), function(b) {
            b.classList.toggle('active', b === tab);
            b.setAttribute('aria-selected', b === tab ? 'true' : 'false');
        });
        renderRows();
    });

    $('confirmAccept').addEventListener('click', acceptApplication);
    $('confirmReject').addEventListener('click', rejectApplication);

    // Picking a reason fills a polite message (unless the supervisor already typed their own)
    $('reasonChips').addEventListener('change', function(e) {
        var reason = e.target.value;
        var current = els.rejectMessage.value.trim();
        if (!current || current === els.rejectMessage.dataset.template) {
            els.rejectMessage.value = reasonTemplates[reason] || '';
            els.rejectMessage.dataset.template = els.rejectMessage.value;
            updateRejectCount();
        }
        if (els.rejectMessage.value.trim().length >= 15) {
            $('messageError').hidden = true;
            els.rejectMessage.classList.remove('is-invalid');
        }
        $('reasonError').hidden = true;
        this.classList.remove('is-invalid');
        if (reason === 'Other') els.rejectMessage.focus();
    });
    els.rejectMessage.addEventListener('input', function() {
        updateRejectCount();
        if (this.value.trim().length >= 15) {
            $('messageError').hidden = true;
            this.classList.remove('is-invalid');
        }
    });

    // Enter inside the modals must not submit the ASP.NET form
    [els.acceptModal, els.rejectModal].forEach(function(m) {
        m.addEventListener('keydown', function(e) {
            if (e.key === 'Enter' && e.target.tagName === 'INPUT') e.preventDefault();
        });
    });

    els.drawer.addEventListener('hidden.bs.offcanvas', function() { state.openId = null; });

    /* =====================================================
       Init
       ===================================================== */
    var fromUrl = new URLSearchParams(window.location.search).get('opp');   // e.g. ?opp=opp1 from Manage Opportunities
    renderOppFilter();
    if (fromUrl && oppById(fromUrl)) {
        state.opp = fromUrl;
        els.oppFilter.value = fromUrl;
    }
    render();
})();
