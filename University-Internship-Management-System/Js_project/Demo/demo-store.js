/*
 * This file intentionally provides no business-data store.
 * Persistent UIMS records must be served by authorized Firebase-backed APIs.
 * In particular, do not add localStorage/sessionStorage persistence here.
 */
(function (scope) {
    'use strict';

    var unavailable = 'Dashboard business data is not connected to the live database yet. No dashboard data was loaded or saved.';

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

})(window);
