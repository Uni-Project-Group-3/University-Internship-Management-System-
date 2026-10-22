"use strict";

let supervisorData = null;
let loading = false;
let saving = false;
let dataVersion = 0;
let refreshAfterLoad = false;
let cameraStream = null;
let cameraOpening = false;
let cameraVersion = 0;
let frameId = 0;
let lastFrameAt = 0;
let lastToken = "";
let lastTokenAt = 0;
const video = document.getElementById("cameraVideo");
const scanCanvas = document.createElement("canvas");
const scanContext = scanCanvas.getContext("2d", { willReadFrequently: true });

// تشخيص مؤقت باختيار المطوّر على localhost فقط؛ لا نسجل الرمز أو بيانات الحساب.
let scannerDiagnosticsEnabled = false;
try {
    scannerDiagnosticsEnabled = ["localhost", "127.0.0.1", "[::1]"].includes(location.hostname) &&
        sessionStorage.getItem("uimsScannerDebug") === "1";
} catch (error) { /* منع التخزين لا يعطل الماسح. */ }
let diagnosticFrames = 0;
let lastFrameDiagnosticAt = 0;
function scannerDiagnostic(stage, details) {
    if (scannerDiagnosticsEnabled) console.info("[Attendance scanner]", stage, details || {});
}

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
            if (action === "scan") scannerDiagnostic("request.network-failed", { errorType: error.name });
            throw new Error(error.name === "AbortError"
                ? "Attendance response timed out. Refresh to check the saved state before retrying."
                : "Cannot reach the attendance server. Keep IIS Express running, reopen this page from its current localhost address, then refresh to check the saved state before retrying.");
        }
        if (action === "scan") scannerDiagnostic("request.response", { status: response.status });
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
            if (action === "scan") scannerDiagnostic("response.parse-failed", { errorType: error.name });
            throw new Error(error.name === "AbortError" ? "Attendance response timed out. Refresh to check the saved state before retrying."
                : "The server did not return attendance JSON. Check the server build and page configuration.");
        }
        if (!response.ok || !result || !result.success) {
            if (action === "scan") scannerDiagnostic("response.rejected", { status: response.status });
            throw new Error(result && result.message || "Attendance request failed.");
        }
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
            const id = session.supervisorId;
            if (!Array.from(selector.options).some(function (option) { return option.value === id; })) {
                const option = document.createElement("option");
                option.value = id; option.textContent = session.label;
                selector.append(option);
            }
        });
        selector.value = data.identityId;
    }
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


let progressStudents = [];
function renderSupervisorProgress(type, data) {
    if (type === "identity") {
        connected(data, data.supervisor.name);
        document.getElementById("connectionStatus").textContent = "Loading attendance…";
        progressStudents = [];
    } else if (type === "student") {
        progressStudents.push(data.student);
        document.getElementById("assignedStudents").textContent = "Assigned trainees: " +
            progressStudents.map(function (student) { return student.name; }).join(", ") + " · Loading records…";
    }
}

function renderSupervisor(data) {
    document.getElementById("recordRows").removeAttribute("aria-busy");
    supervisorData = data;
    connected(data, data.supervisor.name);
    document.getElementById("recordsDate").textContent = "Today: " + data.today + " · Asia/Amman";
    document.getElementById("assignedStudents").textContent = data.students.length
        ? "Assigned trainees: " + data.students.map(function (student) { return student.name; }).join(", ")
        : "No active trainees are assigned to this supervisor.";
    renderRequests();
    renderRecords();
}

async function loadSupervisor() {
    if (loading || saving) return;
    loading = true;
    document.getElementById("connectionStatus").textContent = "Refreshing…";
    const version = dataVersion;
    document.getElementById("recordRows").setAttribute("aria-busy", "true");
    if (!supervisorData) {
        const row = document.createElement("tr"), cell = document.createElement("td");
        cell.colSpan = 7; cell.textContent = "Loading attendance…"; row.append(cell);
        document.getElementById("recordRows").replaceChildren(row);
        document.getElementById("requestList").textContent = "Loading requests…";
    }
    try {
        const data = await api("load", null, function (type, partial) {
            if (version === dataVersion) renderSupervisorProgress(type, partial);
        });
        // قراءة بدأت قبل الحفظ لا يجوز أن تعيد الجدول إلى حالته القديمة.
        if (version === dataVersion) renderSupervisor(data);
    }
    catch (error) {
        if (version === dataVersion) {
            disconnected(error);
            document.getElementById("recordRows").removeAttribute("aria-busy");
            if (!supervisorData) {
                document.getElementById("requestList").textContent = "Requests could not be loaded.";
                const row = document.createElement("tr"), cell = document.createElement("td");
                cell.colSpan = 7; cell.textContent = "Attendance could not be loaded."; row.append(cell);
                document.getElementById("recordRows").replaceChildren(row);
            }
        }
    }
    finally {
        loading = false;
        if (refreshAfterLoad) { refreshAfterLoad = false; loadSupervisor(); }
    }
}

function renderRequests() {
    const list = document.getElementById("requestList");
    list.replaceChildren();
    const requests = supervisorData ? supervisorData.requests : [];
    document.getElementById("requestStatus").textContent = requests.length + " pending";
    requests.forEach(function (request) {
        const row = document.createElement("div");
        row.className = "request-row";
        const info = document.createElement("div");
        const name = document.createElement("strong");
        name.textContent = request.studentName + " · " + request.attendanceDate;
        const reason = document.createElement("p");
        reason.textContent = "Reason: " + request.reason;
        info.append(name, reason);
        const buttons = document.createElement("div");
        buttons.className = "attendance-actions";
        ["rejected", "approved"].forEach(function (decision) {
            const button = document.createElement("button");
            button.type = "button";
            button.className = decision === "approved" ? "primary-action" : "secondary-action";
            button.textContent = decision === "approved" ? "Approve request" : "Reject";
            button.disabled = saving || request.attendanceDate !== supervisorData.today;
            button.addEventListener("click", function () { review(request, decision); });
            buttons.append(button);
        });
        row.append(info, buttons);
        list.append(row);
    });
    if (!requests.length) list.textContent = "No pending early departure requests.";
}

function renderRecords() {
    const list = document.getElementById("recordRows");
    list.replaceChildren();
    const query = document.getElementById("attendanceSearch").value.trim().toLowerCase();
    const records = supervisorData ? supervisorData.records : [];
    records.filter(function (record) {
        return (record.studentName + " " + record.attendanceDate + " " + record.status).toLowerCase().includes(query);
    }).sort(function (a, b) { return b.attendanceDate.localeCompare(a.attendanceDate); }).forEach(function (record) {
        const row = document.createElement("tr");
        const status = record.status === "checkedIn" && !record.checkOutAt && Number(record.scheduledEndAt) < Date.now()
            ? "missingCheckOut" : record.status;
        [record.attendanceDate, record.studentName, time(record.checkInAt), time(record.checkOutAt),
            record.checkOutAt ? (Number(record.workedMinutes) / 60).toFixed(2) + " h" : "—",
            label(status) + (record.isLate ? " · Late" : "")].forEach(function (value) { addCell(row, value); });
        const cell = document.createElement("td");
        if (record.hoursApprovalStatus === "pending" && record.checkOutAt && Number(record.workedMinutes) > 0) {
            const button = document.createElement("button");
            button.type = "button"; button.className = "secondary-action";
            button.textContent = "Approve hours"; button.disabled = saving;
            button.addEventListener("click", function () { review(record, "approveHours"); });
            cell.append(button);
        } else cell.textContent = label(record.hoursApprovalStatus || "Not counted");
        row.append(cell); list.append(row);
    });
    if (!list.children.length) {
        const row = document.createElement("tr"), cell = document.createElement("td");
        cell.colSpan = 7; cell.textContent = "No matching attendance records."; row.append(cell); list.append(row);
    }
}

async function review(record, decision) {
    if (saving) return;
    saving = true; dataVersion++; renderRequests(); renderRecords();
    try {
        renderSupervisor(await api("review", { assignmentId: record.assignmentId, date: record.attendanceDate, decision: decision }));
        showNotice(decision === "approveHours" ? "Hours approved." : "Request updated. A check-out scan is still required.");
    } catch (error) { showNotice(error.message, true); }
    finally { saving = false; renderRequests(); renderRecords(); }
}

// نوقف تكرار قراءة الإطار بالمتصفح، والتحقق النهائي ومنع التسجيل المكرر يبقيان على السيرفر.
async function processToken(token) {
    if (saving) return;
    token = String(token || "").trim();
    if (!token) { showNotice("Scan or paste a temporary UIMS attendance QR first.", true); return; }
    if (token === lastToken && Date.now() - lastTokenAt < 5000) return;
    lastToken = token; lastTokenAt = Date.now();
    scannerDiagnostic("scan.callback", { decoder: typeof jsQR });
    saving = true;
    dataVersion++;
    let saved = false;
    let scanStage = "request";
    const errorBox = document.getElementById("scanError");
    errorBox.hidden = true;
    document.getElementById("verificationDetails").hidden = true;
    document.getElementById("scanStatus").textContent = "Validating and saving…";
    try {
        const result = await api("scan", { token: token });
        scannerDiagnostic("response.accepted");
        scanStage = "display";
        stopCamera();
        showStudent(result);
        scannerDiagnostic("result.displayed");
        // السجل المحفوظ يأتي من C#؛ نحدّث صفه فقط، والتحديث الدوري يجلب باقي السجل.
        if (supervisorData && result.record) {
            const row = Object.assign({}, result.record, { studentName: result.student.name });
            const index = supervisorData.records.findIndex(function (item) {
                return item.assignmentId === row.assignmentId && item.attendanceDate === row.attendanceDate;
            });
            if (index >= 0) supervisorData.records[index] = row;
            else supervisorData.records.push(row);
        }
        saved = true;
        showNotice("Attendance saved. Hours pending approval.");
        if (document.getElementById("tokenInput")) document.getElementById("tokenInput").value = "";
    } catch (error) {
        scannerDiagnostic("scan.failed", { stage: scanStage, errorType: error.name });
        document.getElementById("scanStatus").textContent = "Not recorded / check state";
        errorBox.textContent = error.message; errorBox.hidden = false;
    } finally {
        saving = false;
        renderRequests(); renderRecords();
        // عند الخطأ نقرأ الحالة الحقيقية احتياطًا لاحتمال حفظ العملية قبل انقطاع الرد.
        if (!saved || !supervisorData) {
            if (loading) refreshAfterLoad = true;
            else loadSupervisor();
        }
    }
}

function showStudent(result) {
    const student = result.student, record = result.record;
    document.getElementById("verificationEmpty").hidden = true;
    document.getElementById("verificationDetails").hidden = false;
    document.getElementById("verifiedStudentName").textContent = student.name;
    document.getElementById("verifiedIdentity").textContent = result.localDemo ? "Demo student" : "Assigned trainee";
    document.getElementById("verifiedStudentNumber").textContent = student.studentNumber || "—";
    document.getElementById("verifiedCompany").textContent = result.company.name;
    document.getElementById("verifiedSupervisor").textContent = result.supervisor.name;
    document.getElementById("scannedAction").textContent = result.action === "checkIn" ? "Check-in" : "Check-out";
    document.getElementById("recordedTime").textContent = time(result.action === "checkIn" ? record.checkInAt : record.checkOutAt);
    document.getElementById("scanStatus").textContent = "Recorded successfully";
    document.getElementById("verificationExplanation").textContent = record.checkOutAt
        ? "Worked: " + (Number(record.workedMinutes) / 60).toFixed(2) + " h."
        : "Check-in saved.";
    document.getElementById("studentInitials").textContent = student.name.split(" ").slice(0, 2).map(function (part) { return part[0]; }).join("");
    const photo = document.getElementById("studentPhoto");
    photo.hidden = true;
    document.getElementById("studentInitials").hidden = false;
    if (student.profilePhotoUrl) {
        try {
            const url = new URL(student.profilePhotoUrl, window.location.href);
            if (url.protocol === "https:") {
                photo.referrerPolicy = "no-referrer";
                photo.onload = function () { photo.hidden = false; document.getElementById("studentInitials").hidden = true; };
                photo.onerror = function () { photo.hidden = true; document.getElementById("studentInitials").hidden = false; };
                photo.src = url.href;
            }
        } catch (error) { photo.hidden = true; }
    }
}

function stopCamera() {
    if (cameraStream || cameraOpening) scannerDiagnostic("camera.stopped");
    cameraVersion++;
    cancelAnimationFrame(frameId);
    if (cameraStream) cameraStream.getTracks().forEach(function (track) { track.stop(); });
    cameraStream = null;
    video.srcObject = null; video.hidden = true;
    document.getElementById("cameraPlaceholder").hidden = false;
    document.getElementById("stopCamera").disabled = true;
    document.getElementById("stopCamera").hidden = true;
    document.getElementById("openCamera").disabled = false;
    document.getElementById("cameraMessage").textContent = "Camera stopped.";
}

function scanFrame(timestamp) {
    if (!cameraStream) return;
    if (!saving && timestamp - lastFrameAt > 250 && video.readyState >= 2) {
        lastFrameAt = timestamp;
        const started = scannerDiagnosticsEnabled ? performance.now() : 0;
        const scale = Math.min(1, 800 / video.videoWidth);
        scanCanvas.width = Math.round(video.videoWidth * scale);
        scanCanvas.height = Math.round(video.videoHeight * scale);
        scanContext.drawImage(video, 0, 0, scanCanvas.width, scanCanvas.height);
        const pixels = scanContext.getImageData(0, 0, scanCanvas.width, scanCanvas.height);
        const code = jsQR(pixels.data, pixels.width, pixels.height, { inversionAttempts: "attemptBoth" });
        if (scannerDiagnosticsEnabled) {
            diagnosticFrames++;
            // تقرير دوري مختصر بدل طباعة سجل لكل إطار.
            if (!lastFrameDiagnosticAt || timestamp - lastFrameDiagnosticAt >= 2000 || code) {
                scannerDiagnostic(code ? "qr.detected" : "frame.processed", {
                    frames: diagnosticFrames, width: pixels.width, height: pixels.height,
                    processingMs: Math.round(performance.now() - started), detected: !!code
                });
                lastFrameDiagnosticAt = timestamp;
            }
        }
        if (code) processToken(code.data);
    }
    frameId = requestAnimationFrame(scanFrame);
}

document.getElementById("openCamera").addEventListener("click", async function () {
    if (cameraOpening || cameraStream) return;
    if (typeof jsQR !== "function") { showNotice("The local QR decoder library could not be loaded.", true); return; }
    if (!window.isSecureContext || !navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        showNotice("Camera access needs HTTPS or localhost and a supported browser. You can also scan a QR image below.", true); return;
    }
    cameraOpening = true;
    scannerDiagnostic("camera.requested", { decoder: typeof jsQR, secureContext: window.isSecureContext });
    diagnosticFrames = 0;
    lastFrameDiagnosticAt = 0;
    const version = ++cameraVersion;
    this.disabled = true;
    document.getElementById("stopCamera").disabled = false;
    document.getElementById("stopCamera").hidden = false;
    document.getElementById("cameraMessage").textContent = "Allow camera access…";
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: "environment" } }, audio: false });
        if (version !== cameraVersion) { stream.getTracks().forEach(function (track) { track.stop(); }); return; }
        cameraStream = stream; video.srcObject = stream; video.hidden = false;
        document.getElementById("cameraPlaceholder").hidden = true;
        await video.play();
        if (version === cameraVersion) {
            scannerDiagnostic("camera.opened", { width: video.videoWidth, height: video.videoHeight, readyState: video.readyState });
            document.getElementById("cameraMessage").textContent = "Scanning…";
            frameId = requestAnimationFrame(scanFrame);
        }
    } catch (error) {
        scannerDiagnostic("camera.failed", { errorType: error.name });
        if (version !== cameraVersion) return;
        stopCamera();
        showNotice(error.name === "NotAllowedError" ? "Camera permission denied. Allow access or scan a saved QR image."
            : "Camera unavailable or already in use. Try another camera or a saved QR image.", true);
    } finally { cameraOpening = false; this.disabled = !!cameraStream; }
});
document.getElementById("stopCamera").addEventListener("click", stopCamera);
window.addEventListener("pagehide", stopCamera);
document.addEventListener("visibilitychange", function () { if (document.hidden) stopCamera(); });

document.getElementById("qrImage")?.addEventListener("change", function () {
    const file = this.files[0];
    if (!file) return;
    if (typeof jsQR !== "function") { showNotice("QR decoder unavailable.", true); return; }
    if (file.size > 10 * 1024 * 1024) { showNotice("Choose a QR image smaller than 10 MB.", true); return; }
    stopCamera();
    const image = new Image(), url = URL.createObjectURL(file);
    image.onload = function () {
        try {
            const scale = Math.min(1, 1600 / Math.max(image.width, image.height));
            scanCanvas.width = Math.round(image.width * scale); scanCanvas.height = Math.round(image.height * scale);
            scanContext.drawImage(image, 0, 0, scanCanvas.width, scanCanvas.height);
            const pixels = scanContext.getImageData(0, 0, scanCanvas.width, scanCanvas.height);
            const code = jsQR(pixels.data, pixels.width, pixels.height, { inversionAttempts: "attemptBoth" });
            if (!code) throw new Error("No readable QR was found in this image.");
            processToken(code.data);
        } catch (error) { showNotice(error.message, true); }
        finally { URL.revokeObjectURL(url); }
    };
    image.onerror = function () { URL.revokeObjectURL(url); showNotice("Unable to open this image.", true); };
    image.src = url;
    this.value = "";
});
document.getElementById("submitToken")?.addEventListener("click", function () { processToken(document.getElementById("tokenInput").value); });
document.getElementById("attendanceSearch").addEventListener("input", renderRecords);
document.getElementById("refreshAttendance").addEventListener("click", loadSupervisor);
const identitySelector = document.getElementById("localIdentity");
if (identitySelector) identitySelector.addEventListener("change", async function () {
    if (saving || loading) { if (supervisorData) this.value = supervisorData.identityId; return; }
    stopCamera();
    document.getElementById("verificationDetails").hidden = true;
    document.getElementById("verificationEmpty").hidden = false;
    loading = true;
    try { renderSupervisor(await api("identity", { identityId: this.value })); }
    catch (error) { disconnected(error); }
    finally { loading = false; }
});
// نحمّل البيانات عند فتح الصفحة؛ تحديث تغييرات الطرف الآخر يكون يدويًا من زر Refresh.
loadSupervisor();
