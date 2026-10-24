# Coordinator placement assignment schema proposal

Status: **proposal only; not deployed and not observed in the live database.** The runtime adapter is disabled by default with `CoordinatorAssignmentScopeEnabled=false`.

## Proposed data

The Coordinator's Firebase Authentication UID is the parent key. Each child key is an existing placement key from `/placements`; the child record grants that Coordinator read scope for that placement only.

```json
{
  "coordinatorAssignments": {
    "<VERIFIED_COORDINATOR_UID>": {
      "<EXISTING_PLACEMENT_ID>": {
        "isActive": true,
        "assignedAt": "<UTC_ISO_8601_TIMESTAMP>",
        "assignedBy": "<TRUSTED_PROVISIONER_UID>"
      }
    }
  }
}
```

The angle-bracket values are placeholders, not Firebase records. The live coordinator UID and placement IDs must be resolved from authorized records before approval. No real IDs are listed here because no verified Coordinator browser identity or approved scope was available for this implementation run.

## Validation contract

- The request must have a valid, unexpired Forms Authentication ticket with the Coordinator role; the ticket UID is the only assignment parent key used.
- The matching Firebase Authentication account and `/users/{uid}` profile must still be active, setup-complete, non-deleted, not awaiting password change, and have the Coordinator role.
- Only `/coordinatorAssignments/{verifiedUid}` is read to enumerate scope. Every referenced child must be an object with `isActive: true`, a valid UTC/epoch `assignedAt`, and a non-empty `assignedBy` audit UID.
- Every child key must reference an existing `/placements/{placementId}` whose status is active or confirmed. An optional existing `coordinatorUid` on the assignment or placement, if present, must match the authenticated UID.
- A placement/internship ID collision, malformed assignment, missing placement, inactive placement, or mismatch fails closed. Every subsequent review rechecks the profile, exact assignment child, and placement before access.
- `assignedBy` is audit metadata, not proof of authorization. Provisioning must be performed by a separately authorized admin workflow or controlled deployment identity. No client write rule is proposed here.
- Coordinator assignments are read-only for this page. Evaluation and completion writes remain independently disabled.

## Live changes requiring separate approval

No Firebase paths or values are approved for mutation yet. Once the responsible team verifies the Coordinator UID and the exact placement IDs, the proposed records would be created only at:

`/coordinatorAssignments/<VERIFIED_COORDINATOR_UID>/<EXISTING_PLACEMENT_ID>`

The list of exact sanitized placement references is **pending authoritative scope confirmation**; deriving it from university/college/department matches would be unsafe. No JSON import or Firebase write should occur until that list and the assignment provisioner are approved.

## Deployment and rollback plan

1. Export/back up only the exact target assignment node and verify each placement exists and is in the confirmed/active lifecycle state.
2. Dry-run validation: check the target UID's active Coordinator profile, placement IDs, duplicate internship IDs, and ownership/scope approval. No writes.
3. After separate approval, create only the approved assignment children with create-only/conditional semantics; do not replace the parent node.
4. Run authenticated read-only page tests for the assigned Coordinator and a second Coordinator, plus anonymous/wrong-role negative checks.
5. After acceptance, separately approve setting `CoordinatorAssignmentScopeEnabled=true`. `FirebaseLiveWritesEnabled` remains false.
6. Rollback by setting the scope flag to false first. If approved assignments must be removed, delete only the exact assignment child paths created in step 3 after exporting their current values; never delete placements, users, evaluations, or the full parent node.

## Security Rules

No Security Rules change is included. The adapter uses trusted server-side Firebase access and enforces scope in application code. Before deployment, the Firebase owner must review existing rules and server credential IAM because Admin credentials can bypass RTDB Rules. Do not grant browser/client writes to this path.
