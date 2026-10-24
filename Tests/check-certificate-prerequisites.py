import os
import xml.etree.ElementTree as ET
from pathlib import Path

config = Path(__file__).resolve().parent.parent / 'University-Internship-Management-System/Web.config'
settings = {el.attrib.get('key'): el.attrib.get('value') for el in ET.parse(config).findall('./appSettings/add')}
for key in ['FinalReportWorkflowEnabled', 'FinalReportWorkflowSchemaValidated', 'FinalReportWorkflowWritesEnabled', 'FirebaseLiveWritesEnabled', 'FirebaseSchemaValidated']:
    print(key + ': ' + ('enabled' if settings.get(key, '').lower() == 'true' else 'disabled'))
for key in ['UIMS_SMTP_HOST', 'UIMS_SMTP_FROM', 'UIMS_SMTP_USERNAME', 'UIMS_SMTP_PASSWORD']:
    print(key + ': ' + ('configured' if os.environ.get(key) else 'missing in current process'))
