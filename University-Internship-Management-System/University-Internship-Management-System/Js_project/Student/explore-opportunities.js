"use strict";

const searchInput = document.getElementById("searchInput");
const majorFilter = document.getElementById("majorFilter");
const locationFilter = document.getElementById("locationFilter");
const resetButton = document.getElementById("resetFilters");
const opportunities = document.querySelectorAll(".opportunity-item");

// نعرض البطاقة فقط عندما تطابق البحث وجميع الفلاتر المختارة
function filterOpportunities() {
    const searchText = searchInput.value.trim().toLowerCase();
    let visibleCount = 0;

    opportunities.forEach(function (opportunity) {
        const title = opportunity.querySelector("h3").textContent.toLowerCase();
        const company = opportunity.querySelector(".company-name").textContent.toLowerCase();
        const matchesSearch = title.includes(searchText) || company.includes(searchText);
        const matchesMajor = majorFilter.value === "" || opportunity.dataset.major === majorFilter.value;
        const matchesLocation = locationFilter.value === "" || opportunity.dataset.location === locationFilter.value;

        opportunity.hidden = !(matchesSearch && matchesMajor && matchesLocation);

        if (!opportunity.hidden) {
            visibleCount++;
        }
    });

    document.getElementById("resultCount").textContent = "(" + visibleCount + ")";
    document.getElementById("emptyState").hidden = visibleCount > 0;
}

searchInput.addEventListener("input", filterOpportunities);
majorFilter.addEventListener("change", filterOpportunities);
locationFilter.addEventListener("change", filterOpportunities);

resetButton.addEventListener("click", function () {
    searchInput.value = "";
    majorFilter.value = "";
    locationFilter.value = "";
    filterOpportunities();
    searchInput.focus();
});

// منع إعادة تحميل صفحة Web Forms عند الضغط على Enter داخل البحث
searchInput.addEventListener("keydown", function (event) {
    if (event.key === "Enter") {
        event.preventDefault();
        filterOpportunities();
    }
});

// نافذة واحدة تعرض تفاصيل البطاقة التي اختارها الطالب
document.getElementById("opportunityModal").addEventListener("show.bs.modal", function (event) {
    const opportunity = event.relatedTarget.closest(".opportunity-item");
    document.getElementById("applyLink").href = "ApplicationForm.aspx?opportunity=" + encodeURIComponent(opportunity.dataset.id);

    document.getElementById("modalTitle").textContent = opportunity.querySelector("h3").textContent;
    document.getElementById("modalCompany").textContent = opportunity.querySelector(".company-name").textContent;
    document.getElementById("modalMajor").textContent = opportunity.dataset.major;
    document.getElementById("modalLocation").textContent = opportunity.querySelector(".company-location").textContent;

    const facts = opportunity.querySelector(".opportunity-facts").cloneNode(true);
    document.getElementById("modalFacts").replaceChildren(facts);

    const details = opportunity.querySelector(".opportunity-details").cloneNode(true);
    details.hidden = false;
    document.getElementById("modalDetails").replaceChildren(details);
});

filterOpportunities();


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

const initialSearch = new URLSearchParams(window.location.search).get("search");
if (initialSearch) {
    searchInput.value = initialSearch;
    navbarSearch.value = initialSearch;
    filterOpportunities();
}

document.getElementById("navbarNotifications").addEventListener("click", function () {
    const notice = document.getElementById("navbarNotice");
    notice.textContent = "Notifications preview — live notifications are not connected yet.";
    notice.hidden = !notice.hidden;
});
