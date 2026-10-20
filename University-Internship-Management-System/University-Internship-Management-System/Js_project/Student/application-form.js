"use strict";

// بيانات مؤقتة لنفس الفرص المعروضة في صفحة الاستكشاف
const sampleOpportunities = {
    "sample-web": {
        title: "Web Development Intern",
        company: "North Tech",
        location: "Amman · On-site",
        major: "Software Engineering",
        duration: "8 weeks"
    },
    "sample-data": {
        title: "Data Analysis Intern",
        company: "Data Lab",
        location: "Irbid · On-site",
        major: "Computer Science",
        duration: "10 weeks"
    },
    "sample-security": {
        title: "Security Operations Intern",
        company: "Secure Path",
        location: "Remote",
        major: "Cybersecurity",
        duration: "8 weeks"
    }
};

const applicationForm = document.getElementById("applicationForm");
const applicationFields = document.getElementById("applicationFields");
const motivation = document.getElementById("motivation");
const previewResult = document.getElementById("previewResult");
const opportunityId = new URLSearchParams(window.location.search).get("opportunity");

// نسمح فقط بمعرف موجود في البيانات، ولا نعرض نصوص الرابط مباشرة
const isValidOpportunity = Object.prototype.hasOwnProperty.call(sampleOpportunities, opportunityId);
const selectedOpportunity = isValidOpportunity ? sampleOpportunities[opportunityId] : null;

if (selectedOpportunity) {
    document.getElementById("opportunityHeading").textContent = selectedOpportunity.title;
    document.getElementById("companyName").textContent = selectedOpportunity.company;
    document.getElementById("opportunityLocation").textContent = selectedOpportunity.location;
    document.getElementById("opportunityMajor").textContent = selectedOpportunity.major;
    document.getElementById("opportunityDuration").textContent = selectedOpportunity.duration;
    document.getElementById("major").value = selectedOpportunity.major;
    applicationFields.disabled = false;
} else {
    document.getElementById("invalidOpportunity").hidden = false;
}

motivation.addEventListener("input", function () {
    document.getElementById("characterCount").textContent = motivation.value.length + " / 1000";
});

// منع قبول الحقول النصية التي تحتوي على فراغات فقط
const requiredTextFields = applicationForm.querySelectorAll('input[type="text"], input[type="tel"]');

requiredTextFields.forEach(function (field) {
    field.addEventListener("input", function () {
        field.setCustomValidity(field.value.trim() === "" ? "Please enter a value." : "");
    });
});

// هذه معاينة فقط: لا نحفظ البيانات ولا نرسل طلباً للشركة
applicationForm.addEventListener("submit", function (event) {
    event.preventDefault();

    if (!selectedOpportunity || !applicationForm.reportValidity()) {
        return;
    }

    const fullName = document.getElementById("fullName").value.trim();
    document.getElementById("previewSummary").textContent = fullName + ", your details for " + selectedOpportunity.title + " at " + selectedOpportunity.company + " passed the form checks.";
    previewResult.hidden = false;
    previewResult.focus();
});

document.getElementById("editApplication").addEventListener("click", function () {
    previewResult.hidden = true;
    document.getElementById("fullName").focus();
});

// إخفاء نتيجة المعاينة القديمة عندما يعدل الطالب البيانات
applicationForm.addEventListener("input", function () {
    previewResult.hidden = true;
});


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
