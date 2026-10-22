/*
 * This file intentionally provides no business-data store.
 * Persistent UIMS records must be served by authorized Firebase-backed APIs.
 * In particular, do not add localStorage/sessionStorage persistence here.
 */
(function (scope) {
    'use strict';

    var unavailable = 'Firebase connection is not configured. No application data was loaded or saved.';

    function reject() {
        throw new Error(unavailable);
    }

    scope.UimsDemo = {
        departments: [],
        enabled: function () { return false; },
        view: function () { return null; },
        read: function () { return null; },
        current: function () { return null; },
        transaction: reject,
        login: reject,
        export: reject,
        now: function () { return new Date().toISOString(); },
        logout: function () {
            scope.location.href = '../Auth/Login.aspx';
        }
    };

    function showUnavailable() {
        var page = document.querySelector('main') || document.querySelector('.content');
        if (!page || document.getElementById('uimsFirebaseUnavailable')) return;

        var notice = document.createElement('div');
        notice.id = 'uimsFirebaseUnavailable';
        notice.className = 'alert alert-warning';
        notice.setAttribute('role', 'status');
        notice.textContent = unavailable;
        page.insertBefore(notice, page.firstChild);
    }

    if (document.readyState === 'loading') {
        document.addEventListener('DOMContentLoaded', showUnavailable, { once: true });
    } else {
        showUnavailable();
    }
})(window);
