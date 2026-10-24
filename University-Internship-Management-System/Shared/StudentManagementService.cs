using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Net.Mail;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Shared
{
    public sealed class StudentManagementException : InvalidOperationException
    {
        public StudentManagementException(string message) : base(message) { }
    }
    // These interfaces allow the account workflow to be tested without live accounts or mail.
    public interface IStudentDatabase
    {
        Task<JToken> ReadAsync(string path);
        Task WriteAsync(string path, object value);
        Task PatchAsync(string path, object value);
        Task DeleteAsync(string path);
        Task<StudentVersion> ReadVersionAsync(string path);
        Task<bool> CompareWriteAsync(string path, object value, string etag);
    }

    public sealed class StudentVersion
    {
        public JObject Value { get; set; }
        public string ETag { get; set; }
    }

    public sealed class StudentLoginAccount
    {
        public string Uid { get; set; }
        public string Email { get; set; }
        public bool Disabled { get; set; }
    }

    public interface IStudentAuthentication
    {
        Task<StudentLoginAccount> FindAsync(string uid);
        Task<StudentLoginAccount> FindByEmailAsync(string email);
        Task CreateAsync(string uid, string email, string name, string password);
        Task UpdateAsync(string uid, bool disabled, string password = null);
        Task DeleteAsync(string uid);
        Task<string> ResetLinkAsync(string email);
    }

    public interface IStudentCredentialMailer
    {
        void ValidateConfiguration();
        Task SendWelcomeAsync(string email, string name, string studentId, string password);
        Task SendResetAsync(string email, string name, string link);
    }

    public sealed class StudentListRecord
    {
        public string Uid { get; set; }
        public string FullName { get; set; }
        public string Email { get; set; }
        public string StudentId { get; set; }
        public string UniversityId { get; set; }
        public string CollegeId { get; set; }
        public string MajorId { get; set; }
        public string Department { get; set; }
        public int RequiredHours { get; set; }
        public bool IsActive { get; set; }
        public bool MustChangePassword { get; set; }
        public string SupervisorId { get; set; }
        public string Supervisor { get; set; }
        public string CompletionStatus { get; set; }
        public string AccountStatus { get; set; }
        public string ConfirmedAt { get; set; }
        public string DisableAt { get; set; }
        public string CertificateId { get; set; }
    }

    public sealed class StudentSupervisor
    {
        public string Uid { get; set; }
        public string Name { get; set; }
        public string DepartmentId { get; set; }
        public string UniversityId { get; set; }
        public string CollegeId { get; set; }
    }

    public sealed class StudentManagementService
    {
        private readonly IStudentDatabase database;
        private readonly IStudentAuthentication authentication;
        private readonly IStudentCredentialMailer mailer;
        private readonly Func<DateTimeOffset> now;

        public StudentManagementService(IStudentDatabase database,
            IStudentAuthentication authentication, IStudentCredentialMailer mailer,
            Func<DateTimeOffset> now = null)
        {
            this.database = database;
            this.authentication = authentication;
            this.mailer = mailer;
            this.now = now ?? (() => DateTimeOffset.UtcNow);
        }

        // Resolve a server-authenticated identity, then check the current database role.
        // A client-side demo role, hidden field, or raw session UID is never authorization.
        public async Task<string> RequireCoordinatorAsync(string identity)
        {
            if (string.IsNullOrWhiteSpace(identity)) throw new UnauthorizedAccessException();
            var account = identity.Contains("@")
                ? await authentication.FindByEmailAsync(identity)
                : await authentication.FindAsync(identity);
            if (account == null || account.Disabled) throw new UnauthorizedAccessException();
            var profile = await database.ReadAsync("users/" + Key(account.Uid)) as JObject;
            if (profile == null || University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(Text(profile, "role")) != "coordinator" ||
                profile.Value<bool?>("isActive") != true)
                throw new UnauthorizedAccessException();
            return account.Uid;
        }

        public async Task<UniversityStudentRecord> LookupAsync(string studentId)
        {
            if (string.IsNullOrWhiteSpace(studentId) || studentId.Length > 30 ||
                studentId.Any(c => c < '0' || c > '9'))
                throw new StudentManagementException("Enter a valid numeric student ID.");
            var value = await database.ReadAsync("universityRecords/students/" + studentId);
            if (value == null || value.Type == JTokenType.Null)
                throw new StudentManagementException("Student not found in university records.");
            var student = value.ToObject<UniversityStudentRecord>();
            ValidateStudent(student, studentId);
            return student;
        }

        public static void ValidateStudent(UniversityStudentRecord student, string studentId)
        {
            if (student == null || student.studentId != studentId || !student.isEligible ||
                string.IsNullOrWhiteSpace(student.fullName) ||
                string.IsNullOrWhiteSpace(student.universityId) ||
                string.IsNullOrWhiteSpace(student.collegeId) ||
                string.IsNullOrWhiteSpace(student.majorId) || student.requiredTrainingHours <= 0)
                throw new StudentManagementException("The university record is incomplete, ineligible, or has a different student ID.");
            try
            {
                if (string.IsNullOrWhiteSpace(student.email) || student.email.Length > 254 ||
                    new MailAddress(student.email.Trim()).Address != student.email.Trim())
                    throw new FormatException();
            }
            catch (FormatException)
            {
                throw new StudentManagementException("The university record has an invalid email address.");
            }
        }

        public async Task<List<StudentSupervisor>> SupervisorsAsync()
        {
            var users = await database.ReadAsync("users") as JObject ?? new JObject();
            return users.Properties().Where(p => IsSupervisor(p.Value as JObject))
                .Select(p => new StudentSupervisor
                {
                    Uid = p.Name, Name = Text(p.Value, "fullName"),
                    DepartmentId = Text(p.Value, "majorId", Text(p.Value, "departmentId")),
                    UniversityId = Text(p.Value, "universityId"),
                    CollegeId = Text(p.Value, "collegeId")
                }).OrderBy(s => s.Name).ToList();
        }

        public static bool SupervisorMatches(StudentSupervisor supervisor, UniversityStudentRecord student)
        {
            return supervisor != null && supervisor.DepartmentId == student.majorId &&
                supervisor.UniversityId == student.universityId && supervisor.CollegeId == student.collegeId;
        }

        public async Task<List<StudentListRecord>> ListAsync()
        {
            var users = await database.ReadAsync("users") as JObject ?? new JObject();
            var placements = await database.ReadAsync("placements") as JObject ?? new JObject();
            return users.Properties().Where(p => University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(Text(p.Value, "role")) == "student")
                .Select(p => ToListRecord(p.Name, p.Value as JObject, users, placements))
                .OrderBy(s => s.FullName).ToList();
        }

        public static StudentListRecord ToListRecord(string uid, JObject profile, JObject users, JObject placements)
        {
            profile = profile ?? new JObject();
            string studentId = Text(profile, "studentId");
            string supervisorId = Text(profile, "academicSupervisorId");
            // Placement references must use the Authentication UID, not an ambiguous student number.
            var matching = placements.Properties().Select(p => p.Value as JObject)
                .Where(p => p != null && Text(p, "studentUid", Text(p, "studentId")) == uid).ToList();
            var placement = matching.Count == 1 ? matching[0] : null;
            string rawStatus = Text(placement, "completionStatus", Text(profile, "completionStatus"));
            bool active = profile.Value<bool?>("isActive") == true;
            string setup = Text(profile, "accountSetupStatus");
            string confirmed = Text(placement, "studentConfirmedAt");
            string disableAt = Text(placement, "accountDisableAt");
            string status;
            switch (rawStatus)
            {
                case "InTraining": case "In Training": status = "In Training"; break;
                case "PendingStudentConfirmation": case "Pending Student Confirmation": status = "Pending Student Confirmation"; break;
                case "Completed": status = active ? (string.IsNullOrEmpty(disableAt) ? "Completed" : "Completed — Grace Period") : "Completed — Disabled"; break;
                default: status = matching.Count > 1 || !string.IsNullOrEmpty(rawStatus) ? "Status unavailable" : "Not placed"; break;
            }
            if (matching.Count > 1) status = "Status unavailable";
            return new StudentListRecord
            {
                Uid = uid, FullName = Text(profile, "fullName"), Email = Text(profile, "email"),
                StudentId = studentId, UniversityId = Text(profile, "universityId"),
                CollegeId = Text(profile, "collegeId"), MajorId = Text(profile, "majorId"),
                Department = DisplayName(Text(profile, "majorId")),
                RequiredHours = profile.Value<int?>("requiredTrainingHours") ?? 0,
                IsActive = active, MustChangePassword = profile.Value<bool?>("mustChangePassword") == true,
                SupervisorId = supervisorId,
                Supervisor = string.IsNullOrEmpty(supervisorId) ? "Unassigned" : Text(users[supervisorId], "fullName", "Unavailable supervisor"),
                CompletionStatus = status, ConfirmedAt = confirmed, DisableAt = disableAt,
                CertificateId = Text(placement, "certificateId"),
                AccountStatus = Text(profile, "deletionStatus") == "pending" ? "Deletion pending" :
                    (!string.IsNullOrEmpty(setup) && setup != "complete" ? "Setup pending" : (active ? "Active" : "Inactive"))
            };
        }

        public async Task<string> CreateAsync(string studentId, string supervisorId, string password, string coordinatorUid, bool sendWelcomeEmail = true)
        {
            if (sendWelcomeEmail) mailer.ValidateConfiguration();
            var student = await LookupAsync(studentId); // Never trust the displayed/old university data.
            var supervisor = string.IsNullOrEmpty(supervisorId) ? null : (await SupervisorsAsync()).FirstOrDefault(s => s.Uid == supervisorId);
            if (!string.IsNullOrEmpty(supervisorId) && !SupervisorMatches(supervisor, student))
                throw new StudentManagementException("Choose an active academic supervisor from the student's university, college and department.");
            string uid = ManagedUid(student.universityId, studentId);
            string operationPath = "studentAccountOperations/" + uid;
            var users = await database.ReadAsync("users") as JObject ?? new JObject();
            if (users.Properties().Any(p => p.Name != uid && Text(p.Value, "role") == "student" &&
                Text(p.Value, "studentId") == studentId && Text(p.Value, "universityId") == student.universityId))
                throw new StudentManagementException("This student already has an account.");
            var version = await database.ReadVersionAsync(operationPath);
            var operation = version.Value ?? new JObject();
            if (Text(operation, "state") == "complete")
            {
                // A historical completion marker is not an account. Older page-only
                // deletion removed Auth/profile without updating this marker.
                if (users[uid] != null || await authentication.FindAsync(uid) != null)
                    throw new StudentManagementException("This student already has an account.");
                operation = new JObject();
            }
            var oldProfile = users[uid] as JObject;
            if (oldProfile != null && Text(operation, "kind") != "create")
                throw new StudentManagementException("This account exists and cannot be replaced.");
            if (operation.Count > 0 && Text(operation, "state") != "deleted" &&
                (Text(operation, "kind") != "create" || Text(operation, "studentId") != studentId ||
                 Text(operation, "universityId") != student.universityId ||
                 !string.Equals(Text(operation, "email"), student.email.Trim(), StringComparison.OrdinalIgnoreCase)))
                throw new StudentManagementException("Existing setup has different university data. Review the pending account.");
            EnsureLeaseAvailable(operation);
            bool resume = Text(operation, "kind") == "create" && Text(operation, "state") != "deleted";
            bool mailAccepted = resume && sendWelcomeEmail && operation.Value<bool?>("mailAccepted") == true;
            // Never leave a recovery marker that could later adopt an unrelated login.
            if (!resume && await authentication.FindAsync(uid) != null)
                throw new StudentManagementException("An existing login uses this account ID. Manual review is required.");
            operation = resume ? operation : new JObject();
            operation["kind"] = "create";
            operation["state"] = "pending";
            operation["studentId"] = studentId;
            operation["universityId"] = student.universityId;
            operation["email"] = student.email.Trim();
            operation["coordinatorUid"] = coordinatorUid;
            if (!sendWelcomeEmail) operation["mailAccepted"] = false;
            operation["leaseUntilUtc"] = now().AddMinutes(10).ToString("o");
            if (!await database.CompareWriteAsync(operationPath, operation, version.ETag))
                throw new StudentManagementException("Another request is handling this student. Refresh and retry.");
            try
            {
                var account = await authentication.FindAsync(uid);
                if (account != null && !resume)
                    throw new StudentManagementException("An existing login uses this account ID. Manual review is required.");
                if (account != null && !string.Equals(account.Email, student.email.Trim(), StringComparison.OrdinalIgnoreCase))
                    throw new StudentManagementException("The existing login email differs from the university record.");
                if (operation.Value<bool?>("historyCleaned") != true)
                {
                    await AccountHistoryCleanup.StudentAsync(database, uid, studentId);
                    operation["historyCleaned"] = true;
                    await database.WriteAsync(operationPath, operation);
                }
                if (!mailAccepted)
                {
                    if (string.IsNullOrEmpty(password) || password.Length < 12)
                        throw new StudentManagementException("Generate a temporary password first.");
                    if (account == null)
                        await authentication.CreateAsync(uid, student.email.Trim(), student.fullName.Trim(), password);
                    else
                        await authentication.UpdateAsync(uid, true, password);
                    var profile = new
                    {
                        fullName = student.fullName.Trim(), email = student.email.Trim(), studentId,
                        universityId = student.universityId, collegeId = student.collegeId, majorId = student.majorId,
                        requiredTrainingHours = student.requiredTrainingHours, academicSupervisorId = supervisorId,
                        role = "student", isActive = false, mustChangePassword = false, accountSetupStatus = "pending",
                        createdBy = coordinatorUid, createdAtUtc = now().ToString("o")
                    };
                    if (oldProfile == null)
                    {
                        if (!await database.CompareWriteAsync("users/" + uid, profile, "null_etag"))
                            throw new StudentManagementException("A profile appeared during setup. Review it before retrying.");
                    }
                    else
                    {
                        // Resume without overwriting unrelated profile/history fields.
                        await database.PatchAsync("users/" + uid, profile);
                    }
                    if (sendWelcomeEmail)
                    {
                        await mailer.SendWelcomeAsync(student.email.Trim(), student.fullName.Trim(), studentId, password);
                        operation["mailAccepted"] = true;
                    }
                    operation["credentialDelivery"] = sendWelcomeEmail ? "email" : "coordinator";
                    await database.WriteAsync(operationPath, operation);
                }
                else if (account == null)
                    throw new StudentManagementException("The pending login is missing. Manual recovery is required.");
                await authentication.UpdateAsync(uid, false);
                await database.PatchAsync("users/" + uid, new { isActive = true, accountSetupStatus = "complete" });
                operation["state"] = "complete";
                operation["leaseUntilUtc"] = null;
                operation["completedAtUtc"] = now().ToString("o");
                await database.WriteAsync(operationPath, operation);
                return uid;
            }
            catch
            {
                // Keep the durable operation for recovery, even after session loss.
                // Releasing a lease is best effort; never replace the original error.
                try { await database.PatchAsync(operationPath, new { leaseUntilUtc = (string)null }); } catch { }
                throw;
            }
        }

        public async Task ResetPasswordAsync(string uid)
        {
            mailer.ValidateConfiguration();
            var profile = await RequireStudentAsync(uid);
            if (Text(profile, "accountSetupStatus") == "pending" || Text(profile, "deletionStatus") == "pending")
                throw new StudentManagementException("Finish account setup or deletion before resetting this account.");
            var account = await authentication.FindAsync(uid);
            if (account == null || account.Disabled)
                throw new StudentManagementException("The student login is missing or disabled.");
            string link = await authentication.ResetLinkAsync(account.Email);
            await mailer.SendResetAsync(account.Email, Text(profile, "fullName"), link);
        }

        public async Task SetActiveAsync(string uid, bool active, string coordinatorUid)
        {
            await RequireCoordinatorAsync(coordinatorUid);
            var profile = await RequireStudentAsync(uid);
            if (uid == coordinatorUid) throw new StudentManagementException("You cannot change your own account.");
            if (Text(profile, "accountSetupStatus") == "pending" || Text(profile, "deletionStatus") == "pending")
                throw new StudentManagementException("Complete pending account setup before changing access.");
            string operationPath = "studentAccountOperations/" + Key(uid);
            var version = await database.ReadVersionAsync(operationPath);
            var operation = version.Value ?? new JObject();
            EnsureLeaseAvailable(operation);
            operation["kind"] = active ? "enable" : "disable";
            operation["leaseUntilUtc"] = now().AddMinutes(10).ToString("o");
            if (!await database.CompareWriteAsync(operationPath, operation, version.ETag))
                throw new StudentManagementException("Another account operation is in progress.");
            try
            {
                profile = await RequireStudentAsync(uid);
                if (Text(profile, "accountSetupStatus") == "pending" || Text(profile, "deletionStatus") == "pending")
                    throw new StudentManagementException("Complete pending account setup before changing access.");
                var account = await authentication.FindAsync(uid);
                if (active && account == null) throw new StudentManagementException("The login is missing. Reconcile this existing account before reactivation.");
                if (!active) await database.PatchAsync("users/" + Key(uid), new { isActive = false });
                if (account != null) await authentication.UpdateAsync(uid, !active);
                if (active) await database.PatchAsync("users/" + Key(uid), new { isActive = true });
            }
            finally { await database.PatchAsync(operationPath, new { leaseUntilUtc = (string)null }); }
        }

        public async Task DeleteAsync(string uid, string coordinatorUid)
        {
            var profile = await RequireStudentAsync(uid);
            if (uid == coordinatorUid) throw new StudentManagementException("You cannot delete your own account.");
            string operationPath = "studentAccountOperations/" + Key(uid);
            var version = await database.ReadVersionAsync(operationPath);
            var operation = version.Value ?? new JObject();
            EnsureLeaseAvailable(operation);
            operation["kind"] = "delete";
            operation["state"] = "pending";
            operation["leaseUntilUtc"] = now().AddMinutes(10).ToString("o");
            if (!await database.CompareWriteAsync(operationPath, operation, version.ETag))
                throw new StudentManagementException("Another account operation is in progress.");
            try
            {
                await database.PatchAsync("users/" + uid, new { isActive = false, deletionStatus = "pending" });
                await authentication.DeleteAsync(uid); // Adapter treats an already-missing account as success.
                await AccountHistoryCleanup.StudentAsync(database, uid, Text(profile, "studentId"));
                operation["state"] = "deleted";
                operation["deletedAtUtc"] = now().ToString("o");
                operation["deletedBy"] = coordinatorUid;
                // Mark first: if profile deletion times out, a retry can safely finish cleanup.
                await database.WriteAsync(operationPath, operation);
                await database.DeleteAsync("users/" + uid);
            }
            finally
            {
                try { await database.PatchAsync(operationPath, new { leaseUntilUtc = (string)null }); } catch { }
            }
        }

        private async Task<JObject> RequireStudentAsync(string uid)
        {
            var profile = await database.ReadAsync("users/" + Key(uid)) as JObject;
            if (profile == null || University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(Text(profile, "role")) != "student")
                throw new StudentManagementException("Student account not found. Refresh the list.");
            return profile;
        }

        private void EnsureLeaseAvailable(JObject operation)
        {
            DateTimeOffset until;
            if (DateTimeOffset.TryParse(Text(operation, "leaseUntilUtc"), out until) && until > now())
                throw new StudentManagementException("An account request is already running. Wait before retrying.");
        }

        public static bool IsSupervisor(JObject value)
        {
            string role = Text(value, "role");
            return value != null && value.Value<bool?>("isActive") == true &&
                University_Internship_Management_System.Shared.FirebaseSignInService.CanonicalRole(role) == "academicSupervisor";
        }

        public static string ManagedUid(string universityId, string studentId)
        {
            using (var hash = SHA256.Create())
                return "student_" + BitConverter.ToString(hash.ComputeHash(
                    Encoding.UTF8.GetBytes(universityId + "|" + studentId))).Replace("-", "").ToLowerInvariant();
        }

        public static string GeneratePassword()
        {
            const string alphabet = "ABCDEFGHJKLMNPQRSTUVWXYZabcdefghijkmnpqrstuvwxyz23456789";
            using (var random = RandomNumberGenerator.Create())
            {
                string result;
                do
                {
                    var chars = new char[12];
                    var bytes = new byte[1];
                    int limit = 256 - 256 % alphabet.Length;
                    for (int i = 0; i < chars.Length; i++)
                    {
                        do { random.GetBytes(bytes); } while (bytes[0] >= limit);
                        chars[i] = alphabet[bytes[0] % alphabet.Length];
                    }
                    result = new string(chars);
                } while (!result.Any(char.IsUpper) || !result.Any(char.IsLower) || !result.Any(char.IsDigit));
                return result;
            }
        }

        public static string Key(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > 128 ||
                value.Any(c => c < 32 || ".#$[]/".Contains(c) || c == '\\'))
                throw new StudentManagementException("Invalid account identifier.");
            return Uri.EscapeDataString(value);
        }

        public static string Text(JToken value, string key, string fallback = "")
        {
            var token = (value as JObject)?[key];
            return token == null || token.Type == JTokenType.Null ? fallback : token.ToString();
        }

        public static string DisplayName(string value)
        {
            string spaced = System.Text.RegularExpressions.Regex.Replace(value ?? "", "([a-z0-9])([A-Z])", "$1 $2");
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(spaced.Replace('_', ' ').Replace('-', ' '));
        }
    }
}
