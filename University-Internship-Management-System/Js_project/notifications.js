/* In-app notifications for every signed-in workspace: turns the page's bell into a live alert button with an
   unread badge and a dropdown, backed by NotificationsApi.ashx (Firebase userNotifications/{uid}). */
(function () {
    'use strict';
    const script = document.currentScript;
    if (!document.querySelector('.workspace-canonical-sidebar, .sidebar, .dash-sidebar, .wr-sidebar, .topbar, .dash-topbar, .wr-top, .workspace-navbar') || /\/(Auth|Public)\//i.test(location.pathname)) return;
    const root = new URL('../', script ? script.src : location.href);
    const api = new URL('NotificationsApi.ashx', root).href;
    let items = [], unread = 0, panel = null, bells = [], available = true;

    const style = document.createElement('style');
    style.textContent = `
.uims-bell{position:relative;display:inline-grid;place-items:center;width:42px;height:42px;padding:0;border:1px solid #dfe5ef;border-radius:12px;background:#fff;color:#354c7a;cursor:pointer;transition:background .15s,border-color .15s;flex-shrink:0}
.uims-bell:hover,.uims-bell[aria-expanded="true"]{background:#eef2f9;border-color:#b9c6dc}
.uims-bell svg{width:20px;height:20px;fill:none;stroke:currentColor;stroke-width:1.8;stroke-linecap:round;stroke-linejoin:round}
.uims-bell-badge{position:absolute;top:-6px;right:-6px;min-width:20px;height:20px;padding:0 5px;display:grid;place-items:center;border:2px solid #fff;border-radius:999px;background:#c2412d;color:#fff;font:800 11px/1 Inter,Arial,sans-serif}
.uims-bell-badge[hidden]{display:none}
.uims-bell-floating{position:fixed;top:16px;right:20px;z-index:2000;box-shadow:0 6px 18px rgba(20,32,60,.12)}
.uims-notify-panel{position:fixed;z-index:3000;width:min(380px,calc(100vw - 24px));max-height:min(520px,calc(100dvh - 90px));display:flex;flex-direction:column;border:1px solid #dfe5ef;border-radius:16px;background:#fff;box-shadow:0 24px 60px rgba(20,32,60,.22);overflow:hidden;font-family:Inter,Arial,sans-serif;color:#242e51}
.uims-notify-panel[hidden]{display:none}
.uims-notify-head{display:flex;align-items:center;justify-content:space-between;gap:12px;padding:16px 18px;border-bottom:1px solid #edf0f5}
.uims-notify-head h2{margin:0;font-size:16px;font-weight:800;color:#242e51}
.uims-notify-head button{border:0;background:none;color:#354c7a;font-size:12px;font-weight:700;cursor:pointer;padding:4px}
.uims-notify-head button:disabled{color:#a3adbf;cursor:default}
.uims-notify-list{overflow-y:auto;padding:6px}
.uims-notify-item{display:flex;gap:12px;width:100%;padding:12px;border:0;border-radius:12px;background:none;text-align:left;cursor:pointer;color:inherit;font:inherit}
.uims-notify-item:hover{background:#f3f6fb}
.uims-notify-item.is-unread{background:#f1f5fd}
.uims-notify-icon{flex-shrink:0;width:36px;height:36px;display:grid;place-items:center;border-radius:10px;background:#e8edf6;color:#354c7a;font-weight:800;font-size:13px}
.uims-notify-item.is-unread .uims-notify-icon{background:#354c7a;color:#fff}
.uims-notify-text{flex:1;min-width:0;display:grid;gap:3px}
.uims-notify-text strong{font-size:13.5px;font-weight:700;color:#242e51}
.uims-notify-text span{font-size:12.5px;line-height:1.45;color:#5e6c84;overflow-wrap:anywhere}
.uims-notify-text small{font-size:11px;color:#8592a8}
.uims-notify-dot{flex-shrink:0;width:8px;height:8px;margin-top:6px;border-radius:50%;background:#c2412d}
.uims-notify-empty{padding:36px 20px;text-align:center;color:#6d7f9e;font-size:13px}
`;
    document.head.appendChild(style);

    const icons = { application: 'AP', placement: 'PL', task: 'TK', report: 'WR', assignment: 'AS', message: 'MS' };
    function ago(value) {
        const ms = Date.now() - Date.parse(value); if (!Number.isFinite(ms)) return '';
        const m = Math.round(ms / 60000); if (m < 1) return 'Just now'; if (m < 60) return m + ' min ago';
        const h = Math.round(m / 60); if (h < 24) return h + ' h ago'; const d = Math.round(h / 24); return d < 7 ? d + ' d ago' : new Date(value).toLocaleDateString();
    }
    function node(tag, cls, text) { const el = document.createElement(tag); if (cls) el.className = cls; if (text != null) el.textContent = text; return el; }

    function makeBell() {
        const button = node('button', 'uims-bell'); button.type = 'button'; button.setAttribute('aria-haspopup', 'dialog'); button.setAttribute('aria-expanded', 'false');
        button.innerHTML = '<svg viewBox="0 0 24 24" aria-hidden="true"><path d="M18 8a6 6 0 0 0-12 0c0 7-3 9-3 9h18s-3-2-3-9M10 21h4"/></svg>';
        const badge = node('span', 'uims-bell-badge'); badge.hidden = true; button.append(badge);
        button.addEventListener('click', event => { event.preventDefault(); event.stopPropagation(); toggle(button); });
        return button;
    }
    function placeBells() {
        const found = [...document.querySelectorAll('.workspace-notifications, .icon-btn, .topbar button, .dash-topbar button, header button, header a')]
            .filter(el => el.matches('.workspace-notifications') || el.querySelector('.bi-bell') || /notification|bell/i.test(el.getAttribute('aria-label') || el.title || ''))
            .filter((el, i, all) => !all.some(other => other !== el && other.contains(el)));
        found.forEach(old => { const bell = makeBell(); old.replaceWith(bell); bells.push(bell); });
        if (bells.length) return;
        const bar = document.querySelector('.topbar, .dash-topbar, .wr-top, .workspace-navbar');
        const bell = makeBell();
        if (bar) { const anchor = bar.querySelector('.user-chip, .topbar-actions, .workspace-account, .dropdown'); if (anchor && anchor.parentElement) anchor.parentElement.insertBefore(bell, anchor); else bar.appendChild(bell); }
        else { bell.classList.add('uims-bell-floating'); document.body.appendChild(bell); }
        bells.push(bell);
    }
    function updateBadges() {
        bells.forEach(bell => {
            const badge = bell.querySelector('.uims-bell-badge'); badge.hidden = !unread; badge.textContent = unread > 99 ? '99+' : String(unread);
            bell.setAttribute('aria-label', available ? 'Alerts' + (unread ? ', ' + unread + ' unread' : '') : 'Alerts unavailable');
        });
    }
    function buildPanel() {
        panel = node('div', 'uims-notify-panel'); panel.hidden = true; panel.setAttribute('role', 'dialog'); panel.setAttribute('aria-label', 'Notifications');
        const head = node('div', 'uims-notify-head'), title = node('h2', null, 'Notifications'), all = node('button', null, 'Mark all as read'); all.type = 'button';
        all.addEventListener('click', async () => { await post({}); items.forEach(i => i.read = true); unread = 0; render(); updateBadges(); });
        head.append(title, all); panel.append(head, node('div', 'uims-notify-list')); document.body.appendChild(panel);
        document.addEventListener('click', event => { if (!panel.hidden && !panel.contains(event.target) && !bells.some(b => b.contains(event.target))) close(); });
        document.addEventListener('keydown', event => { if (event.key === 'Escape' && !panel.hidden) { const opener = bells.find(b => b.getAttribute('aria-expanded') === 'true'); close(); opener?.focus(); } });
        window.addEventListener('resize', close);
    }
    function render() {
        const list = panel.querySelector('.uims-notify-list'); list.replaceChildren();
        panel.querySelector('.uims-notify-head button').disabled = !unread;
        if (!available) { list.append(node('p', 'uims-notify-empty', 'Notifications are unavailable right now.')); return; }
        if (!items.length) { list.append(node('p', 'uims-notify-empty', 'You have no notifications yet.')); return; }
        items.forEach(item => {
            const row = node('button', 'uims-notify-item' + (item.read ? '' : ' is-unread')); row.type = 'button';
            const text = node('div', 'uims-notify-text'); text.append(node('strong', null, item.title), node('span', null, item.message), node('small', null, ago(item.createdAt)));
            row.append(node('span', 'uims-notify-icon', icons[item.kind] || 'i'), text); if (!item.read) row.append(node('span', 'uims-notify-dot'));
            row.addEventListener('click', async () => {
                if (!item.read) { item.read = true; unread = Math.max(0, unread - 1); updateBadges(); post({ id: item.id }); }
                if (item.link) location.href = new URL(item.link, root).href; else render();
            });
            list.append(row);
        });
    }
    function toggle(bell) { if (!panel.hidden && bell.getAttribute('aria-expanded') === 'true') { close(); return; } open(bell); }
    function open(bell) {
        const rect = bell.getBoundingClientRect(), width = Math.min(380, innerWidth - 24);
        panel.style.top = Math.min(rect.bottom + 10, innerHeight - 120) + 'px';
        panel.style.left = Math.max(12, Math.min(rect.right - width, innerWidth - width - 12)) + 'px';
        bells.forEach(b => b.setAttribute('aria-expanded', String(b === bell))); panel.hidden = false; render(); load();
    }
    function close() { if (!panel || panel.hidden) return; panel.hidden = true; bells.forEach(b => b.setAttribute('aria-expanded', 'false')); }
    async function load() {
        try {
            const response = await fetch(api, { credentials: 'same-origin', cache: 'no-store' });
            if (!response.ok) throw Error('unavailable');
            const data = await response.json(); items = data.items || []; unread = Number(data.unread) || 0; available = true;
        } catch (_) { available = false; }
        updateBadges(); if (panel && !panel.hidden) render();
    }
    function post(body) { return fetch(api, { method: 'POST', credentials: 'same-origin', headers: { 'Content-Type': 'application/json' }, body: JSON.stringify(body) }).catch(() => null); }

    function start() {
        placeBells(); buildPanel(); load();
        setInterval(() => { if (!document.hidden) load(); }, 30000);
        document.addEventListener('visibilitychange', () => { if (!document.hidden) load(); });
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', start, { once: true }); else start();
}());
