/* Fills every <select data-system-options="listKey"> from the Firebase options lists managed on
   Coordinator/SystemOptions.aspx, and keeps them current: lists reload every 30 seconds, when the tab
   regains focus, and when another tab changes an option.
   Optional attributes: data-placeholder="Select ..." adds an empty first option;
   data-option-value="label" stores the label instead of the generated key. */
(function () {
    'use strict';
    const script = document.currentScript;
    const endpoint = new URL('../SystemOptionsApi.ashx', script ? script.src : location.href).href;
    const cache = new Map(), signals = 'uims.systemOptions.changed';

    async function fetchList(list) {
        const response = await fetch(endpoint + '?list=' + encodeURIComponent(list), { credentials: 'same-origin', cache: 'no-store' });
        const result = await response.json().catch(() => ({}));
        if (!response.ok) throw Error(result.message || 'Options could not be loaded.');
        return result.items || [];
    }
    function fill(select, items) {
        const current = select.value, byLabel = select.dataset.optionValue === 'label';
        const options = [];
        if (select.dataset.placeholder) options.push(new Option(select.dataset.placeholder, ''));
        items.forEach(item => options.push(new Option(item.label, byLabel ? item.label : item.key)));
        // Keep a saved value visible even after its option was removed or deactivated.
        if (current && !options.some(o => o.value === current)) options.push(new Option(current, current));
        select.replaceChildren(...options);
        select.value = current;
        if (select.value !== current && !select.dataset.placeholder && options.length) select.selectedIndex = 0;
    }
    async function refresh(force) {
        const selects = [...document.querySelectorAll('select[data-system-options]')];
        const lists = [...new Set(selects.map(s => s.dataset.systemOptions))];
        await Promise.all(lists.map(async list => {
            try {
                const items = await fetchList(list), signature = JSON.stringify(items);
                if (!force && cache.get(list)?.signature === signature) return;
                cache.set(list, { items, signature });
                selects.filter(s => s.dataset.systemOptions === list).forEach(s => fill(s, items));
                document.dispatchEvent(new CustomEvent('uims:options', { detail: { list, items } }));
            } catch (error) {
                selects.filter(s => s.dataset.systemOptions === list).forEach(s => { s.title = error.message; });
            }
        }));
    }
    window.UimsOptions = {
        refresh: () => refresh(true),
        async get(list) { if (!cache.has(list)) cache.set(list, { items: await fetchList(list), signature: '' }); return cache.get(list).items; },
        // Called by the options manager after a change so other open tabs update at once.
        announce() { try { localStorage.setItem(signals, String(Date.now())); } catch (_) { } }
    };
    const start = () => { refresh(true); setInterval(() => { if (!document.hidden) refresh(false); }, 30000); };
    document.addEventListener('visibilitychange', () => { if (!document.hidden) refresh(false); });
    window.addEventListener('storage', event => { if (event.key === signals) refresh(true); });
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true }); else start();
}());
