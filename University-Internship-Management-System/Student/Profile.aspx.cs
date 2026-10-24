using System;
using System.Collections.Generic;
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

            if (Request.QueryString["download"] == "cv")
            { RegisterAsyncTask(new PageAsyncTask(DownloadCvAsync)); return; }
            if (!IsPostBack)
                RegisterAsyncTask(new PageAsyncTask(LoadProfileAsync));
        }

        private bool fileResponse;
        protected override void Render(HtmlTextWriter writer)
        { if (!fileResponse) base.Render(writer); }
        private async Task DownloadCvAsync()
        {
            fileResponse = true;
            try
            {
                var profile = await GetVerifiedProfileAsync();
                string uid = Context.User.Identity.Name;
                string path = Text(profile, "cvObjectPath");
                if (!path.StartsWith("uims/student-cvs/" + StudentManagementService.Key(uid) + "/", StringComparison.Ordinal))
                    throw new UIMS.Services.FirebaseException(404, "No saved CV is available.");
                var storage = new UIMS.Services.FirebaseFileStorageService(key => ConfigurationManager.AppSettings[key]);
                byte[] bytes = await storage.DownloadAsync(path, Text(profile, "cvStorageProvider"), Response.ClientDisconnectedToken);
                if (bytes.LongLength != (long?)profile["cvSizeBytes"] || University_Internship_Management_System.Services.FinalEvaluation.CompletionReportValidation.Hash(bytes) != Text(profile, "cvSha256"))
                    throw new UIMS.Services.FirebaseException(409, "The saved CV failed integrity verification.");
                Response.Clear(); Response.ContentType = Text(profile, "cvContentType");
                Response.Cache.SetNoStore(); Response.AddHeader("X-Content-Type-Options", "nosniff");
                Response.AddHeader("Content-Disposition", "attachment; filename*=UTF-8''" + Uri.EscapeDataString(UIMS.Services.FirebaseFileStorageService.SanitizeFileName(Text(profile, "cvFileName"))));
                Response.OutputStream.Write(bytes, 0, bytes.Length);
            }
            catch (UIMS.Services.FirebaseException error) { Response.Clear(); Response.StatusCode = error.StatusCode; Response.ContentType = "text/plain"; Response.Write(error.Message); }
            catch (Exception) { Response.Clear(); Response.StatusCode = 503; Response.ContentType = "text/plain"; Response.Write("Your CV could not be downloaded. Sign in again or contact your administrator."); }
            Context.ApplicationInstance.CompleteRequest();
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

        private async Task LoadProfileAsync()
        {
            try
            {
                var profile = await GetVerifiedProfileAsync();
                BindProfile(profile);
            }
            catch (FirebaseLoginException error)
            {
                SetMessage(error.Message, true);
                btnSave.Enabled = false;
            }
            catch (Exception)
            {
                SetMessage("Your Firebase profile could not be loaded. Sign in again or contact your coordinator.", true);
                btnSave.Enabled = false;
            }
        }

        private async Task SaveProfileAsync(string phone, string city, string portfolioUrl)
        {
            UIMS.Services.FirebaseFileStorageService storage = null;
            string uploadedPath = null;
            string previousPath = null;
            string uploadedFileName = null;
            try
            {
                var profile = await GetVerifiedProfileAsync();
                string uid = Context.User.Identity.Name;
                string token = Session["Uims.FirebaseIdToken"] as string;

                var updates = new Dictionary<string, object>
                {
                    ["phone"] = phone,
                    ["city"] = city,
                    ["portfolioUrl"] = portfolioUrl
                };

                bool hasNewCv = cvUpload.HasFile && cvUpload.PostedFile != null;
                if (hasNewCv)
                {
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

                    string objectPath = "uims/student-cvs/" + StudentManagementService.Key(uid) + "/" +
                        UIMS.Services.FirebaseFileStorageService.CreateFileId();
                    storage = new UIMS.Services.FirebaseFileStorageService(key => ConfigurationManager.AppSettings[key]);
                    var stored = await storage.UploadAsync(objectPath, fileName, contentType, bytes, Response.ClientDisconnectedToken);
                    uploadedPath = stored.ObjectPath;
                    uploadedFileName = stored.FileName;
                    previousPath = Text(profile, "cvObjectPath");

                    updates["cvStorageProvider"] = stored.Provider;
                    updates["cvObjectPath"] = stored.ObjectPath;
                    updates["cvFileName"] = stored.FileName;
                    updates["cvContentType"] = stored.ContentType;
                    updates["cvSizeBytes"] = stored.SizeBytes;
                    updates["cvSha256"] = stored.Sha256;
                }

                await FirebaseSignInService.PatchDatabasePathAsync("users/" + StudentManagementService.Key(uid), updates, token);
                uploadedPath = null;

                if (hasNewCv && !string.IsNullOrWhiteSpace(previousPath) && !string.Equals(previousPath, Text(profile, "cvObjectPath"), StringComparison.Ordinal))
                {
                    try { await storage.DeleteObjectAsync(previousPath, Text(profile, "cvStorageProvider"), CancellationToken.None); }
                    catch (Exception) { }
                }

                BindProfile(profile);
                txtPhone.Text = phone;
                txtCity.Text = city;
                txtPortfolioUrl.Text = portfolioUrl;
                if (hasNewCv)
                    litCvFileName.Text = uploadedFileName;
                SetMessage(hasNewCv
                    ? "Your profile details and CV were saved successfully."
                    : "Your contact information and portfolio link were saved to Firebase Realtime Database.", false);
            }
            catch (UIMS.Services.FirebaseException error)
            {
                if (uploadedPath != null && storage != null)
                    try { await storage.DeleteObjectAsync(uploadedPath, CancellationToken.None); } catch { }
                if (cvUpload.HasFile)
                    litCvFileName.Text = Path.GetFileName(cvUpload.FileName);
                SetMessage(error.Message, true);
            }
            catch (FirebaseLoginException error)
            {
                if (uploadedPath != null && storage != null)
                    try { await storage.DeleteObjectAsync(uploadedPath, CancellationToken.None); } catch { }
                if (cvUpload.HasFile)
                    litCvFileName.Text = Path.GetFileName(cvUpload.FileName);
                SetMessage(error.Message, true);
            }
            catch (Exception)
            {
                if (uploadedPath != null && storage != null)
                    try { await storage.DeleteObjectAsync(uploadedPath, CancellationToken.None); } catch { }
                if (cvUpload.HasFile)
                    litCvFileName.Text = Path.GetFileName(cvUpload.FileName);
                SetMessage("Your changes could not be saved. No successful Firebase update was confirmed; check the connection and try again.", true);
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
            if (verified.Profile == null)
                throw new FirebaseLoginException("Your account was verified, but its profile is missing at /users/" +
                    StudentManagementService.Key(uid) + ". Contact your coordinator to restore the profile record.", 404);
            return verified.Profile;
        }

        private void BindProfile(JObject profile)
        {
            if (profile == null)
                throw new FirebaseLoginException("The student profile could not be loaded from Firebase. Sign in again or contact your coordinator.", 404);

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
            litCvFileName.Text = string.IsNullOrWhiteSpace(cvFileName) ? "No file chosen" : cvFileName;
        }

        private void SetMessage(string message, bool isError)
        {
            pnlMessage.Visible = !string.IsNullOrWhiteSpace(message);
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
            if (profile == null)
                return string.Empty;
            var value = profile[field];
            return value == null || value.Type == JTokenType.Null ? string.Empty : value.ToString();
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
