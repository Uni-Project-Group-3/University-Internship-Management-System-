using Newtonsoft.Json.Linq;

namespace UIMS.Services
{
    public static class StudentAuthAuditFailure
    {
        public static JObject Describe(int status, string reason, string projectId, string sdkCode)
        {
            string category = reason == "ACCESS_TOKEN_SCOPE_INSUFFICIENT" ? "oauthScope" :
                reason == "SERVICE_DISABLED" ? "apiDisabled" :
                reason != null && reason.StartsWith("API_KEY_") ? "apiRestriction" :
                reason == "CREDENTIAL_INITIALIZATION" || status == 401 ? "serverCredentials" :
                status == 403 ? "permissionDeniedUnconfirmedCause" : "authTransport";
            string action = category == "oauthScope" ? "Use the existing Firebase Admin Auth integration/ADC for this lookup; do not reuse the database-only scoped token. Do not change IAM or database Rules to fix an OAuth-scope denial." :
                category == "apiDisabled" ? "Administrator must inspect Identity Toolkit API service enablement for the existing project. No API or project setting was changed." :
                category == "apiRestriction" ? "Administrator must inspect the named API restriction. The Admin metadata path uses OAuth, not a browser API key. Do not remove restrictions blindly." :
                "Inspect the existing worker credential identity, target project and the Identity Toolkit API denial. Only if missing IAM permission is confirmed, approve firebaseauth.users.get (Firebase Authentication Viewer) for the existing principal. Do not replace credentials, grant Admin blindly, or change database Rules. Account status remains unverified.";
            return new JObject { ["state"] = "unverified", ["verified"] = false, ["httpStatus"] = status,
                ["method"] = "POST", ["endpoint"] = "https://identitytoolkit.googleapis.com/v1/projects/" + projectId + "/accounts:lookup",
                ["category"] = category, ["reason"] = reason, ["sdkCode"] = sdkCode,
                ["requiredPermission"] = "firebaseauth.users.get",
                ["administratorAction"] = action };
        }
    }
}
