/* Interactions only. All evaluation permissions and mutations are server-side. */
(function () { 'use strict';
 document.addEventListener('DOMContentLoaded', function () {
  var field = document.getElementById('activePanel');
  if (field && field.value && window.bootstrap) {
   var modal = document.getElementById(field.value);
   if (modal) bootstrap.Modal.getOrCreateInstance(modal).show();
  }
  var choice = document.getElementById('reviewChoice');
  if (choice) {
   var reason = document.getElementById('RevisionReason'), section = document.getElementById('revisionReasonSection');
   var confirm = document.getElementById('ConfirmAction');
   function selectReview(value, reset) {
    var revision = value === 'revision'; choice.value = revision ? 'revision' : 'approve';
    document.querySelectorAll('input[name="academicReviewChoice"]').forEach(function (radio) { radio.checked = radio.value === choice.value; });
    if (section) section.hidden = !revision;
    if (reason) { reason.required = revision; reason.setAttribute('aria-required', String(revision)); }
    document.getElementById('ApproveReview').hidden = revision;
    document.getElementById('RequestRevision').hidden = !revision;
    var label = document.querySelector('label[for="ConfirmAction"]');
    if (label) label.textContent = revision ? 'I confirm Request Revision of this Company evaluation for the reason provided.' : 'I confirm Academic Approval of this submitted Company evaluation.';
    if (reset && confirm) confirm.checked = false;
    if (reset && revision) reason?.focus();
   }
   document.querySelectorAll('input[name="academicReviewChoice"]').forEach(function (radio) { radio.addEventListener('change', function () { selectReview(radio.value, true); }); });
   selectReview(choice.value, false);
   document.getElementById('RequestRevision')?.addEventListener('click', function (event) {
    if (!reason.value.trim()) { event.preventDefault(); reason.setCustomValidity('A clear revision reason is required.'); reason.reportValidity(); reason.focus(); }
   });
   reason?.addEventListener('input', function () { reason.setCustomValidity(''); });
  }
  var search = document.getElementById('traineeSearch'), status = document.getElementById('evaluationFilter');
  var tableWrap = document.querySelector('.evaluation-table-wrap');
  var filterStatus = document.createElement('p'); filterStatus.id = 'evaluationFilterStatus'; filterStatus.className = 'workspace-hint'; filterStatus.setAttribute('role', 'status'); filterStatus.setAttribute('aria-live', 'polite');
  var noMatches = document.createElement('p'); noMatches.id = 'evaluationNoMatches'; noMatches.className = 'workspace-hint'; noMatches.textContent = 'No authorized trainees match your search and filters.'; noMatches.hidden = true;
  if (tableWrap) tableWrap.after(filterStatus, noMatches);
  function filter() {
   var rows = [...document.querySelectorAll('[data-evaluation-row]')], shown = 0;
   rows.forEach(function (row) {
    var visible = row.textContent.toLowerCase().includes((search?.value || '').trim().toLowerCase()) && (!status?.value || row.dataset.status === status.value);
    row.hidden = !visible; if (visible) shown++;
   });
   filterStatus.textContent = rows.length ? shown + ' of ' + rows.length + ' authorized trainees shown' : '';
   noMatches.hidden = rows.length === 0 || shown > 0;
  }
  search?.addEventListener('input', filter); status?.addEventListener('change', filter);
  search?.addEventListener('keydown', function (event) { if (event.key === 'Enter') event.preventDefault(); });
  document.getElementById('clearEvaluationFilters')?.addEventListener('click', function () { search.value = ''; status.value = ''; filter(); search.focus(); });
  filter();
  var main = document.querySelector('main'), loading = document.createElement('p'); loading.id = 'evaluationLoading'; loading.className = 'workspace-hint'; loading.setAttribute('role', 'status'); loading.hidden = true;
  main?.prepend(loading);
  document.getElementById('form1')?.addEventListener('submit', function () { main?.setAttribute('aria-busy', 'true'); loading.textContent = 'Processing your request. Please wait for server confirmation.'; loading.hidden = false; });
  window.addEventListener('pageshow', function () { main?.removeAttribute('aria-busy'); loading.hidden = true; });
  // Academic navigation retains its existing shared script. Company retains its original behavior.
  if (!document.getElementById('dashboardSidebar')) {
   var toggle = document.querySelector('.menu-btn'), close = document.querySelector('.sidebar-close');
   var sidebar = document.getElementById('sidebar'), main = document.querySelector('.main');
   var mobile = window.matchMedia('(max-width:991.98px)');
   function setSidebar(open) { var active = mobile.matches && open; document.body.classList.toggle('sidebar-open', active); toggle?.setAttribute('aria-expanded', String(active)); if (sidebar) sidebar.inert = mobile.matches && !active; if (main) main.inert = active; }
   toggle?.addEventListener('click', function () { setSidebar(!document.body.classList.contains('sidebar-open')); });
   close?.addEventListener('click', function () { setSidebar(false); toggle?.focus(); });
   document.querySelector('.sidebar-backdrop')?.addEventListener('click', function () { setSidebar(false); });
   document.addEventListener('keydown', function (event) { if (event.key === 'Escape' && document.body.classList.contains('sidebar-open')) { setSidebar(false); toggle?.focus(); } });
   mobile.addEventListener('change', function () { setSidebar(false); }); setSidebar(false);
  }
 });
})();
