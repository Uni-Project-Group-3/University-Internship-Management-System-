<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="team.aspx.cs" Inherits="University_Internship_Management_System.Public.team" %>
<!DOCTYPE html>
<html lang="en">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<title>Our Team – JUST Internships</title>
<!--
  EXTERNAL DEPENDENCIES: Google Fonts only (Fraunces + Manrope, same as the homepage). No other libraries.
  TO CUSTOMISE: edit CONFIG.team in Js_project/Public/team.js; page styles are in Assets/public/team.css.
-->
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Fraunces:ital,opsz,wght@0,9..144,400;0,9..144,500;1,9..144,400&family=Manrope:wght@400;500;600;700;800&display=swap" rel="stylesheet">
<link rel="stylesheet" href="../Assets/public/team.css?v=20261024" />
</head>
<body>
<header class="nav" role="banner">
 <a class="brand" href="#" data-home="" aria-label="JUST Internships – back to the homepage"><img src="../Assets/public/images/team-brand.png" alt="Jordan University of Science and Technology"></a>
 <nav aria-label="Main"><ul>
  <li><a class="lnk" href="#" data-home="#process">Process</a></li>
  <li><a class="lnk" href="#" data-home="#opportunities">Opportunities</a></li>
  <li><a class="lnk" href="#" data-home="#colleges">Colleges</a></li>
  <li><a class="lnk" href="#" data-home="#about">About</a></li>
  <li><a class="lnk" href="#" data-home="#contact">Contact</a></li>
  <li><a class="lnk" href="#team" aria-current="page">Team</a></li>
  <li><a class="lnk login" href="../Auth/Login.aspx" data-link="login">Login</a></li>
 </ul></nav>
</header>

<main id="team">
 <section class="hero" aria-labelledby="t-h">
  <a class="crumb rise" style="--d:0" href="#" data-home="">← Back to the homepage</a><br>
  <span class="eyebrow rise" style="--d:1">The team behind the platform</span>
  <h1 id="t-h" class="rise" style="--d:2">The people who connect the <em>classroom</em> to the career.</h1>
  <p class="lead rise" style="--d:3">Meet the team building and supporting the Internship Management System. Scroll and watch seven people become one gateway, then meet each of them.</p>
  <div class="cta rise" style="--d:4"><a class="btn pri" href="#story">Meet the team ↓</a><a class="btn ghost" href="#" data-home="">Homepage</a></div>
  <div class="count rise" style="--d:3" aria-hidden="true"><b id="n">00</b><span>members</span></div>
 </section>

 <section class="story" id="story" aria-label="Meet the team">
  <div class="stage" id="stage">
   <svg class="gate" id="gate" aria-hidden="true"><path id="arc"/><circle id="key" r="5"/></svg>
   <div id="tiles"></div>
   <div class="cap" id="cap" aria-hidden="true"><span class="eyebrow" id="capk"></span><h2 id="capt"></h2></div>
   <article class="info" id="info" aria-live="polite"><span class="eyebrow" id="irole"></span><h3 id="iname"></h3><p id="ibio"></p><ul class="soc" id="isoc"></ul>
    <div class="step"><button type="button" id="prev" aria-label="Previous member">←</button><span class="pos" id="pos"></span><button type="button" id="next" aria-label="Next member">→</button></div></article>
  </div>
 </section>
 <ol class="flat" id="flat" aria-label="Team members"></ol>

 <section class="close">
  <span class="eyebrow">Get in touch</span>
  <h2>Questions about your placement?</h2>
  <div class="cta"><a class="btn pri" href="../Contact.aspx" data-link="contact">Contact us</a><a class="btn ghost" href="#" data-home="">Back to the homepage</a></div>
 </section>
</main>

<footer aria-label="Site footer"><div class="fc"><div class="fm">
 <div class="fb"><img src="../Assets/public/images/team-brand.png" alt="Jordan University of Science and Technology"><p>Connecting academic learning<br>with practical experience.</p><span>University Internship Management System</span></div>
 <nav aria-label="Explore"><h2>Explore</h2><a href="#" data-home="">Homepage</a><a href="#" data-home="#opportunities">Internship opportunities</a><a href="#" data-home="#process">How it works</a><a href="#team">Our team</a></nav>
 <nav aria-label="Support"><h2>Get in touch</h2><a href="../Contact.aspx" data-link="contact">Contact</a><a href="https://www.just.edu.jo/" target="_blank" rel="noopener noreferrer">University website ↗</a><a href="../Auth/Login.aspx" data-link="login">University login ↗</a></nav>
</div><div class="fl"><span>© 2026 Jordan University of Science and Technology</span><span>Internship Management System</span></div></div></footer>
<div class="toast" id="toast" role="status"></div>

<script src="../Js_project/Public/team.js?v=20261024-remove-yasmeen-photo"></script>
<noscript><p style="position:fixed;bottom:0;left:0;right:0;margin:0;padding:14px 18px;background:#f4efe3;color:#1d2a3a">The team list needs JavaScript. <a href="home.aspx">Back to the homepage</a></p></noscript>
</body>
</html>
