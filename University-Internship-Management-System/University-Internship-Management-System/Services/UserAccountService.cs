using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Services
{
    public sealed class UimsAccount
    {
        public string Uid { get; set; }
        public string ProfileId { get; set; }
        public string Role { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string CompanyId { get; set; }
        public string Department { get; set; }
        public string ProgramId { get; set; }
        public string AcademicSupervisorId { get; set; }
        public string SecurityVersion { get; set; }
        public string CredentialState { get; set; }
        public string PasswordPolicyVersion { get; set; }
        public bool MustChangePassword { get; set; }
    }

    public sealed class UserAccountService
    {
        private readonly IFirebaseRealtimeDatabaseRepository repository;
        public UserAccountService(IFirebaseRealtimeDatabaseRepository repository) { this.repository = repository; }

        public static bool HasMatchingUid(JObject record, string uid)
        {
            if (record == null || string.IsNullOrEmpty(uid)) return false;
            var firebaseUid = record["firebaseUid"];
            var legacyUid = record["uid"];
            return (firebaseUid != null || legacyUid != null) &&
                (firebaseUid == null || firebaseUid.Type == JTokenType.String && (string)firebaseUid == uid) &&
                (legacyUid == null || legacyUid.Type == JTokenType.String && (string)legacyUid == uid);
        }

        public static bool HasActiveStatus(JObject record)
        {
            if (record == null) return false;
            var accountStatus = record["accountStatus"];
            var legacyStatus = record["status"];
            return (accountStatus != null || legacyStatus != null) &&
                (accountStatus == null || accountStatus.Type == JTokenType.String && string.Equals((string)accountStatus, "active", StringComparison.OrdinalIgnoreCase)) &&
                (legacyStatus == null || legacyStatus.Type == JTokenType.String && string.Equals((string)legacyStatus, "active", StringComparison.OrdinalIgnoreCase));
        }

        public async Task<string> ResolveProfileIdAsync(string uid)
        {
            var mapping = await repository.ReadAsync(FirebaseConfiguration.UidMappingsPath, uid).ConfigureAwait(false);
            string profileId = mapping?.Type == JTokenType.String ? (string)mapping : (string)(mapping as JObject)?["profileId"];
            if (string.IsNullOrWhiteSpace(profileId)) throw LoginDiagnostics.Reject("UsersByUidMappingMissing");
            FirebaseRealtimeDatabaseRepository.ValidateSegment(profileId);
            return profileId;
        }

        public async Task<UimsAccount> GetActiveAsync(VerifiedIdentity identity)
        {
            string profileId = await ResolveProfileIdAsync(identity.Uid).ConfigureAwait(false);
            var record = await repository.ReadAsync(FirebaseConfiguration.UsersPath, profileId).ConfigureAwait(false) as JObject;
            if (record == null || !HasMatchingUid(record, identity.Uid) ||
                record["profileId"] != null && (string)record["profileId"] != profileId)
                throw LoginDiagnostics.Reject("ProfileMissingOrInconsistent");
            if (!HasActiveStatus(record)) throw LoginDiagnostics.Reject("AccountStatusRejected");
            string role = (string)record["role"];
            role = RoleAuthorizationService.NormalizeRole(role);
            if (identity.Email != null && !string.Equals((string)record["email"], identity.Email, StringComparison.OrdinalIgnoreCase)) throw LoginDiagnostics.Reject("ProfileEmailInconsistent");
            if (!RoleAuthorizationService.IsSupported(role)) throw LoginDiagnostics.Reject("RoleNormalizationFailure");
            var account = new UimsAccount
            {
                Uid = identity.Uid, ProfileId = profileId, Role = role, Email = (string)record["email"],
                DisplayName = (string)record["displayName"] ?? (string)record["name"] ?? identity.Email,
                CompanyId = (string)record["companyId"], Department = (string)record["department"], ProgramId = (string)record["programId"],
                AcademicSupervisorId = (string)record["academicSupervisorId"],
                SecurityVersion = (string)record["securityVersion"] ?? "",
                CredentialState = (string)record["credentialState"] ?? "ready",
                PasswordPolicyVersion = ((string)record["securityVersion"] ?? "") + "|" + (record["passwordChangeRequiredAt"]?.ToString(Newtonsoft.Json.Formatting.None) ?? "") + "|" + (record["mustChangePassword"]?.ToString(Newtonsoft.Json.Formatting.None) ?? "missing"),
                MustChangePassword = record["mustChangePassword"]?.Type != JTokenType.Boolean || (bool)record["mustChangePassword"]
            };
            if (role == "company" && string.IsNullOrWhiteSpace(account.CompanyId) || role == "academic" && string.IsNullOrWhiteSpace(account.ProgramId) && string.IsNullOrWhiteSpace(account.Department))
                throw LoginDiagnostics.Reject(role == "academic" ? "AcademicSupervisorRelationshipFailure" : "CompanyRelationshipFailure");
            if (role == "student") LoginDiagnostics.Write(string.IsNullOrWhiteSpace(account.AcademicSupervisorId) ? "AcademicSupervisorRelationshipMissingNotValidated" : "AcademicSupervisorRelationshipPresentNotValidated");
            if (account.MustChangePassword && account.CredentialState == "ready" && PasswordCompletion.IsValid((string)record["firstLoginCompletion"], account))
                account.MustChangePassword = false;
            account.MustChangePassword |= account.CredentialState != "ready";
            return account;
        }
    }
}

namespace University_Internship_Management_System.Services
{
    // Only a server-confirmed Firebase password update may produce this authenticated receipt.
    public static class PasswordCompletion
    {
        internal static string Create(UimsAccount account)
        {
            var payload = new JObject { ["uid"] = account.Uid, ["profile"] = account.ProfileId,
                ["policy"] = account.PasswordPolicyVersion, ["completedUtc"] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() };
            return Convert.ToBase64String(System.Web.Security.MachineKey.Protect(
                System.Text.Encoding.UTF8.GetBytes(payload.ToString(Newtonsoft.Json.Formatting.None)), "Uims.PasswordCompletion.v1"));
        }
        public static bool IsValid(string receipt, UimsAccount account)
        {
            if (account == null || string.IsNullOrEmpty(receipt) || receipt.Length > 8192) return false;
            try
            {
                var bytes = System.Web.Security.MachineKey.Unprotect(Convert.FromBase64String(receipt), "Uims.PasswordCompletion.v1");
                if (bytes == null) return false;
                var payload = JObject.Parse(System.Text.Encoding.UTF8.GetString(bytes));
                return (string)payload["uid"] == account.Uid && (string)payload["profile"] == account.ProfileId &&
                    (string)payload["policy"] == account.PasswordPolicyVersion &&
                    payload["completedUtc"]?.Type == JTokenType.Integer && (long)payload["completedUtc"] > 0 &&
                    (long)payload["completedUtc"] <= DateTimeOffset.UtcNow.AddMinutes(1).ToUnixTimeMilliseconds();
            }
            catch (Exception error) when (error is FormatException || error is System.Security.Cryptography.CryptographicException || error is Newtonsoft.Json.JsonException || error is ArgumentException)
            { return false; }
        }
    }
}
