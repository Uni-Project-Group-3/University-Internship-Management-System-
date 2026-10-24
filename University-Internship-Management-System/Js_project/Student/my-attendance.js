"use strict";

let attendance = null;
let loading = false;
let saving = false;
let dataVersion = 0;
let qrDeadline = 0;
let qrAction = "";
let qrTimer;
let serverSessionVerified = false;

const sidebar = document.getElementById("sidebar");
const menuButton = document.querySelector(".menu-btn");
const mainArea = document.querySelector(".main");
// The canonical workspace drawer is owned by the shared navigation script.
const canonicalSidebar = !!sidebar && sidebar.classList.contains("workspace-canonical-sidebar");
const mobileScreen = window.matchMedia("(max-width: 991.98px)");

function setSidebarOpen(open) {
    if (canonicalSidebar || !sidebar || !menuButton || !mainArea) return;
    const visible = open && mobileScreen.matches;
    document.body.classList.toggle("sidebar-open", visible);
    menuButton.setAttribute("aria-expanded", String(visible));
    sidebar.inert = mobileScreen.matches && !visible;
    mainArea.inert = visible;
    if (visible) sidebar.querySelector("button")?.focus();
}
function toggleSidebar() { setSidebarOpen(!document.body.classList.contains("sidebar-open")); }
document.querySelector(".legacy-sidebar-close")?.addEventListener("click", function () {
    setSidebarOpen(false);
    menuButton?.focus();
});
mobileScreen.addEventListener("change", function () { setSidebarOpen(false); });
sidebar?.addEventListener("keydown", function (event) {
    if (canonicalSidebar) return;
    if (!mobileScreen.matches || !document.body.classList.contains("sidebar-open")) return;
    if (event.key === "Escape") { setSidebarOpen(false); menuButton.focus(); }
    if (event.key === "Tab") {
        const links = sidebar.querySelectorAll("button, a[href]");
        const first = links[0], last = links[links.length - 1];
        if (event.shiftKey && document.activeElement === first) { event.preventDefault(); last.focus(); }
        else if (!event.shiftKey && document.activeElement === last) { event.preventDefault(); first.focus(); }
    }
});
setSidebarOpen(false);
document.getElementById("attendanceForm").addEventListener("submit", function (event) { event.preventDefault(); });

let noticeTimer;
function showNotice(message, error) {
    clearTimeout(noticeTimer);
    const notice = document.getElementById("pageNotice");
    notice.textContent = message;
    notice.classList.toggle("attendance-error", !!error);
    notice.hidden = false;
    notice.setAttribute("role", error ? "alert" : "status");
    // \u0627\u0644\u0646\u062c\u0627\u062d \u064a\u0638\u0647\u0631 \u0645\u0624\u0642\u062a\u064b\u0627\u060c \u0623\u0645\u0627 \u0627\u0644\u062e\u0637\u0623 \u0641\u064a\u0628\u0642\u0649 \u062d\u062a\u0649 \u0627\u0644\u0645\u0639\u0627\u0644\u062c\u0629 \u0623\u0648 \u0627\u0644\u062a\u062d\u062f\u064a\u062b.
    if (!error) noticeTimer = setTimeout(function () { notice.hidden = true; }, 6000);
}
function time(value) {
    return value ? new Date(Number(value)).toLocaleTimeString("en-GB", {
        timeZone: "Asia/Amman", hour: "2-digit", minute: "2-digit"
    }) : "\u2014";
}
function label(value) {
    return String(value || "Not recorded").replace(/([A-Z])/g, " $1").replace(/^./, function (c) { return c.toUpperCase(); });
}
function addCell(row, value) {
    const cell = document.createElement("td");
    cell.textContent = value;
    row.append(cell);
    return cell;
}

// \u0627\u0644\u0637\u0644\u0628\u0627\u062a \u062a\u0630\u0647\u0628 \u0625\u0644\u0649 C# \u0628\u0646\u0641\u0633 \u0627\u0644\u0635\u0641\u062d\u0629\u061b \u0627\u0644\u062a\u0648\u0643\u0646 \u0627\u0644\u0633\u0631\u064a \u0627\u0644\u062e\u0627\u0635 \u0628\u0642\u0627\u0639\u062f\u0629 \u0627\u0644\u0628\u064a\u0627\u0646\u0627\u062a \u0644\u0627 \u064a\u0635\u0644 \u0644\u0644\u0645\u062a\u0635\u0641\u062d.
async function api(action, values, onProgress) {
    if (action === "load") serverSessionVerified = false;
    const url = new URL(window.location.href);
    url.search = "?attendanceApi=1";
    const body = new URLSearchParams(Object.assign({
        action: action,
        csrf: JSON.parse(document.getElementById("attendanceBootstrap").textContent).csrf
    }, values || {}));
    if (action === "load" && onProgress) body.set("stream", "1");
    const localDevelopment = JSON.parse(document.getElementById("attendanceBootstrap").textContent).localDemo;
    let idToken = null;
    let useServerSession = false;
    if (!localDevelopment) {
        try {
            idToken = await window.UimsFirebase.getIdToken();
            if (typeof idToken !== "string" || !idToken.trim())
                throw new Error("The Firebase authentication session is incomplete. Sign in again.");
        } catch (error) {
            const missingSession = error && error.message === "Sign in with your Firebase account to load attendance.";
            // \u0646\u062c\u0631\u0628 \u0627\u0644\u0642\u0631\u0627\u0621\u0629 \u0641\u0642\u0637 \u0623\u0648\u0644\u0627\u064b\u061b C# \u064a\u0648\u062b\u0642 \u062a\u0648\u0643\u0646 Session \u0648\u064a\u0637\u0627\u0628\u0642 UID\u060c \u0648\u0644\u0627 \u064a\u0643\u062a\u0641\u064a \u0628\u0627\u0644\u0640 Cookie.
            if (!missingSession || (action !== "load" && !serverSessionVerified)) {
                serverSessionVerified = false;
                throw error;
            }
            useServerSession = true;
        }
    }
    const controller = new AbortController();
    const timeout = setTimeout(function () { controller.abort(); }, 130000);
    try {
        let response;
        try {
            response = await fetch(url.href, {
                method: "POST", credentials: "same-origin", signal: controller.signal,
                headers: { "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8", ...(idToken ? { "Authorization": "Bearer " + idToken } : {}) }, body: body
            });
        } catch (error) {
            throw new Error(error.name === "AbortError"
                ? "Attendance response timed out. Refresh to check the saved state before retrying."
                : "Cannot reach the attendance server. Keep IIS Express running, reopen this page from its current localhost address, then refresh to check the saved state before retrying.");
        }
        let result;
        try {
            if ((response.headers.get("Content-Type") || "").includes("application/x-ndjson") && response.body) {
                // \u0631\u0633\u0627\u0626\u0644 \u0645\u062a\u0639\u062f\u062f\u0629 \u062f\u0627\u062e\u0644 \u0646\u0641\u0633 \u0627\u0644\u0631\u062f\u061b \u0644\u0627 \u0646\u0646\u0634\u0626 \u0637\u0644\u0628 \u062a\u062d\u0645\u064a\u0644 \u062c\u062f\u064a\u062f\u064b\u0627 \u0644\u0643\u0644 \u0642\u0633\u0645.
                const reader = response.body.getReader();
                const decoder = new TextDecoder();
                let pending = "";
                function accept(line) {
                    if (!line.trim()) return;
                    const message = JSON.parse(line);
                    if (message.type) { if (onProgress) onProgress(message.type, message.data); }
                    else result = message;
                }
                try {
                    while (true) {
                        const chunk = await reader.read();
                        pending += decoder.decode(chunk.value || new Uint8Array(), { stream: !chunk.done });
                        let end;
                        while ((end = pending.indexOf("\n")) >= 0) {
                            accept(pending.slice(0, end)); pending = pending.slice(end + 1);
                        }
                        if (chunk.done) break;
                    }
                    accept(pending);
                } finally { reader.releaseLock(); }
            } else result = await response.json();
        } catch (error) {
            throw new Error(error.name === "AbortError" ? "Attendance response timed out. Refresh to check the saved state before retrying."
                : "The server did not return attendance JSON. Check the server build and page configuration.");
        }
        if (!response.ok || !result || !result.success) throw new Error(result && result.message || "Attendance request failed.");
        // \u0647\u0630\u0627 \u0634\u0631\u0637 \u0648\u0627\u062c\u0647\u0629 \u0641\u0642\u0637\u061b \u0627\u0644\u0633\u064a\u0631\u0641\u0631 \u064a\u0639\u064a\u062f \u0641\u062d\u0635 \u0627\u0644\u0647\u0648\u064a\u0629 \u0648\u0627\u0644\u0635\u0644\u0627\u062d\u064a\u0627\u062a \u0644\u0643\u0644 \u0639\u0645\u0644\u064a\u0629 \u0644\u0627\u062d\u0642\u0629.
        if (useServerSession && action === "load") serverSessionVerified = true;
        return result.data;
    } catch (error) {
        serverSessionVerified = false;
        throw error;
    } finally { clearTimeout(timeout); }
}
function connected(data, name) {
    const notice = document.getElementById("pageNotice");
    if (notice.classList.contains("attendance-error")) {
        notice.hidden = true;
        notice.classList.remove("attendance-error");
    }
    document.getElementById("connectionStatus").textContent = "Firebase connected";
    document.getElementById("navName").textContent = name || "Attendance";
    document.getElementById("modeMessage").textContent = data.localDemo
        ? "Local test accounts \u00b7 real Firebase"
        : "Signed in";

}
function disconnected(error) {
    document.getElementById("connectionStatus").textContent = "Connection / setup required";
    showNotice(error.message || "Unable to load attendance.", true);
}
let dialogOpener;
function openDialog(id, button) {
    dialogOpener = button;
    document.getElementById(id).showModal();
}
document.querySelectorAll("[data-close-dialog]").forEach(function (button) {
    button.addEventListener("click", function () { document.getElementById(button.dataset.closeDialog).close(); });
});
document.querySelectorAll("dialog").forEach(function (dialog) {
    dialog.addEventListener("close", function () {
        if (dialogOpener && !dialogOpener.disabled) dialogOpener.focus();
    });
});
document.getElementById("notificationsButton").addEventListener("click", function () {
    showNotice("Use Refresh attendance to check the latest saved data.");
});


const historyEmptyText = document.getElementById("historyEmpty").textContent;
function renderStudentProgress(type, data) {
    if (type === "identity") {
        if (attendance && attendance.identityId !== data.identityId) {
            attendance = null;
            document.getElementById("historyRows").replaceChildren();
            document.getElementById("historyEmpty").textContent = "Loading attendance\u2026";
            document.getElementById("historyEmpty").hidden = false;
            ["approvedHours", "remainingHours", "progressText", "todayStatus", "companyName", "supervisorName"].forEach(function (id) {
                document.getElementById(id).textContent = "Loading\u2026";
            });
            document.getElementById("actualCheckIn").textContent = "\u2014";
            document.getElementById("actualCheckOut").textContent = "\u2014";
        }
        connected(data, data.student.name);
        document.getElementById("studentName").textContent = data.student.name + (data.student.studentNumber ? " \u00b7 " + data.student.studentNumber : "");
        document.getElementById("connectionStatus").textContent = "Loading attendance\u2026";
    } else if (type === "profile") {
        document.getElementById("companyName").textContent = data.company.name;
        document.getElementById("supervisorName").textContent = data.supervisor.name;
        document.getElementById("requiredHours").textContent = data.assignment.requiredHours + " h";
    }
}

function renderStudent(data) {
    document.getElementById("historyEmpty").textContent = historyEmptyText;
    ["historyRows", "todayStatus", "actualCheckIn", "actualCheckOut"].forEach(function (id) {
        document.getElementById(id).removeAttribute("aria-busy");
    });
    attendance = data;
    const resetButton = document.getElementById("resetTodayButton");
    document.getElementById("resetTodayTools").hidden = !data.canResetToday;
    resetButton.disabled = saving || !data.canResetToday;
    connected(data, data.student.name);
    const assigned = data.hasAssignment;
    const assignment = data.assignment || {};
    const record = data.record || {};
    const schedule = data.schedule || {};
    const request = data.earlyRequest || {};
    const required = Number(assignment.requiredHours || 0);
    const approved = Number(data.approvedMinutes || 0) / 60;
    document.getElementById("studentName").textContent = data.student.name + (data.student.studentNumber ? " \u00b7 " + data.student.studentNumber : "");
    document.getElementById("companyName").textContent = assigned ? data.company.name : "No active assignment";
    document.getElementById("supervisorName").textContent = assigned ? data.supervisor.name : "Not assigned";
    document.getElementById("requiredHours").textContent = assigned ? required + " h" : "\u2014";
    document.getElementById("approvedHours").textContent = assigned ? approved.toFixed(2) + " h" : "\u2014";
    document.getElementById("remainingHours").textContent = assigned ? Math.max(0, required - approved).toFixed(2) + " h" : "\u2014";
    document.getElementById("progressText").textContent = assigned ? approved.toFixed(2) + " / " + required + " hours" : "No active training requirement";
    const progress = document.getElementById("trainingProgress");
    progress.max = required || 1;
    progress.value = Math.min(required, approved);
    document.getElementById("scheduleDate").textContent = data.today + " \u00b7 Asia/Amman" +
        (assignment.startDate && assignment.endDate ? " \u00b7 Training: " + assignment.startDate + " \u2013 " + assignment.endDate : "");
    document.getElementById("scheduledStart").textContent = time(schedule.scheduledStartAt);
    document.getElementById("scheduledEnd").textContent = time(schedule.scheduledEndAt);
    document.getElementById("actualCheckIn").textContent = time(record.checkInAt);
    document.getElementById("actualCheckOut").textContent = time(record.checkOutAt);
    document.getElementById("todayStatus").textContent = !assigned ? "Unassigned" : !schedule.assignmentId ? "No schedule"
        : !schedule.isTrainingDay ? "Day off" : label(record.status || "Not checked in");
    document.getElementById("checkInButton").hidden = !!record.checkInAt;
    document.getElementById("checkOutButton").hidden = !record.checkInAt || !!record.checkOutAt;
    document.getElementById("earlyButton").hidden = !data.canRequestEarly;
    document.getElementById("checkInButton").disabled = saving || !data.canCheckIn;
    document.getElementById("checkOutButton").disabled = saving || !data.canCheckOut;
    document.getElementById("earlyButton").disabled = saving || !data.canRequestEarly;
    document.getElementById("earlyStatus").textContent = request.status ? "Early departure: " + label(request.status) + "." : "";
    document.getElementById("scheduleHelp").textContent = !assigned ? "Your account is verified. No approved training assignment is linked yet. Attendance actions are unavailable."
        : !data.actionsEnabled ? "Attendance actions are disabled in Firebase settings. Reading history remains available."
        : !schedule.assignmentId ? "No schedule exists for today. Ask your supervisor to configure it."
        : !schedule.isTrainingDay ? "Scheduled day off. No absence is counted."
        : record.checkOutAt ? (record.hoursApprovalStatus === "approved"
            ? "Attendance complete. Your approved time is included in Approved hours and deducted from Remaining hours."
            : record.hoursApprovalStatus === "pending" ? (Number(record.workedMinutes) > 0
                ? "Attendance complete: " + Number(record.workedMinutes) + " minute(s) awaiting your supervisor’s hours approval. Refresh attendance after approval to update your totals."
                : "Attendance complete, but less than one full minute was recorded. No whole training minute can be approved. Wait at least one minute between check-in and check-out when testing.")
            : "Attendance complete. Hours status: " + label(record.hoursApprovalStatus) + ".")
        : record.checkInAt ? "Your supervisor can issue your check-out QR now. Actual worked time counts after hours approval."
        : "Flexible attendance today. Generate your check-in QR and ask your supervisor to scan it.";
    if (document.getElementById("qrDialog").open && ((qrAction === "checkIn" && record.checkInAt) || (qrAction === "checkOut" && record.checkOutAt))) {
        document.getElementById("qrDialog").close();
        showNotice("Attendance updated.");
    }
    renderHistory();
}

function renderHistory() {
    const list = document.getElementById("historyRows");
    list.replaceChildren();
    const filter = document.getElementById("historyFilter").value;
    const query = document.getElementById("attendanceSearch").value.trim().toLowerCase();
    const rows = (attendance ? attendance.history : []).filter(function (row) {
        const match = filter === "all" || row.hoursApprovalStatus === filter || filter === "absent" && row.status === "absent";
        return match && (row.attendanceDate + " " + label(row.status)).toLowerCase().includes(query);
    });
    rows.forEach(function (record) {
        const row = document.createElement("tr");
        const status = record.status === "checkedIn" && !record.checkOutAt && Number(record.scheduledEndAt) < Date.now() ? "missingCheckOut" : record.status;
        [record.attendanceDate, time(record.checkInAt), time(record.checkOutAt),
            record.checkOutAt ? (Number(record.workedMinutes) / 60).toFixed(2) + " h" : "\u2014",
            label(status) + (record.isLate ? " \u00b7 Late" : ""), label(record.hoursApprovalStatus)].forEach(function (value) { addCell(row, value); });
        list.append(row);
    });
    document.getElementById("historyEmpty").hidden = rows.length > 0;
}

async function loadAttendance() {
    if (loading || saving) return;
    loading = true;
    document.getElementById("connectionStatus").textContent = "Refreshing\u2026";
    const version = dataVersion;
    ["checkInButton", "checkOutButton", "earlyButton", "resetTodayButton"].forEach(function (id) { document.getElementById(id).disabled = true; });
    ["historyRows", "todayStatus", "actualCheckIn", "actualCheckOut"].forEach(function (id) {
        document.getElementById(id).setAttribute("aria-busy", "true");
    });
    document.getElementById("todayStatus").textContent = "Loading\u2026";
    if (!attendance) {
        document.getElementById("historyEmpty").textContent = "Loading attendance\u2026";
        document.getElementById("historyEmpty").hidden = false;
        document.getElementById("todayStatus").textContent = "Loading\u2026";
        ["approvedHours", "remainingHours", "progressText"].forEach(function (id) { document.getElementById(id).textContent = "Loading\u2026"; });
    }
    try {
        const data = await api("load", null, function (type, partial) {
            if (version === dataVersion) renderStudentProgress(type, partial);
        });
        if (version === dataVersion) renderStudent(data);
    }
    catch (error) {
        if (version !== dataVersion) return;
        disconnected(error);
        ["historyRows", "todayStatus", "actualCheckIn", "actualCheckOut"].forEach(function (id) {
            document.getElementById(id).removeAttribute("aria-busy");
        });
        document.getElementById("todayStatus").textContent = "Unavailable";
        if (!attendance) {
            document.getElementById("historyEmpty").textContent = "Attendance could not be loaded.";
            ["approvedHours", "remainingHours", "progressText"].forEach(function (id) { document.getElementById(id).textContent = "Unavailable"; });
        }
        ["checkInButton", "checkOutButton", "earlyButton", "resetTodayButton"].forEach(function (id) { document.getElementById(id).disabled = true; });
    } finally { loading = false; }
}

async function showQr(action, button) {
    if (loading || saving || !attendance || !(action === "checkIn" ? attendance.canCheckIn : attendance.canCheckOut)) return;
    saving = true;
    button.disabled = true;
    clearInterval(qrTimer);
    const dialog = document.getElementById("qrDialog");
    document.getElementById("qrCode").replaceChildren();
    document.getElementById("qrTitle").textContent = action === "checkIn" ? "Check-in QR" : "Check-out QR";
    document.getElementById("qrCountdown").textContent = "Generating your secure QR\u2026";
    document.getElementById("qrDescription").textContent = "Your assigned supervisor must scan this code to record attendance. Each code is valid for 120 seconds.";
    if (!dialog.open) openDialog("qrDialog", button);
    try {
        if (typeof qrcodegen === "undefined" || typeof qrcodegen.QrCode?.encodeText !== "function") throw new Error("The local QR generation library could not be loaded.");
        const requestedAt = performance.now();
        const qr = await api("qr", { attendanceAction: action });
        if (!dialog.open) return;
        if (typeof qr.token !== "string" || !qr.token.length || !Number.isFinite(Number(qr.expiresAt)) || !Number.isFinite(Number(qr.serverNow))) throw new Error("The server returned an invalid QR response. Refresh attendance and retry.");
        // \u0646\u062e\u0635\u0645 \u0645\u062f\u0629 \u0627\u0644\u0637\u0644\u0628 \u0627\u062d\u062a\u064a\u0627\u0637\u064b\u0627 \u062d\u062a\u0649 \u0644\u0627 \u0646\u0639\u0631\u0636 \u0627\u0644\u0631\u0645\u0632 \u0645\u062f\u0629 \u0623\u0637\u0648\u0644 \u0645\u0646 \u0635\u0644\u0627\u062d\u064a\u062a\u0647 \u0639\u0644\u0649 \u0627\u0644\u0633\u064a\u0631\u0641\u0631.
        qrDeadline = requestedAt + qr.expiresAt - qr.serverNow;
        if (performance.now() >= qrDeadline) throw new Error("QR expired while loading. Generate a new QR.");
        const container = document.getElementById("qrCode");
        container.replaceChildren();
        const matrix = qrcodegen.QrCode.encodeText(qr.token, qrcodegen.QrCode.Ecc.MEDIUM);
        const size = matrix.size + 8;
        const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
        svg.setAttribute("viewBox", "0 0 " + size + " " + size);
        svg.setAttribute("width", "480");
        svg.setAttribute("height", "480");
        svg.setAttribute("role", "img");
        svg.setAttribute("aria-label", "Attendance QR");
        svg.style.cssText = "display:block;width:100%;max-width:480px;height:auto;background:#fff;shape-rendering:crispEdges";
        const background = document.createElementNS(svg.namespaceURI, "rect");
        background.setAttribute("width", String(size)); background.setAttribute("height", String(size)); background.setAttribute("fill", "#fff");
        const path = document.createElementNS(svg.namespaceURI, "path");
        let squares = "";
        for (let y = 0; y < matrix.size; y++)
            for (let x = 0; x < matrix.size; x++)
                if (matrix.getModule(x, y)) squares += "M" + (x + 4) + " " + (y + 4) + "h1v1h-1z";
        path.setAttribute("d", squares); path.setAttribute("fill", "#000");
        svg.append(background, path); container.append(svg);
        document.getElementById("qrTitle").textContent = action === "checkIn" ? "Check-in QR" : "Check-out QR";
        document.getElementById("qrDescription").textContent = attendance.student.name + " \u00b7 " + attendance.company.name + ". Show this QR to your assigned supervisor.";
        qrAction = action;
        clearInterval(qrTimer);
        qrTimer = setInterval(updateCountdown, 250);
        updateCountdown();
    } catch (error) {
        document.getElementById("qrCode").replaceChildren();
        document.getElementById("qrCountdown").textContent = "QR unavailable";
        document.getElementById("qrDescription").textContent = error.message;
        showNotice(error.message, true);
    }
    finally { saving = false; if (attendance) renderStudent(attendance); }
}

function updateCountdown() {
    const seconds = Math.max(0, Math.ceil((qrDeadline - performance.now()) / 1000));
    document.getElementById("qrCountdown").textContent = seconds ? "Expires in " + seconds + " seconds" : "QR expired. Close and ask your supervisor for a new one.";
    if (!seconds) {
        clearInterval(qrTimer);
        document.getElementById("qrCode").replaceChildren();
    }
}
document.getElementById("qrDialog").addEventListener("close", function () {
    clearInterval(qrTimer);
    document.getElementById("qrCode").replaceChildren();
});
document.getElementById("checkInButton").addEventListener("click", function () { showQr("checkIn", this); });
document.getElementById("checkOutButton").addEventListener("click", function () { showQr("checkOut", this); });
document.getElementById("earlyButton").addEventListener("click", function () {
    if (!attendance || !attendance.canRequestEarly) return;
    document.getElementById("reasonError").hidden = true;
    document.getElementById("earlyReason").value = "";
    openDialog("earlyDialog", this);
});
document.getElementById("sendEarlyRequest").addEventListener("click", async function () {
    const reason = document.getElementById("earlyReason").value.trim();
    document.getElementById("reasonError").hidden = !!reason;
    if (!reason || saving) return;
    saving = true; dataVersion++; this.disabled = true;
    try {
        const data = await api("requestEarly", { reason: reason });
        saving = false;
        renderStudent(data);
        document.getElementById("earlyDialog").close();
        showNotice("Request sent. Awaiting supervisor approval.");
    } catch (error) { showNotice(error.message, true); }
    finally { saving = false; this.disabled = false; }
});
document.getElementById("historyFilter").addEventListener("change", renderHistory);
document.getElementById("attendanceSearch").addEventListener("input", renderHistory);
document.getElementById("refreshAttendance").addEventListener("click", function () { loadAttendance(); });
// \u0646\u062d\u0645\u0651\u0644 \u0627\u0644\u0628\u064a\u0627\u0646\u0627\u062a \u0639\u0646\u062f \u0641\u062a\u062d \u0627\u0644\u0635\u0641\u062d\u0629\u061b \u062a\u062d\u062f\u064a\u062b \u062a\u063a\u064a\u064a\u0631\u0627\u062a \u0627\u0644\u0637\u0631\u0641 \u0627\u0644\u0622\u062e\u0631 \u064a\u0643\u0648\u0646 \u064a\u062f\u0648\u064a\u064b\u0627 \u0645\u0646 \u0632\u0631 Refresh.
loadAttendance();

// Local reset still uses the same authenticated/CSRF-protected attendance API.
let resetPreview = null;
document.getElementById("resetTodayButton").addEventListener("click", async function () {
    if (loading || saving || !attendance || !attendance.canResetToday) return;
    saving = true; this.disabled = true; resetPreview = null;
    try {
        resetPreview = await api("resetPreview");
        const minutes = Number(resetPreview.approvedMinutesRemoved || 0);
        document.getElementById("resetTodayImpact").textContent = minutes > 0
            ? "Date: " + resetPreview.date + ". This will remove " + minutes + " approved minute(s) (" + (minutes / 60).toFixed(2) + " h) from your current total."
            : "Date: " + resetPreview.date + ". No approved hours will be removed.";
        document.getElementById("resetTodayError").hidden = true;
        document.getElementById("confirmResetToday").disabled = false;
        openDialog("resetTodayDialog", this);
    } catch (error) { showNotice(error.message, true); }
    finally { saving = false; this.disabled = !attendance || !attendance.canResetToday; }
});
document.getElementById("confirmResetToday").addEventListener("click", async function () {
    if (loading || saving || !resetPreview) return;
    saving = true; this.disabled = true;
    const modal = document.getElementById("resetTodayDialog");
    // Closing remains safe while a request is in flight; the operation is never silently retried.
    try {
        const data = await api("resetToday", { expectedVersion: resetPreview.expectedVersion, expectedDate: resetPreview.date });
        resetPreview = null;
        if (document.getElementById("qrDialog").open) document.getElementById("qrDialog").close();
        clearInterval(qrTimer); document.getElementById("qrCode").replaceChildren();
        saving = false; renderStudent(data); modal.close();
        showNotice("Today’s attendance was reset. Ask your supervisor to issue a new check-in QR.");
    } catch (error) {
        resetPreview = null;
        document.getElementById("resetTodayError").textContent = error.message + " Close this window and refresh attendance before trying again.";
        document.getElementById("resetTodayError").hidden = false;
    } finally { saving = false; }
});
document.getElementById("resetTodayDialog").addEventListener("close", function () { if (!saving) resetPreview = null; });
