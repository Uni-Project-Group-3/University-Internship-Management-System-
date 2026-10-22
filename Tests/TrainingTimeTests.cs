using System;using System.Reflection;using Newtonsoft.Json.Linq;using University_Internship_Management_System.Services;
public class TrainingTimeTests{
 static MethodInfo Parse=typeof(TrainingWorkflowService).GetMethod("Time",BindingFlags.NonPublic|BindingFlags.Static);
 static long Read(JObject p){return (long)Parse.Invoke(null,new object[]{p,"opensAt"});}
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);}
 static void Deny(string value){try{Read(new JObject{{"opensAt",value}});throw new Exception("Unexpected acceptance");}catch(TargetInvocationException e){Check(e.InnerException is ArgumentException,"reject non-UTC or ambiguous input");}}
 public static void Main(){long expected=new DateTimeOffset(2026,10,22,7,0,0,TimeSpan.Zero).ToUnixTimeMilliseconds();
 Check(Read(JObject.Parse("{\"opensAt\":\"2026-10-22T07:00:00.000Z\"}"))==expected,"parsed JSON UTC date retains instant");
 Check(Read(new JObject{{"opensAt","2026-10-22T07:00:00.000Z"}})==expected,"UTC string retains instant");
 Check(Read(new JObject{{"opensAt","2026-10-22T07:00:00+00:00"}})==expected,"explicit zero offset retains instant");Deny("2026-10-22T10:00:00+03:00");Deny("2026-10-22T10:00:00");}
}