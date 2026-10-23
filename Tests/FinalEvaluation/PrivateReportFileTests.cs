using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.AccessControl;
using System.Security.Principal;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using UIMS.Services;
using UIMS.Tasks;

internal static partial class Program
{
    private static async Task RunPrivateReportFileTests()
    {
        string root = Path.Combine(Path.GetTempPath(), "uims-private-file-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var acl = new DirectorySecurity(); acl.SetAccessRuleProtection(true, false);
        acl.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl, InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit, PropagationFlags.None, AccessControlType.Allow));
        new DirectoryInfo(root).SetAccessControl(acl);
        try
        {
            var settings = WeeklySettings(); settings["WeeklyReportFileProvider"] = "PrivateDisk";
            settings["FirebaseStorageEnabled"] = "false"; settings["FirebaseStorageLiveWritesEnabled"] = "false";
            settings["WeeklyReportPrivateRoot"] = root; settings["WeeklyReportPrivateDiskWritesEnabled"] = "true";
            settings["WeeklyReportPrivateQuotaBytes"] = "104857600";
            Func<string, string> get = k => settings.ContainsKey(k) ? settings[k] : null;
            var store = new PrivateReportFileStore(get);
            string path = "uims/weekly-reports/WR-" + new string('a', 64) + "/REV-" + new string('b', 32) + "/" + new string('c', 48) + ".docx";
            await Test("private disk validates DOCX and rejects traversal, duplicate and disabled writes", async () => {
                await Reject<FirebaseException>(() => store.UploadAsync("../report.docx", "report.docx", WeeklyMime, WeeklyDocx(), CancellationToken.None));
                await Reject<FirebaseException>(() => store.UploadAsync(path, "report.docx", WeeklyMime, new byte[] { 1, 2 }, CancellationToken.None));
                var file = await store.UploadAsync(path, "report.docx", WeeklyMime, WeeklyDocx(), CancellationToken.None);
                Assert((await store.DownloadAsync(path, CancellationToken.None)).SequenceEqual(WeeklyDocx()));
                await Reject<FirebaseException>(() => store.UploadAsync(path, "report.docx", WeeklyMime, WeeklyDocx(), CancellationToken.None));
                settings["WeeklyReportPrivateDiskWritesEnabled"] = "false";
                await Reject<FirebaseException>(() => store.DeleteObjectAsync(path, CancellationToken.None));
                settings["WeeklyReportPrivateDiskWritesEnabled"] = "true";
                await store.DeleteObjectAsync(path, CancellationToken.None);
                await Reject<FirebaseException>(() => store.DownloadAsync(path, CancellationToken.None));
            });
            await Test("private disk rejects unsafe permissions and cancelled uploads without partial reports", async () => {
                var permissive = new DirectorySecurity(); permissive.SetAccessRuleProtection(true, false);
                permissive.AddAccessRule(new FileSystemAccessRule(WindowsIdentity.GetCurrent().User, FileSystemRights.FullControl, AccessControlType.Allow));
                permissive.AddAccessRule(new FileSystemAccessRule(new SecurityIdentifier(WellKnownSidType.WorldSid, null), FileSystemRights.Read, AccessControlType.Allow));
                new DirectoryInfo(root).SetAccessControl(permissive);
                try { await Reject<FirebaseException>(() => store.UploadAsync(path, "report.docx", WeeklyMime, WeeklyDocx(), CancellationToken.None)); }
                finally { var restored = new DirectorySecurity(); restored.SetSecurityDescriptorSddlForm(acl.GetSecurityDescriptorSddlForm(AccessControlSections.Access), AccessControlSections.Access); new DirectoryInfo(root).SetAccessControl(restored); }
                var cancel = new CancellationToken(true);
                await Reject<OperationCanceledException>(() => store.UploadAsync(path, "report.docx", WeeklyMime, WeeklyDocx(), cancel));
                Assert(!Directory.GetFiles(root, "*.docx").Any() && !Directory.GetFiles(root, "*.partial").Any());
            });
            await Test("weekly reports use private disk with Firebase Storage disabled and retain supervisor authorization", async () => {
                var db = new WeeklyReportDb(); SeedWeekly(db);
                Func<TaskActor, WeeklyReportService> service = actor => new WeeklyReportService(get, new HttpClient(db), actor, ct => Task.FromResult("isolated-weekly-token"));
                var company = service(WeeklyCompanyActor()); var report = await NewWeeklyReport(company);
                string id = (string)report["reportId"];
                Assert((string)report["revisions"]?[(string)report["currentRevisionId"]]?["storageProvider"] == "PrivateDisk");
                Assert((await service(WeeklyAcademicActor()).DownloadAsync(id, null, CancellationToken.None)).Content.SequenceEqual(WeeklyDocx()));
                Assert((await company.DownloadAsync(id, null, CancellationToken.None)).Content.SequenceEqual(WeeklyDocx()));
                await Reject<FirebaseException>(() => service(WeeklyCompanyActor("CO-2", "CS-2")).DownloadAsync(id, null, CancellationToken.None));
                await Reject<FirebaseException>(() => service(WeeklyAcademicActor("AC-2")).DownloadAsync(id, null, CancellationToken.None));
                await Reject<FirebaseException>(() => service(WeeklyStudentActor()).DownloadAsync(id, null, CancellationToken.None));
                string disk = Directory.GetFiles(root, "*.docx").Single(); File.WriteAllBytes(disk, WeeklyDocx().Concat(new byte[] { 0 }).ToArray());
                await Reject<FirebaseException>(() => company.DownloadAsync(id, null, CancellationToken.None));
            });
            await Test("private disk quota rejects uploads before creating a report object", async () => {
                settings["WeeklyReportPrivateQuotaBytes"] = "10485760";
                string reserve = Path.Combine(root, "isolated-quota-reservation");
                using (var f = File.Create(reserve)) f.SetLength(10485760);
                try { await Reject<FirebaseException>(() => store.UploadAsync(path, "report.docx", WeeklyMime, WeeklyDocx(), CancellationToken.None)); }
                finally { File.Delete(reserve); }
                Assert(!Directory.GetFiles(root, "*.partial").Any());
            });
        }
        finally
        {
            // Only this newly created isolated test directory is removed.
            Directory.Delete(root, true);
        }
    }
}
