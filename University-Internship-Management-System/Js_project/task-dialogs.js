(function () {
    'use strict';
    let serial = 0;
    function validate(container) {
        let first;
        container.querySelectorAll('input,textarea,select').forEach(field => {
            if (field.disabled || ['hidden', 'button', 'submit', 'file'].includes(field.type) && !field.required) return;
            const valid = field.checkValidity() && (!field.required || !['text', 'textarea', 'search'].includes(field.type) || field.value.trim().length > 0);
            field.classList.toggle('is-invalid', !valid); field.setAttribute('aria-invalid', String(!valid));
            let error = field.nextElementSibling;
            if (!error?.classList.contains('field-error')) { error = document.createElement('span'); error.className = 'field-error'; error.id = 'field-error-' + (++serial); field.after(error); field.setAttribute('aria-describedby', error.id); }
            error.textContent = valid ? '' : field.validity.valueMissing || !field.value.trim() ? 'This field is required.' : field.validationMessage || 'Enter a valid value.';
            if (!valid && !first) first = field;
        });
        if (first) { first.scrollIntoView({ block: 'center' }); first.focus(); }
        return !first;
    }
    function open(title, content, options = {}) {
        const previous = document.activeElement, dialog = document.createElement('dialog');
        const activeBootstrap = document.querySelector('.modal.show');
        const focusTrap = activeBootstrap && window.bootstrap?.Modal.getInstance(activeBootstrap)?._focustrap;
        focusTrap?.deactivate();
        dialog.className = 'workspace-dialog';
        const heading = document.createElement('h2'); heading.id = 'workspace-dialog-' + (++serial); heading.textContent = title;
        dialog.setAttribute('aria-labelledby', heading.id);
        const body = document.createElement('div'); body.className = 'workspace-dialog-body';
        if (typeof content === 'string') { const p = document.createElement('p'); p.textContent = content; body.append(p); } else body.append(content);
        const actions = document.createElement('div'); actions.className = 'workspace-dialog-actions';
        const cancel = document.createElement('button'); cancel.type = 'button'; cancel.className = 'btn secondary btn-light'; cancel.textContent = options.cancel || 'Cancel';
        const confirm = document.createElement('button'); confirm.type = 'button'; confirm.className = 'btn primary btn-primary'; confirm.textContent = options.confirm || 'Confirm';
        if (/discard|archive|delete|reject|hide/i.test(confirm.textContent)) confirm.classList.add('workspace-danger');
        if (typeof content === 'string') { dialog.classList.add('workspace-confirmation'); const icon = document.createElement('div'); icon.className = 'workspace-confirm-icon'; icon.setAttribute('aria-hidden', 'true'); icon.textContent = '?'; dialog.prepend(icon); }
        actions.append(cancel, confirm); dialog.append(heading, body, actions); document.body.append(dialog);
        return new Promise(resolve => {
            let finished = false;
            function close(value) { if (finished) return; finished = true; dialog.close(); dialog.remove(); if (!document.querySelector('dialog[open]')) focusTrap?.activate(); previous?.focus(); resolve(value); }
            cancel.onclick = () => close(false);
            confirm.onclick = () => {
                if (options.validate && options.validate(body) === false) { validate(body); return; }
                if (!validate(body)) return;
                close(true);
            };
            dialog.addEventListener('cancel', e => { e.preventDefault(); close(false); });
            dialog.showModal(); (options.focusConfirm ? confirm : cancel).focus();
        });
    }
    function renderBoardShell(host, options = {}) {
        host.replaceChildren(); host.setAttribute('aria-busy', String(!!options.loading));
        const notice = document.createElement('div'); notice.className = 'board-state-notice'; notice.setAttribute('role', options.error ? 'alert' : 'status');
        const text = document.createElement('span'); text.textContent = options.error ? 'Tasks could not be loaded. The board is unavailable until the connection is restored.' : options.loading ? 'Loading your task board…' : 'Your four task columns are ready. Assigned tasks will appear here.'; notice.append(text);
        if (options.error) { notice.classList.add('board-state-error'); const retry = document.createElement('button'); retry.type = 'button'; retry.className = 'btn btn-outline-primary btn-sm'; retry.textContent = 'Retry connection'; retry.onclick = () => host.dispatchEvent(new CustomEvent('task-board-retry')); notice.append(retry); }
        const grid = document.createElement('div'); grid.className = 'task-board-columns';
        for (const [id, title] of [['todo', 'To Do'], ['inProgress', 'In Progress'], ['review', 'Awaiting Review'], ['done', 'Done']]) {
            const column = document.createElement('section'); column.className = 'task-board-column board-shell-column'; column.dataset.columnId = id;
            const header = document.createElement('header'), heading = document.createElement('h5'), count = document.createElement('span'); heading.textContent = title; count.textContent = options.error || options.loading ? '—' : '0'; count.setAttribute('aria-label', options.error || options.loading ? 'Task count unavailable' : '0 tasks'); header.append(heading, count);
            const zone = document.createElement('div'); zone.className = 'task-board-dropzone';
            const empty = document.createElement('div'); empty.className = 'task-board-column-empty'; empty.textContent = options.error ? 'Tasks unavailable' : options.loading ? 'Loading tasks…' : 'No tasks yet'; zone.append(empty); column.append(header, zone); grid.append(column);
        }
        host.append(notice, grid);
    }
    function watch(refresh, canRefresh = () => true) {
        let version = -1, stopped = false, queued = false, refreshing = false;
        const queue = () => { if (stopped || queued) return; queued = true; setTimeout(flush, 120); };
        async function flush() { if (stopped) return; if (document.hidden || !canRefresh() || refreshing) { setTimeout(flush, 300); return; } queued = false; refreshing = true; try { await refresh(); } catch (_) { /* The owning workspace reports service errors without dropping draft fields. */ } finally { refreshing = false; } }
        const channel = window.BroadcastChannel ? new BroadcastChannel('uims-task-events') : null;
        if (channel) channel.onmessage = queue;
        window.addEventListener('task-data-changed', () => { queue(); channel?.postMessage('changed'); });
        (async () => {
            while (!stopped) {
                if (document.hidden) { await new Promise(resolve => setTimeout(resolve, 1000)); continue; }
                try {
                    const response = await fetch('../TasksApi.ashx?action=changes&version=' + version, { credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(40000) });
                    const result = await response.json(); if (!response.ok || !Number.isFinite(result.version)) throw Error('Change feed unavailable');
                    if (version >= 0 && (result.changed || !result.connected)) queue(); version = result.version;
                } catch (_) { await new Promise(resolve => setTimeout(resolve, 5000)); if (!document.hidden) queue(); }
            }
        })();
        window.addEventListener('pagehide', () => { stopped = true; channel?.close(); }, { once: true });
    }
    window.TaskDialogs = { open, validate, watch, renderBoardShell, confirm: (title, detail, label) => open(title, detail, { confirm: label || 'Confirm' }) };
}());
