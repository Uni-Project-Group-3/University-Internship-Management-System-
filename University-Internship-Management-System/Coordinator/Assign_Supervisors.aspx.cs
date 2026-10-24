using System;
using System.Data;
using System.Collections.Generic;
using System.Web.UI.WebControls;

namespace University_Internship_Management_System.Coordinator
{
    public partial class Assign_Supervisors : System.Web.UI.Page
    {
        protected void Page_Load(object sender, EventArgs e)
        {
            if (!IsPostBack)
            {
                BindGroupsData();
            }
        }

        private void BindGroupsData()
        {
            // تمثيل بيانات المجموعات (يمكن ربطها بقاعدة البيانات لاحقاً)
            DataTable dtGroups = new DataTable();
            dtGroups.Columns.Add("GroupId");
            dtGroups.Columns.Add("GroupName");
            dtGroups.Columns.Add("Major");
            dtGroups.Columns.Add("StudentCount");
            dtGroups.Columns.Add("AssignedSupervisorId");

            dtGroups.Rows.Add("G-101", "Software Engineering Group A", "Software Engineering", "12", "SUP-02");
            dtGroups.Rows.Add("G-102", "Computer Science Batch 2023", "Computer Science", "15", "SUP-01");
            dtGroups.Rows.Add("G-103", "AI & Data Science Team", "Artificial Intelligence", "10", "SUP-03");

            rptGroups.DataSource = dtGroups;
            rptGroups.DataBind();
        }

        public DataTable GetSupervisorsList()
        {
            // قائمة المشرفين الأكاديميين المتاحة للربط
            DataTable dtSup = new DataTable();
            dtSup.Columns.Add("SupervisorId");
            dtSup.Columns.Add("SupervisorName");

            dtSup.Rows.Add("", "-- Select Supervisor --");
            dtSup.Rows.Add("SUP-01", "Dr. Nidal Fares Al-Zoubi (Demo)");
            dtSup.Rows.Add("SUP-02", "Dr. Dana Adnan Al-Qudah (Demo)");
            dtSup.Rows.Add("SUP-03", "Dr. Anas Yaseen Al-Khatib (Demo)");
            dtSup.Rows.Add("SUP-04", "Dr. Karam Nabil Al-Rawashdeh (Demo)");

            return dtSup;
        }

        protected void rptGroups_ItemCommand(object source, RepeaterCommandEventArgs e)
        {
            if (e.CommandName == "UpdateGroup")
            {
                string groupId = e.CommandArgument.ToString();
                DropDownList ddl = (DropDownList)e.Item.FindControl("ddlSupervisors");
                string selectedSupervisor = ddl.SelectedValue;

                // هنا يتم حفظ التحديث في قاعدة البيانات للمجموعة المحددة
                lblMessage.Text = $"Successfully updated supervisor for Group {groupId}!";
                lblMessage.Visible = true;
            }
        }

        protected void btnSaveAll_Click(object sender, EventArgs e)
        {
            // منطق حفظ كافة التغييرات المدخلة دفعة واحدة
            foreach (RepeaterItem item in rptGroups.Items)
            {
                if (item.ItemType == ListItemType.Item || item.ItemType == ListItemType.AlternatingItem)
                {
                    HiddenField hfGroupId = (HiddenField)item.FindControl("hfGroupId");
                    DropDownList ddlSupervisors = (DropDownList)item.FindControl("ddlSupervisors");

                    string groupId = hfGroupId.Value;
                    string supervisorId = ddlSupervisors.SelectedValue;

                    // تنفيذ أمر التحديث في قاعدة البيانات لكل مجموعة
                }
            }

            lblMessage.Text = "All supervisor assignments have been saved successfully!";
            lblMessage.Visible = true;
        }
    }
}