"use strict";

let attendance = null;
let loading = false;
let saving = false;
let dataVersion = 0;
let qrDeadline = 0;
let qrAction = "";
let qrTimer;

const sidebar = document.getElementById("sidebar");
const menuButton = document.querySelector(".menu-btn");
const mainArea = document.querySelector(".main");
const mobileScreen = window.matchMedia("(max-width: 991.98px)");

function setSidebarOpen(open) {
    const visible = open && mobileScreen.matches;
    document.body.classList.toggle("sidebar-open", visible);
    menuButton.setAttribute("aria-expanded", String(visible));
    sidebar.inert = mobileScreen.matches && !visible;
    mainArea.inert = visible;
    if (visible) sidebar.querySelector("button").focus();
}
function toggleSidebar() { setSidebarOpen(!document.body.classList.contains("sidebar-open")); }
document.querySelector(".legacy-sidebar-close").addEventListener("click", function () {
    setSidebarOpen(false);
    menuButton.focus();
});
mobileScreen.addEventListener("change", function () { setSidebarOpen(false); });
sidebar.addEventListener("keydown", function (event) {
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
    // النجاح يظهر مؤقتًا، أما الخطأ فيبقى حتى المعالجة أو التحديث.
    if (!error) noticeTimer = setTimeout(function () { notice.hidden = true; }, 6000);
}
function time(value) {
    return value ? new Date(Number(value)).toLocaleTimeString("en-GB", {
        timeZone: "Asia/Amman", hour: "2-digit", minute: "2-digit"
    }) : "—";
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

// الطلبات تذهب إلى C# بنفس الصفحة؛ التوكن السري الخاص بقاعدة البيانات لا يصل للمتصفح.
async function api(action, values, onProgress) {
    const url = new URL(window.location.href);
    url.search = "?attendanceApi=1";
    const body = new URLSearchParams(Object.assign({
        action: action,
        csrf: JSON.parse(document.getElementById("attendanceBootstrap").textContent).csrf
    }, values || {}));
    if (action === "load" && onProgress) body.set("stream", "1");
    const localDevelopment = JSON.parse(document.getElementById("attendanceBootstrap").textContent).localDemo;
    const idToken = localDevelopment ? null : await window.UimsFirebase.getIdToken();
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
                // رسائل متعددة داخل نفس الرد؛ لا ننشئ طلب تحميل جديدًا لكل قسم.
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
        return result.data;
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
        ? "Local demo · real Firebase"
        : "Signed in";
    const selector = document.getElementById("localIdentity");
    if (selector) {
        // الخيارات الجديدة تأتي من جلسات أنشأها السيرفر لهذه الجلسة المحلية فقط.
        (data.demoSessions || []).forEach(function (session) {
            const id = session.studentId;
            if (!Array.from(selector.options).some(function (option) { return option.value === id; })) {
                const option = document.createElement("option");
                option.value = id; option.textContent = session.label;
                selector.append(option);
            }
        });
        selector.value = data.identityId;
    }
    const demoInfo = document.getElementById("demoSessionInfo");
    const demo = (data.demoSessions || []).find(function (session) { return session.studentId === data.identityId; });
    if (demoInfo) demoInfo.textContent = demo
        ? demo.label + " · Firebase record: uimsAttendance/attendance/" + demo.assignmentId + "/" + demo.date
        : "Create a new session to start a separate live demo. Previous Firebase records are preserved.";
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
            document.getElementById("historyEmpty").textContent = "Loading attendance…";
            document.getElementById("historyEmpty").hidden = false;
            ["approvedHours", "remainingHours", "progressText", "todayStatus", "companyName", "supervisorName"].forEach(function (id) {
                document.getElementById(id).textContent = "Loading…";
            });
            document.getElementById("actualCheckIn").textContent = "—";
            document.getElementById("actualCheckOut").textContent = "—";
        }
        connected(data, data.student.name);
        document.getElementById("studentName").textContent = data.student.name + (data.student.studentNumber ? " · " + data.student.studentNumber : "");
        document.getElementById("connectionStatus").textContent = "Loading attendance…";
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
    connected(data, data.student.name);
    const assigned = data.hasAssignment;
    const assignment = data.assignment || {};
    const record = data.record || {};
    const schedule = data.schedule || {};
    const request = data.earlyRequest || {};
    const required = Number(assignment.requiredHours || 0);
    const approved = Number(data.approvedMinutes || 0) / 60;
    document.getElementById("studentName").textContent = data.student.name + (data.student.studentNumber ? " · " + data.student.studentNumber : "");
    document.getElementById("companyName").textContent = assigned ? data.company.name : "No active assignment";
    document.getElementById("supervisorName").textContent = assigned ? data.supervisor.name : "Not assigned";
    document.getElementById("requiredHours").textContent = assigned ? required + " h" : "—";
    document.getElementById("approvedHours").textContent = assigned ? approved.toFixed(2) + " h" : "—";
    document.getElementById("remainingHours").textContent = assigned ? Math.max(0, required - approved).toFixed(2) + " h" : "—";
    document.getElementById("progressText").textContent = assigned ? approved.toFixed(2) + " / " + required + " hours" : "No active training requirement";
    const progress = document.getElementById("trainingProgress");
    progress.max = required || 1;
    progress.value = Math.min(required, approved);
    document.getElementById("scheduleDate").textContent = data.today + " · Asia/Amman" +
        (assignment.startDate && assignment.endDate ? " · Training: " + assignment.startDate + " – " + assignment.endDate : "");
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
    const start = schedule.scheduledStartAt ? time(Number(schedule.scheduledStartAt) - 300000) : "—";
    document.getElementById("scheduleHelp").textContent = !assigned ? "No active assignment. Attendance actions are unavailable."
        : !data.actionsEnabled ? "Attendance actions are disabled in Firebase settings. Reading history remains available."
        : !schedule.assignmentId ? "No schedule exists for today. Ask your supervisor to configure it."
        : !schedule.isTrainingDay ? "Scheduled day off. No absence is counted."
        : record.checkOutAt ? "Attendance complete."
        : record.checkInAt ? "Normal check-out from " + time(schedule.scheduledEndAt) + "."
        : "Check-in from " + start + ".";
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
            record.checkOutAt ? (Number(record.workedMinutes) / 60).toFixed(2) + " h" : "—",
            label(status) + (record.isLate ? " · Late" : ""), label(record.hoursApprovalStatus)].forEach(function (value) { addCell(row, value); });
        list.append(row);
    });
    document.getElementById("historyEmpty").hidden = rows.length > 0;
}

async function loadAttendance() {
    if (loading || saving) return;
    loading = true;
    document.getElementById("connectionStatus").textContent = "Refreshing…";
    if (createDemoButton) createDemoButton.disabled = true;
    const version = dataVersion;
    ["checkInButton", "checkOutButton", "earlyButton"].forEach(function (id) { document.getElementById(id).disabled = true; });
    ["historyRows", "todayStatus", "actualCheckIn", "actualCheckOut"].forEach(function (id) {
        document.getElementById(id).setAttribute("aria-busy", "true");
    });
    document.getElementById("todayStatus").textContent = "Loading…";
    if (!attendance) {
        document.getElementById("historyEmpty").textContent = "Loading attendance…";
        document.getElementById("historyEmpty").hidden = false;
        document.getElementById("todayStatus").textContent = "Loading…";
        ["approvedHours", "remainingHours", "progressText"].forEach(function (id) { document.getElementById(id).textContent = "Loading…"; });
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
        ["checkInButton", "checkOutButton", "earlyButton"].forEach(function (id) { document.getElementById(id).disabled = true; });
    } finally { loading = false; if (createDemoButton) createDemoButton.disabled = saving; }
}

async function showQr(action, button) {
    if (loading || saving || !attendance || !(action === "checkIn" ? attendance.canCheckIn : attendance.canCheckOut)) return;
    saving = true;
    button.disabled = true;
    try {
        if (typeof QRCode !== "function") throw new Error("The local QR generation library could not be loaded.");
        const requestedAt = performance.now();
        const qr = await api("qr", { attendanceAction: action });
        // نخصم مدة الطلب احتياطًا حتى لا نعرض الرمز مدة أطول من صلاحيته على السيرفر.
        qrDeadline = requestedAt + qr.expiresAt - qr.serverNow;
        if (performance.now() >= qrDeadline) throw new Error("QR expired while loading. Generate a new QR.");
        const container = document.getElementById("qrCode");
        container.replaceChildren();
        // التوكن المحمي طويل؛ دقة أعلى تمنع ضياع مربعات QR عند حفظ الصورة وقراءتها.
        new QRCode(container, { text: qr.token, width: 640, height: 640, correctLevel: QRCode.CorrectLevel.L });
        if (document.getElementById("qrToken")) document.getElementById("qrToken").value = qr.token;
        if (document.getElementById("copyTokenStatus")) document.getElementById("copyTokenStatus").textContent = "";
        document.getElementById("qrTitle").textContent = action === "checkIn" ? "Check-in QR" : "Check-out QR";
        document.getElementById("qrDescription").textContent = attendance.student.name + " · " + attendance.company.name + ". Show this QR to your assigned supervisor.";
        qrAction = action;
        clearInterval(qrTimer);
        qrTimer = setInterval(updateCountdown, 250);
        if (document.getElementById("saveQrImage")) document.getElementById("saveQrImage").disabled = false;
        updateCountdown();
        openDialog("qrDialog", button);
    } catch (error) { showNotice(error.message, true); }
    finally { saving = false; if (attendance) renderStudent(attendance); }
}

function updateCountdown() {
    const seconds = Math.max(0, Math.ceil((qrDeadline - performance.now()) / 1000));
    document.getElementById("qrCountdown").textContent = seconds ? "Expires in " + seconds + " seconds" : "QR expired. Close and generate a new one.";
    if (!seconds) {
        clearInterval(qrTimer);
        document.getElementById("qrCode").replaceChildren();
        if (document.getElementById("qrToken")) document.getElementById("qrToken").value = "";
        if (document.getElementById("saveQrImage")) document.getElementById("saveQrImage").disabled = true;
    }
}
document.getElementById("qrDialog").addEventListener("close", function () {
    clearInterval(qrTimer);
    if (document.getElementById("qrToken")) document.getElementById("qrToken").value = "";
    document.getElementById("qrCode").replaceChildren();
});
document.getElementById("saveQrImage")?.addEventListener("click", function () {
    const original = document.querySelector("#qrCode canvas");
    if (!original || performance.now() >= qrDeadline) return;
    const canvas = document.createElement("canvas");
    canvas.width = original.width + 64; canvas.height = original.height + 64;
    const context = canvas.getContext("2d");
    context.fillStyle = "#ffffff"; context.fillRect(0, 0, canvas.width, canvas.height);
    context.drawImage(original, 32, 32);
    const link = document.createElement("a");
    link.download = "UIMS-temporary-attendance-QR.png";
    link.href = canvas.toDataURL("image/png"); link.click();
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
const identitySelector = document.getElementById("localIdentity");
const createDemoButton = document.getElementById("createDemoSession");
if (createDemoButton) createDemoButton.addEventListener("click", async function () {
    if (loading || saving) return;
    saving = true; this.disabled = true;
    if (document.getElementById("qrDialog").open) document.getElementById("qrDialog").close();
    try {
        const data = await api("newDemo", { scenario: document.getElementById("demoScenario").value });
        saving = false;
        renderStudent(data);
        showNotice("Session ready. Refresh the supervisor tab.");
    } catch (error) {
        attendance = null;
        disconnected(error);
        ["checkInButton", "checkOutButton", "earlyButton"].forEach(function (id) { document.getElementById(id).disabled = true; });
    }
    finally { saving = false; this.disabled = false; }
});
if (identitySelector) identitySelector.addEventListener("change", async function () {
    if (saving || loading) { if (attendance) this.value = attendance.identityId; return; }
    if (document.getElementById("qrDialog").open) document.getElementById("qrDialog").close();
    loading = true;
    try { renderStudent(await api("identity", { identityId: this.value })); }
    catch (error) {
        disconnected(error);
        ["checkInButton", "checkOutButton", "earlyButton"].forEach(function (id) { document.getElementById(id).disabled = true; });
    }
    finally { loading = false; }
});
document.getElementById("historyFilter").addEventListener("change", renderHistory);
document.getElementById("attendanceSearch").addEventListener("input", renderHistory);
document.getElementById("refreshAttendance").addEventListener("click", function () { loadAttendance(); });
// نحمّل البيانات عند فتح الصفحة؛ تحديث تغييرات الطرف الآخر يكون يدويًا من زر Refresh.
loadAttendance();

// النسخ يعمل على localhost؛ عند رفض Clipboard نحدد النص للنسخ اليدوي.
document.getElementById("copyQrToken")?.addEventListener("click", async function () {
    const field = document.getElementById("qrToken");
    const status = document.getElementById("copyTokenStatus");
    if (!field.value || performance.now() >= qrDeadline) { status.textContent = "Generate a fresh QR first."; return; }
    try { await navigator.clipboard.writeText(field.value); status.textContent = "Token copied."; }
    catch (error) { field.focus(); field.select(); status.textContent = "Press Ctrl+C to copy."; }
});
