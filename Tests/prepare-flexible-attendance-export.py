import copy, json
from pathlib import Path

source = Path('C:/Users/user/Desktop/just-project-4af15-default-rtdb-export (5).json')
data = json.loads(source.read_text(encoding='utf-8-sig'))
original = copy.deepcopy(data)
student_id = '2023005'
matches = [(key, value) for key, value in data.get('placements', {}).items() if value.get('studentId') == student_id and value.get('status') in ('active', 'confirmed')]
if len(matches) != 1:
    raise ValueError('Expected exactly one active placement for the authorized student.')
placement_id, placement = matches[0]
uid = placement['studentUid']
profile = data['users'][uid]
assert profile.get('studentId') == student_id and profile.get('isActive') is True and profile.get('role') == 'student'
attendance = data.setdefault('uimsAttendance', {})
company_id, supervisor_id = placement['companyId'], placement.get('companySupervisorId') or placement['supervisorId']
company = attendance.get('companies', {}).get(company_id, {})
supervisor = attendance.get('companySupervisors', {}).get(supervisor_id, {})
assert company.get('status') == 'active' and supervisor.get('status') == 'active' and supervisor.get('companyId') == company_id, 'Existing attendance company/supervisor must match.'
placement['startDate'], placement['endDate'] = '2026-10-25', '2026-12-20'
placement['companySupervisorId'] = supervisor_id
hours = placement['requiredHours']
assert isinstance(hours, (int, float)) and hours > 0
student = attendance.setdefault('students', {}).get(student_id, {})
assert not student or student.get('firebaseUid') == uid, 'An existing different student identity must not be replaced.'
assignment_id = student.get('activeAssignmentId') or placement_id
assert assignment_id == placement_id, 'A different current attendance assignment requires explicit reconciliation.'
assignments = attendance.setdefault('trainingAssignments', {})
assignment = assignments.get(assignment_id, {})
for key, expected in [('studentId', student_id), ('placementId', placement_id), ('companyId', company_id), ('companySupervisorId', supervisor_id)]:
    assert key not in assignment or assignment[key] == expected, 'Existing assignment conflict: ' + key
assignment.update(assignmentId=assignment_id, studentId=student_id, placementId=placement_id, companyId=company_id, companySupervisorId=supervisor_id, academicSupervisorId=placement['academicSupervisorId'], status='active', startDate='2026-10-25', endDate='2026-12-20', requiredHours=hours, isTestData=False)
assignments[assignment_id] = assignment
name = profile.get('fullName') or profile.get('displayName') or student_id
student.update(studentId=student_id, studentNumber=student_id, firebaseUid=uid, activeAssignmentId=assignment_id, status='active', name=name, fullName=name, email=profile.get('email', ''), isTestData=False)
attendance['students'][student_id] = student
placement['trainingAssignmentId'] = assignment_id
claims = attendance.setdefault('provisioningClaims', {})
expected_claim = dict(assignmentId=assignment_id, studentId=student_id, firebaseUid=uid, placementId=placement_id)
claim = claims.get(placement_id, {})
assert all(key not in claim or claim[key] == value for key, value in expected_claim.items()), 'Existing provisioning claim conflicts.'
claims[placement_id] = {**claim, **expected_claim}

# Exact preservation checks for all unrelated records, including every attendance event and calendar.
allowed = {('placements', placement_id), ('uimsAttendance', 'students', student_id), ('uimsAttendance', 'trainingAssignments', assignment_id), ('uimsAttendance', 'provisioningClaims', placement_id)}
def changes(before, after, path=()):
    if before == after:
        return []
    if path in allowed:
        return [path]
    if isinstance(before, dict) and isinstance(after, dict):
        found=[]
        for key in before.keys() | after.keys():
            if key not in before and any(item[:len(path)+1] == path+(key,) for item in allowed):
                found += changes({}, after[key], path+(key,))
            elif key not in before or key not in after:
                raise AssertionError('Unexpected added/deleted record: ' + '/'.join(path+(key,)))
            else:
                found += changes(before[key], after[key], path+(key,))
        return found
    raise AssertionError('Unexpected modified value: ' + '/'.join(path))
modified=changes(original, data)
folder=Path('Tests/Reports/FlexibleAttendance-2023005'); folder.mkdir(parents=True, exist_ok=True)
output=folder/'just-project-4af15-flexible-attendance-final.json'
output.write_text(json.dumps(data, ensure_ascii=False, indent=2)+'\n',encoding='utf-8')
assert json.loads(output.read_text(encoding='utf-8')) == data
print('Validated complete export; only these authorized records changed:')
for record in sorted(modified):
    print('/'.join(record))
print('Existing attendance, hours, grades, tasks, reports, accounts and schedules preserved.')
print('Output:', output.resolve())
