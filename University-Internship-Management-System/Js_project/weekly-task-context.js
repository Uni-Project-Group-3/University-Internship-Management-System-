(function () {
    'use strict';
    function node(tag, text, cls) { const el = document.createElement(tag); if (text) el.textContent = text; if (cls) el.className = cls; return el; }
    function render(host, data) {
        host.querySelectorAll('[data-task-report-context]').forEach(el => el.remove());
        const report = data.report, section = node('section', '', 'wr-detail-block'); section.dataset.taskReportContext = 'true';
        section.append(node('h3', 'Task snapshots and private supervisor discussion'));
        const revisionSelect = node('select'); revisionSelect.setAttribute('aria-label', 'Report revision');
        for (const [id, revision] of Object.entries(report.revisions || {})) { const option = node('option', 'Revision ' + revision.revisionNumber); option.value = id; revisionSelect.append(option); }
        revisionSelect.value = report.currentRevisionId;
        const taskSelect = node('select'); taskSelect.setAttribute('aria-label', 'Comment on a task or the report');
        const snapshots = node('div'), history = node('div'), message = node('textarea'); message.maxLength = 3000; message.rows = 3; message.placeholder = 'Private comment to the other supervisor'; message.setAttribute('aria-label', 'Private supervisor discussion');
        function update() {
            taskSelect.replaceChildren(); const all = node('option', 'General report discussion'); all.value = ''; taskSelect.append(all); snapshots.replaceChildren();
            for (const task of report.revisions?.[revisionSelect.value]?.taskSnapshot || []) { const option = node('option', task.title || task.taskId); option.value = task.taskId; taskSelect.append(option); snapshots.append(node('p', (task.title || task.taskId) + ' · ' + task.status + (task.score == null ? '' : ' · Company score ' + task.score + ' / ' + task.maximumScore), 'wr-small')); }
            history.replaceChildren();
            for (const comment of Object.values(report.discussions || {})) if (comment.revisionId === revisionSelect.value) history.append(node('p', comment.authorRole + ' · ' + comment.createdAt + (comment.taskId ? ' · Task ' + comment.taskId : '') + '\n' + comment.message, 'wr-history-item'));
        }
        const send = node('button', 'Send private comment', 'wr-secondary'); send.type = 'button';
        send.onclick = async () => {
            if (!message.value.trim()) { message.focus(); return; } send.disabled = true;
            try { const response = await fetch('../WeeklyReportsApi.ashx?action=discussion', { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify({ reportId: report.reportId, revisionId: revisionSelect.value, taskId: taskSelect.value, message: message.value, requestId: crypto.randomUUID() }) }); const result = await response.json(); if (!response.ok) throw new Error(result.message || 'Comment failed.'); history.append(node('p', message.value, 'wr-history-item')); message.value = ''; }
            catch (error) { const status = node('p', error.message, 'wr-notice error'); section.append(status); } finally { send.disabled = false; }
        };
        revisionSelect.onchange = update; section.append(revisionSelect, snapshots, history, taskSelect, message, send); update(); host.append(section);
        if (document.getElementById('academicDetail') && ['submitted', 'resubmitted'].includes(report.status)) {
            const review = [...host.querySelectorAll('section')].find(el => el.querySelector('h3')?.textContent === 'Review current revision');
            if (review && !document.getElementById('weeklyAcademicScore')) {
                const fields = node('div', '', 'wr-progress-grid');
                const scoreLabel = node('label', 'Independent academic score / 100'), score = node('input'); score.id = 'weeklyAcademicScore'; score.type = 'number'; score.min = '0'; score.max = '100'; score.step = '0.01'; score.required = true; scoreLabel.append(score);
                const ratingLabel = node('label', 'Independent descriptive rating'), rating = node('select'); rating.id = 'weeklyAcademicRating'; rating.required = true;
                for (const text of ['', 'Excellent', 'Very good', 'Good', 'Satisfactory', 'Needs improvement']) { const option = node('option', text || 'Choose a rating'); option.value = text; rating.append(option); } ratingLabel.append(rating); fields.append(scoreLabel, ratingLabel); review.prepend(fields);
                const feedback = review.querySelector('textarea'); feedback.placeholder = 'Academic feedback · visible to the student after approval';
            }
        }
    }
    window.TaskReportContext = { render };
}());
