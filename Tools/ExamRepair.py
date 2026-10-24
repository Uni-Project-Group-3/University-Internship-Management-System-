"""Offline exam repair planner. No network, credentials, database writes, or inferred answers."""
import argparse
import copy
import hashlib
import json
import math
from pathlib import Path


def digest(value):
    return hashlib.sha256(json.dumps(value, sort_keys=True, separators=(",", ":"), ensure_ascii=False).encode()).hexdigest()


def key_map(value):
    if isinstance(value, dict):
        return copy.deepcopy(value)
    if isinstance(value, list):
        return {str(i): copy.deepcopy(v) for i, v in enumerate(value) if v is not None}
    return None


def normalize(value):
    result = key_map(value)
    if result is not None and isinstance(result.get("draftVersions"), list):
        result["draftVersions"] = key_map(result["draftVersions"])
    return result


def validate_keys(definition, keys):
    questions = definition.get("questions")
    if not isinstance(questions, dict) or not questions or not isinstance(keys, dict) or set(keys) != set(questions):
        return False
    for qid, question in questions.items():
        answer = keys[qid]
        if not isinstance(question, dict) or not isinstance(answer, dict):
            return False
        points = question.get("points")
        if type(points) not in (int, float) or not math.isfinite(points) or points <= 0 or type(answer.get("points")) not in (int, float) or answer["points"] != points:
            return False
        if question.get("type") == "multipleChoice":
            options = question.get("options")
            options = list(options.values()) if isinstance(options, dict) else options
            if not isinstance(options, list) or not isinstance(answer.get("correctOptionId"), str) or answer["correctOptionId"] not in [o.get("id") for o in options if isinstance(o, dict)]:
                return False
        elif question.get("type") == "trueFalse":
            if type(answer.get("correctAnswer")) is not bool:
                return False
        else:
            return False
    return True


def plan(snapshot, trusted=None):
    trusted = trusted or {}
    report = {"dryRun": True, "networkRequests": 0, "productionWrites": 0, "records": [], "patchCount": 0, "securityFindings": [], "deployedRulesVerified": False}
    patches = []
    exams = snapshot.get("taskExams", {})
    private = snapshot.get("taskExamAnswerKeys", {})
    if not isinstance(exams, dict) or not isinstance(private, dict):
        raise ValueError("Export must contain keyed taskExams and taskExamAnswerKeys collections.")
    for exam_id, exam in exams.items():
        if not isinstance(exam, dict):
            report["records"].append({"examId": exam_id, "blocker": "Exam must be an object; manual provenance review required."})
            continue
        original = private.get(exam_id)
        candidate = normalize(original)
        row = {"examId": exam_id, "path": "taskExamAnswerKeys/" + exam_id, "issues": [], "actions": [], "cause": "Provenance unverified; inspect trusted audit/export history."}
        if isinstance(original, list) or isinstance(original, dict) and isinstance(original.get("draftVersions"), list):
            row["cause"] = "Numeric revision map encoded as an array; read compatibility requires no production migration."
            row["actions"].append("normalizeNumericMapInOfflineCopy")
        if candidate is None and original is not None:
            row["issues"].append("Malformed private root; no automatic replacement of scalar records.")
            report["records"].append(row)
            continue
        candidate = candidate or {}
        definitions = []
        draft = exam.get("draft")
        if isinstance(draft, dict):
            definitions.append(("draftVersions/" + str(exam.get("draftRevision", "")), draft))
        versions = key_map(exam.get("versions")) or {}
        definitions.extend((version, definition) for version, definition in versions.items() if isinstance(definition, dict))
        if exam.get("status") == "published" and str(exam.get("publishedVersion", "")) not in versions:
            row["issues"].append("Published version is missing; requires trusted source review.")
        if not definitions:
            row["issues"].append("No exam definitions found; cannot approve repair.")
        for relative, definition in definitions:
            parts = relative.split("/")
            container = candidate
            for part in parts[:-1]:
                value = container.get(part)
                if value is None:
                    container[part] = {}
                if not isinstance(container[part], dict):
                    container = None
                    break
                container = container[part]
            current = container.get(parts[-1]) if container is not None else None
            if current is not None:
                if not validate_keys(definition, current):
                    row["issues"].append(relative + ": existing keys invalid; never overwritten.")
                continue
            supplied = trusted.get(exam_id, {}).get(relative)
            if container is not None and supplied is not None and validate_keys(definition, supplied):
                container[parts[-1]] = copy.deepcopy(supplied)
                row["actions"].append("restoreFromAdministratorSuppliedSource:" + relative)
            else:
                row["issues"].append(relative + ": missing valid key revision; administrator must supply original verified answers.")
        # Do not produce a partial repair for an exam with unresolved blockers.
        if not row["issues"] and candidate != original:
            patches.append({"path": row["path"], "expectedSha256": digest(original), "original": copy.deepcopy(original), "replacement": candidate})
        report["records"].append(row)
    report["patchCount"] = len(patches)
    attempts = snapshot.get("taskExamAttempts", {})
    for exam_id, students in (attempts if isinstance(attempts, dict) else {}).items():
        for student_id, records in (students if isinstance(students, dict) else {}).items():
            for attempt_id, attempt in (records if isinstance(records, dict) else {}).items():
                if isinstance(attempt, dict) and attempt.get("resultStatus") != "released" and ("score" in attempt or "maximumScore" in attempt):
                    report["securityFindings"].append({"path": "taskExamAttempts/" + exam_id + "/" + student_id + "/" + attempt_id, "issue": "Legacy unreleased raw grade; deployed direct-client access must be denied. No grade was changed."})
    report["securityReviewRequired"] = bool(report["securityFindings"])
    report["allCompatible"] = all(not row.get("issues") and not row.get("blocker") for row in report["records"])
    return report, patches


def apply_to_copy(snapshot, patches):
    result = copy.deepcopy(snapshot)
    for patch in patches:
        parts = patch["path"].split("/")
        if len(parts) != 2 or parts[0] != "taskExamAnswerKeys":
            raise ValueError("Repair patch is outside private exam keys.")
        current = result.setdefault(parts[0], {}).get(parts[1])
        if current == patch["replacement"]:
            continue
        if digest(current) != patch["expectedSha256"]:
            raise ValueError("Source changed; regenerate the dry run. Nothing was applied to production.")
        result[parts[0]][parts[1]] = copy.deepcopy(patch["replacement"])
    return result


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("export", type=Path)
    parser.add_argument("--trusted-keys", type=Path, help="Administrator-verified original keys by exam ID then revision path; never guessed.")
    parser.add_argument("--private-package", type=Path, help="Optional sensitive offline package containing originals, patches and repaired export.")
    parser.add_argument("--approve-offline-copy", action="store_true")
    args = parser.parse_args()
    snapshot = json.loads(args.export.read_text(encoding="utf-8-sig"))
    trusted = json.loads(args.trusted_keys.read_text(encoding="utf-8-sig")) if args.trusted_keys else None
    report, patches = plan(snapshot, trusted)
    print(json.dumps(report, indent=2))  # No answer keys, question text, or grades.
    if args.private_package:
        if not args.approve_offline_copy:
            parser.error("Private package requires --approve-offline-copy; the default is dry-run only.")
        with args.private_package.open("x", encoding="utf-8") as output:
            json.dump({"report": report, "patches": patches, "repairedExport": apply_to_copy(snapshot, patches)}, output, indent=2)


if __name__ == "__main__":
    main()
