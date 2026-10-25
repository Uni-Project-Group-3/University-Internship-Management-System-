<%@ Page Language="C#" Async="true" %>
<%@ Register Src="~/Shared/CoordinatorSidebar.ascx" TagPrefix="shared" TagName="RoleSidebar" %>
<%@ Import Namespace="System" %>
<%@ Import Namespace="System.Linq" %>
<%@ Import Namespace="System.Text" %>
<%@ Import Namespace="System.Threading.Tasks" %>
<%@ Import Namespace="System.Web" %>
<%@ Import Namespace="System.Web.Security" %>
<%@ Import Namespace="Newtonsoft.Json.Linq" %>
<%@ Import Namespace="University_Internship_Management_System.Shared" %>
<script runat="server">
 protected void Page_Load(object sender, EventArgs e) {
  Response.Cache.SetNoStore();Response.Cache.SetCacheability(HttpCacheability.NoCache);
  var identity=Context.User == null ? null : Context.User.Identity as FormsIdentity;
  if(identity==null || !identity.IsAuthenticated || identity.Ticket.Expired){Response.Redirect(ResolveUrl("~/Auth/Login.aspx?account=new"),false);Context.ApplicationInstance.CompleteRequest();return;}
  if(FirebaseSignInService.CanonicalRole(identity.Ticket.UserData)!="coordinator"){Response.StatusCode=403;recordsMarkup.Text="<p role='alert'>Coordinator access is required.</p>";return;}
  RegisterAsyncTask(new System.Web.UI.PageAsyncTask(async ()=> await LoadRecords(identity.Name)));
 }
 private static string E(string value){return HttpUtility.HtmlEncode(value ?? "");}
 private static string V(JToken value,string key){return value==null?"":(string)value[key] ?? "";}
 private async Task LoadRecords(string identity) {
  try {
   var gateway=new StudentFirebaseGateway();var authorization=new StudentManagementService(gateway,gateway,null);
   await authorization.RequireCoordinatorAsync(identity);
   var studentsTask=gateway.ReadAsync("universityRecords/students");var academicsTask=gateway.ReadAsync("universityRecords/academicSupervisors");
   await Task.WhenAll(studentsTask,academicsTask);
   recordsMarkup.Text=RenderSection("students","Students",studentsTask.Result as JObject,true)+RenderSection("academics","Academic supervisors",academicsTask.Result as JObject,false);
  }catch(UnauthorizedAccessException){Response.StatusCode=403;recordsMarkup.Text="<p class='completion-notice' role='alert'>Coordinator access could not be verified. Sign in with an active coordinator account.</p>";}
  catch(Exception error){Trace.Warn("UniversityRecords",error.GetType().Name);recordsMarkup.Text="<p class='completion-notice' role='alert'>University records could not be loaded. Check the connection and refresh this page.</p>";}
 }
 private static string RenderSection(string id,string title,JObject records,bool student){
  var b=new StringBuilder("<section class='records-section completion-panel' data-records-section><header><div><h2>"+E(title)+"</h2><p>Official university directory &middot; read only</p></div></header><div class='records-filters'><label>Search<input type='search' data-record-search placeholder='Name, ID or email' /></label><label>University<select data-record-university><option value=''>All universities</option></select></label><label>College<select data-record-college><option value=''>All colleges</option></select></label><label>"+(student?"Major":"Department")+"<select data-record-department><option value=''>All</option></select></label><button type='button' data-record-clear>Clear filters</button></div><div class='completion-table-wrap'><table><thead><tr><th>"+(student?"Student ID":"Staff ID")+"</th><th>Full name</th><th>University email</th><th>University</th><th>College</th><th>"+(student?"Major":"Department")+"</th>"+(student?"<th>Required hours</th><th>Eligibility</th>":"")+"</tr></thead><tbody>");
  if(records!=null)foreach(var entry in records.Properties().Where(p=>p.Value is JObject).OrderBy(p=>V(p.Value,"fullName"))){var v=entry.Value;string university=V(v,"universityId"),college=V(v,"collegeId"),department=V(v,student?"majorId":"departmentId");b.Append("<tr data-university='"+HttpUtility.HtmlAttributeEncode(university)+"' data-college='"+HttpUtility.HtmlAttributeEncode(college)+"' data-department='"+HttpUtility.HtmlAttributeEncode(department)+"'><td>"+E(V(v,student?"studentId":"staffId"))+"</td><td>"+E(V(v,"fullName"))+"</td><td>"+E(V(v,"email"))+"</td><td>"+E(university)+"</td><td>"+E(college)+"</td><td>"+E(department)+"</td>");if(student)b.Append("<td>"+E(V(v,"requiredTrainingHours"))+"</td><td>"+(v.Value<bool?>("isEligible")==true?"Eligible":v["isEligible"]==null?"Not provided":"Not eligible")+"</td>");b.Append("</tr>");}
  b.Append("</tbody></table></div></section>");return b.ToString();
 }
</script>
<!DOCTYPE html><html lang="en"><head runat="server"><meta charset="utf-8"/><meta name="viewport" content="width=device-width, initial-scale=1"/><title>University Records | JUST Internships</title><link href="../Assets/shared/colors.css" rel="stylesheet"/><link href="../Assets/coordinator/manage-students.css" rel="stylesheet"/><link href="../Assets/shared/workspace-identity.css?v=20261025-account-button" rel="stylesheet"/><script src="../Js_project/workspace-navigation-unified.js?v=20261024-refine" defer></script><script src="../Js_project/Coordinator/university-records.js" defer></script></head><body class="coordinator-banner dashboard-direct app-page management-page"><form runat="server"><div class="app-layout"><shared:RoleSidebar ID="RoleNavigation" runat="server" SidebarId="sidebar"/><div class="main"><header class="topbar"><span>University Internship Management System</span></header><main class="app-content"><header class="management-hero"><div><p class="completion-eyebrow">OFFICIAL UNIVERSITY DIRECTORY</p><h1>University Records</h1><p class="completion-subtitle">Browse official student and academic supervisor records. These records cannot be edited here.</p></div></header><div class="records-switch" role="group" aria-label="Choose university records"><button type="button" data-record-view="0" aria-pressed="true">Students</button><button type="button" data-record-view="1" aria-pressed="false">Academic supervisors</button></div><asp:Literal ID="recordsMarkup" runat="server"/></main></div></div></form></body></html>
