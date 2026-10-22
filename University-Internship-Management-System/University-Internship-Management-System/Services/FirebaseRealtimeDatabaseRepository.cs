using System;
using System.Net;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Google.Apis.Auth.OAuth2;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Services
{
    public interface IFirebaseRealtimeDatabaseRepository
    {
        Task<JToken> ReadAsync(string collection, string id);
    }

    public sealed class StudentDataReadException : HttpRequestException
    {
        public int HttpStatus { get; }
        public StudentDataReadException(int status) : base("Firebase read was not accepted.") { HttpStatus = status; }
    }

    public sealed class FirebaseRecord
    {
        public JToken Value { get; set; }
        public string ETag { get; set; }
    }

    public interface IFirebaseAccountRepository : IFirebaseRealtimeDatabaseRepository
    {
        Task<FirebaseRecord> ReadVersionAsync(string collection, string id);
        Task WriteVersionAsync(string collection, string id, JToken value, string expectedETag);
    }

    public sealed class AccountConflictException : Exception
    {
        public AccountConflictException() : base("The account changed during this request. Reload its details before retrying.") { }
    }

    public sealed class FirebaseRealtimeDatabaseRepository : IFirebaseAccountRepository
    {
        private readonly string idToken;
        private readonly HttpClient client;
        public FirebaseRealtimeDatabaseRepository(string idToken = null, HttpClient client = null) { this.idToken = idToken; this.client = client ?? FirebaseHttp.Client; }
        private static readonly Lazy<GoogleCredential> Credential = new Lazy<GoogleCredential>(() =>
        {
            FirebaseConfiguration.ValidateCredentials();
            return GoogleCredential.GetApplicationDefault().CreateScoped(
                "https://www.googleapis.com/auth/firebase.database", "https://www.googleapis.com/auth/userinfo.email");
        });

        public async Task<JToken> ReadAsync(string collection, string id)
        {
            return (await ReadVersionAsync(collection, id).ConfigureAwait(false)).Value;
        }

        public async Task<FirebaseRecord> ReadVersionAsync(string collection, string id)
        {
            using (var request = await RequestAsync(HttpMethod.Get, collection, id).ConfigureAwait(false))
            {
                request.Headers.Add("X-Firebase-ETag", "true");
                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode) LoginDiagnostics.Write(response.StatusCode == HttpStatusCode.Unauthorized || response.StatusCode == HttpStatusCode.Forbidden ? "RtdbPermissionDenied" : "RtdbRequestFailure", (int)response.StatusCode);
                    if (!response.IsSuccessStatusCode) throw new StudentDataReadException((int)response.StatusCode);
                    return new FirebaseRecord { ETag = response.Headers.ETag?.ToString(),
                        Value = JToken.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false)) };
                }
            }
        }

        public Task<JObject> ReadStudentRecordsAsync(string collection, UimsAccount account)
        {
            RequireStudent(account);
            if (collection != "applications") throw new UnauthorizedAccessException();
            return ReadOwnedQueryAsync("applications", "studentId", account.ProfileId);
        }

        public async Task<JObject> ReadStudentProfileAsync(UimsAccount account)
        {
            RequireStudent(account);
            var record = await ReadAsync("students", account.ProfileId).ConfigureAwait(false) as JObject;
            if (record == null || (string)record["id"] != account.ProfileId) throw new System.IO.InvalidDataException("Student schema mismatch.");
            return record;
        }

        public async Task<JObject> ReadStudentPlacementAsync(UimsAccount account, string placementId)
        {
            RequireStudent(account); ValidateSegment(placementId);
            var record = await ReadAsync("placements", placementId).ConfigureAwait(false) as JObject;
            if (record == null || (string)record["id"] != placementId || (string)record["studentId"] != account.ProfileId)
                throw new System.IO.InvalidDataException("Placement ownership/schema mismatch.");
            return record;
        }

        public Task<JObject> ReadPlacementTasksAsync(UimsAccount account, string placementId)
        {
            RequireStudent(account); ValidateSegment(placementId);
            return ReadOwnedQueryAsync("tasks", "placementId", placementId);
        }

        public Task<JObject> ReadPublishedOpportunitiesAsync(UimsAccount account)
        {
            RequireStudent(account);
            return ReadOwnedQueryAsync("opportunities", "status", "published");
        }

        public Task<JObject> ReadStudentNotificationsAsync(UimsAccount account)
        {
            RequireStudent(account);
            return ReadOwnedQueryAsync("notifications", "recipientId", account.ProfileId);
        }

        public Task<JObject> ReadRoleDashboardRecordsAsync(UimsAccount account, string collection, string placementId = null)
        {
            if (string.IsNullOrEmpty(idToken) || account == null || account.MustChangePassword) throw new UnauthorizedAccessException();
            if (account.Role == "company")
            {
                if (string.IsNullOrWhiteSpace(account.CompanyId)) throw new UnauthorizedAccessException();
                if (collection == "opportunities" || collection == "applications" || collection == "placements") return ReadOwnedQueryAsync(collection, "companyId", account.CompanyId);
                if (collection == "tasks" || collection == "progressReports") return ReadVerifiedPlacementRecordsAsync(account, collection, placementId);
            }
            if (account.Role == "academic" && collection == "students") return ReadAssignedAcademicStudentsAsync(account);
            if (account.Role == "academic" && collection == "tasks") return ReadVerifiedPlacementRecordsAsync(account, collection, placementId);
            if (account.Role == "admin" && new[] { "students", "companies", "opportunities", "placements", "completionRecords", "contactMessages", "academicSupervisors", "companySupervisors" }.Contains(collection)) return ReadDashboardCollectionAsync(collection);
            throw new UnauthorizedAccessException();
        }

        private async Task<JObject> ReadAssignedAcademicStudentsAsync(UimsAccount account)
        {
            // Trusted index contains IDs only. Each profile is reauthorized by Firebase Rules
            // and checked for BOTH current supervisor assignment and academic program.
            var index = await ReadAsync("academicStudentIndex", account.ProfileId).ConfigureAwait(false);
            if (!(index is JObject ids)) throw new System.IO.InvalidDataException("The trusted academic student index is not configured.");
            var records = new JObject();
            foreach (var item in ids.Properties())
            {
                ValidateSegment(item.Name);
                if (item.Value.Type != JTokenType.Boolean || !(bool)item.Value) throw new System.IO.InvalidDataException("Invalid academic index.");
                var student = await ReadAsync("students", item.Name).ConfigureAwait(false) as JObject;
                if (student == null || (string)student["id"] != item.Name || (string)student["academicSupervisorId"] != account.ProfileId || string.IsNullOrEmpty(account.ProgramId) || (string)student["programId"] != account.ProgramId) throw new System.IO.InvalidDataException("Academic ownership/program mismatch.");
                records[item.Name] = student;
            }
            return records;
        }

        private async Task<JObject> ReadVerifiedPlacementRecordsAsync(UimsAccount account, string collection, string placementId)
        {
            ValidateSegment(placementId);
            var placement = await ReadAsync("placements", placementId).ConfigureAwait(false) as JObject;
            if (placement == null || (string)placement["id"] != placementId) throw new System.IO.InvalidDataException("Invalid placement.");
            if (account.Role == "company" && (string)placement["companyId"] != account.CompanyId) throw new UnauthorizedAccessException();
            if (account.Role == "academic")
            {
                string studentId = (string)placement["studentId"]; ValidateSegment(studentId);
                var student = await ReadAsync("students", studentId).ConfigureAwait(false) as JObject;
                if (student == null || (string)student["academicSupervisorId"] != account.ProfileId || string.IsNullOrEmpty(account.ProgramId) || (string)student["programId"] != account.ProgramId || (string)student["placementId"] != placementId || (string)placement["academicSupervisorId"] != account.ProfileId) throw new UnauthorizedAccessException();
            }
            return await ReadOwnedQueryAsync(collection, "placementId", placementId).ConfigureAwait(false);
        }

        private async Task<JObject> ReadDashboardCollectionAsync(string collection)
        {
            using (var response = await client.GetAsync(FirebaseConfiguration.DatabaseUrl + "/" + collection + ".json?auth=" + Uri.EscapeDataString(idToken)).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode) throw new StudentDataReadException((int)response.StatusCode);
                var value = JToken.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                var records = value as JObject ?? (value.Type == JTokenType.Null ? new JObject() : throw new System.IO.InvalidDataException("Invalid collection."));
                foreach (var item in records.Properties()) if (!(item.Value is JObject row) || row["id"] != null && (string)row["id"] != item.Name) throw new System.IO.InvalidDataException("Record identity mismatch.");
                return records;
            }
        }

        private void RequireStudent(UimsAccount account)
        {
            if (string.IsNullOrEmpty(idToken) || account == null || account.Role != "student" || account.MustChangePassword || string.IsNullOrEmpty(account.ProfileId))
                throw new UnauthorizedAccessException();
        }

        private async Task<JObject> ReadOwnedQueryAsync(string collection, string field, string ownerId)
        {
            ValidateSegment(ownerId);
            string url = FirebaseConfiguration.DatabaseUrl + "/" + collection + ".json?auth=" + Uri.EscapeDataString(idToken) +
                "&orderBy=" + Uri.EscapeDataString(JsonConvert.SerializeObject(field)) + "&equalTo=" + Uri.EscapeDataString(JsonConvert.SerializeObject(ownerId));
            using (var request = new HttpRequestMessage(HttpMethod.Get, url))
            using (var response = await client.SendAsync(request).ConfigureAwait(false))
            {
                if (!response.IsSuccessStatusCode) throw new StudentDataReadException((int)response.StatusCode);
                var value = JToken.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                var records = value as JObject ?? (value.Type == JTokenType.Null ? new JObject() : throw new System.IO.InvalidDataException("Invalid record collection."));
                foreach (var item in records.Properties())
                    if (!(item.Value is JObject row) || row[field]?.Type != JTokenType.String || (string)row[field] != ownerId ||
                        row["id"] != null && (string)row["id"] != item.Name)
                        throw new System.IO.InvalidDataException("Record ownership/schema mismatch.");
                return records;
            }
        }




        public async Task<JObject> ReadActiveTrainingStudentAsync(UimsAccount account,JObject placement)
        {
            RequireTraining(account);
            if(account.Role!="company"||placement==null||(string)placement["companyId"]!=account.CompanyId||(string)placement["status"]!="active")throw new UnauthorizedAccessException();
            string studentId=(string)placement["studentId"];ValidateSegment(studentId);
            var profile=new JObject();
            foreach(string field in new[]{"accountStatus","role","firebaseUid","displayName"})
            {
                string url=FirebaseConfiguration.DatabaseUrl+"/users/"+Uri.EscapeDataString(studentId)+"/"+field+".json?auth="+Uri.EscapeDataString(idToken);
                using(var response=await client.GetAsync(url).ConfigureAwait(false))
                {
                    if(response.StatusCode==HttpStatusCode.Unauthorized||response.StatusCode==HttpStatusCode.Forbidden)return null;
                    if(!response.IsSuccessStatusCode)throw new StudentDataReadException((int)response.StatusCode);
                    profile[field]=JToken.Parse(await response.Content.ReadAsStringAsync().ConfigureAwait(false));
                }
            }
            string uid=(string)profile["firebaseUid"];
            if((string)profile["accountStatus"]!="active"||(string)profile["role"]!="student"||string.IsNullOrWhiteSpace(uid))return null;
            ValidateSegment(uid);
            try{var mapping=await ReadAsync("usersByUid",uid).ConfigureAwait(false);if(mapping.Type!=JTokenType.String||(string)mapping!=studentId)return null;}
            catch(StudentDataReadException ex)when(ex.HttpStatus==401||ex.HttpStatus==403){return null;}
            profile["id"]=studentId;return profile;
        }

        private void RequireTraining(UimsAccount account)
        {
            if(string.IsNullOrEmpty(idToken)||account==null||account.MustChangePassword||string.IsNullOrEmpty(account.Uid)||
                !new[]{"student","company","academic"}.Contains(account.Role))throw new UnauthorizedAccessException();
        }
        private static readonly string[] TrainingNodes={"taskGroups","taskAssignments","taskSubmissions","examAttempts","examResults","taskGrades","weeklyReports","examKeys","examQuestions","questionImages","taskActivity","weeklyReportIndex"};
        private string TrainingUrl(string node,string id=null)
        {
            if(!TrainingNodes.Contains(node))throw new UnauthorizedAccessException();
            ValidateSegment(node);if(id!=null)ValidateSegment(id);
            return FirebaseConfiguration.DatabaseUrl+"/cycle3Demo/"+node+(id==null?"":"/"+Uri.EscapeDataString(id))+".json?auth="+Uri.EscapeDataString(idToken);
        }
        public async Task<FirebaseRecord> ReadTrainingVersionAsync(UimsAccount account,string node,string id)
        {
            RequireTraining(account);
            using(var request=new HttpRequestMessage(HttpMethod.Get,TrainingUrl(node,id)))
            {
                request.Headers.Add("X-Firebase-ETag","true");
                using(var response=await client.SendAsync(request))
                {
                    if(!response.IsSuccessStatusCode)throw new StudentDataReadException((int)response.StatusCode);
                    return new FirebaseRecord{ETag=response.Headers.ETag?.ToString(),Value=JToken.Parse(await response.Content.ReadAsStringAsync())};
                }
            }
        }
        public async Task<JObject> ReadTrainingQueryAsync(UimsAccount account,string node)
        {
            RequireTraining(account);
            if(!new[]{"taskGroups","taskAssignments","taskSubmissions","examAttempts","examResults","taskGrades","weeklyReports","taskActivity"}.Contains(node))throw new UnauthorizedAccessException();
            if(account.Role=="academic"){
                if(node!="weeklyReports")throw new UnauthorizedAccessException();
                var assigned=await ReadRoleDashboardRecordsAsync(account,"students");
                var index=(await ReadTrainingVersionAsync(account,"weeklyReportIndex",account.ProfileId)).Value as JObject;
                var reports=new JObject();if(index==null)return reports;
                foreach(var student in assigned.Properties()){
                    var ids=index[student.Name] as JObject;if(ids==null)continue;
                    foreach(var item in ids.Properties()){
                        var report=(await ReadTrainingVersionAsync(account,"weeklyReports",item.Name)).Value as JObject;
                        if(report==null||(string)report["studentId"]!=student.Name||(string)report["academicSupervisorId"]!=account.ProfileId)throw new UnauthorizedAccessException();
                        reports[item.Name]=report;
                    }
                }
                return reports;
            }
            string field=account.Role=="company"?"companyId":"studentId";
            string owner=account.Role=="company"?account.CompanyId:account.ProfileId;ValidateSegment(owner);
            using(var response=await client.GetAsync(TrainingUrl(node)+"&orderBy="+Uri.EscapeDataString(JsonConvert.SerializeObject(field))+"&equalTo="+Uri.EscapeDataString(JsonConvert.SerializeObject(owner))))
            {
                if(!response.IsSuccessStatusCode)throw new StudentDataReadException((int)response.StatusCode);
                var parsed=JToken.Parse(await response.Content.ReadAsStringAsync());
                var rows=parsed as JObject??(parsed.Type==JTokenType.Null?new JObject():throw new System.IO.InvalidDataException("Invalid training records."));
                if(rows.Count>500)throw new System.IO.InvalidDataException("The training query needs pagination.");
                foreach(var p in rows.Properties())if(!(p.Value is JObject row)||(string)row[field]!=owner)throw new UnauthorizedAccessException();
                return rows;
            }
        }
        public async Task CommitTrainingAsync(UimsAccount account,JObject patch)
        {
            RequireTraining(account);
            if(patch==null||patch.Count==0||patch.Count>300)throw new ArgumentException("Invalid training operation.");
            foreach(var p in patch.Properties())
            {
                var parts=p.Name.Split('/');if(parts.Length<2||!TrainingNodes.Contains(parts[0]))throw new UnauthorizedAccessException();
                foreach(string part in parts)ValidateSegment(part);
            }
            using(var request=new HttpRequestMessage(new HttpMethod("PATCH"),FirebaseConfiguration.DatabaseUrl+"/cycle3Demo/.json?auth="+Uri.EscapeDataString(idToken)))
            {
                request.Content=new StringContent(patch.ToString(Formatting.None),Encoding.UTF8,"application/json");
                using(var response=await client.SendAsync(request))if(!response.IsSuccessStatusCode)throw new StudentDataReadException((int)response.StatusCode);
            }
        }
        public async Task WriteTrainingVersionAsync(UimsAccount account,string node,string id,JObject record,string etag)
        {
            RequireTraining(account);if(string.IsNullOrEmpty(etag))throw new AccountConflictException();
            using(var request=new HttpRequestMessage(HttpMethod.Put,TrainingUrl(node,id)))
            {
                request.Headers.TryAddWithoutValidation("If-Match",etag);
                request.Content=new StringContent(record.ToString(Formatting.None),Encoding.UTF8,"application/json");
                using(var response=await client.SendAsync(request))
                {
                    if(response.StatusCode==HttpStatusCode.PreconditionFailed)throw new AccountConflictException();
                    if(!response.IsSuccessStatusCode)throw new StudentDataReadException((int)response.StatusCode);
                }
            }
        }

        public async Task CommitApplicationAsync(UimsAccount account, JObject patch)
        {
            RequireStudent(account);
            if(patch==null||patch.Count==0||patch.Count>6)throw new UnauthorizedAccessException();
            foreach(var item in patch.Properties())
            {
                var segments=item.Name.Split('/');foreach(string segment in segments)ValidateSegment(segment);
                if(!new[]{"applications","applicationIndex","placements","students","opportunitySeats"}.Contains(segments[0]))throw new UnauthorizedAccessException();
                if(segments[0]=="students"&&(segments.Length!=3||segments[1]!=account.ProfileId||segments[2]!="placementId"))throw new UnauthorizedAccessException();
                if(segments[0]=="applicationIndex"&&(segments.Length!=3||segments[1]!=account.ProfileId))throw new UnauthorizedAccessException();
            }
            using(var request=new HttpRequestMessage(new HttpMethod("PATCH"),FirebaseConfiguration.DatabaseUrl+"/.json?auth="+Uri.EscapeDataString(idToken)))
            {
                request.Content=new StringContent(patch.ToString(Formatting.None),Encoding.UTF8,"application/json");
                using(var response=await client.SendAsync(request).ConfigureAwait(false))
                    if(!response.IsSuccessStatusCode)throw new StudentDataReadException((int)response.StatusCode);
            }
        }
        public async Task WriteApplicationReviewAsync(UimsAccount account,string id,JObject record,string etag)
        {
            if(string.IsNullOrEmpty(idToken)||account==null||account.MustChangePassword||account.Role!="company"||
                string.IsNullOrEmpty(account.Uid)||(string)record?["companyId"]!=account.CompanyId||(string)record?["id"]!=id)
                throw new UnauthorizedAccessException();
            if(string.IsNullOrEmpty(etag))throw new AccountConflictException();
            using(var request=await RequestAsync(HttpMethod.Put,"applications",id))
            {
                request.Headers.TryAddWithoutValidation("If-Match",etag);
                request.Content=new StringContent(record.ToString(Formatting.None),Encoding.UTF8,"application/json");
                using(var response=await client.SendAsync(request).ConfigureAwait(false))
                {
                    if(response.StatusCode==HttpStatusCode.PreconditionFailed)throw new AccountConflictException();
                    if(!response.IsSuccessStatusCode)throw new StudentDataReadException((int)response.StatusCode);
                }
            }
        }

        public async Task WriteCompanyOpportunityAsync(UimsAccount account, string id, JObject value, string etag)
        {
            if (string.IsNullOrEmpty(idToken) || account == null || account.MustChangePassword || account.Role != "company" ||
                string.IsNullOrWhiteSpace(account.Uid) || string.IsNullOrWhiteSpace(account.CompanyId) ||
                value == null || (string)value["companyId"] != account.CompanyId || (string)value["id"] != id)
                throw new UnauthorizedAccessException();
            ValidateSegment(id);
            if (string.IsNullOrEmpty(etag)) throw new ArgumentException("Reload this opportunity before saving.");
            using (var request = await RequestAsync(HttpMethod.Put, "opportunities", id).ConfigureAwait(false))
            {
                request.Headers.TryAddWithoutValidation("If-Match", etag);
                request.Content = new StringContent(value.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.PreconditionFailed) throw new AccountConflictException();
                    if (!response.IsSuccessStatusCode) throw new StudentDataReadException((int)response.StatusCode);
                }
            }
        }

        public async Task UpdateStudentContactAsync(UimsAccount account, JObject changes)
        {
            RequireStudent(account);
            if (changes == null || changes.Count != 3 || changes.Properties().Any(item => item.Name != "phone" && item.Name != "city" && item.Name != "portfolioUrl")) throw new UnauthorizedAccessException();
            using (var request = await RequestAsync(new HttpMethod("PATCH"), "students", account.ProfileId).ConfigureAwait(false))
            {
                request.Content = new StringContent(changes.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode) throw new StudentDataReadException((int)response.StatusCode);
                }
            }
        }

        public async Task SavePasswordCompletionAsync(UimsAccount account, string receipt)
        {
            if (string.IsNullOrEmpty(idToken) || !PasswordCompletion.IsValid(receipt, account)) throw new UnauthorizedAccessException();
            ValidateSegment(account.ProfileId);
            // PATCH only this leaf: roles, status, UID and mustChangePassword cannot be supplied here.
            using (var request = await RequestAsync(new HttpMethod("PATCH"), "users", account.ProfileId).ConfigureAwait(false))
            {
                request.Content = new StringContent(new JObject { ["firstLoginCompletion"] = receipt }.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    if (!response.IsSuccessStatusCode) LoginDiagnostics.Write("PasswordCompletionPersistenceDenied", (int)response.StatusCode);
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        public async Task WriteVersionAsync(string collection, string id, JToken value, string expectedETag)
        {
            if (idToken != null) throw new UnauthorizedAccessException("End-user account metadata writes are not permitted through this repository.");
            // Account writes only; task schema writes have a separate authorization boundary.
            if (collection != FirebaseConfiguration.UsersPath && collection != FirebaseConfiguration.UidMappingsPath)
                throw new UnauthorizedAccessException();
            if (string.IsNullOrEmpty(expectedETag)) throw new ArgumentException("A database version is required.");
            using (var request = await RequestAsync(HttpMethod.Put, collection, id).ConfigureAwait(false))
            {
                request.Headers.TryAddWithoutValidation("If-Match", expectedETag);
                request.Content = new StringContent(value.ToString(Formatting.None), Encoding.UTF8, "application/json");
                using (var response = await client.SendAsync(request).ConfigureAwait(false))
                {
                    if (response.StatusCode == HttpStatusCode.PreconditionFailed) throw new AccountConflictException();
                    response.EnsureSuccessStatusCode();
                }
            }
        }

        private async Task<HttpRequestMessage> RequestAsync(HttpMethod method, string collection, string id)
        {
            ValidateSegment(collection); ValidateSegment(id);
            if (idToken != null)
                return new HttpRequestMessage(method, FirebaseConfiguration.DatabaseUrl + "/" + Uri.EscapeDataString(collection) + "/" +
                    Uri.EscapeDataString(id) + ".json?auth=" + Uri.EscapeDataString(idToken));
            string token = await ((ITokenAccess)Credential.Value).GetAccessTokenForRequestAsync().ConfigureAwait(false);
            var request = new HttpRequestMessage(method, FirebaseConfiguration.DatabaseUrl + "/" +
                Uri.EscapeDataString(collection) + "/" + Uri.EscapeDataString(id) + ".json");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return request;
        }

        public static void ValidateSegment(string segment)
        {
            if (string.IsNullOrWhiteSpace(segment) || segment.Length > 768 || segment.IndexOfAny(new[] { '.', '$', '#', '[', ']', '/', '\\' }) >= 0)
                throw new ArgumentException("Invalid Firebase path segment.");
        }
    }
}
