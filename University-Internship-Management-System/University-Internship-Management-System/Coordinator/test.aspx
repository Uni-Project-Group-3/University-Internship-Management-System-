<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="test.aspx.cs" Inherits="University_Internship_Management_System.Coordinator.test" %>
<%@ Register Src="~/Shared/Sidebar.ascx" TagPrefix="shared" TagName="Sidebar" %>

<!DOCTYPE html>
<html xmlns="http://www.w3.org/1999/xhtml" lang="en">
<head runat="server">
    <title>Coordinator - Internship Management</title>
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/shared/sidebar.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/coordinator/coordinator.css" rel="stylesheet" />
</head>
<body class="app-page">
    <form id="form1" runat="server">
        <div class="app-layout">
            <shared:Sidebar runat="server" HomeUrl="~/Coordinator/test.aspx" UserRole="Coordinator" />
            <main class="app-content">
                <h1>Coordinator</h1>
                <!-- Add this role's page content here. -->
            </main>
        </div>
    </form>
<script src="../Js_project/Public/tab-session.js" defer="defer"></script>
</body>
</html>
