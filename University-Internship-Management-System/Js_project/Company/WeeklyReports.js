(function () {
  'use strict';
  const by = id => document.getElementById(id), api = '../WeeklyReportsApi.ashx';
  const list = by('companyReports'), notice = by('wrNotice'), editor = by('weeklySubmit');
  let reports = {}, placements = {}, selectedRevisionReport = '', busy = false, dirty = false, sequence = 0, taskSelectionReady = false;
  const node = (tag, text, cls) => { const el = document.createElement(tag); if (text != null) el.textContent = String(text); if (cls) el.className = cls; if (tag === 'button') el.type = 'button'; return el; };
  const labels = { submitted: 'Pending review', resubmitted: 'Pending review', revisionRequested: 'Changes requested', approved: 'Approved' };
  function say(message, kind) { notice.textContent = message; notice.hidden = !message; notice.className = 'wr-notice' + (kind ? ' ' + kind : ''); }
  async function request(url, options) {
    const response = await fetch(url, Object.assign({ credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(45000) }, options || {}));
    let data; try { data = await response.json(); } catch (_) { throw Error('Unable to read the server response.'); }
    if (!response.ok) throw Error(data.message || 'Unable to load reports.'); return data;
  }
  function badge(status) { return node('span', labels[status] || status || 'Unavailable', 'wr-badge ' + (Object.hasOwn(labels, status) ? status : '')); }
  function date(value) { const parsed = new Date(value); return Number.isNaN(parsed.getTime()) ? '—' : parsed.toLocaleDateString('en-GB', { timeZone: 'Asia/Amman' }); }
  function dialog(title) {
    const d = node('dialog', null, 'wr-company-dialog'), header = node('header'), heading = node('h2', title), close = node('button', '×', 'wr-dialog-close');
    heading.id = 'weekly-dialog-' + crypto.randomUUID(); d.setAttribute('aria-labelledby', heading.id); close.setAttribute('aria-label', 'Close');
    header.append(heading, close); const body = node('div', null, 'wr-dialog-content'); d.append(header, body); document.body.append(d);
    const previous = document.activeElement; close.onclick = () => d.close(); d.oncancel = e => { e.preventDefault(); close.click(); };
    d.addEventListener('close', () => { previous?.focus(); if (d !== compose) d.remove(); }); return { d, body, close, heading };
  }
  const composition = dialog('New weekly report'), compose = composition.d;
  editor.classList.add('wr-compose-card'); composition.body.append(editor);
  const formError = node('p', '', 'wr-notice error'); formError.hidden = true; formError.setAttribute('role', 'alert'); editor.prepend(formError);
  const footer = node('footer'), cancel = node('button', 'Cancel', 'wr-secondary'); footer.append(cancel, by('submitReport')); compose.append(footer);
  const taskSelection = node('details', null, 'wr-task-picker'), taskHeading = node('summary', 'Task activity'), taskChoices = node('div'); taskSelection.append(taskHeading, taskChoices); by('companyComments').parentElement.after(taskSelection);
  function resetCompose() { selectedRevisionReport = ''; dirty = false; by('placementSelect').value = ''; ['weekNumber','weekStart','weekEnd','companyComments'].forEach(id => { by(id).value = ''; by(id).setCustomValidity(''); }); editor.querySelectorAll('.field-error').forEach(el => el.remove()); editor.querySelectorAll('.is-invalid').forEach(el => { el.classList.remove('is-invalid'); el.removeAttribute('aria-invalid'); }); by('submitReport').textContent='Submit report'; taskSelection.hidden=true; taskSelectionReady=false; ++sequence; updateRecipient(); }
  async function closeCompose() { if (busy) return; if (dirty && !await TaskDialogs.confirm('Discard changes?', 'Your unsent report will not be saved.', 'Discard changes')) return; compose.close(); resetCompose(); }
  composition.close.onclick = cancel.onclick = closeCompose;
  editor.addEventListener('input', event => { dirty = true; const field=event.target; if(field.id==='weekEnd')field.setCustomValidity(''); if(field.classList.contains('is-invalid')&&field.checkValidity()){field.classList.remove('is-invalid');field.setAttribute('aria-invalid','false');if(field.nextElementSibling?.classList.contains('field-error'))field.nextElementSibling.textContent='';} }); editor.addEventListener('change', () => { dirty = true; });
  function openCompose() { formError.hidden = true; composition.heading.textContent = selectedRevisionReport ? 'Revise weekly report' : 'New weekly report'; compose.showModal(); by('placementSelect').focus(); }
  document.querySelector('.wr-hero-action').addEventListener('click', e => { e.preventDefault(); openCompose(); });
  const tools = node('div', null, 'wr-history-tools'), searchLabel = node('label', 'Student'), search = node('input'), filterLabel = node('label', 'Status'), filter = node('select');
  search.type = 'search'; search.placeholder = 'Name or student ID'; searchLabel.append(search); filter.append(new Option('All statuses', ''), ...Object.entries(labels).filter(([key]) => key !== 'resubmitted').map(([key, label]) => new Option(label, key))); filterLabel.append(filter); tools.append(searchLabel, filterLabel); list.before(tools); search.oninput = filter.onchange = renderReports;
  function updateRecipient() {
    const p = placements[by('placementSelect').value], active = !!p;
    [['routingStudentId', p?.studentId], ['routingStudentName', p?.studentName], ['routingAcademicId', p?.academicSupervisorId], ['routingAcademicName', p?.academicSupervisorName]].forEach(([id, value]) => { by(id).textContent = value || '—'; });
    by('reportRecipient').textContent = p?.submissionBlocker || ''; by('reportRecipient').hidden = !p?.submissionBlocker;
    by('reportRecipient').classList.toggle('wr-recipient-error', !!p?.submissionBlocker);
    by('submitReport').disabled = busy || !active || p.canSubmit === false || !!p.submissionBlocker;
  }
  async function loadTaskSuggestions() {
    const token = ++sequence; taskSelectionReady = false; taskChoices.replaceChildren();
    const placement = by('placementSelect').value, start = by('weekStart').value, end = by('weekEnd').value;
    taskSelection.hidden = !placement || !start || !end; if (taskSelection.hidden) return;
    taskChoices.append(node('p', 'Loading tasks…', 'wr-small'));
    try {
      const data = await request(api + '?action=taskSuggestions&placementId=' + encodeURIComponent(placement) + '&weekStart=' + encodeURIComponent(start) + '&weekEnd=' + encodeURIComponent(end)); if (token !== sequence) return;
      taskChoices.replaceChildren(); const tasks = data.tasks || []; taskHeading.textContent = 'Task activity (' + tasks.length + ')';
      tasks.forEach(task => { const label = node('label'), input = node('input'); input.type = 'checkbox'; input.checked = true; input.value = task.taskId; label.append(input, node('span', task.title), node('small', task.status || '')); taskChoices.append(label); });
      if (!tasks.length) taskChoices.append(node('p', 'No tasks for this period.', 'wr-small')); taskSelectionReady = true;
    } catch (e) { if (token === sequence) taskChoices.replaceChildren(node('p', e.message, 'wr-inline-error')); }
  }
  ['placementSelect', 'weekStart', 'weekEnd'].forEach(id => by(id).addEventListener('change', loadTaskSuggestions));
  by('placementSelect').addEventListener('change', () => { selectedRevisionReport = ''; by('submitReport').textContent = 'Submit report'; composition.heading.textContent = 'New weekly report'; updateRecipient(); });
  async function loadPlacements() {
    const data = await request(api + '?action=placements'), select = by('placementSelect'), previous = select.value;
    placements = {}; select.replaceChildren(new Option('Select a student', ''));
    (data.placements || []).forEach(p => { placements[p.placementId] = p; select.add(new Option((p.studentName || p.studentId) + ' · ' + p.studentId, p.placementId)); });
    if (!Object.keys(placements).length) select.options[0].text = 'No active trainees'; if (placements[previous]) select.value = previous; updateRecipient();
  }
  function renderReports() {
    list.replaceChildren(); const query = search.value.trim().toLowerCase();
    const entries = Object.entries(reports).filter(([, r]) => (!filter.value || (filter.value === 'submitted' ? ['submitted', 'resubmitted'].includes(r.status) : r.status === filter.value)) && (!query || [r.studentId, r.studentName, placements[r.placementId]?.studentName].join(' ').toLowerCase().includes(query))).sort((a, b) => String(b[1].submittedAt || '').localeCompare(String(a[1].submittedAt || '')));
    if (!entries.length) { list.append(node('p', Object.keys(reports).length ? 'No matching reports.' : 'No weekly reports yet.', 'wr-empty')); return; }
    const grid = node('div', null, 'wr-company-cards');
    entries.forEach(([id, r]) => {
      const name = r.studentName || placements[r.placementId]?.studentName || r.studentId || 'Student', card = node('div', null, 'wr-company-card'), top = node('div', null, 'wr-company-card-top'), week = node('div', null, 'wr-week'), who = node('div', null, 'wr-company-card-who');
      week.append(node('small', 'Week'), node('strong', r.weekNumber || '\u2014'));
      who.append(node('h3', name), node('p', r.studentId || '', 'wr-student-id'), badge(r.status));
      top.append(week, who);
      const facts = node('dl', null, 'wr-company-card-facts');
      for (const [label, value] of [['Period', date(r.weekStart) + ' \u2013 ' + date(r.weekEnd)], ['Submitted', r.submittedAt ? date(r.submittedAt) : 'Not yet']]) { const row = node('div'); row.append(node('dt', label), node('dd', value)); facts.append(row); }
      const actions = node('div', null, 'wr-company-card-actions'), view = node('button', 'View report', 'wr-primary'); view.type = 'button'; view.onclick = () => showDetails(id);
      const download = node('a', 'PDF', 'wr-secondary'); download.href = api + '?action=download&reportId=' + encodeURIComponent(id); download.setAttribute('aria-label', 'Download report for ' + name + ', week ' + r.weekNumber);
      actions.append(view, download); card.append(top, facts, actions); grid.append(card);
    }); list.append(grid);
  }
  async function loadReports() { const data = await request(api + '?action=companyReports'); reports = data.reports || {}; renderReports(); }
  async function showDetails(id) {
    const { d, body } = dialog('Weekly report'); body.append(node('p', 'Loading report…', 'wr-small')); d.showModal();
    try {
      const result = await request(api + '?action=report&reportId=' + encodeURIComponent(id)); if (!d.open) return; const r = result.report || {}; r.reportId = r.reportId || id; body.replaceChildren();
      const overview = node('section', null, 'wr-report-overview'); overview.append(node('h3', (r.studentName || placements[r.placementId]?.studentName || r.studentId) + ' · Week ' + r.weekNumber), badge(r.status), node('p', date(r.weekStart) + ' – ' + date(r.weekEnd), 'wr-small')); body.append(overview);
      const revisions = Object.entries(r.revisions || {}).sort((a, b) => (b[1].revisionNumber || 0) - (a[1].revisionNumber || 0));
      for (const [revisionId, rev] of revisions) {
        const box = node('details', null, 'wr-company-revision'); box.open = revisionId === r.currentRevisionId;
        box.append(node('summary', 'Revision ' + (rev.revisionNumber || '1') + ' · ' + date(rev.submittedAt)));
        if (rev.companyComments) box.append(node('p', rev.companyComments)); const link = node('a', 'Download PDF', 'wr-secondary'); link.href = api + '?action=download&reportId=' + encodeURIComponent(id) + '&revisionId=' + encodeURIComponent(revisionId); box.append(link); body.append(box);
      }
      const reviews = Object.values(result.reviews || {}); if (reviews.length) { const section = node('section', null, 'wr-company-feedback'); section.append(node('h3', 'Academic feedback')); reviews.sort((a,b)=>String(b.reviewedAt||'').localeCompare(String(a.reviewedAt||''))).forEach(review => { const item = node('article'); item.append(node('strong', review.status === 'stale' ? 'Previous review' : labels[review.decision] || review.decision || 'Review'), node('p', review.feedback || 'No comments'), node('small', date(review.reviewedAt))); section.append(item); }); body.append(section); }
      if (r.status === 'revisionRequested') { const revise = node('button', 'Revise report', 'wr-primary'); revise.onclick = async () => { if (compose.open) return; d.close(); by('placementSelect').value = r.placementId || ''; selectedRevisionReport = id; by('weekNumber').value = r.weekNumber || ''; by('weekStart').value = r.weekStart || ''; by('weekEnd').value = r.weekEnd || ''; by('companyComments').value = r.revisions?.[r.currentRevisionId]?.companyComments || ''; by('submitReport').textContent = 'Submit revision'; updateRecipient(); dirty = false; openCompose(); await loadTaskSuggestions(); }; body.append(revise); }
      const discussion = node('details', null, 'wr-company-revision'); discussion.append(node('summary', 'Supervisor discussion')); TaskReportContext.render(discussion, result); body.append(discussion);
    } catch (e) { if (d.open) body.replaceChildren(node('p', e.message, 'wr-notice error')); }
  }
  by('submitReport').addEventListener('click', async () => {
    if (busy) return; formError.hidden = true; const select = by('placementSelect'), p = placements[select.value];
    by('weekEnd').setCustomValidity(by('weekEnd').value < by('weekStart').value ? 'End date must be on or after the start date.' : '');
    if (!TaskDialogs.validate(editor)) return;
    if (!p || p.canSubmit === false || p.submissionBlocker) { formError.textContent = p?.submissionBlocker || 'Choose an eligible trainee.'; formError.hidden = false; return; }
    busy = true; updateRecipient();
    try {
      if (!await TaskDialogs.confirm('Submit weekly report?', (p.studentName || p.studentId) + ' · Week ' + by('weekNumber').value + '. Send to the assigned academic supervisor?', 'Submit report')) return;
      const form = new FormData(); form.append('placementId', select.value); form.append('expectedAcademicSupervisorId', p.academicSupervisorId || '');
      ['weekNumber', 'weekStart', 'weekEnd', 'companyComments'].forEach(id => form.append(id, by(id).value)); form.append('generate', 'true');
      if (taskSelectionReady) form.append('taskIds', JSON.stringify(Array.from(taskChoices.querySelectorAll('input:checked')).map(el => el.value)));
      if (selectedRevisionReport) form.append('reportId', selectedRevisionReport);
      await request(api + '?action=' + (selectedRevisionReport ? 'resubmit' : 'submit'), { method: 'POST', body: form });
      dirty = false; compose.close(); resetCompose(); say('Report submitted.', 'success');
      try { await loadReports(); } catch (_) { say('Report saved. Refresh to reload the table.', 'error'); }
    } catch (e) { formError.textContent = e.message; formError.hidden = false; }
    finally { busy = false; updateRecipient(); }
  });
  by('weekEnd').addEventListener('input', () => by('weekEnd').setCustomValidity(''));
  by('refreshCompany').onclick = refresh;
  async function refresh() { const button = by('refreshCompany'); if (button.disabled) return; button.disabled = true; say('Loading reports…'); try { await Promise.all([loadPlacements(), loadReports()]); renderReports(); say(''); } catch (e) { say(e.message, 'error'); } finally { button.disabled = false; } }
  taskSelection.hidden = true; refresh();
}());
