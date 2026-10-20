"use strict";

// أمثلة افتراضية فقط، ولا تمثل معايير الجامعة أو تقييمات طلاب حقيقيين
const sampleReviews = {
    "sample-ready": {
        name: "Ahmad Ali",
        number: "20230001",
        major: "Software Engineering",
        company: "North Tech",
        internship: "Web Development Intern",
        period: "23 Aug 2026 – 19 Oct 2026",
        companySupervisor: "Omar Khaled",
        academicSupervisor: "Dr. Lina Ahmad",
        approvedHours: 240,
        requiredHours: 240,
        reportsComplete: true,
        tasksComplete: true,
        companyEvaluation: {
            submitted: true,
            date: "20 Oct 2026",
            recommendation: "Recommend completion",
            feedback: "Completed the assigned work, responded to feedback, and demonstrated steady development in teamwork and web development."
        },
        academicEvaluation: {
            submitted: true,
            date: "20 Oct 2026",
            recommendation: "Recommend completion",
            feedback: "Reviewed the company evaluation, submitted work, and training progress. The sample completion requirements have been met."
        }
    },
    "sample-incomplete": {
        name: "Sara Omar",
        number: "20230002",
        major: "Computer Science",
        company: "Data Lab",
        internship: "Data Analysis Intern",
        period: "01 Sep 2026 – 09 Nov 2026 (planned)",
        companySupervisor: "Rami Hasan",
        academicSupervisor: "Dr. Hala Sami",
        approvedHours: 200,
        requiredHours: 240,
        reportsComplete: false,
        tasksComplete: true,
        companyEvaluation: {
            submitted: true,
            date: "19 Oct 2026",
            recommendation: "Complete outstanding requirements",
            feedback: "Assigned tasks are complete. Additional approved training hours and the required report are still outstanding."
        },
        academicEvaluation: {
            submitted: false,
            date: "Not submitted",
            recommendation: "Awaiting academic evaluation",
            feedback: "The final academic evaluation has not yet been sent to the coordinator."
        }
    }
};

const studentSelect = document.getElementById("studentSelect");
const acknowledgment = document.getElementById("reviewAcknowledgment");
const approveButton = document.getElementById("approveButton");
const approvalModal = document.getElementById("approvalModal");
const approvedStudents = new Set();
const approvalDates = new Map();
const report = document.getElementById("completionReport");
let studentInConfirmation = null;

function getSelectedReview() {
    return sampleReviews[studentSelect.value];
}

function requirementsAreMet(student) {
    return student.companyEvaluation.submitted && student.academicEvaluation.submitted &&
        student.approvedHours >= student.requiredHours && student.reportsComplete && student.tasksComplete;
}

function displayEvaluation(prefix, evaluation) {
    const status = document.getElementById(prefix + "Status");
    status.textContent = evaluation.submitted ? "Submitted" : "Awaiting submission";
    status.className = "status-badge " + (evaluation.submitted ? "status-success" : "status-pending");
    document.getElementById(prefix + "Date").textContent = evaluation.date;
    document.getElementById(prefix + "Recommendation").textContent = evaluation.recommendation;
    document.getElementById(prefix + "Feedback").textContent = evaluation.feedback;
}

function updateDecision() {
    const student = getSelectedReview();
    const isApproved = approvedStudents.has(studentSelect.value);
    const isEligible = requirementsAreMet(student);
    const status = document.getElementById("reviewStatus");

    status.textContent = isApproved ? "Approved · Preview" : (isEligible ? "Awaiting review" : "Requirements incomplete");
    status.className = "status-badge " + (isApproved ? "status-success" : "status-pending");
    approveButton.disabled = isApproved || !isEligible || !acknowledgment.checked;
    approveButton.textContent = isApproved ? "Completion approved (preview)" : "Approve completion →";
    acknowledgment.disabled = isApproved || !isEligible;
    document.getElementById("reviewNotes").disabled = isApproved;
    document.getElementById("generateReport").disabled = !isApproved;
    document.getElementById("eligibilityMessage").textContent = isApproved
        ? "Approved in this preview. No approval or notes have been saved."
        : (isEligible ? "Sample requirements are met. Review both evaluations and check the confirmation below to continue."
            : "Approval is unavailable until both evaluations and all completion requirements are complete.");
}

function displayStudent() {
    const student = getSelectedReview();
    document.getElementById("studentName").textContent = student.name;
    document.getElementById("studentNumber").textContent = student.number;
    document.getElementById("studentMajor").textContent = student.major;
    document.getElementById("companyName").textContent = student.company;
    document.getElementById("internshipTitle").textContent = student.internship;
    document.getElementById("companySupervisor").textContent = student.companySupervisor;
    document.getElementById("academicSupervisor").textContent = student.academicSupervisor;
    displayEvaluation("company", student.companyEvaluation);
    displayEvaluation("academic", student.academicEvaluation);

    document.getElementById("hoursText").textContent = student.approvedHours + " / " + student.requiredHours + " hours";
    const progress = document.getElementById("hoursProgress");
    progress.max = student.requiredHours;
    progress.value = student.approvedHours;
    progress.textContent = student.approvedHours + " of " + student.requiredHours + " hours";

    const requirements = [
        { label: "Company final evaluation received", met: student.companyEvaluation.submitted },
        { label: "Final academic evaluation received", met: student.academicEvaluation.submitted },
        { label: "Required approved hours completed", met: student.approvedHours >= student.requiredHours },
        { label: "Required reports completed", met: student.reportsComplete },
        { label: "Required tasks completed", met: student.tasksComplete }
    ];

    const list = document.getElementById("requirementsList");
    list.replaceChildren();

    requirements.forEach(function (requirement) {
        const item = document.createElement("li");
        const label = document.createElement("span");
        const result = document.createElement("span");
        label.textContent = requirement.label;
        result.textContent = requirement.met ? "Met" : "Outstanding";
        result.className = requirement.met ? "requirement-met" : "requirement-missing";
        item.append(label, result);
        list.append(item);
    });

    // لا تنتقل الملاحظات أو موافقة المراجعة من طالب إلى آخر
    acknowledgment.checked = approvedStudents.has(studentSelect.value);
    document.getElementById("reviewNotes").value = "";
    document.getElementById("approvalNotice").hidden = true;
    report.hidden = true;
    updateDecision();
}

studentSelect.addEventListener("change", displayStudent);
acknowledgment.addEventListener("change", updateDecision);

approvalModal.addEventListener("show.bs.modal", function (event) {
    if (approveButton.disabled) {
        event.preventDefault();
        return;
    }

    studentInConfirmation = studentSelect.value;
    document.getElementById("approvalSummary").textContent = "Reviewing completion for " + getSelectedReview().name + " (" + getSelectedReview().number + ").";
});

// نعيد فحص الأهلية قبل التأكيد، ولا نعتمد على حالة الزر وحدها
document.getElementById("confirmApproval").addEventListener("click", function () {
    if (studentInConfirmation !== studentSelect.value || approvedStudents.has(studentSelect.value) ||
        !requirementsAreMet(getSelectedReview()) || !acknowledgment.checked) {
        return;
    }

    approvedStudents.add(studentSelect.value);
    approvalDates.set(studentSelect.value, new Date().toLocaleDateString("en-GB", {
        timeZone: "Asia/Amman", day: "2-digit", month: "short", year: "numeric"
    }));
    updateDecision();
    const notice = document.getElementById("approvalNotice");
    notice.textContent = "Preview: completion approved for " + getSelectedReview().name + ". You can now preview and print the sample completion report. Nothing was saved or sent. This approval resets when you refresh.";
    notice.hidden = false;
    bootstrap.Modal.getInstance(approvalModal).hide();
});

approvalModal.addEventListener("hidden.bs.modal", function () {
    studentInConfirmation = null;

    if (!document.getElementById("approvalNotice").hidden) {
        document.getElementById("approvalNotice").focus();
    }
});

// المعاينة لا ترسل أي بيانات ولا تستخدم الحفظ المحلي
document.getElementById("reviewForm").addEventListener("submit", function (event) {
    event.preventDefault();
});

// بيانات التقرير محددة صراحة حتى لا تتسرب التقييمات السرية أو الملاحظات
function populateReport() {
    const student = getSelectedReview();
    document.getElementById("reportName").textContent = student.name;
    document.getElementById("reportNumber").textContent = student.number;
    document.getElementById("reportMajor").textContent = student.major;
    document.getElementById("reportCompany").textContent = student.company;
    document.getElementById("reportInternship").textContent = student.internship;
    document.getElementById("reportPeriod").textContent = student.period;
    document.getElementById("reportHours").textContent = student.approvedHours + " hours";
    document.getElementById("reportRequiredHours").textContent = student.requiredHours + " hours";
    document.getElementById("reportApprovalDate").textContent = approvalDates.get(studentSelect.value);
}

document.getElementById("generateReport").addEventListener("click", function () {
    if (!approvedStudents.has(studentSelect.value) || !requirementsAreMet(getSelectedReview())) {
        return;
    }

    populateReport();
    report.hidden = false;
    report.scrollIntoView({ block: "start" });
    report.focus({ preventScroll: true });
});

document.getElementById("printReport").addEventListener("click", function () {
    if (report.hidden || !approvedStudents.has(studentSelect.value) || !requirementsAreMet(getSelectedReview())) {
        return;
    }

    populateReport();
    window.print();
});

document.getElementById("closeReport").addEventListener("click", function () {
    report.hidden = true;
    document.getElementById("generateReport").focus();
});

displayStudent();
