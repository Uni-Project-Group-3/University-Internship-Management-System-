using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Web;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using System.Web.UI.WebControls;
using FirebaseAdmin.Auth;
using Newtonsoft.Json;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Coordinator
{
    public partial class ManageStudents : Page
    {
        private const string CandidateKey = "ManageStudents.Candidate";
        private const string CsrfKey = "ManageStudents.Csrf";
        private StudentManagementService service;
        private Task<string> authorization;
        private bool authorized;
        private bool openEditor;
        private bool openDetails;
        private bool emailConfigured;

        private StudentManagementService Service
        {
            get
            {
                if (service == null)
                {
                    var gateway = new StudentFirebaseGateway(new UIMS.Services.FirebaseConfiguration("true",
                    new UIMS.Services.FirebaseConfiguration().ProjectId,
                    new UIMS.Services.FirebaseConfiguration().DatabaseUrl,
                    new UIMS.Services.FirebaseConfiguration().ApiKey,
                    writes: "true", schemaValidated: "true", authenticationEnabled:
                    new UIMS.Services.FirebaseConfiguration().AuthenticationEnabled ? "true" : "false"));
                    service = new StudentManagementService(gateway, gateway, new StudentCredentialMailer());
                }
                return service;
            }
        }

        public sealed class StudentCandidate
        {
            public UniversityStudentRecord Record { get; set; }
            public string Password { get; set; }
            public bool Created { get; set; }
            public DateTimeOffset ExpiresUtc { get; set; }
        }

        protected override void OnInit(EventArgs e)
        {
            base.OnInit(e);
            ViewStateUserKey = Session.SessionID;
            if (Session[CsrfKey] == null)
            {
                var bytes = new byte[32];
                using (var random = RandomNumberGenerator.Create()) random.GetBytes(bytes);
                Session[CsrfKey] = Convert.ToBase64String(bytes);
            }
        }

        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetCacheability(HttpCacheability.NoCache);
            Response.Cache.SetNoStore();
            Response.Headers["X-Content-Type-Options"] = "nosniff";
            Response.Headers["Referrer-Policy"] = "same-origin";
            try { new StudentCredentialMailer().ValidateConfiguration(); emailConfigured = true; }
            catch (Exception) { emailConfigured = false; }
            if (!IsPostBack)
                RegisterAsyncTask(new PageAsyncTask(() => RunAsync(LoadStudentsAsync)));
        }

        private async Task<string> AuthorizeAsync()
        {
            if (!Request.IsAuthenticated || User?.Identity == null)
                throw new UnauthorizedAccessException();
            if (IsPostBack && (!string.Equals(studentCsrf.Value, Session[CsrfKey] as string, StringComparison.Ordinal) ||
                string.IsNullOrEmpty(studentCsrf.Value)))
                throw new StudentManagementException("Your session expired. Reload the page before trying again.");
            if (authorization == null) authorization = Service.RequireCoordinatorAsync(User.Identity.Name);
            string uid = await authorization;
            authorized = true;
            return uid;
        }

        // One boundary translates infrastructure errors into safe messages, without exposing keys or passwords.
        private async Task RunAsync(Func<Task> action, bool editor = false)
        {
            openEditor = editor;
            try
            {
                await AuthorizeAsync();
                await action();
            }
            catch (UnauthorizedAccessException)
            {
                authorized = false;
                Session.Remove(CandidateKey);
                studentListMessage.Text = "Sign in with an active coordinator account to manage students.";
                studentLookupMessage.Text = studentListMessage.Text;
            }
            catch (Exception error)
            {
                string message = "The request could not be completed. Refresh or retry; pending account setup is recoverable.";
                var workflow = error as StudentManagementException;
                var firebase = error as FirebaseAuthException;
                if (workflow != null) message = workflow.Message;
                else if (firebase?.AuthErrorCode == AuthErrorCode.EmailAlreadyExists)
                    message = "A login already exists for this university email. Review that account instead of creating another.";
                else if (firebase?.AuthErrorCode == AuthErrorCode.UidAlreadyExists)
                    message = "Account setup already started. Look up this student again to resume.";
                studentListMessage.Text = HttpUtility.HtmlEncode(message);
                if (editor) studentLookupMessage.Text = HttpUtility.HtmlEncode(message);
                Trace.Warn("ManageStudents", error.GetType().Name); // No credential or remote-response logging.
            }
        }

        protected async void findStudentButton_Click(object sender, EventArgs e)
        {
            await RunAsync(async () =>
            {
                Session.Remove(CandidateKey);
                var record = await Service.LookupAsync(studentUniversityId.Value.Trim());
                StoreCandidate(new StudentCandidate { Record = record, Password = StudentManagementService.GeneratePassword(), ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15) });
                studentLookupMessage.Text = "";
            }, true);
        }

        protected async void generateStudentPassword_Click(object sender, EventArgs e)
        {
            await RunAsync(() =>
            {
                var candidate = Candidate();
                if (candidate.Created) throw new StudentManagementException("This account has already been created.");
                candidate.Password = StudentManagementService.GeneratePassword();
                candidate.ExpiresUtc = DateTimeOffset.UtcNow.AddMinutes(15);
                StoreCandidate(candidate);
                studentLookupMessage.Text = "";
                return Task.CompletedTask;
            }, true);
        }

        protected async void saveStudentButton_Click(object sender, EventArgs e)
        {
            await RunAsync(async () =>
            {
                var candidate = Candidate();
                var fresh = await Service.LookupAsync(candidate.Record.studentId);
                if (JsonConvert.SerializeObject(fresh) != JsonConvert.SerializeObject(candidate.Record))
                    throw new StudentManagementException("The university record changed. Find the student again before confirming.");
                string uid = await AuthorizeAsync();
                if (candidate.Created) throw new StudentManagementException("This account has already been created.");
                await Service.CreateAsync(fresh.studentId, "", candidate.Password, uid, emailConfigured);
                candidate.Created = true;
                StoreCandidate(candidate);
                await LoadStudentsAsync();
                string message = emailConfigured
                    ? "Student account created. The email provider accepted the welcome message."
                    : "Student account saved. No email was sent. Copy the temporary password below if needed; the student must change it on first sign-in.";
                studentListMessage.Text = message;
                studentLookupMessage.Text = message;
                openEditor = true;
            }, true);
        }

        protected async void RefreshStudents_Click(object sender, EventArgs e)
        {
            await RunAsync(async () =>
            {
                studentDetailsPanel.Visible = false;
                await LoadStudentsAsync();
                studentListMessage.Text = "Student records refreshed.";
            });
        }

        private async Task LoadStudentsAsync()
        {
            var students = await Service.ListAsync();
            studentsGrid.DataSource = students;
            studentsGrid.DataBind();
            if (studentsGrid.HeaderRow != null) studentsGrid.HeaderRow.TableSection = TableRowSection.TableHeader;
            SetOptions(studentDepartmentFilter, students.Select(s => new KeyValuePair<string, string>(s.MajorId, s.Department)), "All departments");
            SetOptions(studentSupervisorFilter, students.Select(s => new KeyValuePair<string, string>(
                string.IsNullOrEmpty(s.SupervisorId) ? "__unassigned" : s.SupervisorId, s.Supervisor)), "All supervisors");
            SetOptions(completionStatusFilter, students.Select(s => new KeyValuePair<string, string>(s.CompletionStatus, s.CompletionStatus)), "All completion statuses");
            studentListMessage.Text = "";
        }

        private static void SetOptions(HtmlSelect control, IEnumerable<KeyValuePair<string, string>> options, string title)
        {
            string selected = control.Value;
            control.Items.Clear();
            control.Items.Add(new ListItem(title, ""));
            foreach (var option in options.Where(o => !string.IsNullOrWhiteSpace(o.Key)).GroupBy(o => o.Key)
                .Select(g => g.First()).OrderBy(o => o.Value))
                control.Items.Add(new ListItem(option.Value, option.Key));
            control.Value = control.Items.FindByValue(selected) != null ? selected : "";
        }

        protected void StudentGrid_RowDataBound(object sender, GridViewRowEventArgs e)
        {
            var student = e.Row.DataItem as StudentListRecord;
            if (student == null) return;
            e.Row.Attributes["data-student-row"] = "true";
            e.Row.Attributes["data-search"] = (student.FullName + " " + student.StudentId + " " + student.Email).ToLowerInvariant();
            e.Row.Attributes["data-department"] = student.MajorId;
            e.Row.Attributes["data-supervisor"] = string.IsNullOrEmpty(student.SupervisorId) ? "__unassigned" : student.SupervisorId;
            e.Row.Attributes["data-completion"] = student.CompletionStatus;
        }

        protected async void ViewDetails_Click(object sender, EventArgs e)
        {
            await RunAsync(async () =>
            {
                string uid = studentsGrid.SelectedDataKey?.Value as string;
                var student = (await Service.ListAsync()).FirstOrDefault(s => s.Uid == uid);
                if (student == null) throw new StudentManagementException("Student account not found. Refresh the list.");
                studentDetailsContent.Text = "<dl class=\"completion-info\">" +
                    DetailRow("Full name", student.FullName) + DetailRow("Student ID", student.StudentId) +
                    DetailRow("Email", student.Email) + DetailRow("University", student.UniversityId) +
                    DetailRow("College", student.CollegeId) + DetailRow("Department", student.Department) +
                    DetailRow("Required hours", student.RequiredHours.ToString()) + DetailRow("Academic supervisor", student.Supervisor) +
                    DetailRow("Account", student.AccountStatus) + DetailRow("Completion", student.CompletionStatus) +
                    DetailRow("Student confirmed (UTC)", UtcDate(student.ConfirmedAt)) +
                    DetailRow("Disable deadline (UTC)", UtcDate(student.DisableAt)) +
                    DetailRow("Certificate", student.CertificateId) +
                    DetailRow("Password change required", student.MustChangePassword ? "Yes" : "No") + "</dl>";
                studentDetailsPanel.Visible = true;
                openDetails = true;
            });
        }

        protected void DeleteStudent_Click(object sender, EventArgs e)
        {
            // The posted UID is untrusted: authorization and the fresh student profile are checked before deletion.
            string uid = studentDeleteUid.Value;
            RegisterAsyncTask(new PageAsyncTask(() => RunAsync(async () =>
            {
                if (string.IsNullOrWhiteSpace(uid))
                    throw new StudentManagementException("Select a student from the refreshed list.");
                await Service.DeleteAsync(uid, await AuthorizeAsync());
                Session.Remove(CandidateKey);
                studentDetailsPanel.Visible = false;
                studentDetailsContent.Text = "";
                studentsGrid.SelectedIndex = -1;
                await LoadStudentsAsync();
                studentListMessage.Text = "Student account and linked internship history deleted. Official university records were retained; recreating this student starts fresh.";
            })));
        }
        private StudentCandidate Candidate()
        {
            string json = Session[CandidateKey] as string;
            var candidate = string.IsNullOrEmpty(json) ? null : JsonConvert.DeserializeObject<StudentCandidate>(json);
            if (candidate == null || candidate.Record == null || candidate.ExpiresUtc <= DateTimeOffset.UtcNow ||
                candidate.Record.studentId != studentUniversityId.Value.Trim())
            {
                Session.Remove(CandidateKey);
                throw new StudentManagementException("Find the student again; the lookup expired or the ID changed.");
            }
            return candidate;
        }

        private void StoreCandidate(StudentCandidate candidate)
        {
            Session[CandidateKey] = JsonConvert.SerializeObject(candidate);
        }

        protected override void OnPreRenderComplete(EventArgs e)
        {
            // A password must never be persisted in client-readable ViewState.
            studentTemporaryPassword.EnableViewState = false;
            studentTemporaryPassword.Value = "";
            studentName.Value = "";
            studentEmail.Value = "";
            studentDepartment.Value = "";
            StudentCandidate candidate = null;
            if (authorized && openEditor)
            {
                try { candidate = Candidate(); } catch (StudentManagementException) { }
                if (candidate != null)
                {
                    studentName.Value = candidate.Record.fullName;
                    studentEmail.Value = candidate.Record.email;
                    studentDepartment.Value = StudentManagementService.DisplayName(candidate.Record.majorId);
                    studentTemporaryPassword.Value = candidate.Password ?? "";
                }
            }
            findStudentButton.Enabled = authorized;
            generateStudentPassword.Enabled = authorized && candidate != null && !candidate.Created;
            saveStudentButton.Enabled = authorized && candidate != null && !candidate.Created && !string.IsNullOrEmpty(candidate.Password);
            emailDeliveryNotice.Visible = authorized && !emailConfigured;
            if (!authorized)
            {
                studentsGrid.Visible = false;
                studentDetailsPanel.Visible = false;
                openEditor = false;
                openDetails = false;
            }
            studentCsrf.Value = Session[CsrfKey] as string ?? "";
            studentPageState.Value = JsonConvert.SerializeObject(new
            {
                canManage = authorized, openEditor, openDetails,
                canGenerate = authorized && candidate != null, emailConfigured
            });
            base.OnPreRenderComplete(e);
        }

        private static string DetailRow(string label, string value)
        {
            return "<div><dt>" + HttpUtility.HtmlEncode(label) + "</dt><dd>" +
                HttpUtility.HtmlEncode(string.IsNullOrWhiteSpace(value) ? "—" : value) + "</dd></div>";
        }

        private static string UtcDate(string value)
        {
            DateTimeOffset date;
            return DateTimeOffset.TryParse(value, out date) ? date.UtcDateTime.ToString("yyyy-MM-dd HH:mm 'UTC'") : "—";
        }
    }
}
