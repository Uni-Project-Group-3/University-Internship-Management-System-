import copy
import unittest
from ExamRepair import plan, apply_to_copy, normalize


class ExamRepairTests(unittest.TestCase):
    def fixture(self):
        keys = {"Q1": {"correctOptionId": "B", "points": 2}}
        definition = {"questions": {"Q1": {"type": "multipleChoice", "points": 2, "options": [{"id": "A"}, {"id": "B"}]}}}
        return {"taskExams": {"E1": {"status": "published", "publishedVersion": 1, "versions": {"1": definition}}}, "taskExamAnswerKeys": {"E1": [None, keys]}, "taskExamAttempts": {"E1": {"S1": {"A1": {"score": 2}}}}}

    def test_normalization_preserves_answers_history_and_original(self):
        source = self.fixture()
        original = copy.deepcopy(source)
        report, patches = plan(source)
        self.assertEqual(report["patchCount"], 1)
        repaired = apply_to_copy(source, patches)
        self.assertEqual(source, original)
        self.assertEqual(repaired["taskExamAnswerKeys"]["E1"]["1"], original["taskExamAnswerKeys"]["E1"][1])
        self.assertEqual(repaired["taskExamAttempts"], original["taskExamAttempts"])
        self.assertEqual(apply_to_copy(repaired, patches), repaired)
        self.assertEqual(plan(repaired)[1], [])

    def test_no_invented_or_overwritten_keys(self):
        source = self.fixture()
        source["taskExamAnswerKeys"] = {}
        report, patches = plan(source)
        self.assertFalse(report["allCompatible"])
        self.assertEqual(patches, [])
        supplied = {"E1": {"1": {"Q1": {"points": 2, "correctOptionId": "B"}}}}
        report, patches = plan(source, supplied)
        self.assertTrue(report["allCompatible"])
        repaired = apply_to_copy(source, patches)
        self.assertEqual(repaired["taskExamAnswerKeys"]["E1"]["1"], supplied["E1"]["1"])
        source["taskExamAnswerKeys"]["E1"] = "malformed-original"
        self.assertEqual(plan(source, supplied)[1], [])

    def test_stale_source_rejected(self):
        source = self.fixture()
        patches = plan(source)[1]
        source["taskExamAnswerKeys"]["E1"] = {"changed": True}
        with self.assertRaises(ValueError):
            apply_to_copy(source, patches)

    def test_draft_map_and_invalid_source(self):
        keys = {"Q1": {"correctAnswer": False, "points": 1}}
        self.assertEqual(normalize({"draftVersions": [None, keys]})["draftVersions"]["1"], keys)
        source = self.fixture()
        source["taskExamAnswerKeys"] = {}
        supplied = {"E1": {"1": {"Q1": {"points": 2, "correctOptionId": "INVENTED"}}}}
        self.assertEqual(plan(source, supplied)[1], [])


if __name__ == "__main__":
    unittest.main()
