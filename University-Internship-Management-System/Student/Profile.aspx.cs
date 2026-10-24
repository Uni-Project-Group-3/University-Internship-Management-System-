using System;
using System.Configuration;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;
using System.Web.Security;
using System.Web.UI;
using System.Web.UI.HtmlControls;
using Newtonsoft.Json.Linq;
using University_Internship_Management_System.Shared;

namespace University_Internship_Management_System.Student
{
    public partial class Profile : Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            Response.Cache.SetNoStore();
            if (!Context.User.Identity.IsAuthenticated || !Context.User.IsInRole("student"))
            {
                Response.Redirect("../Auth/Login.aspx", true);
                return;
            }

            cvUpload.Attributes["accept"] = ".pdf,.doc,.docx,application/pdf,application/msword,application/vnd.openxmlformats-officedocument.wordprocessingml.document";

            if (!IsPostBack)
                RegisterAsyncTask(new PageAsyncTask(LoadProfileAsync));
        }

        protected void btnSave_Click(object sender, EventArgs e)
        {
            string phone = txtPhone.Text.Trim();
            string city = txtCity.Text.Trim();
            string portfolioUrl = txtPortfolioUrl.Text.Trim();

            if (phone.Length > 0 && !Regex.IsMatch(phone, @"\A[+0-9() .-]{7,25}\z"))
            {
                SetMessage("Enter a valid phone number (7 to 25 characters), or leave it blank.", true);
                return;
            }
            if (city.Length > 80)
            {
                SetMessage("City must be 80 characters or fewer.", true);
                return;
            }
            if (portfolioUrl.Length > 2000 || (portfolioUrl.Length > 0 && !IsHttpsUrl(portfolioUrl)))
            {
                SetMessage("Enter a valid portfolio link starting with https://, or leave it blank.", true);
                return;
            }

            RegisterAsyncTask(new PageAsyncTask(() => SaveProfileAsync(phone, city, portfolioUrl)));
        }

        protected void btnUploadCv_Click(object sender, EventArgs e)
        {
            RegisterAsyncTask(new PageAsyncTask(UploadCvAsync));
        }

        private async Task LoadProfileAsync()
        {
            try
            {
                var profile = await GetVerifiedProfileAsync();
                BindProfile(profile);
                SetMessage("Your profile was loaded from Firebase Realtime Database.", false);
            }
            catch (Exception)
            {
                SetMessage("Your Firebase profile could not be loaded. Sign in again or contact your coordinator.", true);
                btnSave.Enabled = false;
            }
        }

        private async Task SaveProfileAsync(string phone, string city, string portfolioUrl)
        {
            try
            {
                var profile = await GetVerifiedProfileAsync();
                string uid = Context.User.Identity.Name;
                string token = Session["Uims.FirebaseIdToken"] as string;
                await FirebaseSignInService.PatchDatabasePathAsync("users/" + StudentManagementService.Key(uid), new
                {
                    phone = phone,
                    city = city,
                    portfolioUrl = portfolioUrl
                }, token);

                BindProfile(profile);
                txtPhone.Text = phone;
                txtCity.Text = city;
                txtPortfolioUrl.Text = portfolioUrl;
                SetMessage("Your contact information and portfolio link were saved to Firebase Realtime Database.", false);
            }
            catch (Exception)
            {
                SetMessage("Your changes could not be saved. No successful Firebase update was confirmed; check the connection and try again.", true);
            }
        }

        private async Task UploadCvAsync()
        {
            UIMS.Services.FirebaseFileStorageService storage = null;
            string uploadedPath = null;
            try
            {
                var profile = await GetVerifiedProfileAsync();
                if (!cvUpload.HasFile || cvUpload.PostedFile == null)
                    throw new UIMS.Services.FirebaseException(400, "Choose your CV file first.");
                if (cvUpload.PostedFile.ContentLength < 1 || cvUpload.PostedFile.ContentLength > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes)
                    throw new UIMS.Services.FirebaseException(413, "Choose a non-empty CV file no larger than 10 MiB.");

                string fileName = UIMS.Services.FirebaseFileStorageService.SanitizeFileName(cvUpload.FileName);
                string extension = Path.GetExtension(fileName).ToLowerInvariant();
                string contentType;
                switch (extension)
                {
                    case ".pdf": contentType = "application/pdf"; break;
                    case ".doc": contentType = "application/msword"; break;
                    case ".docx": contentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document"; break;
                    default: throw new UIMS.Services.FirebaseException(415, "Allowed CV types are PDF, DOC, and DOCX.");
                }

                byte[] bytes;
                using (var output = new MemoryStream())
                {
                    var buffer = new byte[81920];
                    int count;
                    while ((count = await cvUpload.PostedFile.InputStream.ReadAsync(buffer, 0, buffer.Length, Response.ClientDisconnectedToken)) > 0)
                    {
                        if (output.Length + count > UIMS.Services.FirebaseFileStorageService.MaximumFileBytes)
                            throw new UIMS.Services.FirebaseException(413, "CV files are limited to 10 MiB.");
                        await output.WriteAsync(buffer, 0, count, Response.ClientDisconnectedToken);
                    }
                    bytes = output.ToArray();
                }

                string uid = Context.User.Identity.Name;
                string objectPath = "uims/student-cvs/" + StudentManagementService.Key(uid) + "/" +
                    UIMS.Services.FirebaseFileStorageService.CreateFileId();
                storage = new UIMS.Services.FirebaseFileStorageService(key => ConfigurationManager.AppSettings[key]);
                var stored = await storage.UploadAsync(objectPath, fileName, contentType, bytes, Response.ClientDisconnectedToken);
                uploadedPath = stored.ObjectPath;

                string token = Session["Uims.FirebaseIdToken"] as string;
                await FirebaseSignInService.PatchDatabasePathAsync("users/" + StudentManagementService.Key(uid), new
                {
                    cvObjectPath = stored.ObjectPath,
                    cvFileName = stored.FileName,
                    cvContentType = stored.ContentType,
                    cvSizeBytes = stored.SizeBytes,
                    cvSha256 = stored.Sha256
                }, token);

                string previousPath = Text(profile, "cvObjectPath");
                uploadedPath = null;
                if (!string.IsNullOrWhiteSpace(previousPath) && !string.Equals(previousPath, stored.ObjectPath, StringComparison.Ordinal))
                {
                    try { await storage.DeleteObjectAsync(previousPath, CancellationToken.None); }
                    catch (Exception) { }
                }

                litCvStatus.Text = "CV uploaded: " + stored.FileName;
                SetMessage("Your CV was uploaded and linked to your student profile.", false);
            }
            catch (UIMS.Services.FirebaseException error)
            {
                if (uploadedPath != null && storage != null)
                    try { await storage.DeleteObjectAsync(uploadedPath, CancellationToken.None); } catch { }
                litCvStatus.Text = string.Empty;
                SetMessage(error.Message, true);
            }
            catch (Exception)
            {
                if (uploadedPath != null && storage != null)
                    try { await storage.DeleteObjectAsync(uploadedPath, CancellationToken.None); } catch { }
                litCvStatus.Text = string.Empty;
                SetMessage("The CV could not be uploaded. Check Firebase Storage setup and your connection, then try again.", true);
            }
        }

        private async Task<JObject> GetVerifiedProfileAsync()
        {
            string uid = Context.User.Identity.Name;
            string token = Session["Uims.FirebaseIdToken"] as string;
            if (string.IsNullOrWhiteSpace(uid) || string.IsNullOrWhiteSpace(token))
            {
                FormsAuthentication.SignOut();
                Response.Redirect("../Auth/Login.aspx", true);
                return null;
            }

            var verified = await new FirebaseSignInService().VerifySessionAsync(token);
            if (!string.Equals(verified.Uid, uid, StringComparison.Ordinal) || verified.Role != "student")
                throw new HttpException(403, "A verified student account is required.");
            return verified.Profile;
        }

        private void BindProfile(JObject profile)
        {
            string name = Text(profile, "fullName");
            string email = First(profile, "universityEmail", "email");
            string studentId = First(profile, "studentId", "universityStudentId");
            string major = First(profile, "majorName", "majorId");
            if (major.Length == 0) major = Text(profile, "departmentId");
            string hours = First(profile, "requiredTrainingHours", "requiredHours");

            litFullName.Text = Fallback(name);
            litStudentId.Text = Fallback(studentId);
            litMajor.Text = Fallback(major);
            litRequiredHours.Text = hours.Length == 0 ? "Not provided" : hours + " hours";
            litUniversityEmail.Text = Fallback(email);
            litSummaryName.Text = Fallback(name, "Student Profile");
            litSummaryInitials.Text = GetInitials(name);
            litSummaryDetails.Text = Join(major, studentId);
            lblUserName.InnerText = ShortName(name);
            lblInitials.InnerText = GetInitials(name);

            txtPhone.Text = Text(profile, "phone");
            txtCity.Text = Text(profile, "city");
            txtPortfolioUrl.Text = Text(profile, "portfolioUrl");
            string cvFileName = Text(profile, "cvFileName");
            litCvStatus.Text = string.IsNullOrWhiteSpace(cvFileName) ? "No CV uploaded yet." : "Current CV: " + cvFileName;
        }

        private void SetMessage(string message, bool isError)
        {
            litProfileMessage.Text = message;
            pnlMessage.Attributes["role"] = isError ? "alert" : "status";
        }

        private static bool IsHttpsUrl(string value)
        {
            Uri uri;
            return Uri.TryCreate(value, UriKind.Absolute, out uri) &&
                string.Equals(uri.Scheme, Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) &&
                string.IsNullOrEmpty(uri.UserInfo) && uri.Host.Length > 0;
        }

        private static string Text(JObject profile, string field)
        {
            return profile[field] == null || profile[field].Type == JTokenType.Null
                ? string.Empty : profile[field].ToString();
        }

        private static string First(JObject profile, string first, string second)
        {
            string value = Text(profile, first);
            return value.Length == 0 ? Text(profile, second) : value;
        }

        private static string Fallback(string value, string fallback = "Not provided")
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private static string Join(string first, string second)
        {
            if (string.IsNullOrWhiteSpace(first) && string.IsNullOrWhiteSpace(second)) return "Student account";
            if (string.IsNullOrWhiteSpace(first)) return "Student ID: " + second;
            if (string.IsNullOrWhiteSpace(second)) return first;
            return first + " · Student ID: " + second;
        }

        private static string GetInitials(string name)
        {
            var parts = (name ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length == 0) return "ST";
            string initials = char.ToUpperInvariant(parts[0][0]).ToString();
            if (parts.Length > 1) initials += char.ToUpperInvariant(parts[parts.Length - 1][0]);
            return initials;
        }

        private static string ShortName(string name)
        {
            var parts = (name ?? string.Empty).Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2) return Fallback(name, "Student account");
            return parts[0] + " " + char.ToUpperInvariant(parts[parts.Length - 1][0]) + ".";
        }
    }
}
