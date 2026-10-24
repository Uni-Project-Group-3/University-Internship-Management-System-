/* Coordinator/SystemOptions.aspx: every change is written to Firebase through SystemOptionsApi.ashx,
   then the lists are reloaded from Firebase so the page always shows the saved state. */
(function () {
    'use strict';
    const by = id => document.getElementById(id);
    const endpoint = new URL('../SystemOptionsApi.ashx', location.href).href;
    let lists = [], current = '', busy = false;
    const node = (tag, text, className) => { const e = document.createElement(tag); if (text != null) e.textContent = String(text); if (className) e.className = className; return e; };
    const notice = (text, error) => { const box = by('optionsNotice'); box.textContent = text; box.classList.toggle('is-error', !!error); };

    async function api(body) {
        const response = await fetch(endpoint, { method: body ? 'POST' : 'GET', credentials: 'same-origin', cache: 'no-store', headers: body ? { 'Content-Type': 'application/json' } : {}, body: body ? JSON.stringify(body) : undefined });
        const result = await response.json().catch(() => ({ message: 'The server response could not be read.' }));
        if (!response.ok) throw Error(result.message || 'The request failed.');
        return result;
    }
    async function load() {
        lists = (await api()).lists || [];
        if (!lists.some(l => l.key === current)) current = lists[0]?.key || '';
        render();
    }
    async function run(body, done) {
        if (busy) return;
        busy = true; document.body.classList.add('options-busy');
        try {
            const result = await api(body);
            if (body.action === 'saveList' && result.key) current = result.key;
            await load();
            window.UimsOptions?.announce();
            notice(done + ' Saved to Firebase.');
        } catch (e) { notice(e.message, true); }
        finally { busy = false; document.body.classList.remove('options-busy'); }
    }
    function render() {
        const host = by('optionLists'); host.replaceChildren();
        lists.forEach(l => {
            const b = node('button', null, 'options-list-button' + (l.key === current ? ' is-active' : ''));
            b.type = 'button'; b.append(node('strong', l.name), node('small', l.items.length + ' options · ' + l.key));
            b.onclick = () => { current = l.key; render(); };
            host.append(b);
        });
        if (!lists.length) host.append(node('p', 'No lists yet. Create one with "+ New list".', 'options-hint'));
        const list = lists.find(l => l.key === current);
        by('itemsTitle').textContent = list ? list.name : 'Options';
        by('listKeyHint').textContent = list ? 'Key: ' + list.key : '';
        ['renameListButton', 'deleteListButton', 'newOptionLabel', 'addOptionButton'].forEach(id => by(id).disabled = !list);
        const rows = by('optionRows'); rows.replaceChildren();
        (list?.items || []).forEach(item => {
            const tr = node('tr');
            const label = node('input'); label.value = item.label; label.maxLength = 120; label.setAttribute('aria-label', 'Label for ' + item.label);
            const order = node('input'); order.type = 'number'; order.value = item.order; order.className = 'options-order'; order.setAttribute('aria-label', 'Order for ' + item.label);
            const active = node('input'); active.type = 'checkbox'; active.checked = item.active; active.setAttribute('aria-label', 'Active: ' + item.label);
            const save = () => run({ action: 'saveOption', list: list.key, key: item.key, label: label.value, order: Number(order.value) || 0, active: active.checked }, 'Option updated.');
            label.onchange = save; order.onchange = save; active.onchange = save;
            const del = node('button', 'Delete', 'options-danger'); del.type = 'button';
            del.onclick = () => { if (confirm('Delete "' + item.label + '" from ' + list.name + '?')) run({ action: 'deleteOption', list: list.key, key: item.key }, 'Option deleted.'); };
            const cells = [label, node('code', item.key), order, active, del].map(child => { const td = node('td'); td.append(child); return td; });
            cells[4].className = 'text-end';
            tr.append(...cells); rows.append(tr);
        });
        if (!rows.children.length) { const tr = node('tr'), td = node('td', list ? 'No options yet. Add the first one above.' : 'Choose a list.'); td.colSpan = 5; tr.append(td); rows.append(tr); }
    }
    by('newListButton').onclick = () => { const name = prompt('Name of the new list (for example: Cities):'); if (name && name.trim()) run({ action: 'saveList', name: name.trim() }, 'List created.'); };
    by('renameListButton').onclick = () => { const list = lists.find(l => l.key === current); const name = list && prompt('New name for this list:', list.name); if (name && name.trim()) run({ action: 'saveList', list: list.key, name: name.trim() }, 'List renamed.'); };
    by('deleteListButton').onclick = () => { const list = lists.find(l => l.key === current); if (list && confirm('Delete the list "' + list.name + '" and all its options? Dropdowns using it will become empty.')) run({ action: 'deleteList', list: list.key }, 'List deleted.'); };
    const add = () => { const input = by('newOptionLabel'); if (!input.value.trim() || !current) return; run({ action: 'saveOption', list: current, label: input.value.trim(), active: true }, 'Option added.').then(() => { if (!by('optionsNotice').classList.contains('is-error')) input.value = ''; }); };
    by('addOptionButton').onclick = add;
    by('newOptionLabel').addEventListener('keydown', e => { if (e.key === 'Enter') { e.preventDefault(); add(); } });
    document.forms[0].addEventListener('submit', e => { if (e.submitter?.id !== 'btnLogout') e.preventDefault(); });
    // Pick up changes another coordinator made, without losing edits in progress.
    setInterval(() => { if (!busy && !document.hidden && !document.activeElement?.closest('#optionRows,.options-add')) load().catch(() => { }); }, 30000);
    load().then(() => notice('Options loaded from Firebase.')).catch(e => notice(e.message, true));
}());
