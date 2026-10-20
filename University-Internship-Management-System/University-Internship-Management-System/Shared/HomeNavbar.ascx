<%@ Control Language="C#" %>
<header class="home-header">
    <nav class="home-navbar" aria-label="Home navigation">
        <a class="home-navbar__brand" runat="server" href="~/Default.aspx">
            <span class="home-navbar__mark" aria-hidden="true">J</span>
            <span><strong>JUST</strong><span class="home-navbar__subtitle">Internships</span></span>
        </a>
        <div class="home-navbar__links">
            <a class="home-navbar__link is-active" runat="server" href="~/Default.aspx" aria-current="page">Home</a>
            <a class="home-navbar__link" runat="server" href="~/About.aspx">About</a>
            <a class="home-navbar__link" runat="server" href="~/Contact.aspx">Contact</a>
        </div>
    </nav>
</header>
