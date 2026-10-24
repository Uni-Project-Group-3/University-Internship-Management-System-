"use strict";

let supervisorData = null;
let loading = false;
let saving = false;
let dataVersion = 0;
let serverSessionVerified = false;
let refreshAfterLoad = false;
let cameraStream = null;
let cameraOpening = false;
let cameraVersion = 0;
let frameId = 0;
let lastFrameAt = 0;
let scanScaleIndex = 0;
let nativeQrDetector = null;
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
            // القراءة أولاً؛ C# يوثق توكن Session ويطابق UID ودور المشرف، ولا يكتفي بالـ Cookie.
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
        ? "Local test accounts · real Firebase"
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
    document.getElementById("issueQrButton").disabled = saving || !data.students.length;
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
                // انتهى الطلب بالفشل؛ لا نترك الواجهة توحي بأن التحميل ما زال جاريًا.
                document.getElementById("assignedStudents").textContent = "Assigned trainees could not be loaded.";
                document.getElementById("requestStatus").textContent = "Unavailable";
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
    if (!token) { scannerDiagnostic("qr.empty-result"); return; }
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
        // السجل المحفوظ يأتي من C#؛ نحدّث صفه مباشرة، وباقي السجل من زر Refresh.
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
    document.getElementById("verifiedIdentity").textContent = result.localDemo ? "Local test student" : "Assigned trainee";
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

async function scanFrame(timestamp) {
    if (!cameraStream) return;
    const version = cameraVersion;
    try {
    if (!saving && timestamp - lastFrameAt > 120 && video.readyState >= 2 && video.videoWidth > 0 && video.videoHeight > 0) {
        lastFrameAt = timestamp;
        // Use the browser's accelerated decoder when available; jsQR remains the offline fallback.
        if (nativeQrDetector) {
            try {
                const detected = await nativeQrDetector.detect(video);
                if (version !== cameraVersion || !cameraStream) return;
                const match = detected.find(item => typeof item.rawValue === "string" && item.rawValue.trim().startsWith("UIMSATT1:"));
                if (match) {
                    processToken(match.rawValue);
                    if (cameraStream) frameId = requestAnimationFrame(scanFrame);
                    return;
                }
            } catch (_) { nativeQrDetector = null; }
        }
        if (version !== cameraVersion || !cameraStream) return;
        if (saving) { frameId = requestAnimationFrame(scanFrame); return; }
        const started = scannerDiagnosticsEnabled ? performance.now() : 0;
        // Alternate the full frame with an unscaled central crop. Upscaling a low-resolution
        // camera adds work, not detail, and can stall the UI before the next useful frame.
        const pass = scanScaleIndex++ % 3;
        const crop = pass !== 0;
        const sourceWidth = crop ? Math.min(video.videoWidth, video.videoHeight) * 0.8 : video.videoWidth;
        const sourceHeight = crop ? sourceWidth : video.videoHeight;
        const scale = Math.min(1, (pass === 2 ? 1280 : 960) / sourceWidth);
        scanCanvas.width = Math.round(sourceWidth * scale);
        scanCanvas.height = Math.round(sourceHeight * scale);
        scanContext.imageSmoothingEnabled = false;
        scanContext.drawImage(video, (video.videoWidth-sourceWidth)/2, (video.videoHeight-sourceHeight)/2,
            sourceWidth, sourceHeight, 0, 0, scanCanvas.width, scanCanvas.height);
        const pixels = scanContext.getImageData(0, 0, scanCanvas.width, scanCanvas.height);
        const code = jsQR(pixels.data, pixels.width, pixels.height, { inversionAttempts: pass === 2 ? "attemptBoth" : "dontInvert" });
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
        // بعض الإطارات تعطي نتيجة فارغة؛ نكمل المسح حتى قراءة نص الرمز فعليًا.
        if (code) {
            let decoded = typeof code.data === "string" ? code.data.trim() : "";
            // توكن الحضور ASCII؛ بعض نتائج المكتبة تحتوي البايتات بدون النص.
            if (!decoded && code.binaryData && code.binaryData.length <= 4096 &&
                code.binaryData.every(function (value) { return value >= 32 && value <= 126; })) {
                decoded = String.fromCharCode.apply(null, code.binaryData);
            }
            if (decoded.startsWith("UIMSATT1:")) processToken(decoded);
        }
    }
    } catch (error) {
        // خطأ قراءة الإطار يجب أن يحرر الكاميرا ويتيح إعادة المحاولة، لا أن يوقف المسح بصمت.
        scannerDiagnostic("frame.failed", { errorType: error.name });
        stopCamera();
        showNotice("The camera frame could not be processed. Open the camera again to retry. No attendance confirmation was received.", true);
        return;
    }
    if (cameraStream && version === cameraVersion) frameId = requestAnimationFrame(scanFrame);
}

document.getElementById("openCamera").addEventListener("click", async function () {
    if (cameraOpening || cameraStream) return;
    if (typeof jsQR !== "function") { showNotice("The local QR decoder library could not be loaded.", true); return; }
    if (!window.isSecureContext || !navigator.mediaDevices || !navigator.mediaDevices.getUserMedia) {
        showNotice("Camera access needs HTTPS or localhost and a supported browser. Allow camera access to scan attendance.", true); return;
    }
    cameraOpening = true;
    nativeQrDetector = null;
    try { if (typeof window.BarcodeDetector === "function") nativeQrDetector = new window.BarcodeDetector({ formats: ["qr_code"] }); } catch (_) { /* Use jsQR. */ }
    lastFrameAt = 0; scanScaleIndex = 0;
    scannerDiagnostic("camera.requested", { decoder: typeof jsQR, secureContext: window.isSecureContext });
    diagnosticFrames = 0;
    lastFrameDiagnosticAt = 0;
    const version = ++cameraVersion;
    this.disabled = true;
    document.getElementById("stopCamera").disabled = false;
    document.getElementById("stopCamera").hidden = false;
    document.getElementById("cameraMessage").textContent = "Allow camera access…";
    try {
        const stream = await navigator.mediaDevices.getUserMedia({ video: { facingMode: { ideal: "environment" }, width: { ideal: 1920 }, height: { ideal: 1080 } }, audio: false });
        if (version !== cameraVersion) { stream.getTracks().forEach(function (track) { track.stop(); }); return; }
        cameraStream = stream; video.srcObject = stream; video.hidden = false;
        document.getElementById("cameraPlaceholder").hidden = true;
        await video.play();
        if (version === cameraVersion) {
            scannerDiagnostic("camera.opened", { width: video.videoWidth, height: video.videoHeight, readyState: video.readyState });
            const track = stream.getVideoTracks()[0];
            try {
                const capabilities = track.getCapabilities?.();
                if (capabilities?.focusMode?.includes("continuous")) await track.applyConstraints({ advanced: [{ focusMode: "continuous" }] });
            } catch (_) { /* Focus controls vary by camera; scanning remains available. */ }
            if (version !== cameraVersion) return;
            document.getElementById("cameraMessage").textContent = "Scanning… Center the QR in the camera and keep it steady.";
            frameId = requestAnimationFrame(scanFrame);
        }
    } catch (error) {
        scannerDiagnostic("camera.failed", { errorType: error.name });
        if (version !== cameraVersion) return;
        stopCamera();
        showNotice(error.name === "NotAllowedError" ? "Camera permission denied. Allow camera access in your browser settings."
            : "Camera unavailable or already in use. Close other camera apps and try again.", true);
    } finally { cameraOpening = false; this.disabled = !!cameraStream; }
});
document.getElementById("stopCamera").addEventListener("click", stopCamera);
window.addEventListener("pagehide", stopCamera);
document.addEventListener("visibilitychange", function () { if (document.hidden) stopCamera(); });

document.getElementById("attendanceSearch").addEventListener("input", renderRecords);
document.getElementById("refreshAttendance").addEventListener("click", loadSupervisor);
// نحمّل البيانات عند فتح الصفحة؛ تحديث تغييرات الطرف الآخر يكون يدويًا من زر Refresh.
loadSupervisor();

// اختيار الطلاب يغيّر رموزهم المؤقتة فقط؛ لا يسجل حضورًا.
document.getElementById("issueQrButton").addEventListener("click", function () {
    const list = document.getElementById("qrStudentList");
    list.replaceChildren();
    (supervisorData ? supervisorData.students : []).forEach(function (student) {
        const row = document.createElement("p"), label = document.createElement("label");
        const checkbox = document.createElement("input");
        checkbox.type = "checkbox"; checkbox.value = student.id; checkbox.name = "qrStudent";
        // رقم الطالب يميّز الحساب الصحيح عند تشابه الأسماء أو وجود أكثر من مشرف.
        label.append(checkbox, " " + student.name + " · Student ID: " + student.id +
            " · Assignment: " + student.assignmentId);
        row.append(label); list.append(row);
    });
    document.getElementById("selectAllQrStudents").checked = false;
    document.getElementById("issueQrStatus").textContent = "";
    openDialog("issueQrDialog", this);
});
document.getElementById("selectAllQrStudents").addEventListener("change", function () {
    const checked = this.checked;
    document.querySelectorAll("#qrStudentList input").forEach(function (box) { box.checked = checked; });
});
document.getElementById("confirmIssueQr").addEventListener("click", async function () {
    if (saving) return;
    const ids = Array.from(document.querySelectorAll("#qrStudentList input:checked")).map(function (box) { return box.value; });
    const status = document.getElementById("issueQrStatus");
    if (!ids.length) { status.textContent = "Select at least one student."; return; }
    saving = true; this.disabled = true;
    try {
        const result = await api("issueQr", { studentIds: JSON.stringify(ids), attendanceAction: document.getElementById("issuedQrAction").value });
        status.textContent = result.issued.length + " QR codes issued. Each student can now open their own QR.";
        if (result.failed.length) status.textContent += " Not issued: " + result.failed.map(function (row) {
            return "Student " + row.studentId + ": " + row.message;
        }).join("; ");
    } catch (error) { status.textContent = error.message; }
    finally { saving = false; this.disabled = false; }
});
