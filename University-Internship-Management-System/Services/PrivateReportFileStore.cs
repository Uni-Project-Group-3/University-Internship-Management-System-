using System;
using System.IO;
using System.Linq;
using System.Security.AccessControl;
using System.Security.Cryptography;
using System.Security.Principal;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Web;

namespace UIMS.Services
{
    // Single-server, private persistent storage. Never mapped to a public/static URL.
    // WeeklyReportService performs authenticated placement authorization before calling this transport.
    public sealed class PrivateReportFileStore
    {
        private readonly Func<string, string> settings;
        public PrivateReportFileStore(Func<string, string> settings) { this.settings = settings; }
        private string Root(bool write)
        {
            if (write && !string.Equals(settings("WeeklyReportPrivateDiskWritesEnabled"), "true", StringComparison.OrdinalIgnoreCase))
                throw new FirebaseException(503, "Private report uploads are unavailable in this environment.");
            string configured = settings("WeeklyReportPrivateRoot");
            if (string.IsNullOrWhiteSpace(configured) || !Path.IsPathRooted(configured) || configured.StartsWith(@"\\", StringComparison.Ordinal))
                throw new FirebaseException(503, "Configure a private persistent local report directory outside the website.");
            string root = Path.GetFullPath(configured).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            string web = System.Web.Hosting.HostingEnvironment.IsHosted ? System.Web.Hosting.HostingEnvironment.ApplicationPhysicalPath : AppDomain.CurrentDomain.GetData(".appPath") as string;
            if (!string.IsNullOrEmpty(web) && root.StartsWith(Path.GetFullPath(web).TrimEnd('\\') + "\\", StringComparison.OrdinalIgnoreCase))
                throw new FirebaseException(503, "The private report directory cannot be inside the website.");
            var directory = new DirectoryInfo(root);
            if (!directory.Exists) throw new FirebaseException(503, "The private persistent report directory is unavailable.");
            for (var parent = directory; parent != null; parent = parent.Parent)
                if ((parent.Attributes & FileAttributes.ReparsePoint) != 0)
                    throw new FirebaseException(503, "Private report storage cannot use directory links.");
            var acl = directory.GetAccessControl();
            if (!acl.AreAccessRulesProtected) throw new FirebaseException(503, "Private report storage must have explicit restricted Windows permissions.");
            string own = WindowsIdentity.GetCurrent().User.Value;
            foreach (FileSystemAccessRule rule in acl.GetAccessRules(true, true, typeof(SecurityIdentifier)))
                if (rule.AccessControlType == AccessControlType.Allow && rule.IdentityReference.Value != own &&
                    rule.IdentityReference.Value != "S-1-5-18" && rule.IdentityReference.Value != "S-1-5-32-544")
                    throw new FirebaseException(503, "Private report storage grants access to an unapproved Windows identity.");
            return root;
        }
        private static string Key(string objectPath)
        {
            if (!Regex.IsMatch(objectPath ?? "", @"\Auims/weekly-reports/WR-[a-f0-9]{64}/REV-[a-f0-9]{32}/[a-f0-9]{48}\.docx\z"))
                throw new FirebaseException(400, "Invalid private weekly report object path.");
            using (var sha = SHA256.Create()) return BitConverter.ToString(sha.ComputeHash(Encoding.UTF8.GetBytes(objectPath))).Replace("-", "").ToLowerInvariant() + ".docx";
        }
        private static void NoLink(string path)
        {
            if (File.Exists(path) && (File.GetAttributes(path) & FileAttributes.ReparsePoint) != 0)
                throw new FirebaseException(503, "Private report storage cannot use file links.");
        }
        private static async Task<FileStream> Lock(string root, CancellationToken ct)
        {
            string path = Path.Combine(root, ".upload.lock"); NoLink(path);
            for (int i = 0; i < 50; i++)
            {
                ct.ThrowIfCancellationRequested();
                try { return new FileStream(path, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None); }
                catch (IOException) { await Task.Delay(100, ct).ConfigureAwait(false); }
            }
            throw new FirebaseException(503, "Private report storage is busy; retry the upload.");
        }
        public async Task<StoredFile> UploadAsync(string objectPath, string name, string type, byte[] bytes, CancellationToken ct)
        {
            string safe = FirebaseFileStorageService.SanitizeFileName(name);
            string mime = FirebaseFileStorageService.ValidateFile(safe, type, bytes);
            if (!safe.EndsWith(".docx", StringComparison.OrdinalIgnoreCase)) throw new FirebaseException(415, "Weekly reports require a valid DOCX file.");
            string root = Root(true), path = Path.Combine(root, Key(objectPath));
            using (await Lock(root, ct).ConfigureAwait(false))
            {
                NoLink(path);
                if (File.Exists(path)) throw new FirebaseException(409, "The private report already exists; no file was overwritten.");
                long quota;
                if (!long.TryParse(settings("WeeklyReportPrivateQuotaBytes"), out quota) || quota < FirebaseFileStorageService.MaximumFileBytes)
                    throw new FirebaseException(503, "Configure a private report storage quota.");
                long used = new DirectoryInfo(root).EnumerateFiles().Sum(f => f.Length);
                if (used > quota - bytes.LongLength) throw new FirebaseException(507, "Private report storage is full. No report was saved.");
                string temp = Path.Combine(root, Guid.NewGuid().ToString("N") + ".partial");
                try
                {
                    using (var file = new FileStream(temp, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, true))
                    { await file.WriteAsync(bytes, 0, bytes.Length, ct).ConfigureAwait(false); file.Flush(true); }
                    ct.ThrowIfCancellationRequested(); File.Move(temp, path);
                }
                finally { if (File.Exists(temp)) File.Delete(temp); }
            }
            string hash; using (var sha = SHA256.Create()) hash = BitConverter.ToString(sha.ComputeHash(bytes)).Replace("-", "").ToLowerInvariant();
            return new StoredFile { ObjectPath = objectPath, FileName = safe, ContentType = mime, SizeBytes = bytes.Length, Sha256 = hash, Generation = Guid.NewGuid().ToString("N") };
        }
        public async Task<byte[]> DownloadAsync(string objectPath, CancellationToken ct)
        {
            string path = Path.Combine(Root(false), Key(objectPath)); NoLink(path);
            if (!File.Exists(path)) throw new FirebaseException(404, "The private report file is unavailable on this server.");
            using (var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 81920, true))
            {
                if (file.Length == 0 || file.Length > FirebaseFileStorageService.MaximumFileBytes) throw new FirebaseException(413, "Invalid private report size.");
                byte[] bytes = new byte[(int)file.Length]; int offset = 0, count;
                while ((count = await file.ReadAsync(bytes, offset, bytes.Length - offset, ct).ConfigureAwait(false)) > 0) offset += count;
                if (offset != bytes.Length) throw new FirebaseException(502, "The private report file is incomplete.");
                return bytes;
            }
        }
        // Compensation is used only for a newly generated object that failed to acquire report metadata.
        public async Task DeleteObjectAsync(string objectPath, CancellationToken ct)
        {
            string root = Root(true), path = Path.Combine(root, Key(objectPath));
            using (await Lock(root, ct).ConfigureAwait(false)) { NoLink(path); if (File.Exists(path)) File.Delete(path); }
        }
    }
}
