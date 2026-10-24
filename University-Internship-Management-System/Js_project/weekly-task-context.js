(function () {
    'use strict';
    function node(tag, text, cls) { const el = document.createElement(tag); if (text) el.textContent = text; if (cls) el.className = cls; return el; }
    function render(host, data) {
        host.querySelectorAll('[data-task-report-context]').forEach(el => el.remove());
        const report = data.report, section = node('section', '', 'wr-detail-block'); section.dataset.taskReportContext = 'true';
        section.classList.add('wr-conversation');
        section.append(node('h3', 'Company & academic supervisor discussion'), node('p', 'Private replies between supervisors. Students cannot see this conversation.', 'wr-small'));
        const revisionSelect = node('select'); revisionSelect.setAttribute('aria-label', 'Report revision');
        for (const [id, revision] of Object.entries(report.revisions || {})) { const option = node('option', 'Revision ' + revision.revisionNumber); option.value = id; revisionSelect.append(option); }
        revisionSelect.value = report.currentRevisionId;
        const taskSelect = node('select'); taskSelect.setAttribute('aria-label', 'Comment on a task or the report');
        const snapshots = node('div'), history = node('div'), message = node('textarea'); message.maxLength = 3000; message.rows = 3; message.placeholder = 'Private comment to the other supervisor'; message.setAttribute('aria-label', 'Private supervisor discussion');
        function update() {
            taskSelect.replaceChildren(); const all = node('option', 'General report discussion'); all.value = ''; taskSelect.append(all); snapshots.replaceChildren();
            for (const task of report.revisions?.[revisionSelect.value]?.taskSnapshot || []) { const option = node('option', task.title || task.taskId); option.value = task.taskId; taskSelect.append(option); snapshots.append(node('p', (task.title || task.taskId) + ' · ' + task.status + (task.score == null ? '' : ' · Company score ' + task.score + ' / ' + task.maximumScore), 'wr-small')); }
            history.replaceChildren();
            for (const comment of Object.values(report.discussions || {}).sort((a,b)=>String(a.createdAt).localeCompare(String(b.createdAt)))) if (comment.revisionId === revisionSelect.value) {
                const company = comment.authorRole === 'companySupervisor', box = node('article', '', 'wr-message ' + (company ? 'from-company' : 'from-academic'));
                box.append(node('strong', company ? 'Company supervisor' : 'Academic supervisor'), node('small', new Date(comment.createdAt).toLocaleString('en-GB', {timeZone:'Asia/Amman'}), 'wr-small'), node('p', comment.message)); history.append(box);
            }
        }
        const send = node('button', 'Send reply', 'wr-primary'); send.type = 'button';
        let lastContent = '', retryId = '';
        send.onclick = async () => {
            if (!message.value.trim()) { message.focus(); return; } send.disabled = true;
            try {
                const content = JSON.stringify([revisionSelect.value, taskSelect.value, message.value]);
                if (lastContent !== content) { lastContent = content; retryId = crypto.randomUUID(); }
                const response = await fetch('../WeeklyReportsApi.ashx?action=discussion', { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ reportId: report.reportId, revisionId: revisionSelect.value, taskId: taskSelect.value, message: message.value, requestId: retryId }) });
                const result = await response.json(); if (!response.ok) throw new Error(result.message || 'Comment failed.');
                const reload = await fetch('../WeeklyReportsApi.ashx?action=report&reportId=' + encodeURIComponent(report.reportId), {credentials:'same-origin',cache:'no-store'});
                const persisted = await reload.json(); if (!reload.ok) throw Error('Reply saved. Reopen the report to refresh it.');
                report.discussions = persisted.report.discussions; update(); message.value = ''; lastContent = '';
            }
            catch (error) { const status = node('p', error.message, 'wr-notice error'); section.append(status); } finally { send.disabled = false; }
        };
        revisionSelect.onchange = update; section.append(revisionSelect, snapshots, history, taskSelect, message, send); update(); host.append(section);

    }
    window.TaskReportContext = { render };
}());
