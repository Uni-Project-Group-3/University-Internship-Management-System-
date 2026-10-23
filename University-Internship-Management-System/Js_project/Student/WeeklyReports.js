(function () {
    'use strict';
    const list = document.getElementById('studentReports'), notice = document.getElementById('wrNotice');
    function node(tag, text, cls) { const el = document.createElement(tag); el.textContent = String(text ?? ''); if (cls) el.className = cls; return el; }
    async function load() {
        notice.textContent = 'Loading your read-only report status…';
        const response = await fetch('../WeeklyReportsApi.ashx?action=studentReports', { credentials: 'same-origin', cache: 'no-store' }), data = await response.json();
        if (!response.ok) throw new Error(data.message || 'Report status could not be loaded.');
        list.replaceChildren();
        for (const report of Object.values(data.reports || {})) {
            const card = node('article', '', 'wr-report'), info = node('div');
            info.append(node('h3', 'Training week ' + report.weekNumber), node('span', report.status, 'wr-badge ' + report.status), node('p', (report.weekStart || '') + ' — ' + (report.weekEnd || '')));
            const evaluation = report.academicEvaluation;
            if (evaluation) info.append(node('h4', 'Latest approved academic evaluation'), node('p', evaluation.score + ' / 100 · ' + evaluation.rating), node('p', evaluation.feedback), node('p', 'Approved: ' + new Date(evaluation.approvedAt).toLocaleString(), 'wr-small'));
            if (report.status === 'approved' && report.companyComments) info.append(node('p', 'Company comments: ' + report.companyComments));
            info.append(node('p', 'Submitted: ' + (report.submittedAt || 'Not submitted'), 'wr-small')); card.append(info); list.append(card);
        }
        if (!list.children.length) list.append(node('p', 'Your company supervisor has not submitted a weekly report yet.', 'wr-empty'));
        notice.textContent = Object.keys(data.reports || {}).length + ' report status record(s) loaded.';
    }
    function refresh() { load().catch(error => { notice.textContent = error.message; notice.className = 'wr-notice error'; }); }
    document.getElementById('refreshStudent').addEventListener('click', refresh); refresh();
}());
