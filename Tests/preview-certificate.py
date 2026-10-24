import os, subprocess, shutil
from pathlib import Path

root = Path(__file__).resolve().parent.parent
preview = root / 'Tests' / 'Reports' / 'certificate-preview.pdf'
preview.parent.mkdir(exist_ok=True)
env = os.environ.copy()
env['UIMS_CERTIFICATE_PREVIEW'] = str(preview)
logo = root / 'Tests/FinalEvaluation/bin/Assets/shared/images/JUST-Logo-01.png'
logo.parent.mkdir(parents=True, exist_ok=True)
shutil.copyfile(root / 'University-Internship-Management-System/Assets/shared/images/JUST-Logo-01.png', logo)
result = subprocess.run([str(root / 'Tests/FinalEvaluation/bin/FinalEvaluation.Tests.exe')], env=env, capture_output=True, text=True)
print('\n'.join(result.stdout.splitlines()[-2:]))
if result.returncode:
    print(result.stderr)
    raise SystemExit(result.returncode)
subprocess.run(['pdftoppm', '-scale-to', '1200', '-png', '-singlefile', str(preview), str(preview.with_suffix(''))], check=True)
print(preview.with_suffix('.png'))
