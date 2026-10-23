# Free HTTPS deployment preparation — no activation performed

Firebase stays on Spark. Private report files remain on permanent disk; do not enable Firebase Storage or paid hosting. This folder contains a **read-only readiness checker** and a **review-only HTTPS configuration fragment**. Neither opens ports, exposes the site, installs software, changes certificates/Rules/IAM, or modifies the live application.

## Current evidence

- Current server is IIS Express on localhost:55448. Full IIS/W3SVC and appcmd are not installed/available in the checked environment. No cloudflared/ngrok/caddy/ACME client was found. No approved reachable DNS hostname or matching trusted certificate was provided.
- The C# attendance transport requires actual HTTPS plus an exact hostname allowlist for remote writes. Arbitrary X-Forwarded-Proto is intentionally ignored. An HTTPS public tunnel terminating into this HTTP origin alone will not satisfy that protection; do not weaken it to make a tunnel work.
- `C:\Users\USER\UIMS-Data\PrivateReports` is permanent private local storage with verified IIS Express account ACL and verified restart survival. A remote deployment must copy the files and provide its own persistent folder/ACL/backup; a static website host cannot run ASP.NET Web Forms.

## Access the team must provide

1. The exact Windows PC/server to host this single-instance Web Forms app, with administrator/operator approval to install/enable IIS ASP.NET features, create its application pool/site/HTTPS binding and approve network access. Existing licensed hardware may avoid new hosting fees; no free commercial Windows host is assumed or purchased.
2. An approved DNS hostname controlled by the team and DNS-management access. For public certificates, prove domain control using an ACME challenge. Let's Encrypt provides free certificates, but an ACME client and renewal setup are still required. Do not buy a domain or accept service terms automatically.
3. A reachable network path for the intended phones (LAN/VPN/public, explicitly chosen), and authorized firewall/router/DNS changes. No public tunnel or wildcard binding is created without an agreed exposure plan.
4. The actual application-pool identity and authorized Firebase credential provisioning for that identity, plus access to the existing external QR signing key. Do not paste keys/passwords/tokens into chat or copy them under the website. Current user's ADC does not automatically grant a new pool identity access.

## Operator runbook once access is provided

1. Publish a copy of the reviewed project to a separate permanent site directory. Back up existing IIS/site/local config before edits. Preserve Login/team functionality and all Firebase IDs/history. Keep the current localhost project usable until HTTPS is verified.
2. Configure a .NET v4 integrated application pool and ASP.NET support, provide credentials outside webroot, and set a permanent private report folder restricted to its runtime Windows identity, SYSTEM and administrators. Back up files together with RTDB metadata. Do not use ephemeral storage or independent disks on multiple instances.
3. Obtain/install the matching trusted server certificate with automated renewal. Configure a hostname-specific HTTPS binding in IIS. Do not bypass certificate errors or install a certificate authority on phones without the device owner's explicit participation.
4. Merge `Https-settings.example.xml` into the deployment **copy**, preserving existing options and applying the exact host in the local override too. Require HTTPS and Secure/HttpOnly cookies. Do not blindly replace the shared Web.config or set Secure cookies on the current HTTP development endpoint.
5. Run the read-only checker as the actual runtime identity (or supply its SID explicitly):

   ```powershell
   .\Test-Readiness.ps1 -ProjectPath 'C:\path\to\deployed-UIMS' -HostName 'YOUR_APPROVED_DNS_NAME'
   ```

   A missing prerequisite gives exit code 2. No hostname means a local static check only. It never downloads private keys, reads ADC contents, signs in, changes Firebase, or creates a report. Passing is not an end-to-end acceptance test.

6. On a phone, open the real HTTPS Login without warnings, sign in yourself, verify Secure/HttpOnly cookies and correct-role access, then use an authorized current placement for the camera test. Never reset today's completed attendance to repeat a test. Actual camera scanning remains a separate human step.
7. Test restart/redeploy persistence and restore on the actual deployment. Genuine report upload/download stays pending until the team supplies its real DOCX and approved week/dates. Keep all records and review revisions intact.

## Official references

- [Microsoft: HTTPS certificates and bindings in IIS](https://learn.microsoft.com/en-gb/iis/manage/configuring-security/how-to-set-up-ssl-on-iis)
- [Microsoft: Forms Authentication requireSSL](https://learn.microsoft.com/dotnet/api/system.web.security.formsauthentication.requiressl)
- [Let's Encrypt: free certificate setup and ACME client requirements](https://letsencrypt.org/getting-started/)
- [Let's Encrypt: proving domain control](https://letsencrypt.org/how-it-works/)
