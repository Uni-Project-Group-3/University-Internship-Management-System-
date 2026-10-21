<%@ Control Language="C#" AutoEventWireup="true" CodeBehind="HomeNavbar.ascx.cs" Inherits="University_Internship_Management_System.Shared.HomeNavbar" %>
<header class="home-header">
    <nav class="home-navbar" aria-label="Public navigation">
        <a class="home-navbar__brand" runat="server" href="~/Default.aspx">
            <img class="home-navbar__logo" runat="server" src="~/Assets/shared/images/just-logo.png" width="40" height="40" alt="Official JUST emblem" />
            <span><strong>UIMS</strong><span class="home-navbar__subtitle">JUST Internships</span></span>
        </a>
        <button class="home-navbar__toggle" type="button" aria-label="Open navigation menu" aria-expanded="false" aria-controls="publicNavigationLinks" hidden="hidden">
            <svg viewBox="0 0 24 24" width="22" height="22" aria-hidden="true"><path d="M4 6h16M4 12h16M4 18h16" /></svg>
        </button>
        <div id="publicNavigationLinks" class="home-navbar__links">
            <a id="HomeLink" class="home-navbar__link" runat="server" href="~/Default.aspx">Home</a>
            <a id="AboutLink" class="home-navbar__link" runat="server" href="~/About.aspx">About Us</a>
            <a id="ContactLink" class="home-navbar__link" runat="server" href="~/Contact.aspx">Contact Us</a>
            <a id="LoginLink" class="home-navbar__link home-navbar__sign-in" runat="server" href="~/Auth/Login.aspx">Sign In</a>
        </div>
    </nav>
</header>
<script src="<%= ResolveUrl("~/Js_project/Public/navbar.js") %>" defer="defer"></script>
