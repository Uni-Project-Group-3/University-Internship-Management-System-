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
