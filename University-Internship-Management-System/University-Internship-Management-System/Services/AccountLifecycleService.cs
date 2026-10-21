using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Services
{
    public sealed class NewAccountRequest
    {
        public string RequestId { get; set; }
        public string Email { get; set; }
        public string DisplayName { get; set; }
        public string Role { get; set; }
        public string CompanyId { get; set; }
        public string Department { get; set; }
    }

    public sealed class IssuedCredential
    {
        public string Uid { get; set; }
        public string ProfileId { get; set; }
        public string Email { get; set; }
        // Returned once over HTTPS. Never written to the database, cache or logs.
        public string TemporaryPassword { get; set; }
    }

    public sealed class AccountLifecycleService
    {
        private static readonly ConcurrentDictionary<string, SemaphoreSlim> Operations = new ConcurrentDictionary<string, SemaphoreSlim>();
        private readonly IFirebaseAccountRepository repository;
        private readonly IFirebaseAccountAdmin admin;
        public AccountLifecycleService(IFirebaseAccountRepository repository, IFirebaseAccountAdmin admin)
        { this.repository = repository; this.admin = admin; }

        public Task<IssuedCredential> CreateAsync(UimsAccount actor, NewAccountRequest request)
        {
            RequireAdmin(actor); ValidateNewAccount(request);
            return SerializedAsync("uims-" + Guid.Parse(request.RequestId).ToString("N"), () => CreateCoreAsync(actor, request));
        }
        public Task<IssuedCredential> ResetAsync(UimsAccount actor, string uid)
        {
            RequireAdmin(actor); FirebaseRealtimeDatabaseRepository.ValidateSegment(uid);
            return SerializedAsync(uid, () => ResetCoreAsync(actor, uid));
        }
        public Task SetDisabledAsync(UimsAccount actor, string uid, bool disabled)
        {
            RequireAdmin(actor); FirebaseRealtimeDatabaseRepository.ValidateSegment(uid);
            return SerializedAsync(uid, () => SetDisabledCoreAsync(actor, uid, disabled));
        }
        public Task DeleteAsync(UimsAccount actor, string uid)
        {
            RequireAdmin(actor); FirebaseRealtimeDatabaseRepository.ValidateSegment(uid);
            return SerializedAsync(uid, () => DeleteCoreAsync(actor, uid));
        }
        public Task ChangeFirstPasswordAsync(UimsAccount account, string password, string confirmation)
        {
            if (account == null) throw new UnauthorizedAccessException();
            FirebaseRealtimeDatabaseRepository.ValidateSegment(account.Uid);
            return SerializedAsync(account.Uid, () => ChangeFirstPasswordCoreAsync(account, password, confirmation));
        }

        private async Task<IssuedCredential> CreateCoreAsync(UimsAccount actor, NewAccountRequest request)
        {
            RequireAdmin(actor);
            ValidateNewAccount(request);
            // A repeated request always targets the same Auth UID and profile; it cannot create another identity.
            string id = Guid.Parse(request.RequestId).ToString("N");
            string uid = "uims-" + id, profileId = "UIMS-" + id;
            var before = await repository.ReadVersionAsync("users", profileId).ConfigureAwait(false);
            if (before.Value.Type != JTokenType.Null) throw new AccountConflictException();
            if (request.Role == "company")
            {
                var company = await repository.ReadAsync("companies", request.CompanyId).ConfigureAwait(false);
                if (!(company is JObject)) throw new ArgumentException("Select an existing company.");
            }
            string password = GenerateTemporaryPassword();
            await admin.CreateAsync(uid, request.Email.Trim(), request.DisplayName.Trim(), password).ConfigureAwait(false);
            try
            {
                var profile = new JObject
                {
                    ["uid"] = uid, ["email"] = request.Email.Trim(), ["displayName"] = request.DisplayName.Trim(),
                    ["role"] = request.Role, ["status"] = "provisioning", ["mustChangePassword"] = true,
                    ["companyId"] = request.CompanyId, ["department"] = request.Department,
                    ["securityVersion"] = Guid.NewGuid().ToString("N"), ["credentialState"] = "provisioning",
                    ["createdAt"] = Timestamp(), ["createdByUid"] = actor.Uid
                };
                await repository.WriteVersionAsync("users", profileId, profile, before.ETag).ConfigureAwait(false);
                var mapping = await repository.ReadVersionAsync("usersByUid", uid).ConfigureAwait(false);
                if (mapping.Value.Type != JTokenType.Null) throw new AccountConflictException();
                await repository.WriteVersionAsync("usersByUid", uid, new JValue(profileId), mapping.ETag).ConfigureAwait(false);
                await admin.SetDisabledAsync(uid, false).ConfigureAwait(false);
                var current = await TargetAsync(uid).ConfigureAwait(false);
                current.Record["status"] = "active"; current.Record["credentialState"] = "ready";
                await SaveAsync(current).ConfigureAwait(false);
                return new IssuedCredential { Uid = uid, ProfileId = profileId, Email = request.Email.Trim(), TemporaryPassword = password };
            }
            catch
            {
                // Leave a recoverable disabled identity/profile; never remove academic history.
                try { await admin.SetDisabledAsync(uid, true).ConfigureAwait(false); } catch { }
                throw;
            }
        }

        private async Task<IssuedCredential> ResetCoreAsync(UimsAccount actor, string uid)
        {
            RequireAdmin(actor); DenySelf(actor, uid);
            var target = await TargetAsync(uid).ConfigureAwait(false);
            if ((string)target.Record["status"] != "active") throw new UnauthorizedAccessException();
            string operation = Guid.NewGuid().ToString("N");
            Restrict(target.Record, "resetPending", actor.Uid, operation);
            await SaveAsync(target).ConfigureAwait(false);
            string password = GenerateTemporaryPassword();
            await admin.SetPasswordAsync(uid, password).ConfigureAwait(false);
            await admin.RevokeAsync(uid).ConfigureAwait(false);
            target = await TargetAsync(uid).ConfigureAwait(false);
            RequireOperation(target.Record, operation);
            target.Record["credentialState"] = "ready";
            await SaveAsync(target).ConfigureAwait(false);
            return new IssuedCredential { Uid = uid, ProfileId = target.ProfileId,
                Email = (string)target.Record["email"], TemporaryPassword = password };
        }

        private async Task SetDisabledCoreAsync(UimsAccount actor, string uid, bool disabled)
        {
            RequireAdmin(actor); DenySelf(actor, uid);
            var target = await TargetAsync(uid).ConfigureAwait(false);
            if ((string)target.Record["status"] == "deleted") throw new UnauthorizedAccessException();
            // Block application requests before Auth changes. Enable only after Auth confirms.
            target.Record["status"] = "disabled";
            target.Record["securityVersion"] = Guid.NewGuid().ToString("N");
            string operation = Guid.NewGuid().ToString("N");
            target.Record["accountOperationId"] = operation;
            target.Record["accountOperation"] = disabled ? "disablePending" : "enablePending";
            await SaveAsync(target).ConfigureAwait(false);
            await admin.SetDisabledAsync(uid, disabled).ConfigureAwait(false);
            await admin.RevokeAsync(uid).ConfigureAwait(false);
            target = await TargetAsync(uid).ConfigureAwait(false);
            if ((string)target.Record["accountOperationId"] != operation || (string)target.Record["status"] != "disabled") throw new AccountConflictException();
            target.Record["accountOperation"] = disabled ? "disabled" : "enabled";
            target.Record["status"] = disabled ? "disabled" : "active";
            target.Record["accountUpdatedAt"] = Timestamp();
            await SaveAsync(target).ConfigureAwait(false);
        }

        private async Task DeleteCoreAsync(UimsAccount actor, string uid)
        {
            RequireAdmin(actor); DenySelf(actor, uid);
            var target = await TargetAsync(uid).ConfigureAwait(false);
            target.Record["status"] = "deleted";
            target.Record["securityVersion"] = Guid.NewGuid().ToString("N");
            string operation = Guid.NewGuid().ToString("N");
            target.Record["accountOperationId"] = operation;
            target.Record["accountOperation"] = "deletePending";
            await SaveAsync(target).ConfigureAwait(false);
            await admin.DeleteAsync(uid).ConfigureAwait(false);
            target = await TargetAsync(uid).ConfigureAwait(false);
            if ((string)target.Record["accountOperationId"] != operation || (string)target.Record["status"] != "deleted") throw new AccountConflictException();
            target.Record["accountOperation"] = "deleted"; target.Record["deletedAt"] = Timestamp();
            await SaveAsync(target).ConfigureAwait(false);
        }

        private async Task ChangeFirstPasswordCoreAsync(UimsAccount account, string password, string confirmation)
        {
            ValidatePassword(password, confirmation);
            var target = await TargetAsync(account.Uid).ConfigureAwait(false);
            if ((string)target.Record["status"] != "active" || (bool?)target.Record["mustChangePassword"] == false)
                throw new UnauthorizedAccessException();
            if (((string)target.Record["securityVersion"] ?? "") != (account.SecurityVersion ?? "")) throw new UnauthorizedAccessException();
            string state = (string)target.Record["credentialState"] ?? "ready";
            if (state != "ready" && state != "passwordChangePending") throw new AccountConflictException();
            string operation = Guid.NewGuid().ToString("N");
            Restrict(target.Record, "passwordChangePending", account.Uid, operation);
            await SaveAsync(target).ConfigureAwait(false);
            await admin.SetPasswordAsync(account.Uid, password).ConfigureAwait(false);
            await admin.RevokeAsync(account.Uid).ConfigureAwait(false);
            target = await TargetAsync(account.Uid).ConfigureAwait(false);
            RequireOperation(target.Record, operation);
            target.Record["mustChangePassword"] = false;
            target.Record["credentialState"] = "ready";
            target.Record["passwordChangedAt"] = Timestamp();
            await SaveAsync(target).ConfigureAwait(false);
        }

        public static void ValidatePassword(string password, string confirmation)
        {
            if (password == null || password.Length < 12 || password.Length > 128 || password != confirmation ||
                !Regex.IsMatch(password, "[a-z]") || !Regex.IsMatch(password, "[A-Z]") ||
                !Regex.IsMatch(password, "[0-9]") || !Regex.IsMatch(password, "[^a-zA-Z0-9\\s]"))
                throw new ArgumentException("Use 12–128 characters with uppercase, lowercase, number and symbol; passwords must match.");
        }

        public static string GenerateTemporaryPassword()
        {
            byte[] bytes = new byte[24];
            using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
            return "Aa7!" + Convert.ToBase64String(bytes);
        }

        private static void RequireAdmin(UimsAccount actor)
        {
            if (actor == null || string.IsNullOrWhiteSpace(actor.Uid) || actor.Role != "admin" || actor.MustChangePassword ||
                !string.IsNullOrEmpty(actor.CredentialState) && actor.CredentialState != "ready") throw new UnauthorizedAccessException();
        }
        private static void DenySelf(UimsAccount actor, string uid)
        { if (actor.Uid == uid) throw new UnauthorizedAccessException(); }

        private async Task<Target> TargetAsync(string uid)
        {
            string profileId = await new UserAccountService(repository).ResolveProfileIdAsync(uid).ConfigureAwait(false);
            var snapshot = await repository.ReadVersionAsync("users", profileId).ConfigureAwait(false);
            var record = snapshot.Value as JObject;
            if (record == null || (string)record["uid"] != uid) throw new UnauthorizedAccessException();
            return new Target { ProfileId = profileId, Record = record, ETag = snapshot.ETag };
        }
        private Task SaveAsync(Target target) => repository.WriteVersionAsync("users", target.ProfileId, target.Record, target.ETag);
        private static void RequireOperation(JObject record, string operation)
        {
            if ((string)record["credentialOperationId"] != operation || (string)record["status"] != "active")
                throw new AccountConflictException();
        }
        private static void Restrict(JObject record, string state, string actorUid, string operation)
        {
            record["mustChangePassword"] = true; record["credentialState"] = state;
            record["securityVersion"] = Guid.NewGuid().ToString("N");
            record["credentialOperationId"] = operation; record["credentialRequestedByUid"] = actorUid;
            record["credentialRequestedAt"] = Timestamp();
        }
        private static async Task<T> SerializedAsync<T>(string uid, Func<Task<T>> action)
        {
            var gate = Operations.GetOrAdd(uid, key => new SemaphoreSlim(1, 1));
            await gate.WaitAsync().ConfigureAwait(false);
            try { return await action().ConfigureAwait(false); }
            finally { gate.Release(); }
        }
        private static Task SerializedAsync(string uid, Func<Task> action) => SerializedAsync(uid, async () =>
        { await action().ConfigureAwait(false); return true; });

        private static JObject Timestamp() => new JObject { [".sv"] = "timestamp" };
        private static void ValidateNewAccount(NewAccountRequest request)
        {
            Guid id;
            if (request == null || !Guid.TryParseExact(request.RequestId, "D", out id) ||
                string.IsNullOrWhiteSpace(request.DisplayName) || request.DisplayName.Length > 100 ||
                !RoleAuthorizationService.IsSupported(request.Role)) throw new ArgumentException("Check account details.");
            var email = new MailAddress(request.Email ?? "");
            if (email.Address != request.Email || request.Email.Length > 254) throw new ArgumentException("Enter a valid email address.");
            if (request.Role == "company" && string.IsNullOrWhiteSpace(request.CompanyId) ||
                (request.Role == "academic" || request.Role == "student") && string.IsNullOrWhiteSpace(request.Department))
                throw new ArgumentException("The account needs its company or department relationship.");
            if ((request.Role == "academic" || request.Role == "student") &&
                Array.IndexOf(new[] { "Computer Science", "Artificial Intelligence", "Cybersecurity", "Computer Information Systems" }, request.Department) < 0)
                throw new ArgumentException("Select a recognized Faculty of Information Technology department.");
            if (request.Role == "company") FirebaseRealtimeDatabaseRepository.ValidateSegment(request.CompanyId);
        }
        private sealed class Target
        {
            public string ProfileId;
            public JObject Record;
            public string ETag;
        }
    }
}
