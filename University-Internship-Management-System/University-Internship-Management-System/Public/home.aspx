<%@ Page Language="C#" AutoEventWireup="true" CodeBehind="home.aspx.cs" Inherits="University_Internship_Management_System.Public.home" %>
<!DOCTYPE html>
<html lang="en" dir="ltr">
<head>
<meta charset="utf-8">
<meta name="viewport" content="width=device-width, initial-scale=1, viewport-fit=cover">
<title>JUST campus walkthrough – environment</title>
<!--
  EXTERNAL DEPENDENCIES (internet required on first load):
   1. three.js r128  – cdnjs.cloudflare.com (script below)
   2. Noto Naskh Arabic – Google Fonts (optional; the Arabic signs fall back to system Arabic fonts if offline)
   3. Bootstrap 5 is NOT loaded. Add its <link> in the head when the homepage content is added.
  OPTIONAL LOCAL ASSET:  assets/just-crest.png  (official JUST crest, transparent PNG, square). See README.
-->
<link rel="preconnect" href="https://fonts.googleapis.com">
<link rel="preconnect" href="https://fonts.gstatic.com" crossorigin>
<link href="https://fonts.googleapis.com/css2?family=Noto+Naskh+Arabic:wght@600;700&display=swap" rel="stylesheet">
<link rel="stylesheet" href="../Assets/public/home.css">
</head>
<body>

<div id="scene-layer"><canvas id="scene" role="img" aria-label="Three-dimensional walkthrough of the campus: main gate, campus courtyard and the Faculty of Computer and Information Technology"></canvas></div>

<!--
  =====================  CONTENT LAYER  =====================
  Order matters: travel → hold → travel → hold → travel → hold.
  The camera MOVES while a .travel block scrolls and is STATIONARY while a .hold block scrolls.
  Add content only inside the three .slot containers. A taller slot makes its .hold taller,
  so the view simply stays longer – movement speed never changes.
-->
<main id="content-layer"><section class="hold" data-stage="0" id="stage-gateIntro"><div class="slot" id="slot-gateIntro" aria-label="Future gateIntro content"></div></section>
<div class="travel" data-travel="0"></div><section class="hold" data-stage="1" id="stage-gateOverview"><div class="slot" id="slot-gateOverview" aria-label="Future gateOverview content"></div></section>
<div class="travel" data-travel="1"></div><section class="hold" data-stage="2" id="stage-walkwayOverview"><div class="slot" id="slot-walkwayOverview" aria-label="Future walkwayOverview content"></div></section>
<div class="travel" data-travel="2"></div><section class="hold" data-stage="3" id="stage-opportunityColumn"><div class="slot" id="slot-opportunityColumn" aria-label="Future opportunityColumn content"></div></section>
<div class="travel" data-travel="3"></div><section class="hold" data-stage="4" id="stage-courtyardReturn"><div class="slot" id="slot-courtyardReturn" aria-label="Future courtyardReturn content"></div></section>
<div class="travel" data-travel="4"></div><section class="hold" data-stage="5" id="stage-platformVideo"><div class="slot" id="slot-platformVideo" aria-label="Future platformVideo content"></div></section>
<div class="travel" data-travel="5"></div><section class="hold" data-stage="6" id="stage-facultyQuestions"><div class="slot" id="slot-facultyQuestions" aria-label="Future facultyQuestions content"></div></section><div style="height:100vh" aria-hidden="true"></div></main>

<p id="fallback" hidden role="alert">The 3D campus view could not be started on this device (WebGL or a required asset is unavailable). Please try a current browser with hardware acceleration enabled.</p>

<script src="../Js_project/Public/home.js"></script>

</body>
</html>
