<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="Assign_Supervisors.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.Assign_Supervisors" %>

<!DOCTYPE html>
<html lang="en">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Assign Academic Supervisors | JUST Internships</title>

    <!-- ملفات CSS -->
    <link href="../Content/bootstrap.min.css" rel="stylesheet" />
    <link href="https://cdn.jsdelivr.net/npm/bootstrap-icons@1.11.3/font/bootstrap-icons.min.css" rel="stylesheet" />
    <link href="../Assets/shared/colors.css" rel="stylesheet" />
    <link href="../Assets/coordinator/Assign_Supervisors.css?v=20261021-original-ui" rel="stylesheet" />

    <!-- ملف JS الخاص ببوتستراب وضعناه هنا ليقرأه المتصفح أولاً -->
    <script src="https://cdn.jsdelivr.net/npm/bootstrap@5.3.3/dist/js/bootstrap.bundle.min.js"></script>
</head>
<body data-demo-role="admin">
    <form id="form1" runat="server">
        <asp:ScriptManager ID="ScriptManager1" runat="server"></asp:ScriptManager>
        <div class="app">

            <!-- ============ SIDEBAR ============ -->
            <aside class="sidebar" id="sidebar" aria-label="Coordinator navigation">
                <a href="CoordinatorDashboard.aspx" class="brand">
                    <img class="brand-logo" src="../Assets/shared/images/JUST-Logo-01.png" alt="JUST logo" width="48" height="48" />
                    <div class="brand-text"><strong>JUST</strong><span>Internships</span></div>
                </a>
                <ul class="side-nav">
                    <li><a href="CoordinatorDashboard.aspx"><i class="bi bi-house-door"></i><span>Dashboard</span></a></li>
                    <li><a href="ManageStudents.aspx"><i class="bi bi-mortarboard"></i><span>Manage Students</span></a></li>
                    <li><a href="ManageCompanies.aspx"><i class="bi bi-building"></i><span>Manage Companies</span></a></li>
                    <li><a href='<%= ResolveUrl("~/Coordinator/Manage_Academic_Supervisor.aspx") %>'><i class="bi bi-person-badge"></i><span>Manage Academic Supervisors</span></a></li>
                    <li><a href="Assign_Supervisors.aspx" class="active"><i class="bi bi-diagram-3"></i><span>Assign Academic Supervisors</span></a></li>
                    <li><a href="MonitorPlacementsProgress.aspx"><i class="bi bi-activity"></i><span>Monitor Placements &amp; Progress</span></a></li>
                    <li><a href="FinalCompletionReview.aspx"><i class="bi bi-patch-check"></i><span>Final Completion Review</span></a></li>
                    <li><a href="ContactMessages.aspx"><i class="bi bi-envelope"></i><span>Contact Messages</span></a></li>
                </ul>
                <div class="side-divider"></div>
                <ul class="side-nav">
                    <li><asp:LinkButton ID="btnLogout" runat="server" OnClick="btnLogout_Click" CssClass="text-danger"><i class="bi bi-box-arrow-right"></i>Logout</asp:LinkButton></li>
                </ul>
            </aside>

            <div class="main">
                <!-- ============ TOPBAR ============ -->
                <header class="topbar">
                    <div class="search">
                        <i class="bi bi-search"></i>
                        <input type="text" id="searchInput" placeholder="Search groups or supervisors..." />
                    </div>
                    <div class="topbar-actions ms-auto">
                        <div class="user-chip">
                            <span class="avatar">CO</span>
                            <span class="name">Coordinator Portal<span class="role">Live System</span></span>
                        </div>
                    </div>
                </header>

                <!-- ============ CONTENT ============ -->
                <div class="content">
                    <div class="page-header d-flex justify-content-between align-items-center mb-4">
                        <div>
                            <h1 class="greeting">Assign Supervisors</h1>
                            <p class="subtitle mb-0">Manage student groups and link them to academic supervisors.</p>
                        </div>
                        <button type="button" class="btn custom-btn-primary" data-bs-toggle="modal" data-bs-target="#addGroupModal">
                            <i class="bi bi-plus-lg me-2"></i> Create New Group
                        </button>
                    </div>

                    <asp:Label ID="lblFirebaseStatus" runat="server" CssClass="alert d-block" EnableViewState="false"></asp:Label>

                    <!-- STATS CARDS -->
                    <div class="stats-grid mb-4">
                        <div class="stat-card">
                            <div class="stat-details">
                                <h3><asp:Label ID="lblTotalGroups" runat="server" Text="0" /></h3>
                                <span>Total Groups</span>
                            </div>
                        </div>
                        <div class="stat-card">
                            <div class="stat-details">
                                <h3><asp:Label ID="lblAssignedGroups" runat="server" Text="0" /></h3>
                                <span>Assigned Groups</span>
                            </div>
                        </div>
                        <div class="stat-card border-danger-left">
                            <div class="stat-details">
                                <h3 class="text-danger"><asp:Label ID="lblUnassignedGroups" runat="server" Text="0" /></h3>
                                <span class="text-danger fw-semibold">Action Required (Unassigned)</span>
                            </div>
                        </div>
                    </div>

                    <!-- MAIN TABLE PANEL -->
                    <div class="panel">
                        <h3 class="panel-title mb-3">Student Groups</h3>
                        <div class="table-responsive">
                            <table class="table custom-table align-middle">
                                <thead>
                                    <tr>
                                        <th>Group Details</th>
                                        <th>Department</th>
                                        <th>Students</th>
                                        <th>Assigned Supervisor</th>
                                        <th class="text-end">Manage</th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <asp:Repeater ID="rptGroups" runat="server" OnItemCommand="rptGroups_ItemCommand">
                                        <ItemTemplate>
                                            <tr>
                                                <td>
                                                    <div class="fw-bold"><%#: Eval("GroupId") %></div>
                                                    <div class="text-muted small">Students: <%#: Eval("StudentCount") %></div>
                                                </td>
                                                <td><span class="badge bg-secondary"><%#: Eval("Department") %></span></td>
                                                <td>
                                                    <asp:LinkButton ID="btnViewStudents" runat="server" CssClass="btn btn-sm btn-outline-primary" CommandName="ViewStudents" CommandArgument='<%# Eval("GroupId") %>'>
                                                        <i class="bi bi-people"></i> Manage Students
                                                    </asp:LinkButton>
                                                    <div class="small text-muted mt-1"><%#: Eval("StudentNames") %></div>
                                                </td>
                                                <td>
                                                    <div class="fw-bold"><%#: Eval("SupervisorName") %></div>
                                                </td>
                                                <td class="text-end">
                                                    <asp:LinkButton ID="btnAssign" runat="server" CssClass="btn btn-sm btn-primary me-2" CommandName="AssignSupervisor" CommandArgument='<%# Eval("GroupId") %>'>
                                                        <i class="bi bi-link-45deg"></i> Assign
                                                    </asp:LinkButton>
                                                    <asp:LinkButton ID="btnDelete" runat="server" CssClass="btn btn-sm btn-danger" CommandName="DeleteGroup" CommandArgument='<%# Eval("GroupId") %>'>
                                                        <i class="bi bi-trash"></i>
                                                    </asp:LinkButton>
                                                </td>
                                            </tr>
                                        </ItemTemplate>
                                        <FooterTemplate>
                                            <tr runat="server" visible="<%# rptGroups.Items.Count == 0 %>">
                                                <td colspan="5" class="text-center py-4">No groups found in Firebase.</td>
                                            </tr>
                                        </FooterTemplate>
                                    </asp:Repeater>
                                </tbody>
                            </table>
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============ 1. ADD NEW GROUP MODAL ============ -->
            <div class="modal fade" id="addGroupModal" tabindex="-1">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content custom-modal">
                        <div class="modal-header border-0 pb-0 pt-4 px-4">
                            <h5 class="modal-title fw-bold">Create New Student Group</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body p-4">
                            <div class="mb-4">
                                <label class="form-label">Group Name <span class="text-danger">*</span></label>
                                <asp:TextBox ID="newGroupName" runat="server" CssClass="form-control"></asp:TextBox>
                            </div>
                            <div class="mb-2">
                                <label class="form-label">Department <span class="text-danger">*</span></label>
                                <asp:DropDownList ID="newGroupDept" runat="server" CssClass="form-select">
                                    <asp:ListItem Text="Select Department..." Value=""></asp:ListItem>
                                    <asp:ListItem Text="Information Technology" Value="Information Technology"></asp:ListItem>
                                    <asp:ListItem Text="Computer Science" Value="Computer Science"></asp:ListItem>
                                    <asp:ListItem Text="Software Engineering" Value="Software Engineering"></asp:ListItem>
                                </asp:DropDownList>
                            </div>
                        </div>
                        <div class="modal-footer border-0">
                            <button type="button" class="btn btn-light" data-bs-dismiss="modal">Cancel</button>
                            <asp:Button ID="btnCreateGroup" runat="server" CssClass="btn btn-primary" Text="Create Group" OnClick="btnCreateGroup_Click" />
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============ 2. ASSIGN SUPERVISOR MODAL ============ -->
            <div class="modal fade" id="assignModal" tabindex="-1">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content custom-modal">
                        <div class="modal-header border-0 pb-0 pt-4 px-4">
                            <h5 class="modal-title fw-bold">Assign Academic Supervisor</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <div class="modal-body p-4">
                            <asp:HiddenField ID="hfAssignGroupId" runat="server" />
                            <div class="mb-4">
                                <label class="form-label text-muted">Selected Group</label>
                                <asp:TextBox ID="txtAssignGroupName" runat="server" CssClass="form-control bg-light" ReadOnly="true"></asp:TextBox>
                            </div>
                            <div class="mb-2">
                                <label class="form-label">Select Supervisor <span class="text-danger">*</span></label>
                                <asp:DropDownList ID="ddlSupervisors" runat="server" CssClass="form-select"></asp:DropDownList>
                            </div>
                        </div>
                        <div class="modal-footer border-0">
                            <button type="button" class="btn btn-light" data-bs-dismiss="modal">Cancel</button>
                            <asp:Button ID="btnConfirmAssign" runat="server" CssClass="btn btn-primary" Text="Save Assignment" OnClick="btnConfirmAssign_Click" />
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============ 3. VIEW/ADD STUDENTS MODAL ============ -->
            <div class="modal fade" id="viewStudentsModal" tabindex="-1">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content custom-modal">
                        <div class="modal-header border-0 pb-3 pt-4 px-4 border-bottom">
                            <h5 class="modal-title fw-bold">Manage Group Students</h5>
                            <button type="button" class="btn-close" data-bs-dismiss="modal"></button>
                        </div>
                        <asp:HiddenField ID="hfSelectedGroupId" runat="server" />
                        <div class="modal-body p-4">
                            <h6 class="fw-bold text-primary mb-3">Group: <asp:Label ID="lblModalGroupName" runat="server"></asp:Label></h6>
                            <label class="form-label fw-semibold">Add Unassigned Student</label>
                            <asp:DropDownList ID="ddlUnassignedStudents" runat="server" CssClass="form-select mb-3"></asp:DropDownList>
                            <asp:Button ID="btnAddStudentToGroup" runat="server" CssClass="btn btn-outline-primary w-100 fw-bold" Text="Add Student to Group" OnClick="btnAddStudentToGroup_Click" />
                        </div>
                    </div>
                </div>
            </div>

            <!-- ============ 4. DELETE GROUP MODAL ============ -->
            <div class="modal fade" id="deleteGroupModal" tabindex="-1">
                <div class="modal-dialog modal-dialog-centered">
                    <div class="modal-content custom-modal text-center">
                        <div class="modal-body p-4">
                            <asp:HiddenField ID="hfDeleteGroupId" runat="server" />
                            <i class="bi bi-exclamation-circle text-danger fs-1 mb-3 d-block"></i>
                            <h4 class="fw-bold mb-2">Delete this Group?</h4>
                            <p class="text-muted mb-4">Are you sure you want to delete <asp:Label ID="lblDeleteGroupName" runat="server" CssClass="fw-bold"></asp:Label>? All assigned students will become unassigned.</p>
                            <div class="d-flex justify-content-center gap-3">
                                <button type="button" class="btn btn-light px-4" data-bs-dismiss="modal">Cancel</button>
                                <asp:Button ID="btnDeleteGroupConfirm" runat="server" CssClass="btn btn-danger px-4" Text="Yes, Delete" OnClick="btnDeleteGroupConfirm_Click" />
                            </div>
                        </div>
                    </div>
                </div>
            </div>

        </div>
    </form>
    
</body>
</html>