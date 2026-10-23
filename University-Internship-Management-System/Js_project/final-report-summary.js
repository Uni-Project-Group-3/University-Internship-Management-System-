(function () {
    'use strict';
    const main = document.querySelector('main') || document.querySelector('.content') || document.body;
    const card = document.createElement('section'); card.className = 'final-completion-card'; card.setAttribute('aria-label', 'Final training approval');
    card.style.cssText = 'padding:20px;margin:20px 0;background:white;border:1px solid #dce4ef;border-radius:15px';
    const title = document.createElement('h2'); title.textContent = 'Final training approval';
    const text = document.createElement('p'), notifications = document.createElement('p'), link = document.createElement('a'); link.href = '../FinalReports.aspx'; link.textContent = 'Open final reports & approvals'; card.append(title, text, notifications, link); main.prepend(card);
    let loading = false;
    async function refresh() {
        if (loading || document.hidden) return; loading = true;
        try {
            const response = await fetch('../FinalReportsApi.ashx?action=list', { credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(60000) });
            const data = await response.json(); if (!response.ok) throw Error(data.message || 'Final completion unavailable.');
            const completed = data.items.filter(i => i.completed || i.status === 'completed');
            text.textContent = completed.length ? completed.length + ' training placement(s) finally approved by administration.' : 'Training is not finally completed until administration approves the current academic-approved revision.';
            notifications.textContent = data.role === 'student' && completed.length ? completed.map(i => i.notification?.message || '').filter(Boolean).join(' ') : data.items.filter(i => i.status === 'pendingAdminReview').length + ' report(s) awaiting final admin approval.';
            card.dataset.completed = completed.length ? 'true' : 'false';
        } catch (e) { try { const response = await fetch('../FinalReportsApi.ashx?action=connection', { credentials: 'same-origin', cache: 'no-store', signal: AbortSignal.timeout(30000) }); const connection = await response.json(); text.textContent = response.ok && connection.connected ? 'Firebase is connected. Final-report activation is awaiting verified workflow prerequisites.' : e.message; } catch (_) { text.textContent = e.message; } notifications.textContent = ''; card.dataset.completed = 'false'; }
        finally { loading = false; }
    }
    refresh(); setInterval(refresh, 15000); window.addEventListener('focus', refresh);
}());
