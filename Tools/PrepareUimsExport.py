"""Prepare a separate UIMS export and audit. Offline only; never invent identities."""
import argparse
import copy
import hashlib
import json
import re
from collections import defaultdict
from pathlib import Path

FROZEN = {"taskExamAnswerKeys", "taskExamAttempts", "taskExams", "academicAccountOperations", "academicAssignmentOperations", "companyAccountOperations", "studentAccountOperations", "activity", "history", "audit", "instructionVersions", "submissions", "revisions", "notifications", "messages", "academicFeedback", "completionReviews", "finalEvaluationReviewsV2", "placements", "applications", "opportunities", "assignments", "supervisorAssignments", "placementClaims", "groups", "uimsAttendance", "fileAttachments", "weeklyReports", "taskBoards"}
DISPLAY = {"displayName", "fullName", "name", "studentName", "companyName", "collegeName", "universityName", "description", "instructions", "address"}


def prepare(original):
    data = copy.deepcopy(original)
    report = {"changes": [], "unresolved": [], "sourceSha256": hashlib.sha256(json.dumps(original, sort_keys=True, ensure_ascii=False).encode()).hexdigest(), "productionWrites": 0}
    users = original.get("users", {})
    students = defaultdict(list)
    for uid, user in users.items():
        if isinstance(user, dict) and user.get("role") == "student" and user.get("studentId"):
            students[str(user["studentId"])].append(uid)

    def change(node, key, replacement, path, reason):
        if node.get(key) != replacement:
            report["changes"].append({"path": path + "/" + key, "reason": reason})
            node[key] = replacement

    def walk(node, path=""):
        if isinstance(node, dict):
            sid = str(node.get("studentId", ""))
            if sid == "2023301" or "2023301" in path:
                return
            for key, value in list(node.items()):
                if key in FROZEN:
                    continue
                target = path + "/" + key
                if key in DISPLAY and isinstance(value, str):
                    cleaned = re.sub(r"\s*\(\s*demo\s*\)", "", value, flags=re.I)
                    cleaned = re.sub(r"\b(?:demo|fictional)\b\s*", "", cleaned, flags=re.I).strip()
                    cleaned = re.sub(r"[ \t]{2,}", " ", cleaned)
                    if cleaned:
                        change(node, key, cleaned, path, "Removed demo/fictional presentation labels; retained existing name/text")
                    if key in {"displayName", "fullName", "name"} and (not cleaned or cleaned.lower() in {"company supervisor", "academic supervisor", "student", "test", "tester"}):
                        report["unresolved"].append({"path": target, "reason": "A real verified name is required; no name was invented"})
                if key == "College " and "College" not in node:
                    node["College"] = value
                    del node[key]
                    report["changes"].append({"path": target, "reason": "Normalized trailing-space field name to College"})
                else:
                    walk(node.get(key), target)
        elif isinstance(node, list):
            for i, value in enumerate(node):
                walk(value, path + "/" + str(i))

    protected_student = copy.deepcopy(original.get("universityRecords", {}).get("students", {}).get("2023301"))
    walk(data)
    if protected_student is not None:
        data["universityRecords"]["students"]["2023301"] = protected_student
        report["changes"] = [c for c in report["changes"] if not c["path"].startswith("/universityRecords/students/2023301/")]
    for uid, user in users.items():
        if str(user.get("studentId", "")) == "2023301":
            data["users"][uid] = copy.deepcopy(user)
            report["changes"] = [c for c in report["changes"] if not c["path"].startswith("/users/" + uid + "/")]
    placements = data.get("placements", {})
    tasks = data.get("tasks", {})
    for pid, placement in placements.items():
        sid = str(placement.get("studentId", ""))
        if len(students[sid]) != 1:
            report["unresolved"].append({"path": "/placements/" + pid, "reason": "Missing or ambiguous student account", "studentId": sid})
    for tid, task in tasks.items():
        placement = placements.get(task.get("placementId"))
        if not placement or str(task.get("studentId")) != str(placement.get("studentId")):
            report["unresolved"].append({"path": "/tasks/" + tid, "reason": "Task/placement/student relation requires manual review"})
    for eid, exam in data.get("taskExams", {}).items():
        task = tasks.get(exam.get("taskId"))
        if not task or task.get("placementId") != exam.get("placementId"):
            report["unresolved"].append({"path": "/taskExams/" + eid, "reason": "Exam task/placement relation requires manual review"})
    for name in ["taskExamAnswerKeys", "taskExamAttempts", "taskExams", "fileAttachments", "weeklyReports", "uimsAttendance"]:
        # Immutable workflows and Storage references must remain byte-for-byte equivalent as JSON.
        if name in {"fileAttachments", "weeklyReports", "uimsAttendance"}:
            if name in original:
                data[name] = copy.deepcopy(original[name])
            report["changes"] = [c for c in report["changes"] if not c["path"].startswith("/" + name + "/")]
        assert data.get(name) == original.get(name)
    for uid, user in users.items():
        for key in ("email", "studentId", "role", "companySupervisorId", "academicSupervisorId", "companyId"):
            assert data["users"][uid].get(key) == user.get(key)
    assert set(data) == set(original)
    report["counts"] = {key: len(value) for key, value in data.items() if isinstance(value, (dict, list))}
    report["changeCount"] = len(report["changes"])
    report["unresolvedCount"] = len(report["unresolved"])
    report["readyForProductionReplacement"] = False
    return data, report


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("source", type=Path)
    parser.add_argument("destination", type=Path)
    parser.add_argument("report", type=Path)
    args = parser.parse_args()
    original = json.loads(args.source.read_text(encoding="utf-8-sig"))
    data, report = prepare(original)
    args.destination.parent.mkdir(parents=True, exist_ok=True)
    for path, payload in [(args.destination, data), (args.report, report)]:
        with path.open("x", encoding="utf-8") as stream:
            json.dump(payload, stream, ensure_ascii=False, indent=2)
    print(json.dumps({key: report[key] for key in ["changeCount", "unresolvedCount", "productionWrites", "readyForProductionReplacement"]}))


if __name__ == "__main__":
    main()
