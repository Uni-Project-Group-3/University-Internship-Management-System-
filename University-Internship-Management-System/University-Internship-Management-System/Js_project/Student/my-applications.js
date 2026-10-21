"use strict";

const applicationCards = document.querySelectorAll(".application-card");
const statusFilter = document.getElementById("statusFilter");
const confirmModal = document.getElementById("confirmModal");
let selectedApplication = null;
let confirmedApplicationId = null;

function filterApplications() {
    let visibleCount = 0;

    applicationCards.forEach(function (application) {
        application.hidden = statusFilter.value !== "" && application.dataset.status !== statusFilter.value;

        if (!application.hidden) {
            visibleCount++;
        }
    });

    document.getElementById("resultCount").textContent = "(" + visibleCount + ")";
    document.getElementById("emptyState").hidden = visibleCount > 0;
}

statusFilter.addEventListener("change", filterApplications);

// نسخ البيانات الظاهرة من الطلب إلى نافذة التفاصيل دون تكرارها
document.getElementById("applicationModal").addEventListener("show.bs.modal", function (event) {
    const application = event.relatedTarget.closest(".application-card");

    document.getElementById("detailsTitle").textContent = application.querySelector("h3").textContent;
    document.getElementById("detailsCompany").textContent = application.querySelector(".company-name").textContent;
    document.getElementById("detailsStatus").textContent = "Status: " + application.querySelector(".status-badge").textContent;
    document.getElementById("detailsDecision").textContent = application.querySelector(".decision-note").textContent;
    document.getElementById("detailsMeta").replaceChildren(application.querySelector(".application-meta").cloneNode(true));
});

confirmModal.addEventListener("show.bs.modal", function (event) {
    const application = event.relatedTarget.closest(".application-card");

    if (confirmedApplicationId || application.dataset.status !== "Accepted") {
        event.preventDefault();
        return;
    }

    selectedApplication = application;
    document.getElementById("selectedOffer").textContent = application.querySelector("h3").textContent + " at " + application.querySelector(".company-name").textContent;
});

// التأكيد في الذاكرة للمعاينة فقط؛ لاحقاً يجب فرض العرض الواحد في الخلفية أيضاً
document.getElementById("confirmOffer").addEventListener("click", function () {
    if (!selectedApplication || confirmedApplicationId || selectedApplication.dataset.status !== "Accepted") {
        return;
    }

    confirmedApplicationId = selectedApplication.dataset.id;
    selectedApplication.classList.add("is-confirmed");
    selectedApplication.querySelector(".status-badge").textContent = "Accepted · Confirmed";
    selectedApplication.querySelector(".decision-note").textContent = "You confirmed this offer in the preview. No internship has been saved to the system.";

    // لا نرفض الطلبات الأخرى تلقائياً؛ نعطل إمكانية تأكيد عرض ثانٍ فقط
    document.querySelectorAll(".application-card .confirm-button").forEach(function (button) {
        button.disabled = true;
        button.textContent = button.closest(".application-card").dataset.id === confirmedApplicationId ? "Offer confirmed" : "Another offer confirmed";
    });

    const notice = document.getElementById("confirmationNotice");
    notice.textContent = "Preview: you confirmed " + selectedApplication.querySelector("h3").textContent + " at " + selectedApplication.querySelector(".company-name").textContent + ". You cannot confirm another offer. This choice resets when you refresh.";
    notice.hidden = false;
    bootstrap.Modal.getInstance(confirmModal).hide();
});

confirmModal.addEventListener("hidden.bs.modal", function () {
    selectedApplication = null;

    if (confirmedApplicationId) {
        document.getElementById("confirmationNotice").focus();
    }
});

filterApplications();


// سلوك القائمة الجانبية مكرر داخل ملف كل صفحة، دون ملف مشترك
function setupStudentSidebar() {
    const sidebar = document.getElementById("studentSidebar");
    const main = document.getElementById("studentMain");
    const openButton = document.getElementById("openSidebar");
    const closeButton = document.getElementById("closeSidebar");
    const backdrop = document.getElementById("sidebarBackdrop");
    const mobileScreen = window.matchMedia("(max-width: 991.98px)");

    function setSidebarOpen(open) {
        const isOpen = open && mobileScreen.matches;
        document.body.classList.toggle("sidebar-open", isOpen);
        openButton.setAttribute("aria-expanded", String(isOpen));
        main.inert = isOpen;
        sidebar.inert = mobileScreen.matches && !isOpen;

        if (isOpen) {
            closeButton.focus();
        }
    }

    function closeSidebar() {
        setSidebarOpen(false);
        openButton.focus();
    }

    openButton.addEventListener("click", function () { setSidebarOpen(true); });
    closeButton.addEventListener("click", closeSidebar);
    backdrop.addEventListener("click", closeSidebar);
    mobileScreen.addEventListener("change", function () { setSidebarOpen(false); });

    // إبقاء تنقل لوحة المفاتيح داخل القائمة المفتوحة وإغلاقها بزر Escape
    sidebar.addEventListener("keydown", function (event) {
        if (!mobileScreen.matches || !document.body.classList.contains("sidebar-open")) {
            return;
        }

        if (event.key === "Escape") {
            event.preventDefault();
            closeSidebar();
        }

        if (event.key === "Tab") {
            const links = sidebar.querySelectorAll('a[href], button');
            const first = links[0];
            const last = links[links.length - 1];

            if (event.shiftKey && document.activeElement === first) {
                event.preventDefault();
                last.focus();
            } else if (!event.shiftKey && document.activeElement === last) {
                event.preventDefault();
                first.focus();
            }
        }
    });

    setSidebarOpen(false);
}

setupStudentSidebar();


// البحث العلوي ينتقل لصفحة الفرص للطالب أو يختار الطالب المطابق لدى المنسق
const navbarSearch = document.getElementById("navbarSearch");
navbarSearch.addEventListener("keydown", function (event) {
    if (event.key !== "Enter") { return; }
    event.preventDefault();
    const search = navbarSearch.value.trim();
    window.location.href = "ExploreOpportunities.aspx?search=" + encodeURIComponent(search);
});

document.getElementById("navbarNotifications").addEventListener("click", function () {
    const notice = document.getElementById("navbarNotice");
    notice.textContent = "Notifications preview — live notifications are not connected yet.";
    notice.hidden = !notice.hidden;
});
