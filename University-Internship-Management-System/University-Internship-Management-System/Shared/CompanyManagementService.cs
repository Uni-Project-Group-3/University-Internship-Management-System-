using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Shared
{
    public interface ICompanyAuthentication : IStudentAuthentication
    {
        Task UpdateProfileAsync(string uid, string email, string name);
    }

    public sealed class CompanyInput
    {
        public string Id { get; set; }
        public string RequestId { get; set; }
        public string Version { get; set; }
        public string Name { get; set; }
        public string Industry { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public string Website { get; set; }
        public string Address { get; set; }
        public string Description { get; set; }
        public string SupervisorUid { get; set; }
        public string SupervisorName { get; set; }
        public string SupervisorEmail { get; set; }
        public string SupervisorPhone { get; set; }
    }

    public sealed class CompanyManagementService
    {
        private readonly IStudentDatabase db;
        private readonly ICompanyAuthentication auth;
        private readonly Func<DateTimeOffset> now;
        public CompanyManagementService(IStudentDatabase db, ICompanyAuthentication auth, Func<DateTimeOffset> now = null)
        { this.db = db; this.auth = auth; this.now = now ?? (() => DateTimeOffset.UtcNow); }
        private static string T(JToken value, string key) { return StudentManagementService.Text(value, key); }
        private static string Key(string value) { return StudentManagementService.Key(value); }
        private static bool IsCompanyUser(JObject profile)
        { return new[] { "companySupervisor", "company_supervisor", "company" }.Contains(T(profile, "role")); }
        private static string Limited(string value, string field, int max, bool required = false)
        {
            value = (value ?? "").Trim();
            if ((required && value.Length == 0) || value.Length > max || value.Any(c => char.IsControl(c) && c != '\r' && c != '\n' && c != '\t'))
                throw new StudentManagementException("Check " + field + "." );
            return value;
        }
        private static string Email(string value, string label, bool required)
        {
            value = Limited(value, label, 254, required);
            if (value.Length == 0) return "";
            try { if (new MailAddress(value).Address != value) throw new FormatException(); }
            catch { throw new StudentManagementException("Enter a valid " + label + "."); }
            return value;
        }
        public static void Validate(CompanyInput f)
        {
            if (f == null || !Regex.IsMatch(f.Id ?? "", "\\A[a-f0-9]{32}\\z") ||
                !Regex.IsMatch(f.RequestId ?? "", "\\A[a-f0-9]{32}\\z"))
                throw new StudentManagementException("Reopen the company form and try again.");
            f.Name = Limited(f.Name, "company name", 160, true);
            f.Industry = Limited(f.Industry, "industry", 100);
            f.Email = Email(f.Email, "company email", false);
            f.Phone = Limited(f.Phone, "company phone", 40);
            f.Website = Limited(f.Website, "company website", 300);
            if (f.Website != "")
            {
                Uri uri;
                if (!Uri.TryCreate(f.Website, UriKind.Absolute, out uri) || (uri.Scheme != "https" && uri.Scheme != "http") || uri.UserInfo != "")
                    throw new StudentManagementException("Enter a website starting with https:// or http://.");
            }
            f.Address = Limited(f.Address, "address", 300);
            f.Description = Limited(f.Description, "description", 1000);
            f.SupervisorName = Limited(f.SupervisorName, "supervisor name", 160, true);
            f.SupervisorEmail = Email(f.SupervisorEmail, "supervisor email", true);
            f.SupervisorPhone = Limited(f.SupervisorPhone, "supervisor phone", 40);
            if (!string.IsNullOrEmpty(f.SupervisorUid)) Key(f.SupervisorUid);
        }
        public async Task<JObject> DirectoryAsync()
        {
            var companies = await db.ReadAsync("companies") as JObject ?? new JObject();
            var users = await db.ReadAsync("users") as JObject ?? new JObject();
            var operations = await db.ReadAsync("companyAccountOperations") as JObject ?? new JObject();
            var items = new JArray();
            foreach (var c in companies.Properties().Where(p => p.Value is JObject))
            {
                var company = new JObject { ["id"] = c.Name };
                foreach (string field in new[] { "name", "industry", "email", "phone", "website", "address", "description", "revision", "accountSetupStatus" }) company[field] = T(c.Value, field);
                company["isActive"] = c.Value.Value<bool?>("isActive") == true;
                company["isArchived"] = c.Value.Value<bool?>("isArchived") == true;
                var operation = operations[c.Name] as JObject;
                if (T(operation, "kind") == "save" && T(operation, "state") == "pending" && operation.Value<bool?>("recoverable") == true)
                    company["pendingForm"] = operation["form"]?.DeepClone();
                company["supervisors"] = new JArray(users.Properties().Where(p => IsCompanyUser(p.Value as JObject) && T(p.Value, "companyId") == c.Name)
                    .Select(p => new JObject { ["uid"] = p.Name, ["fullName"] = T(p.Value, "fullName"), ["email"] = T(p.Value, "email"),
                        ["phone"] = T(p.Value, "phone"), ["isActive"] = p.Value.Value<bool?>("isActive") == true,
                        ["mustChangePassword"] = p.Value.Value<bool?>("mustChangePassword") == true,
                        ["accountSetupStatus"] = T(p.Value, "accountSetupStatus"), ["deletionStatus"] = T(p.Value, "deletionStatus") }));
                items.Add(company);
            }
            return new JObject { ["companies"] = items };
        }
        private async Task<JObject> CompanyAsync(string id)
        {
            var value = await db.ReadAsync("companies/" + Key(id)) as JObject;
            if (value == null) throw new StudentManagementException("Company not found. Refresh the directory.");
            return value;
        }
        private async Task<JObject> SupervisorAsync(string id, string uid)
        {
            var value = await db.ReadAsync("users/" + Key(uid)) as JObject;
            if (!IsCompanyUser(value) || T(value, "companyId") != id)
                throw new StudentManagementException("This supervisor is not linked to the selected company. Refresh the directory.");
            return value;
        }
        private async Task<JObject> AcquireAsync(string id, string kind, string actor, string requestId, string uid, string email = "")
        {
            var version = await db.ReadVersionAsync("companyAccountOperations/" + Key(id));
            var op = version.Value ?? new JObject();
            DateTimeOffset until;
            if (DateTimeOffset.TryParse(T(op, "leaseUntilUtc"), out until) && until > now())
                throw new StudentManagementException("A company request is already running. Wait briefly and refresh.");
            if (T(op, "state") == "pending" && T(op, "kind") == "save" && op.Value<bool?>("recoverable") == true &&
                (kind != "save" || T(op, "requestId") != requestId || T(op, "uid") != uid || !string.Equals(T(op, "email"), email, StringComparison.OrdinalIgnoreCase)))
                throw new StudentManagementException("Finish this company's pending setup before changing its account. Use Resume setup in the directory.");
            op = new JObject { ["kind"] = kind, ["state"] = "pending", ["actor"] = actor, ["requestId"] = requestId,
                ["uid"] = uid, ["email"] = email, ["leaseUntilUtc"] = now().AddMinutes(2).ToString("o") };
            if (!await db.CompareWriteAsync("companyAccountOperations/" + Key(id), op, version.ETag))
                throw new StudentManagementException("The company changed during this request. Refresh and retry.");
            return op;
        }
        private async Task ReleaseAsync(string id) { try { await db.PatchAsync("companyAccountOperations/" + Key(id), new { leaseUntilUtc = (string)null }); } catch { } }
        private async Task CompleteAsync(string id, JObject op)
        {
            op["state"] = "complete"; op["leaseUntilUtc"] = null; op["completedAtUtc"] = now().ToString("o");
            await db.WriteAsync("companyAccountOperations/" + Key(id), op);
        }
        public async Task<JObject> SaveAsync(CompanyInput f, string actor)
        {
            Validate(f);
            string path = "companies/" + Key(f.Id);
            var company = await db.ReadAsync(path) as JObject;
            var previousOp = await db.ReadAsync("companyAccountOperations/" + Key(f.Id)) as JObject;
            bool retry = previousOp != null && T(previousOp, "requestId") == f.RequestId && T(previousOp, "kind") == "save";
            if (retry && T(previousOp, "state") == "complete")
                return new JObject { ["message"] = "This request was already saved. The original temporary password cannot be retrieved. Review the existing supervisor account." };
            if (company != null && !retry && T(company, "revision") != (f.Version ?? ""))
                throw new StudentManagementException("This company was changed elsewhere. Refresh before editing.");
            if (company?.Value<bool?>("isArchived") == true) throw new StudentManagementException("Restore this archived company before editing or adding supervisors.");
            string uid = string.IsNullOrEmpty(f.SupervisorUid) ? "company_" + f.RequestId : f.SupervisorUid;
            var existing = await db.ReadAsync("users/" + Key(uid)) as JObject;
            bool provision = string.IsNullOrEmpty(f.SupervisorUid) || T(existing, "accountSetupStatus") == "pending";
            if (!provision) await SupervisorAsync(f.Id, uid);
            else if (existing != null && (!IsCompanyUser(existing) || T(existing, "companyId") != f.Id))
                throw new StudentManagementException("An unrelated account uses this identifier. Reopen the form.");
            var account = await auth.FindAsync(uid);
            if (provision && !retry && account != null)
                throw new StudentManagementException("An existing login cannot be adopted by this company.");
            if (!provision && account == null) throw new StudentManagementException("Supervisor login is missing. Delete the missing account profile, then add a supervisor.");
            var byEmail = await auth.FindByEmailAsync(f.SupervisorEmail);
            if (byEmail != null && byEmail.Uid != uid) throw new StudentManagementException("That supervisor email already has a login. Use a different email.");
            var op = await AcquireAsync(f.Id, "save", actor, f.RequestId, uid, f.SupervisorEmail);
            try
            {
                // Re-read after obtaining the company lease, before touching Authentication.
                var freshCompany = await db.ReadAsync(path) as JObject;
                if (!retry && freshCompany != null && T(freshCompany, "revision") != (f.Version ?? ""))
                    throw new StudentManagementException("This company was changed elsewhere. Refresh before editing.");
                string revision = Guid.NewGuid().ToString("N");
                var values = new JObject { ["name"] = f.Name, ["industry"] = f.Industry, ["email"] = f.Email, ["phone"] = f.Phone,
                    ["website"] = f.Website, ["address"] = f.Address, ["description"] = f.Description,
                    ["revision"] = revision, ["updatedBy"] = actor, ["updatedAtUtc"] = now().ToString("o") };
                string password = null;
                if (provision)
                {
                    op["recoverable"] = true;
                    op["form"] = JObject.FromObject(f);
                    await db.WriteAsync("companyAccountOperations/" + Key(f.Id), op);
                    if (freshCompany == null)
                    {
                        var draft = (JObject)values.DeepClone(); draft["isActive"] = false; draft["accountSetupStatus"] = "pending";
                        draft["setupRequestId"] = f.RequestId; draft["createdBy"] = actor; draft["createdAtUtc"] = now().ToString("o");
                        if (!await db.CompareWriteAsync(path, draft, "null_etag")) throw new StudentManagementException("A company already uses this ID. Refresh the directory.");
                    }
                    password = StudentManagementService.GeneratePassword();
                    if (account == null) await auth.CreateAsync(uid, f.SupervisorEmail, f.SupervisorName, password);
                    else await auth.UpdateAsync(uid, true, password);
                    var user = new JObject { ["fullName"] = f.SupervisorName, ["email"] = f.SupervisorEmail, ["phone"] = f.SupervisorPhone,
                        ["role"] = "companySupervisor", ["companyId"] = f.Id, ["isActive"] = false, ["mustChangePassword"] = true,
                        ["accountSetupStatus"] = "pending", ["setupRequestId"] = f.RequestId, ["createdBy"] = actor, ["createdAtUtc"] = now().ToString("o") };
                    if (existing == null)
                    { if (!await db.CompareWriteAsync("users/" + Key(uid), user, "null_etag")) throw new StudentManagementException("An account profile appeared during setup. Retry the same form."); }
                    else await db.PatchAsync("users/" + Key(uid), user);
                    await auth.UpdateProfileAsync(uid, f.SupervisorEmail, f.SupervisorName);
                    await auth.UpdateAsync(uid, false);
                    await db.PatchAsync("users/" + Key(uid), new { isActive = true, accountSetupStatus = "complete" });
                    values["accountSetupStatus"] = "complete";
                    values["isActive"] = freshCompany == null || T(freshCompany, "accountSetupStatus") == "pending" || freshCompany.Value<bool?>("isActive") == true;
                }
                else
                {
                    if (T(existing, "deletionStatus") == "pending") throw new StudentManagementException("Finish deleting this supervisor before editing.");
                    await auth.UpdateProfileAsync(uid, f.SupervisorEmail, f.SupervisorName);
                    await db.PatchAsync("users/" + Key(uid), new { fullName = f.SupervisorName, email = f.SupervisorEmail, phone = f.SupervisorPhone });
                }
                await db.PatchAsync(path, values);
                await CompleteAsync(f.Id, op);
                return new JObject { ["message"] = provision ? "Company and supervisor saved. No email was sent. Copy these sign-in details; a password change is required on first login." : "Company and supervisor updated.",
                    ["email"] = provision ? f.SupervisorEmail : null, ["temporaryPassword"] = password };
            }
            finally { await ReleaseAsync(f.Id); }
        }
        public async Task<JObject> ActAsync(string id, string uid, string action, string actor)
        {
            var company = await CompanyAsync(id);
            if (!new[] { "enable", "disable", "delete", "activateCompany", "deactivateCompany", "archiveCompany", "restoreCompany" }.Contains(action))
                throw new StudentManagementException("Unsupported company action.");
            if (company.Value<bool?>("isArchived") == true && action != "restoreCompany" && action != "delete" && action != "archiveCompany")
                throw new StudentManagementException("Restore this archived company before changing supervisor access.");
            if (uid == actor) throw new StudentManagementException("You cannot change your own account here.");
            bool companyAction = action.EndsWith("Company", StringComparison.Ordinal);
            if (!companyAction) await SupervisorAsync(id, uid);
            var op = await AcquireAsync(id, action, actor, Guid.NewGuid().ToString("N"), uid ?? "");
            try
            {
                string password = null, email = null;
                if (action == "archiveCompany")
                {
                    var users = await db.ReadAsync("users") as JObject ?? new JObject();
                    foreach (var user in users.Properties().Where(p => IsCompanyUser(p.Value as JObject) && T(p.Value, "companyId") == id))
                    {
                        await db.PatchAsync("users/" + Key(user.Name), new { isActive = false });
                        if (await auth.FindAsync(user.Name) != null) await auth.UpdateAsync(user.Name, true);
                    }
                    await db.PatchAsync("companies/" + Key(id), new { isActive = false, isArchived = true, revision = Guid.NewGuid().ToString("N"), archivedAtUtc = now().ToString("o"), archivedBy = actor });
                }
                else if (action == "restoreCompany")
                    await db.PatchAsync("companies/" + Key(id), new { isArchived = false, isActive = false, revision = Guid.NewGuid().ToString("N") });
                else if (companyAction)
                    await db.PatchAsync("companies/" + Key(id), new { isActive = action == "activateCompany", revision = Guid.NewGuid().ToString("N"), updatedBy = actor, updatedAtUtc = now().ToString("o") });
                else
                {
                    var user = await SupervisorAsync(id, uid);
                    if (T(user, "accountSetupStatus") == "pending" && action != "delete") throw new StudentManagementException("Resume this supervisor's setup before changing the account.");
                    if (T(user, "deletionStatus") == "pending" && action != "delete") throw new StudentManagementException("Finish deleting this supervisor first.");
                    if (action == "delete")
                    {
                        await db.PatchAsync("users/" + Key(uid), new { isActive = false, deletionStatus = "pending" });
                        await auth.DeleteAsync(uid);
                        await db.DeleteAsync("users/" + Key(uid));
                    }
                    else
                    {
                        var account = await auth.FindAsync(uid);
                        if (account == null) throw new StudentManagementException("Supervisor login is missing. Refresh and review the account.");
                        email = account.Email;
                        if (action == "disable")
                        {
                            await db.PatchAsync("users/" + Key(uid), new { isActive = false });
                            await auth.UpdateAsync(uid, true);
                        }
                        else
                        {
                            await auth.UpdateAsync(uid, false);
                            await db.PatchAsync("users/" + Key(uid), new { isActive = true });
                        }
                    }
                }
                await CompleteAsync(id, op);
                return new JObject { ["message"] = action == "archiveCompany" ? "Company archived and linked supervisor logins disabled. All records and history were retained." : action == "restoreCompany" ? "Company restored as inactive. Supervisor logins remain disabled until you enable them individually." : action == "delete" ? "Supervisor login and profile deleted. Company records and internship history were retained." : "Status updated.",
                    ["temporaryPassword"] = password, ["email"] = password == null ? null : email };
            }
            finally { await ReleaseAsync(id); }
        }
    }
}
