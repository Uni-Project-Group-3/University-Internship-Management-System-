using System;using University_Internship_Management_System.Shared;
class CompanyPhoneTests {
 static CompanyInput Form(){return new CompanyInput{Id=new string('a',32),RequestId=new string('b',32),Name="Company",SupervisorName="Supervisor",SupervisorEmail="person@example.invalid"};}
 static void Check(bool ok,string label){if(!ok)throw new Exception(label);Console.WriteLine("PASS "+label);}
 static void Main(){var f=Form();f.Phone="0790123456";f.SupervisorPhone="00962790123456";CompanyManagementService.Validate(f);Check(f.Phone=="0790123456"&&f.SupervisorPhone.StartsWith("00"),"numeric phones preserve leading zeros");f=Form();CompanyManagementService.Validate(f);Check(f.Phone==""&&f.SupervisorPhone=="","optional phone fields can be blank");foreach(var bad in new[]{"fds","079abc","079 123","+96279","123-456"})foreach(bool supervisor in new[]{true,false}){f=Form();if(supervisor)f.SupervisorPhone=bad;else f.Phone=bad;bool rejected=false;try{CompanyManagementService.Validate(f);}catch(StudentManagementException){rejected=true;}Check(rejected,"reject invalid "+(supervisor?"supervisor":"company")+" phone");}}
}
