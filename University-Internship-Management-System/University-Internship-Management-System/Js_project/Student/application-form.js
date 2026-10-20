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
