<%@ Page Title="Contact Us" Language="C#" AutoEventWireup="true" CodeBehind="Contact.aspx.cs" Inherits="University_Internship_Management_System.Contact" %>
<!DOCTYPE html>
<html lang="en" dir="ltr">
<head runat="server">
    <meta charset="utf-8" />
    <meta name="viewport" content="width=device-width, initial-scale=1" />
    <title>Contact Us | JUST Internships</title>
    <link runat="server" href="~/Assets/shared/colors.css" rel="stylesheet" />
    <link runat="server" href="~/Assets/public/contact.css?v=2" rel="stylesheet" />
</head>
<body class="contact-page">
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
            <a id="ContactLink" class="home-navbar__link is-active" aria-current="page" runat="server" href="~/Contact.aspx">Contact Us</a>
            <a id="LoginLink" class="home-navbar__link home-navbar__sign-in" runat="server" href="~/Auth/Login.aspx">Sign In</a>
        </div>
    </nav>
</header>
    <main id="contactMain" class="contact-main">
        <header class="contact-heading">
            <p class="contact-eyebrow">HERE TO HELP</p>
            <h1>Let&#8217;s keep your<br /><span>internship moving.</span></h1>
            <p>Have a question about your training journey? Find the right support or prepare a message for the internship team.</p>
        </header>
        <div class="contact-layout">
            <aside class="contact-support" aria-labelledby="supportTitle">
                <div class="contact-support-heading">
                    <span class="contact-small-label">CONTACT US</span>
                    <h2 id="supportTitle">A little guidance.<br />A clear next step.</h2>
                    <p>Start with the team responsible for your question.</p>
                </div>
                <div class="contact-route">
                    <span class="contact-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><path d="m3 9 9-5 9 5-9 5-9-5Z" /><path d="M7 12v5c3 3 7 3 10 0v-5M21 9v7" /></svg></span>
                    <div><h3>Academic &amp; training questions</h3><p>Contact your internship coordinator about eligibility, required hours, and academic information.</p></div>
                </div>
                <div class="contact-route">
                    <span class="contact-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><rect x="4" y="7" width="16" height="13" rx="2" /><path d="M9 7V4h6v3M4 12h16M10 12v3h4v-3" /></svg></span>
                    <div><h3>Internship opportunities</h3><p>Ask your coordinator about placement requirements, applications, and company coordination.</p></div>
                </div>
                <div class="contact-route">
                    <span class="contact-icon" aria-hidden="true"><svg viewBox="0 0 24 24"><rect x="3" y="4" width="18" height="13" rx="2" /><path d="M8 21h8M12 17v4m-4-11 2 2 5-5" /></svg></span>
                    <div><h3>Account &amp; technical help</h3><p>Describe the page and issue you encountered. Never include your password or verification codes.</p></div>
                </div>
                <div class="contact-availability"><span aria-hidden="true">i</span><p>Official contact details will appear here once they are provided by the university.</p></div>
            </aside>
            <section class="contact-form-card" aria-labelledby="messageTitle">
                <div class="contact-card-heading"><span class="contact-small-label">GET IN TOUCH</span><h2 id="messageTitle">Send us a message</h2><p>Tell us a little about yourself and how we can help.</p></div>
                <form id="contactForm" runat="server" aria-describedby="contactStatus">
                    <div class="contact-fields">
                        <div class="contact-field"><label for="contactName">Full name <span aria-hidden="true">*</span></label><input id="contactName" name="fullName" type="text" autocomplete="name" placeholder="Your full name" required="required" maxlength="100" /></div>
                        <div class="contact-field"><label for="contactEmail">Email address <span aria-hidden="true">*</span></label><input id="contactEmail" name="email" type="email" autocomplete="email" placeholder="you@university.edu" required="required" maxlength="254" /></div>
                        <div class="contact-field contact-field-wide"><label for="contactSubject">Subject <span aria-hidden="true">*</span></label><input id="contactSubject" name="subject" type="text" placeholder="What would you like help with?" required="required" maxlength="150" /></div>
                        <div class="contact-field contact-field-wide"><label for="contactMessage">Message <span aria-hidden="true">*</span></label><textarea id="contactMessage" name="message" rows="5" placeholder="Share the details of your question&#8230;" required="required" maxlength="2000" aria-describedby="messageHelp"></textarea><span id="messageHelp" class="contact-help">Up to 2,000 characters. Please leave out sensitive personal information.</span></div>
                    </div>
                    <div id="contactStatus" class="contact-status" role="note"><strong>Message delivery is not available yet.</strong><p>You can try the fields, but this form does not send or save messages.</p></div>
                    <div class="contact-form-footer"><span class="contact-help">Fields marked * are required.</span><button type="button" disabled="disabled" aria-describedby="contactStatus">Send Message <span aria-hidden="true">&#8599;</span></button></div>
                </form>
            </section>
        </div>
        <section class="contact-faq" aria-labelledby="faqTitle">
            <div><span class="contact-small-label">BEFORE YOU REACH OUT</span><h2 id="faqTitle">A few helpful answers</h2><p>Your next step might already be here.</p></div>
            <div class="contact-faq-list">
                <details><summary>Who can update my academic information?</summary><p>Your internship coordinator manages academic information, including your major and required training hours. Contact them if your details need correction.</p></details>
                <details><summary>Can I send my CV through this form?</summary><p>This form does not accept attachments or deliver messages yet. The Student Profile page lets you select a CV file, but uploading and saving still require account integration.</p></details>
                <details><summary>What should I include in a technical inquiry?</summary><p>Include the page name, what you were trying to do, and the error message you saw. Do not share your password or verification codes.</p></details>
            </div>
        </section>
    </main>
    <footer class="contact-footer"><span>UIMS &middot; JUST Internships</span><span>Supporting your next chapter.</span></footer>
<script>
// @ts-nocheck
(() => {
    "use strict";

    document.querySelectorAll(".home-navbar").forEach(nav => {
        const toggle = nav.querySelector(".home-navbar__toggle");
        const links = nav.querySelector(".home-navbar__links");
        const mobile = window.matchMedia("(max-width: 700px)");

        /** @param {boolean} open */
        function setOpen(open) {
            toggle.setAttribute("aria-expanded", String(open));
            toggle.setAttribute("aria-label", open ? "Close navigation menu" : "Open navigation menu");
            links.classList.toggle("is-open", open);
        }

        nav.classList.add("has-menu-toggle");
        toggle.hidden = false;
        toggle.addEventListener("click", () => setOpen(toggle.getAttribute("aria-expanded") !== "true"));
        nav.addEventListener("keydown", event => {
            if (event.key === "Escape" && toggle.getAttribute("aria-expanded") === "true") {
                setOpen(false);
                toggle.focus();
            }
        });
        nav.addEventListener("focusout", event => {
            if (!nav.contains(event.relatedTarget)) setOpen(false);
        });
        document.addEventListener("click", event => {
            if (!nav.contains(event.target)) setOpen(false);
        });
        mobile.addEventListener("change", () => setOpen(false));
    });
})();

</script>
<script src="Js_project/Public/contact-demo.js"></script></body>
</html>
