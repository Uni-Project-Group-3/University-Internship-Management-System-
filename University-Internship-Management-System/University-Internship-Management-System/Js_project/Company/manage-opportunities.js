// @ts-nocheck
/* =========================================================
   UIMS – Manage Opportunities (Company Supervisor)
   Front-end only: uses the sample data below.
   Later, replace loadData() with a call to the server / Firebase,
   and send changes from saveOpportunity() / setStatus().

   Load AFTER bootstrap.bundle.min.js, at the end of <body>.
   ========================================================= */
(function() {
    'use strict';

    /* =====================================================
       Constants + sample data
       (ids match the sample data on Applications Review)
       ===================================================== */
    // Specializations of the Faculty of Computer & Information Technology at JUST
    var MAJORS = [
        'Computer Science',
        'Computer Engineering',
        'Software Engineering',
        'Computer Information Systems',
        'Cybersecurity',
        'Data Science & AI',
        'Network Engineering & Security'
    ];

    var DAY = 24 * 60 * 60 * 1000;
    function todayAt0() { var d = new Date(); d.setHours(0, 0, 0, 0); return d; }
    function addDays(n) { return new Date(todayAt0().getTime() + n * DAY); }

    function loadData() {
        return [
            {
                id: 'opp1', title: 'Frontend Developer Intern',
                description: 'Build and polish customer-facing web pages for Orange Money using modern JavaScript.',
                requirements: 'HTML, CSS, JavaScript; basic Git; a portfolio or GitHub link.',
                majors: ['Computer Science', 'Software Engineering'],
                seats: 4, accepted: 2, applicants: 6, pending: 3, location: 'Amman', workMode: 'Hybrid',
                deadline: addDays(9), start: addDays(20), duration: 8, status: 'Open', createdAt: addDays(-6)
            },
            {
                id: 'opp2', title: 'Network Operations Intern',
                description: 'Support the NOC team in monitoring, troubleshooting, and documenting the core network.',
                requirements: 'Networking fundamentals (CCNA level), Linux basics.',
                majors: ['Network Engineering & Security', 'Computer Engineering'],
                seats: 3, accepted: 3, applicants: 4, pending: 1, location: 'Amman', workMode: 'On-site',
                deadline: addDays(2), start: addDays(12), duration: 8, status: 'Open', createdAt: addDays(-14)
            },
            {
                id: 'opp3', title: 'Data Analyst Intern',
                description: 'Analyze customer usage data and build Power BI dashboards for the marketing team.',
                requirements: 'SQL, Excel; Python or Power BI is a plus.',
                majors: ['Data Science & AI', 'Computer Information Systems'],
                seats: 2, accepted: 0, applicants: 3, pending: 2, location: 'Irbid', workMode: 'Remote',
                deadline: addDays(15), start: addDays(30), duration: 10, status: 'Open', createdAt: addDays(-2)
            },
            {
                id: 'opp4', title: 'Cybersecurity Intern',
                description: 'Assist the SOC team with log review, phishing analysis, and security awareness material.',
                requirements: 'Security fundamentals, Linux, curiosity for incident response.',
                majors: ['Cybersecurity'],
                seats: 2, accepted: 2, applicants: 3, pending: 0, location: 'Amman', workMode: 'On-site',
                deadline: addDays(-30), start: addDays(-20), duration: 8, status: 'Closed', createdAt: addDays(-40)
            }
        ];
    }

    var opportunities = loadData();

    /* =====================================================
       State + element refs
       ===================================================== */
    var state = { status: 'all', q: '', sort: 'newest', editingId: null, closingId: null };

    var $ = function(id) { return document.getElementById(id); };
    var els = {
        grid: $('oppGrid'), empty: $('emptyState'), tabs: $('statusTabs'),
        sortBy: $('sortBy'), search: $('searchBox'),
        modal: $('oppModal'), closeModal: $('closeModal'), toast: $('pageToast'),
        title: $('oppTitle'), description: $('oppDescription'), requirements: $('oppRequirements'),
        majors: $('majorPicker'), seats: $('oppSeats'), location: $('oppLocation'),
        deadline: $('oppDeadline'), start: $('oppStart'), duration: $('oppDuration')
    };

    var oppModal = bootstrap.Modal.getOrCreateInstance(els.modal);
    var closeModal = bootstrap.Modal.getOrCreateInstance(els.closeModal);
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
    function formatDate(d, withWeekday) {
        var opts = { month: 'short', day: 'numeric', year: 'numeric' };
        if (withWeekday) opts.weekday = 'short';
        return d.toLocaleDateString('en-US', opts);
    }
    function toInputDate(d) {               // Date → yyyy-MM-dd
        return d.getFullYear() + '-' + pad(d.getMonth() + 1) + '-' + pad(d.getDate());
    }
    function fromInputDate(value) {          // yyyy-MM-dd → Date (or null)
        if (!value) return null;
        var d = new Date(value + 'T00:00:00');
        return isNaN(d) ? null : d;
    }
    function oppById(id) {
        for (var i = 0; i < opportunities.length; i++) if (opportunities[i].id === id) return opportunities[i];
        return null;
    }
    function seatsLeft(o) { return Math.max(0, o.seats - o.accepted); }
    function isOpen(o) { return o.status === 'Open'; }
    function daysUntil(d) { return Math.round((d - todayAt0()) / DAY); }

    function deadlineLabel(o) {
        if (!isOpen(o)) return 'Closed';
        var n = daysUntil(o.deadline);
        if (n < 0) return 'Deadline passed';
        if (n === 0) return 'Closes today';
        if (n === 1) return 'Closes tomorrow';
        if (n <= 3) return n + ' days left';
        return 'Apply by ' + formatDate(o.deadline);
    }

    /* =====================================================
       Rendering
       ===================================================== */
    function visibleOpps() {
        var q = state.q.toLowerCase();
        var list = opportunities.filter(function(o) {
            if (state.status !== 'all' && o.status !== state.status) return false;
            if (q) {
                var hay = (o.title + ' ' + o.location + ' ' + o.workMode + ' ' + o.majors.join(' ')).toLowerCase();
                if (hay.indexOf(q) === -1) return false;
            }
            return true;
        });
        list.sort(function(a, b) {
            switch (state.sort) {
                case 'deadline': return a.deadline - b.deadline;
                case 'applicants': return b.applicants - a.applicants;
                case 'title': return a.title.localeCompare(b.title);
                default: return b.createdAt - a.createdAt;
            }
        });
        return list;
    }

    function cardHtml(o) {
        var open = isOpen(o);
        var pct = o.seats ? Math.min(100, Math.round(o.accepted * 100 / o.seats)) : 0;
        var full = o.accepted >= o.seats;
        var soon = open && daysUntil(o.deadline) <= 3 && daysUntil(o.deadline) >= 0;
        var passed = open && daysUntil(o.deadline) < 0;

        return '' +
            '<div class="col-md-6 col-xxl-4">' +
            '<article class="opp-card' + (open ? '' : ' is-closed') + '" data-id="' + o.id + '">' +
            '<div class="opp-top">' +
            '<span class="status ' + (open ? 'success' : 'neutral') + '">' + o.status + '</span>' +
            '<span class="opp-deadline' + (soon ? ' is-soon' : '') + (passed ? ' is-late' : '') + '">' +
            '<i class="bi bi-calendar-event"></i> ' + deadlineLabel(o) + '</span>' +
            '</div>' +
            '<h3 class="opp-title">' + esc(o.title) + '</h3>' +
            '<div class="opp-meta">' +
            '<span><i class="bi bi-geo-alt"></i>' + esc(o.location) + '</span>' +
            '<span><i class="bi bi-laptop"></i>' + esc(o.workMode) + '</span>' +
            '<span><i class="bi bi-hourglass-split"></i>' + o.duration + ' weeks</span>' +
            '</div>' +
            '<p class="opp-desc">' + esc(o.description) + '</p>' +
            '<div class="opp-majors">' + o.majors.map(function(m) { return '<span class="tag">' + esc(m) + '</span>'; }).join('') + '</div>' +
            '<div class="opp-seats">' +
            '<div class="d-flex justify-content-between">' +
            '<span><strong>' + o.accepted + '</strong> of ' + o.seats + ' seats filled' +
            (full ? ' <span class="full-flag">Full</span>' : '') + '</span>' +
            '<span class="text-secondary">' + o.applicants + ' applicants</span>' +
            '</div>' +
            '<div class="progress-slim"><span class="' + (full ? 'success' : '') + '" style="width: ' + pct + '%"></span></div>' +
            '</div>' +
            '<div class="opp-actions">' +
            '<a class="btn-soft" href="ApplicationsReview.aspx?opp=' + encodeURIComponent(o.id) + '"><i class="bi bi-inbox"></i> Applications</a>' +
            '<button type="button" class="btn-ghost" data-action="edit" data-id="' + o.id + '"><i class="bi bi-pencil"></i> Edit</button>' +
            (open
                ? '<button type="button" class="btn-ghost danger" data-action="close" data-id="' + o.id + '"><i class="bi bi-lock"></i> Close</button>'
                : '<button type="button" class="btn-ghost success" data-action="reopen" data-id="' + o.id + '"><i class="bi bi-unlock"></i> Reopen</button>') +
            '</div>' +
            '</article>' +
            '</div>';
    }

    function renderStats() {
        var open = opportunities.filter(isOpen);
        $('statTotal').textContent = pad(opportunities.length);
        $('statOpen').textContent = pad(open.length);
        $('statSeats').textContent = pad(open.reduce(function(s, o) { return s + seatsLeft(o); }, 0));
        $('statApplicants').textContent = pad(open.reduce(function(s, o) { return s + o.applicants; }, 0));

        var counts = { all: opportunities.length, Open: open.length, Closed: opportunities.length - open.length };
        Array.prototype.forEach.call(els.tabs.querySelectorAll('[data-count]'), function(span) {
            span.textContent = counts[span.getAttribute('data-count')];
        });
    }

    function renderGrid() {
        var list = visibleOpps();
        els.grid.innerHTML = list.map(cardHtml).join('');
        els.empty.hidden = list.length > 0;
        if (!list.length) {
            var filtered = state.q || state.status !== 'all';
            $('emptyTitle').textContent = filtered ? 'Nothing matches' : 'No opportunities here yet';
            $('emptyText').textContent = filtered
                ? 'Try another tab or search word.'
                : 'Post your first internship so JUST students can start applying.';
        }
    }

    function render() { renderStats(); renderGrid(); }

    /* =====================================================
       Add / Edit form
       ===================================================== */
    // Build the specialization chips once
    els.majors.innerHTML = MAJORS.map(function(m, i) {
        return '<li><input type="checkbox" id="major' + i + '" value="' + esc(m) + '" />' +
            '<label for="major' + i + '">' + esc(m) + '</label></li>';
    }).join('');

    function selectedMajors() {
        return Array.prototype.map.call(els.majors.querySelectorAll('input:checked'), function(i) { return i.value; });
    }
    function selectedWorkMode() {
        var r = document.querySelector('input[name="workMode"]:checked');
        return r ? r.value : 'On-site';
    }

    function fillForm(o) {
        els.title.value = o ? o.title : '';
        els.description.value = o ? o.description : '';
        els.requirements.value = o ? o.requirements : '';
        Array.prototype.forEach.call(els.majors.querySelectorAll('input'), function(i) {
            i.checked = o ? o.majors.indexOf(i.value) !== -1 : false;
        });
        els.seats.value = o ? o.seats : 1;
        var mode = o ? o.workMode : 'On-site';
        Array.prototype.forEach.call(document.querySelectorAll('input[name="workMode"]'), function(r) { r.checked = r.value === mode; });
        els.location.value = o ? o.location : '';
        els.deadline.value = toInputDate(o ? o.deadline : addDays(14));
        els.start.value = toInputDate(o ? o.start : addDays(30));
        els.duration.value = o ? o.duration : 8;
        clearErrors();
    }

    function openForm(id) {
        var o = id ? oppById(id) : null;
        state.editingId = o ? o.id : null;

        $('oppModalTitle').textContent = o ? 'Edit opportunity' : 'New opportunity';
        $('oppModalSub').textContent = o
            ? 'Update the details below. Changes show to students as soon as you save.'
            : 'Fill in the details below. Students in the selected majors will see it right away.';
        $('oppModalIcon').className = o ? 'bi bi-pencil-square' : 'bi bi-briefcase';
        $('saveOppText').textContent = o ? 'Save changes' : 'Publish opportunity';

        fillForm(o);
        updateCounters();
        updatePreview();
        oppModal.show();
    }

    /* ----- Validation ----- */
    function setError(fieldId, message) {
        var box = document.querySelector('[data-error-for="' + fieldId + '"]');
        var field = $(fieldId);
        if (box) { box.textContent = message || ''; box.hidden = !message; }
        if (field) {
            field.classList.toggle('is-invalid', !!message);
            var wrap = field.closest('.stepper, .input-icon');
            if (wrap) wrap.classList.toggle('is-invalid', !!message);
        }
    }
    function clearErrors() {
        Array.prototype.forEach.call(els.modal.querySelectorAll('[data-error-for]'), function(box) {
            setError(box.getAttribute('data-error-for'), '');
        });
    }

    function validate() {
        clearErrors();
        var errors = {};
        var editing = state.editingId ? oppById(state.editingId) : null;

        if (!els.title.value.trim()) errors.oppTitle = 'Enter a title.';
        if (!els.description.value.trim()) errors.oppDescription = 'Enter a description.';
        if (!els.requirements.value.trim()) errors.oppRequirements = 'Enter the requirements.';
        if (!selectedMajors().length) errors.majorPicker = 'Choose at least one specialization.';
        if (!els.location.value.trim()) errors.oppLocation = 'Enter a location.';

        var seats = parseInt(els.seats.value, 10);
        if (isNaN(seats)) errors.oppSeats = 'Enter the number of seats.';
        else if (seats < 1 || seats > 50) errors.oppSeats = 'Seats must be 1–50.';
        else if (editing && seats < editing.accepted)
            errors.oppSeats = editing.accepted + ' students are already accepted — seats can\'t be lower than that.';

        var weeks = parseInt(els.duration.value, 10);
        if (isNaN(weeks)) errors.oppDuration = 'Enter the duration.';
        else if (weeks < 1 || weeks > 52) errors.oppDuration = 'Duration must be 1–52 weeks.';

        var deadline = fromInputDate(els.deadline.value);
        var start = fromInputDate(els.start.value);
        if (!deadline) errors.oppDeadline = 'Pick a deadline.';
        else if (!editing && deadline < todayAt0()) errors.oppDeadline = 'The deadline can\'t be in the past.';
        if (!start) errors.oppStart = 'Pick a start date.';
        if (deadline && start && deadline > start && !errors.oppDeadline)
            errors.oppDeadline = 'Must be on or before the start date.';

        Object.keys(errors).forEach(function(id) { setError(id, errors[id]); });

        var first = Object.keys(errors)[0];
        if (first) {
            var el = $(first);
            el.scrollIntoView({ behavior: 'smooth', block: 'center' });
            if (el.focus && el.tagName !== 'UL') el.focus({ preventScroll: true });
        }
        return !first;
    }

    /* ----- Save ----- */
    function saveOpportunity() {
        if (!validate()) return;

        var values = {
            title: els.title.value.trim(),
            description: els.description.value.trim(),
            requirements: els.requirements.value.trim(),
            majors: selectedMajors(),
            seats: parseInt(els.seats.value, 10),
            workMode: selectedWorkMode(),
            location: els.location.value.trim(),
            deadline: fromInputDate(els.deadline.value),
            start: fromInputDate(els.start.value),
            duration: parseInt(els.duration.value, 10)
        };

        // TODO (back-end): send `values` to the server / Firebase here.
        if (state.editingId) {
            var o = oppById(state.editingId);
            for (var k in values) o[k] = values[k];
            showToast('Changes to "' + o.title + '" are saved.', 'success');
        } else {
            values.id = 'opp' + Date.now();
            values.accepted = 0;
            values.applicants = 0;
            values.pending = 0;
            values.status = 'Open';
            values.createdAt = new Date();
            opportunities.push(values);
            showToast('"' + values.title + '" is published. Students in the selected majors can now apply.', 'success');
            // Show the new card: switch to a tab where it is visible
            if (state.status === 'Closed') selectTab('all');
        }

        oppModal.hide();
        render();
        highlight(state.editingId || values.id);
    }

    function highlight(id) {
        var card = els.grid.querySelector('[data-id="' + id + '"]');
        if (!card) return;
        card.classList.add('is-new');
        card.scrollIntoView({ behavior: 'smooth', block: 'center' });
        setTimeout(function() { card.classList.remove('is-new'); }, 1800);
    }

    /* ----- Counters, steppers, live preview ----- */
    function updateCounters() {
        Array.prototype.forEach.call(els.modal.querySelectorAll('.char-count[data-for]'), function(c) {
            var input = $(c.getAttribute('data-for'));
            var max = parseInt(input.getAttribute('maxlength'), 10) || 0;
            c.textContent = input.value.length + (max ? ' / ' + max : '');
            c.classList.toggle('near-limit', max && input.value.length >= max * 0.9);
        });
    }

    function setText(id, text, fallback) {
        var el = $(id);
        var value = String(text == null ? '' : text).trim();
        el.textContent = value || fallback;
        el.classList.toggle('is-placeholder', !value);
    }

    function updatePreview() {
        var editing = state.editingId ? oppById(state.editingId) : null;
        setText('pvTitle', els.title.value, 'Opportunity title');
        setText('pvDesc', els.description.value, 'A short description of the internship appears here.');
        setText('pvLocation', els.location.value, 'Location');
        setText('pvMode', selectedWorkMode(), 'On-site');
        setText('pvDuration', els.duration.value, '—');
        setText('pvSeats', els.seats.value, '—');

        var deadline = fromInputDate(els.deadline.value);
        setText('pvDeadline', deadline ? formatDate(deadline) : '', '—');

        var accepted = editing ? editing.accepted : 0;
        var seats = parseInt(els.seats.value, 10) || 0;
        $('pvAccepted').textContent = accepted;
        $('pvApplicants').textContent = editing ? editing.applicants : 0;
        $('pvBar').style.width = (seats ? Math.min(100, Math.round(accepted * 100 / seats)) : 0) + '%';

        var status = $('pvStatus');
        var open = !editing || isOpen(editing);
        status.textContent = open ? 'Open' : 'Closed';
        status.className = 'status ' + (open ? 'success' : 'neutral');

        var majors = selectedMajors();
        $('pvMajors').innerHTML = majors.length
            ? majors.map(function(m) { return '<span class="tag">' + esc(m) + '</span>'; }).join('')
            : '<span class="tag tag-empty">No majors selected</span>';

        updateDateHint();
    }

    function updateDateHint() {
        var hint = $('dateHint'), text = $('dateHintText');
        var start = fromInputDate(els.start.value);
        var deadline = fromInputDate(els.deadline.value);
        var weeks = parseInt(els.duration.value, 10);

        hint.classList.remove('is-warning');
        if (start && deadline && deadline > start) {
            hint.classList.add('is-warning');
            text.textContent = 'The application deadline is after the start date — move it earlier.';
        } else if (start && weeks > 0) {
            var end = new Date(start.getTime() + weeks * 7 * DAY);
            text.innerHTML = 'Training runs <strong>' + formatDate(start) + '</strong> → <strong>' +
                formatDate(end, true) + '</strong> (' + weeks + ' weeks).';
        } else {
            text.textContent = 'Pick a start date and duration to see when the internship ends.';
        }
    }

    els.modal.addEventListener('input', function(e) {
        updateCounters();
        updatePreview();
        // Clear a field's error as soon as the user fixes it
        if (e.target.id && e.target.classList.contains('is-invalid')) setError(e.target.id, '');
    });
    els.modal.addEventListener('change', function(e) {
        updatePreview();
        if (e.target.closest('#majorPicker') && selectedMajors().length) setError('majorPicker', '');
    });

    Array.prototype.forEach.call(els.modal.querySelectorAll('.step-btn'), function(btn) {
        btn.addEventListener('click', function() {
            var input = $(btn.getAttribute('data-target'));
            var min = parseInt(input.min, 10) || 1;
            var max = parseInt(input.max, 10) || 999;
            var value = parseInt(input.value, 10) || min;
            input.value = Math.min(max, Math.max(min, value + parseInt(btn.getAttribute('data-step'), 10)));
            input.dispatchEvent(new Event('input', { bubbles: true }));
        });
    });

    // Enter inside the popup must not submit the ASP.NET form
    els.modal.addEventListener('keydown', function(e) {
        if (e.key === 'Enter' && e.target.tagName === 'INPUT') {
            e.preventDefault();
            saveOpportunity();
        }
    });

    els.modal.addEventListener('shown.bs.modal', function() { els.title.focus(); });

    $('saveOpp').addEventListener('click', saveOpportunity);

    /* =====================================================
       Close / Reopen
       ===================================================== */
    function askClose(id) {
        var o = oppById(id);
        if (!o) return;
        state.closingId = id;
        $('closeSub').textContent = '"' + o.title + '" — ' + o.accepted + ' of ' + o.seats + ' seats filled';

        var pending = o.pending || 0;
        var line = $('closePendingLine');
        line.hidden = pending === 0;
        line.querySelector('span').textContent = pending + ' application' + (pending === 1 ? '' : 's') + ' still waiting — you can still review them.';
        closeModal.show();
    }

    function setStatus(id, status) {
        var o = oppById(id);
        if (!o) return;
        // TODO (back-end): save the new status to the server / Firebase here.
        o.status = status;
        render();
    }

    $('confirmClose').addEventListener('click', function() {
        var o = oppById(state.closingId);
        if (!o) return;
        setStatus(o.id, 'Closed');
        closeModal.hide();
        showToast('"' + o.title + '" is closed. Students can no longer apply.', 'neutral');
    });

    function reopen(id) {
        var o = oppById(id);
        if (!o) return;
        if (daysUntil(o.deadline) < 0) {
            showToast('The deadline has passed — set a new deadline, then reopen.', 'error');
            openForm(id);
            return;
        }
        setStatus(id, 'Open');
        showToast('"' + o.title + '" is open again for applications.', 'success');
        highlight(id);
    }

    /* =====================================================
       Toast
       ===================================================== */
    function showToast(text, kind) {
        $('toastText').textContent = text;
        els.toast.classList.remove('is-success', 'is-error', 'is-neutral');
        els.toast.classList.add('is-' + (kind || 'success'));
        $('toastIcon').className = kind === 'error' ? 'bi bi-exclamation-circle-fill'
            : kind === 'neutral' ? 'bi bi-info-circle-fill'
                : 'bi bi-check-circle-fill';
        toast.show();
    }

    /* =====================================================
       Toolbar + page events
       ===================================================== */
    function selectTab(status) {
        state.status = status;
        Array.prototype.forEach.call(els.tabs.querySelectorAll('button'), function(b) {
            var on = b.getAttribute('data-status') === status;
            b.classList.toggle('active', on);
            b.setAttribute('aria-selected', on ? 'true' : 'false');
        });
    }

    els.tabs.addEventListener('click', function(e) {
        var tab = e.target.closest('button[data-status]');
        if (!tab) return;
        selectTab(tab.getAttribute('data-status'));
        renderGrid();
    });

    els.sortBy.addEventListener('change', function() { state.sort = this.value; renderGrid(); });
    els.search.addEventListener('input', function() { state.q = this.value.trim(); renderGrid(); });
    els.search.addEventListener('keydown', function(e) { if (e.key === 'Enter') e.preventDefault(); });

    document.addEventListener('click', function(e) {
        var btn = e.target.closest('[data-action]');
        if (!btn) return;
        var id = btn.getAttribute('data-id');
        switch (btn.getAttribute('data-action')) {
            case 'toggle-sidebar': document.body.classList.toggle('sidebar-open'); break;
            case 'new': openForm(null); break;
            case 'edit': openForm(id); break;
            case 'close': askClose(id); break;
            case 'reopen': reopen(id); break;
        }
    });

    /* =====================================================
       Init
       ===================================================== */
    render();
})();
