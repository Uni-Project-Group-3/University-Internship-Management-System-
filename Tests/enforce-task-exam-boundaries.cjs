const fs=require('fs'),p='University-Internship-Management-System/Services/TaskService.cs';let s=fs.readFileSync(p,'utf8');
s=s.replaceAll('await EnsureStudentOwnsTaskAsync(task, cancellation).ConfigureAwait(false);\r\n                RequireSubmissionDeadline(task);','await EnsureStudentOwnsTaskAsync(task, cancellation).ConfigureAwait(false);\r\n                RequireStandardTask(task);\r\n                RequireSubmissionDeadline(task);');
s=s.replace('await EnsureCompanyOwnsTaskAsync(task, cancellation).ConfigureAwait(false);\r\n                RequireTaskOpen(task);','await EnsureCompanyOwnsTaskAsync(task, cancellation).ConfigureAwait(false);\r\n                RequireStandardTask(task);\r\n                RequireTaskOpen(task);');
s=s.replace('await EnsureStudentOwnsTaskAsync(task, cancellation).ConfigureAwait(false);\r\n            RequireSubmissionDeadline(task);','await EnsureStudentOwnsTaskAsync(task, cancellation).ConfigureAwait(false);\r\n            RequireStandardTask(task);\r\n            RequireSubmissionDeadline(task);');
s=s.replace('        private static void RequireSubmissionDeadline(JObject task)',`        private static void RequireStandardTask(JObject task)
        {
            if (Text(task, "taskType") == "exam" || Text(task, "examWorkflowStatus").Length > 0)
                throw new UIMS.Services.FirebaseException(409, "Use the examination submission and result approval workflow for this task.");
        }

        private static void RequireSubmissionDeadline(JObject task)`);fs.writeFileSync(p,s);
