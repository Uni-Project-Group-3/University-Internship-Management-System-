# Firebase setup for the UIMS integration workspace

The intended project is `intership-managment-system` and its Realtime Database URL is `https://intership-managment-system-default-rtdb.europe-west1.firebasedatabase.app`. `Web.config` contains this target, but Firebase, Authentication, Storage, and all live-write switches remain disabled. No live request was made while preparing the seed and Rules files.

## Demo JSON

`../firebase-final.json` is a root import containing fictional, interconnected records. It is for an isolated development database only. A Firebase root import replaces the database root; never import it into a database with data you need to keep. It deliberately contains no uploaded-file metadata, Storage object claims, weekly reports, final evaluations, approved university policy, or notifications. Weekly reports require a real private DOCX object; final evaluations require an actual approved immutable university policy; those records must be created through authorized workflows after setup.

All `DEMO_UID_*` keys are placeholders, not Firebase Authentication users. Their profiles use `accountSetupStatus: "demoOnly"`; sign-in rejects that setup status. To use an isolated demo account, create a test account through the intended Firebase Authentication provider, take the UID issued by Firebase, move the matching profile from its `DEMO_UID_*` key to `/users/{actualUid}`, set `accountSetupStatus` to `"complete"` and `isActive` to `true`, replace the `.invalid` email with that test account's email, then replace every matching UID reference in `studentUid` and the attendance `firebaseUid` mapping. Do not put passwords in Realtime Database or this JSON. Do not reuse demo accounts in production.

The fictional contact details, CV links, task content, grades, and attendance are demonstration values only. No real users or private data are included. The JSON has a sample exam answer key at `/taskExamAnswerKeys`; its access remains server-only under the proposed default-deny Rules.

## Rules and backend access

`../firebase-final-rules.json` is a separate Rules deployment proposal. Its root `.read` and `.write` are `false`; the child `.indexOn` declarations support the equality queries used by current Firebase services and grant no access. The application must use its authenticated, authorized C# service endpoints. Trusted server ADC/Admin credentials can bypass these Rules, so every server operation must enforce verified UID, profile status, role, business ownership, placement, and supervisor assignment. Never expose ADC credentials to the browser.

Before isolated integration testing, verify the Firebase project ID and Web API key pairing in the console, enable the intended Auth provider, configure the server's Application Default Credentials for the isolated project, configure the private Firebase Storage bucket, and review/deploy the Rules only to the isolated project. Keep production and live-write flags disabled until auth, ownership, unauthorized access, persistence, and cross-role workflows have passed against that isolated Firebase project. No Storage bucket name, Auth provider state, ADC identity, or Rules deployment was verified here.

## Import steps

1. Select a new isolated development Realtime Database for the matching project.
2. Confirm it contains no data you need; a root import replaces existing data.
3. In Realtime Database, open the data view, choose the root import action, and select `firebase-final.json`.
4. Review `firebase-final-rules.json` separately in Rules; do not paste it into the data import dialog. Deploy only to the isolated project after reviewing the backend's privileged access model.
5. Create isolated Firebase Authentication test users, remap the UID placeholders as described above, and keep the project-level live gates off until those accounts and server credentials are checked.
6. Configure the private Storage bucket before testing uploads. Do not add `/fileAttachments` metadata manually for objects that were not uploaded.

Offline syntax checks do not verify live Auth, Realtime Database, Storage, deployed Rules, or server IAM behavior. The sample data is not proof that each application page is currently wired to dynamic Firebase CRUD; remaining gaps are listed in `FINAL_VERIFICATION_REPORT.md`.
