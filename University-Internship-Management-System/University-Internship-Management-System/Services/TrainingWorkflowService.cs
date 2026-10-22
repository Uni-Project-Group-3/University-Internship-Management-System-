using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Globalization;
using Newtonsoft.Json.Linq;

namespace University_Internship_Management_System.Services
{
    public sealed class TrainingWorkflowService
    {
        private readonly FirebaseRealtimeDatabaseRepository db;
        public TrainingWorkflowService(FirebaseRealtimeDatabaseRepository db){this.db=db;}
        private static long Now => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        private static string Stamp(long ms)=>DateTimeOffset.FromUnixTimeMilliseconds(ms).UtcDateTime.ToString("o");
        private static string Id(JObject p,string key="id"){string id=(string)p?[key];FirebaseRealtimeDatabaseRepository.ValidateSegment(id);return id;}
        private static string Str(JObject p,string key,int max,bool required=true){if(p?[key]?.Type!=JTokenType.String)throw new ArgumentException("Invalid "+key+".");var s=((string)p[key]).Trim();if(s.Length>max||required&&s.Length==0)throw new ArgumentException("Invalid "+key+".");return s;}
        private static decimal Number(JObject p,string key,decimal min,decimal max){if(p?[key]==null||p[key].Type!=JTokenType.Integer&&p[key].Type!=JTokenType.Float)throw new ArgumentException("Invalid "+key+".");decimal n=(decimal)p[key];if(n<min||n>max)throw new ArgumentException("Invalid "+key+".");return n;}
        private static long Time(JObject p,string key)
        {
            var token=p?[key];DateTimeOffset instant;
            // Json.NET may already have parsed an ISO Z string into a UTC DateTime.
            // Casting that date token to string uses a non-round-trip representation.
            if(token?.Type==JTokenType.Date)
            {
                var raw=((JValue)token).Value;
                if(raw is DateTime utc && utc.Kind==DateTimeKind.Utc)instant=new DateTimeOffset(utc);
                else if(raw is DateTimeOffset offset && offset.Offset==TimeSpan.Zero)instant=offset;
                else throw new ArgumentException("Enter a valid Jordan Time date and time for "+key+".");
            }
            else
            {
                string value=token?.Type==JTokenType.String?(string)token:null;
                if(string.IsNullOrEmpty(value)||!(value.EndsWith("Z",StringComparison.Ordinal)||value.EndsWith("+00:00",StringComparison.Ordinal))||
                    !DateTimeOffset.TryParse(value,CultureInfo.InvariantCulture,DateTimeStyles.None,out instant)||instant.Offset!=TimeSpan.Zero)
                    throw new ArgumentException("Enter a valid Jordan Time date and time for "+key+".");
            }
            return instant.ToUnixTimeMilliseconds();
        }
        private static JObject Pick(JObject r,params string[] fields){var o=new JObject();foreach(var f in fields)if(r[f]!=null)o[f]=r[f].DeepClone();return o;}
        private static void Require(UimsAccount a,string role=null){if(a==null||a.MustChangePassword||string.IsNullOrEmpty(a.Uid)||string.IsNullOrEmpty(a.ProfileId)||role!=null&&a.Role!=role||!new[]{"student","company","academic"}.Contains(a.Role))throw new UnauthorizedAccessException();}
        private static void AssertOwned(UimsAccount a,JObject r)
        {
            Require(a);if(r==null||(int?)r["liveVersion"]!=1)throw new UnauthorizedAccessException();
            if(a.Role=="company"&&(string)r["companyId"]!=a.CompanyId||a.Role=="student"&&(string)r["studentId"]!=a.ProfileId||a.Role=="academic"&&(string)r["academicSupervisorId"]!=a.ProfileId)throw new UnauthorizedAccessException();
        }
        private async Task<JObject> Record(UimsAccount a,string node,string id){return (await db.ReadTrainingVersionAsync(a,node,id)).Value as JObject;}
        private async Task<FirebaseRecord> Assignment(UimsAccount a,string id){var v=await db.ReadTrainingVersionAsync(a,"taskAssignments",id);AssertOwned(a,v.Value as JObject);return v;}
        private static JObject Base(UimsAccount a,string id,string company,string student=null,string academic=null,string placement=null)
        {
            var r=new JObject{["id"]=id,["liveVersion"]=1,["companyId"]=company};
            if(student!=null)r["studentId"]=student;if(academic!=null)r["academicSupervisorId"]=academic;if(placement!=null)r["placementId"]=placement;return r;
        }
        private async Task<JObject> Placements(UimsAccount a)
        {
            if(a.Role=="company")return await db.ReadRoleDashboardRecordsAsync(a,"placements");
            var result=new JObject();
            if(a.Role=="student"){var s=await db.ReadStudentProfileAsync(a);string id=(string)s["placementId"];if(!string.IsNullOrEmpty(id))result[id]=await db.ReadStudentPlacementAsync(a,id);}
            else {var students=await db.ReadRoleDashboardRecordsAsync(a,"students");foreach(var s in students.Properties()){string id=(string)s.Value["placementId"];if(string.IsNullOrEmpty(id))continue;var p=await db.ReadAsync("placements",id) as JObject;if(p==null||(string)p["studentId"]!=s.Name||(string)p["academicSupervisorId"]!=a.ProfileId)throw new UnauthorizedAccessException();result[id]=p;}}
            return result;
        }
        private static JObject FindPlacement(JObject placements,string student,string company=null)
        {
            var rows=placements.Properties().Select(p=>p.Value as JObject).Where(p=>p!=null&&(string)p["studentId"]==student&&(company==null||(string)p["companyId"]==company)&&(string)p["status"]=="active").ToArray();
            if(rows.Length!=1)throw new ArgumentException("Choose a trainee with one active confirmed placement at your company.");return rows[0];
        }
        public async Task<JObject> LoadAsync(UimsAccount a)
        {
            Require(a);if(a.Role=="academic")return new JObject{["weeklyReports"]=new JArray((await db.ReadTrainingQueryAsync(a,"weeklyReports")).Properties().Select(p=>p.Value.DeepClone())),["serverUtc"]=Stamp(Now)};var placements=await Placements(a);
            var assignments=await db.ReadTrainingQueryAsync(a,"taskAssignments");
            var submissions=await db.ReadTrainingQueryAsync(a,"taskSubmissions");
            var attempts=await db.ReadTrainingQueryAsync(a,"examAttempts");
            var activity=await db.ReadTrainingQueryAsync(a,"taskActivity");
            var reports=a.Role=="student"?new JObject():await db.ReadTrainingQueryAsync(a,"weeklyReports");
            var groups=new JArray();var tasks=new JArray();var viewAttempts=new JArray();
            var groupMap=new Dictionary<string,JObject>();
            var ownedGroups=a.Role=="company"?await db.ReadTrainingQueryAsync(a,"taskGroups"):null;
            if(ownedGroups!=null)foreach(var p in ownedGroups.Properties())if((int?)p.Value["liveVersion"]==1)groupMap[p.Name]=(JObject)p.Value;
            foreach(var item in assignments.Properties())
            {
                var r=item.Value as JObject;AssertOwned(a,r);string groupId=(string)r["groupId"];
                if(!groupMap.ContainsKey(groupId)){var g=await Record(a,"taskGroups",groupId);if(g==null||(int?)g["liveVersion"]!=1)throw new InvalidDataException("Task group missing.");groupMap[groupId]=g;}
            }
            foreach(var pair in groupMap)
            {
                var g=pair.Value;var projected=Pick(g,"id","title","description","studentIds","createdBy","createdByName","companyId","opensAt","closesAt","status","priority","week","maximum","checklist","links","attachments","allowExamAfterClosing","requestId");
                if(g["exam"] is JObject e)
                {
                    projected["exam"]=e.DeepClone();var q=(JObject)null;
                    bool active=attempts.Properties().Any(p=>(string)p.Value["groupId"]==pair.Key&&(string)p.Value["status"]=="active"&&(long)p.Value["expiresAtMs"]>Now);
                    if(a.Role=="company"||active)q=await Record(a,"examQuestions",pair.Key);
                    var questions=q?["questions"] as JArray??new JArray();projected["exam"]["questions"]=questions.DeepClone();
                    if(a.Role=="company"){
                        var key=await Record(a,"examKeys",pair.Key);foreach(var question in ((JArray)projected["exam"]["questions"]).OfType<JObject>())question["correct"]=key?["answers"]?[(string)question["id"]]?.DeepClone();
                    }
                }
                groups.Add(projected);
            }
            foreach(var item in assignments.Properties())
            {
                var r=(JObject)item.Value;var g=groupMap[(string)r["groupId"]];
                var t=Pick(r,"id","groupId","studentId","companyId","placementId","status","isRead","gradePublished","attemptCount","lastAttemptId","checklist");
                foreach(string f in new[]{"title","description","priority","week","maximum","opensAt","closesAt","attachments","links"})t[f]=g[f]?.DeepClone();
                if((string)r["status"]=="revision")t["feedback"]=r["revisionFeedback"]?.DeepClone();t["assignedBy"]=g["createdBy"]?.DeepClone();t["assignedByName"]=g["createdByName"]?.DeepClone();t["deadline"]=g["closesAt"]?.DeepClone();t["submissions"]=new JArray(submissions.Properties().Where(s=>(string)s.Value["taskId"]==item.Name).Select(s=>Pick((JObject)s.Value,"id","description","link","fileName","createdAt","revision")));
                t["comments"]=new JArray(activity.Properties().Where(s=>(string)s.Value["taskId"]==item.Name&&(string)s.Value["kind"]=="comment").Select(s=>Pick((JObject)s.Value,"id","message","authorName","createdAt")));
                t["history"]=new JArray(activity.Properties().Where(s=>(string)s.Value["taskId"]==item.Name).Select(s=>Pick((JObject)s.Value,"id","message","createdAt","kind","authorName")));
                bool visible=a.Role!="student"||(bool?)r["gradePublished"]==true;
                if(visible){var grade=await Record(a,"taskGrades",item.Name);if(grade!=null){t["score"]=grade["score"]?.DeepClone();t["feedback"]=grade["feedback"]?.DeepClone();}}
                tasks.Add(t);
                foreach(var at in attempts.Properties().Where(p=>(string)p.Value["taskId"]==item.Name))
                {
                    var ar=(JObject)at.Value;
                    if((string)ar["status"]=="active"&&(long)ar["expiresAtMs"]<=Now&&a.Role!="academic"){
                        var version=await db.ReadTrainingVersionAsync(a,"examAttempts",at.Name);ar=(JObject)version.Value;ar["status"]="submitted";ar["submittedAtMs"]=Now;
                        try{await db.CommitTrainingAsync(a,new JObject{["examAttempts/"+at.Name+"/status"]="submitted",["examAttempts/"+at.Name+"/submittedAtMs"]=ar["submittedAtMs"].DeepClone()});}catch(AccountConflictException){ar=await Record(a,"examAttempts",at.Name);}
                    }
                    var va=Pick(ar,"id","taskId","groupId","studentId","status","answers");if(ar["submittedAtMs"]!=null)va["submittedAt"]=Stamp((long)ar["submittedAtMs"]);va["expiresAt"]=Stamp((long)ar["expiresAtMs"]);va["published"]=(bool?)r["gradePublished"]==true;
                    if((string)ar["status"]=="active"){va["questions"]=groups.OfType<JObject>().First(x=>(string)x["id"]==(string)r["groupId"])["exam"]?["questions"]?.DeepClone()??new JArray();}
                    if(a.Role=="company"&&(string)ar["status"]=="submitted")await GradeExam(a,ar,g);
                    bool examVisible=a.Role!="student" || r["publishedAttempts"]?[(string)ar["id"]]?.Type==JTokenType.Boolean && (bool)r["publishedAttempts"][(string)ar["id"]];va["published"]=examVisible;
                    if((string)ar["status"]=="submitted"&&examVisible){var result=await Record(a,"examResults",at.Name);if(result!=null)foreach(var f in new[]{"score","maximum","percentage","passed"})va[f]=result[f]?.DeepClone();}
                    viewAttempts.Add(va);
                }
            }
            var users=new JArray(new JObject{["id"]=a.ProfileId,["name"]=a.DisplayName,["role"]=a.Role,["companyId"]=a.CompanyId});foreach(var gg in groupMap.Values){string author=(string)gg["createdBy"];if(!users.OfType<JObject>().Any(u=>(string)u["id"]==author))users.Add(new JObject{["id"]=author,["name"]=(string)gg["createdByName"]??author,["role"]="company",["companyId"]=(string)gg["companyId"]});}var progress=new JArray();
            foreach(var p in placements.Properties().Select(p=>(JObject)p.Value)){
                string student=(string)p["studentId"];JObject studentAccount=null;if(a.Role=="company"){if((string)p["status"]!="active")continue;studentAccount=await db.ReadActiveTrainingStudentAsync(a,p);if(studentAccount==null)continue;}string studentName=(string)studentAccount?["displayName"]??student;if(!users.OfType<JObject>().Any(u=>(string)u["id"]==student))users.Add(new JObject{["id"]=student,["name"]=studentName,["universityId"]=student,["department"]="Active verified trainee"});
                progress.Add(new JObject{["id"]=student,["name"]=studentName,["status"]=(string)p["status"],["approvedHours"]=p["approvedHours"]?.DeepClone(),["placement"]=new JObject{["id"]=(string)p["id"],["completionStatus"]=(string)p["status"]=="active"?"InTraining":(string)p["status"]},["supervisor"]=new JObject{["id"]=(string)p["academicSupervisorId"],["name"]=(string)p["academicSupervisorId"]}});
            }
            return new JObject{["user"]=new JObject{["id"]=a.ProfileId,["name"]=a.DisplayName,["email"]=a.Email,["role"]=a.Role,["mustChangePassword"]=false},["serverUtc"]=Stamp(Now),["users"]=users,["progress"]=progress,["taskGroups"]=groups,["tasks"]=tasks,["examAttempts"]=viewAttempts,["weeklyReports"]=new JArray(reports.Properties().Select(p=>p.Value.DeepClone())),["companies"]=new JArray(groupMap.Values.Select(g=>(string)g["companyId"]).Distinct().Select(id=>new JObject{["id"]=id,["name"]=id})),["submissions"]=new JArray(submissions.Properties().Select(s=>Pick((JObject)s.Value,"id","taskId","studentId","description","link","fileName","createdAt"))),["taskActivity"]=new JArray(activity.Properties().Select(s=>{var record=Pick((JObject)s.Value,"id","taskId","message","authorId","authorName","kind");record["at"]=s.Value["createdAt"]?.DeepClone();return record;})),["placements"]=new JArray(placements.Properties().Select(p=>Pick((JObject)p.Value,"id","studentId","companyId","status","approvedHours","requiredHours"))),["applications"]=new JArray(),["messages"]=new JArray(),["notifications"]=new JArray()};
        }
        private async Task GradeExam(UimsAccount a,JObject attempt,JObject group)
        {
            Require(a,"company");var prior=await db.ReadTrainingVersionAsync(a,"examResults",(string)attempt["id"]);if(prior.Value.Type!=JTokenType.Null)return;
            var key=await Record(a,"examKeys",(string)group["id"]);var questions=await Record(a,"examQuestions",(string)group["id"]);
            decimal score=0,total=0;foreach(var q in ((JArray)questions["questions"]).OfType<JObject>()){decimal marks=(decimal)q["marks"];total+=marks;string id=(string)q["id"];if(attempt["answers"]?[id]?.Type==JTokenType.Integer&&(int)attempt["answers"][id]==(int)key["answers"][id])score+=marks;}
            var r=Base(a,(string)attempt["id"],a.CompanyId,(string)attempt["studentId"],(string)attempt["academicSupervisorId"],(string)attempt["placementId"]);r["taskId"]=(string)attempt["taskId"];r["score"]=score;r["maximum"]=total;r["percentage"]=total>0?100*score/total:0;r["passed"]=total>0&&100*score/total>=(decimal)group["exam"]["passing"];r["gradedAt"]=Stamp(Now);r["gradedBy"]=a.ProfileId;
            try{await db.WriteTrainingVersionAsync(a,"examResults",(string)r["id"],r,prior.ETag);}catch(AccountConflictException){}
        }
        public async Task<JObject> ExecuteAsync(UimsAccount a,string action,JObject p)
        {
            Require(a);if(p==null||p.Properties().Any(x=>new[]{"uid","role","companyId","createdBy","authorId","academicSupervisorId"}.Contains(x.Name)))throw new UnauthorizedAccessException();
            if(action=="c3Create")return await Create(a,p);
            if(action=="c3Weekly")return await Weekly(a,p);
            if(action=="c3AcademicComment")return await AcademicComment(a,p);
            if(action=="c3RegisterImage"||action=="c3Image")return await ImageAsync(a,action,p);
            var version=await Assignment(a,Id(p));var task=(JObject)version.Value;var group=await Record(a,"taskGroups",(string)task["groupId"]);long now=Now;
            if(group==null||(string)group["status"]!="published")throw new UnauthorizedAccessException();
            if(action=="c3MarkRead"||action=="c3Read"){Require(a,"student");task["isRead"]=true;}
            else if(action=="c3Move"){
                if(a.Role!="student"&&a.Role!="company")throw new UnauthorizedAccessException();Window(group,now);string status=Str(p,"status",30);if(!new[]{"assigned","in-progress"}.Contains(status)||!new[]{"assigned","in-progress","revision"}.Contains((string)task["status"]))throw new ArgumentException("Invalid task transition.");task["status"]=status;
            }
            else if(action=="submitTask"){
                Require(a,"student");Window(group,now);if(!new[]{"assigned","in-progress","revision"}.Contains((string)task["status"]))throw new ArgumentException("This task is not accepting a submission.");
                if(!string.IsNullOrEmpty((string)p["fileName"]))throw new ArgumentException("Use a secure HTTPS work link. File transfer is not configured; filenames are not uploads.");
                var sub=Base(a,"SUB-"+Guid.NewGuid().ToString("N"),(string)task["companyId"],a.ProfileId,(string)task["academicSupervisorId"],(string)task["placementId"]);sub["taskId"]=(string)task["id"];sub["description"]=Str(p,"description",5000);sub["link"]=Str(p,"link",2000,false);ValidateLink((string)sub["link"]);sub["createdAt"]=Stamp(now);sub["revision"]=(string)task["status"]=="revision";
                var patch=new JObject{["taskSubmissions/"+(string)sub["id"]]=sub,["taskAssignments/"+(string)task["id"]+"/status"]="submitted",["taskAssignments/"+(string)task["id"]+"/lastSubmissionId"]=(string)sub["id"]};await db.CommitTrainingAsync(a,patch);return new JObject{["success"]=true};
            }
            else if(action=="reviewTask"){
                Require(a,"company");if((string)task["status"]!="submitted")throw new ArgumentException("Only submitted work can be evaluated.");
                string status=Str(p,"status",30),feedback=Str(p,"feedback",5000);if(status!="revision"&&status!="completed")throw new ArgumentException("Choose Revision or Completed.");
                if(status=="completed"){
                    decimal maximum=(decimal)group["maximum"];decimal score;if(!decimal.TryParse((string)p["score"],NumberStyles.Number,CultureInfo.InvariantCulture,out score)||score<0||score>maximum)throw new ArgumentException("Enter a score within the task maximum.");
                    var grade=Base(a,(string)task["id"],a.CompanyId,(string)task["studentId"],(string)task["academicSupervisorId"],(string)task["placementId"]);grade["taskId"]=(string)task["id"];grade["score"]=score;grade["maximum"]=maximum;grade["feedback"]=feedback;grade["reviewedBy"]=a.ProfileId;grade["reviewedAt"]=Stamp(now);
                    await db.CommitTrainingAsync(a,new JObject{["taskGrades/"+(string)task["id"]]=grade,["taskAssignments/"+(string)task["id"]+"/status"]=status});return new JObject{["success"]=true};
                }
                task["status"]="revision";task["revisionFeedback"]=feedback;
            }
            else if(action=="c3Publish"){
                Require(a,"company");bool any=false;var grade=await Record(a,"taskGrades",(string)task["id"]);if(grade!=null){task["gradePublished"]=true;any=true;}
                var attempts=await db.ReadTrainingQueryAsync(a,"examAttempts");var publications=task["publishedAttempts"] as JObject??new JObject();
                foreach(var item in attempts.Properties().Where(at=>(string)at.Value["taskId"]==(string)task["id"]&&(string)at.Value["status"]=="submitted")){await GradeExam(a,(JObject)item.Value,group);publications[item.Name]=true;any=true;}
                if(!any)throw new ArgumentException("Review a submitted practical or exam result before publishing.");task["publishedAttempts"]=publications;
            }
            else if(action=="c3StartExam")return await StartExam(a,task,group,version);
            else if(action=="c3SaveAnswer"||action=="c3SubmitExam")return await SaveExam(a,task,group,p,action);
            else if(action=="c3Comment"){
                var comment=Base(a,"ACT-"+Guid.NewGuid().ToString("N"),(string)task["companyId"],(string)task["studentId"],(string)task["academicSupervisorId"],(string)task["placementId"]);comment["taskId"]=(string)task["id"];comment["kind"]="comment";comment["message"]=Str(p,"message",3000);comment["authorName"]=a.DisplayName;comment["authorId"]=a.ProfileId;comment["createdAt"]=Stamp(now);await db.CommitTrainingAsync(a,new JObject{["taskActivity/"+(string)comment["id"]]=comment});return new JObject{["success"]=true};
            }
            else if(action=="c3Checklist"){Require(a,"student");Window(group,now);int index=(int?)p["index"]??-1;var checklist=task["checklist"] as JArray;if(checklist==null||index<0||index>=checklist.Count||p["done"]?.Type!=JTokenType.Boolean)throw new ArgumentException("Invalid checklist item.");checklist[index]["done"]=(bool)p["done"];}
            else throw new ArgumentException("Unknown training operation.");
            if(a.Role=="student"){
                string path="taskAssignments/"+(string)task["id"]+"/";
                var patch=new JObject();
                if(action=="c3MarkRead"||action=="c3Read")patch[path+"isRead"]=true;
                else if(action=="c3Move")patch[path+"status"]=task["status"].DeepClone();
                else if(action=="c3Checklist")patch[path+"checklist/"+((int)p["index"])+"/done"]=p["done"].DeepClone();
                else throw new UnauthorizedAccessException();
                await db.CommitTrainingAsync(a,patch);
            }else await db.WriteTrainingVersionAsync(a,"taskAssignments",(string)task["id"],task,version.ETag);
            return new JObject{["success"]=true};
        }
        private static void Window(JObject group,long now){if(now<(long)group["opensAtMs"]||now>=(long)group["closesAtMs"])throw new ArgumentException("This action is outside the task opening and closing period.");}
        private static void ValidateLink(string link){if(link.Length==0)return;Uri uri;if(!Uri.TryCreate(link,UriKind.Absolute,out uri)||uri.Scheme!="https"||!string.IsNullOrEmpty(uri.UserInfo))throw new ArgumentException("Use a secure HTTPS work link.");}


        public async Task<JObject> ImageAsync(UimsAccount a,string action,JObject p)
        {
            Require(a);if(action=="c3Image"){
                var image=await Record(a,"questionImages",Id(p));if(image==null)throw new ArgumentException("Image unavailable.");
                if(a.Role=="company"){if((string)image["companyId"]!=a.CompanyId)throw new UnauthorizedAccessException();}
                else{var group=await Record(a,"taskGroups",(string)image["groupId"]);if(group?["assignees"]?[a.ProfileId]?.Type!=JTokenType.Boolean||!(bool)group["assignees"][a.ProfileId]||group["exam"]==null||Now<(long)group["exam"]["startsAtMs"])throw new UnauthorizedAccessException();}
                return Pick(image,"id","mime","data","size","width","height");
            }
            Require(a,"company");Guid request;if(!Guid.TryParse((string)p["requestId"],out request))throw new ArgumentException("Save the task draft before uploading its image.");
            string groupId="LIVE-"+request.ToString("N");byte[] bytes;try{bytes=Convert.FromBase64String((string)p["data"]??"");}catch(FormatException){throw new ArgumentException("Invalid image content.");}
            if(bytes.Length<30||bytes.Length>2*1024*1024)throw new ArgumentException("Choose an image up to 2 MB.");
            string mime;int width,height;
            if(bytes[0]==0x89&&bytes[1]==0x50&&bytes[2]==0x4e&&bytes[3]==0x47){
                mime="image/png";width=(bytes[16]<<24)|(bytes[17]<<16)|(bytes[18]<<8)|bytes[19];height=(bytes[20]<<24)|(bytes[21]<<16)|(bytes[22]<<8)|bytes[23];
            }else if(System.Text.Encoding.ASCII.GetString(bytes,0,4)=="RIFF"&&System.Text.Encoding.ASCII.GetString(bytes,8,4)=="WEBP"){
                mime="image/webp";string chunk=System.Text.Encoding.ASCII.GetString(bytes,12,4);
                if(chunk=="VP8X"){width=1+bytes[24]+(bytes[25]<<8)+(bytes[26]<<16);height=1+bytes[27]+(bytes[28]<<8)+(bytes[29]<<16);}
                else if(chunk=="VP8L"&&bytes[20]==0x2f){width=1+bytes[21]+((bytes[22]&0x3f)<<8);height=1+(bytes[22]>>6)+(bytes[23]<<2)+((bytes[24]&0xf)<<10);}
                else if(chunk=="VP8 "&&bytes[23]==0x9d&&bytes[24]==1&&bytes[25]==0x2a){width=(bytes[26]+(bytes[27]<<8))&0x3fff;height=(bytes[28]+(bytes[29]<<8))&0x3fff;}
                else throw new ArgumentException("Unsupported WebP image.");
            }else if(bytes[0]==0xff&&bytes[1]==0xd8&&bytes[2]==0xff){
                mime="image/jpeg";width=0;height=0;int pos=2;while(pos+8<bytes.Length){if(bytes[pos++]!=0xff)continue;int marker=bytes[pos++];if(marker==0xd8||marker==0xd9)continue;int length=(bytes[pos]<<8)|bytes[pos+1];if(length<2||pos+length>bytes.Length)break;if(new[]{0xc0,0xc1,0xc2,0xc3}.Contains(marker)){height=(bytes[pos+3]<<8)|bytes[pos+4];width=(bytes[pos+5]<<8)|bytes[pos+6];break;}pos+=length;}
            }else throw new ArgumentException("Choose JPEG, PNG or WebP content.");
            if(width<1||height<1||width>8000||height>8000||(long)width*height>16000000)throw new ArgumentException("Invalid image dimensions.");
            string id="IMG-"+Guid.NewGuid().ToString("N");var record=Base(a,id,a.CompanyId);record["groupId"]=groupId;record["mime"]=mime;record["size"]=bytes.Length;record["width"]=width;record["height"]=height;record["data"]=Convert.ToBase64String(bytes);
            await db.CommitTrainingAsync(a,new JObject{["questionImages/"+id]=record});return Pick(record,"id","mime","size","width","height");
        }

        private static decimal ConfigNumber(JObject p,string key,decimal min,decimal max,bool integer=false)
        {
            decimal n;if(!decimal.TryParse((string)p?[key],NumberStyles.Number,CultureInfo.InvariantCulture,out n)||n<min||n>max||integer&&decimal.Truncate(n)!=n)throw new ArgumentException("Invalid "+key+".");return n;
        }
        private async Task<JObject> Create(UimsAccount a,JObject p)
        {
            Require(a,"company");Guid request;if(!Guid.TryParse((string)p["requestId"],out request))throw new ArgumentException("Reload the task studio.");
            string id="LIVE-"+request.ToString("N");bool draft=(bool?)p["draft"]==true;
            var version=await db.ReadTrainingVersionAsync(a,"taskGroups",id);var prior=version.Value as JObject;
            if(prior!=null){if((string)prior["companyId"]!=a.CompanyId||(int?)prior["liveVersion"]!=1)throw new UnauthorizedAccessException();if((string)prior["status"]=="published")return new JObject{["success"]=true,["id"]=id};}
            string title=Str(p,"title",200),description=Str(p,"description",10000,false);long opening=Time(p,"opensAt"),closing=Time(p,"closesAt");
            if(opening>=closing)throw new ArgumentException("Task opening must precede closing.");
            if(!(p["studentIds"] is JArray students)||students.Count>100||students.Any(s=>s.Type!=JTokenType.String)||students.Select(s=>(string)s).Distinct().Count()!=students.Count||!draft&&students.Count==0)throw new ArgumentException("Choose eligible confirmed trainees.");
            if(p["attachments"] is JArray files&&files.Count>0)throw new ArgumentException("Task file transfer is not configured. Use task instructions; filenames alone are not attachments.");
            var placements=await Placements(a);var selected=new Dictionary<string,JObject>();foreach(string sid in students){FirebaseRealtimeDatabaseRepository.ValidateSegment(sid);var placement=FindPlacement(placements,sid,a.CompanyId);if(await db.ReadActiveTrainingStudentAsync(a,placement)==null)throw new ArgumentException("Select an active student account with a verified Firebase UID and confirmed placement.");selected[sid]=placement;}
            var group=Base(a,id,a.CompanyId);group["createdBy"]=a.ProfileId;group["createdByName"]=a.DisplayName;group["requestId"]=request.ToString();group["title"]=title;group["description"]=description;group["studentIds"]=students.DeepClone();group["assignees"]=new JObject(selected.Keys.Select(s=>new JProperty(s,true)));group["opensAt"]=Stamp(opening);group["closesAt"]=Stamp(closing);group["opensAtMs"]=opening;group["closesAtMs"]=closing;group["status"]=draft?"draft":"published";group["priority"]="medium";group["week"]=1;group["maximum"]=10;group["checklist"]=new JArray();group["links"]=new JArray();group["attachments"]=new JArray();group["allowExamAfterClosing"]=(bool?)p["allowExamAfterClosing"]==true;group["createdAt"]=prior?["createdAt"]?.DeepClone()??new JValue(Stamp(Now));
            var patch=new JObject{["taskGroups/"+id]=group};
            if(p["exam"] is JObject exam)
            {
                long start=Time(exam,"startsAt");int duration=(int)ConfigNumber(exam,"duration",1,240,true),attempts=(int)ConfigNumber(exam,"attempts",1,5,true),count=(int)ConfigNumber(exam,"questionCount",1,200,true);
                decimal total=ConfigNumber(exam,"totalMarks",1,10000),passing=ConfigNumber(exam,"passing",0,100);long end=checked(start+duration*60000L);
                if(start<opening||start>=closing||end>closing&&(bool?)group["allowExamAfterClosing"]!=true)throw new ArgumentException("The exam must start inside task availability. Enable the explicit extension to finish after task closing.");
                var questions=exam["questions"] as JArray;if(questions==null||questions.Count!=count)throw new ArgumentException("Question count must match the exam configuration.");
                var safeQuestions=new JArray();var keys=new JObject();decimal sum=0;
                foreach(var token in questions)
                {
                    var q=token as JObject;if(q==null)throw new ArgumentException("Invalid question.");
                    string qid=Id(q),type=Str(q,"type",10),text=Str(q,"text",6000);if(keys[qid]!=null)throw new ArgumentException("Question IDs must be unique.");
                    decimal marks=ConfigNumber(q,"marks",.01m,10000);sum+=marks;var choices=q["choices"] as JArray;
                    if((type!="mcq"&&type!="tf")||choices==null||choices.Count<2||choices.Count>12||choices.Any(c=>c.Type!=JTokenType.String||string.IsNullOrWhiteSpace((string)c)||((string)c).Length>2000))throw new ArgumentException("Each question needs valid answer choices.");
                    if(type=="tf"&&(choices.Count!=2||(string)choices[0]!="True"||(string)choices[1]!="False"))throw new ArgumentException("True/False questions require True and False choices.");
                    int correct=(int)ConfigNumber(q,"correct",0,choices.Count-1,true);keys[qid]=correct;
                    var safe=new JObject{["id"]=qid,["type"]=type,["text"]=text,["marks"]=marks,["choices"]=choices.DeepClone()};
                    if(q["image"] is JObject image){var stored=await Record(a,"questionImages",Id(image));if(stored==null||(string)stored["companyId"]!=a.CompanyId||(string)stored["groupId"]!=id)throw new UnauthorizedAccessException();safe["image"]=Pick(stored,"id","mime","size","width","height");}
                    safeQuestions.Add(safe);
                }
                if(sum!=total)throw new ArgumentException("The sum of question marks must equal total exam marks.");
                group["exam"]=new JObject{["id"]=id,["title"]=Str(exam,"title",200),["instructions"]=(string)exam["instructions"]??"",["questionCount"]=count,["totalMarks"]=total,["passing"]=passing,["startsAt"]=Stamp(start),["endsAt"]=Stamp(end),["startsAtMs"]=start,["endsAtMs"]=end,["duration"]=duration,["attempts"]=attempts,["policy"]="close"};
                var qdoc=Base(a,id,a.CompanyId);qdoc["groupId"]=id;qdoc["questions"]=safeQuestions;qdoc["byId"]=new JObject(safeQuestions.OfType<JObject>().Select(q=>new JProperty((string)q["id"],new JObject{["choiceCount"]=((JArray)q["choices"]).Count,["marks"]=q["marks"].DeepClone()})));patch["examQuestions/"+id]=qdoc;
                var keydoc=Base(a,id,a.CompanyId);keydoc["groupId"]=id;keydoc["answers"]=keys;patch["examKeys/"+id]=keydoc;
            }
            if(!draft)foreach(var pair in selected)
            {
                string aid=id+"-"+pair.Key;var placement=pair.Value;
                var assignment=Base(a,aid,a.CompanyId,pair.Key,(string)placement["academicSupervisorId"],(string)placement["id"]);assignment["groupId"]=id;assignment["status"]="assigned";assignment["isRead"]=false;assignment["gradePublished"]=false;assignment["attemptCount"]=0;assignment["checklist"]=new JArray();assignment["createdAt"]=Stamp(Now);patch["taskAssignments/"+aid]=assignment;
            }
            await db.CommitTrainingAsync(a,patch);return new JObject{["success"]=true,["id"]=id};
        }
        private async Task<JObject> StartExam(UimsAccount a,JObject task,JObject group,FirebaseRecord taskVersion)
        {
            Require(a,"student");var e=group["exam"] as JObject;long now=Now;
            if(e==null||now<(long)e["startsAtMs"]||now>=(long)e["endsAtMs"])throw new ArgumentException("The exam is not currently available.");
            string previous=(string)task["lastAttemptId"];
            if(!string.IsNullOrEmpty(previous)){var old=await Record(a,"examAttempts",previous);if(old!=null&&(string)old["status"]=="active"&&(long)old["expiresAtMs"]>now)return new JObject{["success"]=true,["id"]=previous};}
            int number=((int?)task["attemptCount"]??0)+1;if(number>(int)e["attempts"])throw new ArgumentException("No exam attempts remain.");
            string id=(string)task["id"]+"-"+number;var attempt=Base(a,id,(string)task["companyId"],a.ProfileId,(string)task["academicSupervisorId"],(string)task["placementId"]);
            attempt["taskId"]=(string)task["id"];attempt["groupId"]=(string)task["groupId"];attempt["number"]=number;attempt["status"]="active";attempt["answers"]=new JObject();attempt["startedAtMs"]=now;attempt["expiresAtMs"]=Math.Min(now+(int)e["duration"]*60000L,(long)e["endsAtMs"]);attempt["startedAt"]=Stamp(now);
            await db.CommitTrainingAsync(a,new JObject{["examAttempts/"+id]=attempt,["taskAssignments/"+(string)task["id"]+"/attemptCount"]=number,["taskAssignments/"+(string)task["id"]+"/lastAttemptId"]=id});return new JObject{["success"]=true,["id"]=id};
        }
        private async Task<JObject> SaveExam(UimsAccount a,JObject task,JObject group,JObject input,string action)
        {
            Require(a,"student");string id=Id(input,"attemptId");var version=await db.ReadTrainingVersionAsync(a,"examAttempts",id);var attempt=version.Value as JObject;AssertOwned(a,attempt);
            if((string)attempt["taskId"]!=(string)task["id"])throw new UnauthorizedAccessException();
            if((string)attempt["status"]=="submitted"){if(action=="c3SubmitExam")return new JObject{["success"]=true};throw new ArgumentException("This attempt is already submitted.");}
            var patch=new JObject();
            if(action=="c3SubmitExam"||Now>=(long)attempt["expiresAtMs"]){patch["examAttempts/"+id+"/status"]="submitted";patch["examAttempts/"+id+"/submittedAtMs"]=Now;}
            else {
                string qid=Id(input,"questionId");var doc=await Record(a,"examQuestions",(string)group["id"]);var q=((JArray)doc["questions"]).OfType<JObject>().FirstOrDefault(qr=>(string)qr["id"]==qid);
                if(q==null||input["answer"]?.Type!=JTokenType.Integer||(int)input["answer"]<0||(int)input["answer"]>=((JArray)q["choices"]).Count)throw new ArgumentException("Invalid answer selection.");
                patch["examAttempts/"+id+"/answers/"+qid]=(int)input["answer"];
            }
            await db.CommitTrainingAsync(a,patch);return new JObject{["success"]=true,["resultPending"]=true};
        }

        private async Task<JObject> Weekly(UimsAccount a,JObject input)
        {
            Require(a,"company");string sid=Id(input,"studentId");int week=(int)ConfigNumber(input,"week",1,52,true);string feedback=Str(input,"feedback",5000);
            var placement=FindPlacement(await Placements(a),sid,a.CompanyId);string academic=(string)placement["academicSupervisorId"];if(string.IsNullOrWhiteSpace(academic))throw new ArgumentException("An academic supervisor must be assigned.");
            var view=await LoadAsync(a);var tasks=((JArray)view["tasks"]).OfType<JObject>().Where(t=>(string)t["studentId"]==sid&&((int?)t["week"]??1)==week).ToArray();
            var snapshot=new JArray();foreach(var task in tasks){var row=Pick(task,"id","title","status","score","maximum","feedback","submissions","history");row["examResults"]=new JArray(((JArray)view["examAttempts"]).OfType<JObject>().Where(at=>(string)at["taskId"]==(string)task["id"]&&(string)at["status"]=="submitted").Select(at=>Pick(at,"id","score","maximum","percentage","passed","submittedAt")));snapshot.Add(row);}
            string id="RPT-"+Guid.NewGuid().ToString("N");var report=Base(a,id,a.CompanyId,sid,academic,(string)placement["id"]);report["week"]=week;report["feedback"]=feedback;report["tasks"]=snapshot;report["approvedHours"]=placement["approvedHours"]?.DeepClone();report["requiredHours"]=placement["requiredHours"]?.DeepClone();report["hoursScope"]="Cumulative approved placement hours; not weekly hours";report["sentBy"]=a.ProfileId;report["sentAt"]=Stamp(Now);report["status"]="sent";
            await db.CommitTrainingAsync(a,new JObject{["weeklyReports/"+id]=report,["weeklyReportIndex/"+academic+"/"+sid+"/"+id]=true});return new JObject{["success"]=true,["id"]=id};
        }
        private async Task<JObject> AcademicComment(UimsAccount a,JObject input)
        {
            Require(a,"academic");string id=Id(input);var version=await db.ReadTrainingVersionAsync(a,"weeklyReports",id);var report=version.Value as JObject;AssertOwned(a,report);
            var assigned=await db.ReadRoleDashboardRecordsAsync(a,"students");if(assigned[(string)report["studentId"]]==null)throw new UnauthorizedAccessException();
            report["academicFeedback"]=Str(input,"message",5000);report["reviewedBy"]=a.ProfileId;report["reviewedAt"]=Stamp(Now);report["status"]="reviewed";
            await db.CommitTrainingAsync(a,new JObject{["weeklyReports/"+id+"/academicFeedback"]=report["academicFeedback"].DeepClone(),["weeklyReports/"+id+"/reviewedBy"]=a.ProfileId,["weeklyReports/"+id+"/reviewedAt"]=report["reviewedAt"].DeepClone(),["weeklyReports/"+id+"/status"]="reviewed"});return new JObject{["success"]=true};
        }
    }
}
