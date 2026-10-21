(() => {
    "use strict";

    const form = document.querySelector(".auth-layout");
    if (!form) return;
    const dialog = document.getElementById("firstLoginDialog");
    const newPassword = document.getElementById("NewPassword");
    const confirmation = document.getElementById("ConfirmPassword");
    const resetDialog = document.getElementById("forgotPasswordDialog");
    const resetOpener = document.getElementById("ForgotPassword");
    const resetEmail = document.getElementById("ResetEmail");
    const resetButton = document.getElementById("SendResetLink");
    const resetError = document.getElementById("resetError");
    const saveChanges = document.getElementById("SaveChanges");
    const requiresPasswordChange = form.dataset.passwordRequired === "true";
    const isPasswordPreview = !requiresPasswordChange &&
        new URLSearchParams(window.location.search).get("preview") === "first-login";
    let resetBusy = false;
    let submitting = false;
    document.querySelectorAll("[data-loading]").forEach(button => { button.dataset.label = button.value; });

    document.querySelectorAll("[data-toggle]").forEach(button => {
        const label = button.getAttribute("aria-label");
        button.addEventListener("click", () => {
            const input = document.getElementById(button.dataset.toggle);
            const showing = input.type === "password";
            input.type = showing ? "text" : "password";
            button.setAttribute("aria-pressed", String(showing));
            button.setAttribute("aria-label", showing ? label.replace("Show", "Hide") : label);
        });
    });

    function passwordRules() {
        const value = newPassword.value;
        return {
            length: value.length >= 12,
            case: /[a-z]/.test(value) && /[A-Z]/.test(value),
            number: /[0-9]/.test(value),
            symbol: /[^a-zA-Z0-9\s]/.test(value)
        };
    }

    function updatePasswordFeedback() {
        const rules = passwordRules();
        const score = Object.values(rules).filter(Boolean).length;
        const meter = document.getElementById("strengthMeter");
        document.querySelectorAll("[data-rule]").forEach(item => {
            item.classList.toggle("is-valid", rules[item.dataset.rule]);
        });
        meter.querySelectorAll("span").forEach((bar, index) => bar.classList.toggle("is-filled", index < score));
        meter.classList.toggle("is-strong", score === 4);
        meter.setAttribute("aria-valuenow", String(score));
        const strength = newPassword.value ? ["Very weak", "Weak", "Fair", "Good", "Strong"][score] : "Password strength";
        meter.setAttribute("aria-valuetext", strength);
        document.getElementById("strengthText").textContent = strength;
        const match = confirmation.value && confirmation.value === newPassword.value;
        const message = document.getElementById("matchText");
        message.textContent = confirmation.value ? (match ? "Passwords match" : "Passwords do not match yet") : "";
        message.classList.toggle("is-valid", Boolean(match));
        message.classList.toggle("is-invalid", Boolean(confirmation.value && !match));
        saveChanges.disabled = submitting || !Object.values(rules).every(Boolean) || !match;
    }

    newPassword.addEventListener("input", updatePasswordFeedback);
    confirmation.addEventListener("input", updatePasswordFeedback);
    dialog.addEventListener("cancel", event => event.preventDefault());

    if (requiresPasswordChange || isPasswordPreview) {
        if (typeof dialog.showModal === "function") {
            dialog.showModal();
        } else {
            dialog.setAttribute("open", "");
            document.getElementById("loginContent").setAttribute("inert", "");
        }
        newPassword.focus();
        document.getElementById("firstLoginPreviewNotice").hidden = !isPasswordPreview;
        document.getElementById("exitPasswordPreview").hidden = !isPasswordPreview;
    }
    updatePasswordFeedback();

    resetOpener.addEventListener("click", () => {
        if (requiresPasswordChange || isPasswordPreview) return;
        document.getElementById("resetFields").hidden = false;
        document.getElementById("resetConfirmation").hidden = true;
        document.getElementById("resetEmailError").textContent = "";
        resetEmail.removeAttribute("aria-invalid");
        resetError.hidden = true;
        resetEmail.value = document.getElementById("Email").value.trim();
        resetDialog.showModal();
        resetEmail.focus();
    });

    document.querySelectorAll("[data-close-reset]").forEach(button => {
        button.addEventListener("click", () => resetDialog.close());
    });
    resetDialog.addEventListener("close", () => resetOpener.focus());
    resetDialog.addEventListener("click", event => {
        const bounds = resetDialog.getBoundingClientRect();
        if (event.target === resetDialog && (event.clientX < bounds.left || event.clientX > bounds.right ||
            event.clientY < bounds.top || event.clientY > bounds.bottom)) {
            resetDialog.close();
        }
    });
    resetEmail.addEventListener("keydown", event => {
        if (event.key === "Enter") {
            event.preventDefault();
            sendResetLink();
        }
    });
    resetButton.addEventListener("click", sendResetLink);

    async function sendResetLink() {
        if (resetBusy) return;
        const valid = Boolean(resetEmail.value.trim()) && resetEmail.validity.valid;
        document.getElementById("resetEmailError").textContent = valid ? "" : "Enter a valid email address.";
        resetEmail.setAttribute("aria-invalid", String(!valid));
        if (!valid) {
            resetEmail.focus();
            return;
        }

        resetBusy = true;
        resetButton.disabled = true;
        resetButton.textContent = "Sending...";
        resetDialog.setAttribute("aria-busy", "true");
        resetError.hidden = true;
        const controller = new AbortController();
        const timeout = window.setTimeout(() => controller.abort(), 20000);
        try {
            const url = new URL(form.action, window.location.href);
            url.searchParams.set("action", "reset");
            const response = await fetch(url, {
                method: "POST",
                credentials: "same-origin",
                signal: controller.signal,
                headers: { "Content-Type": "application/x-www-form-urlencoded;charset=UTF-8" },
                body: new URLSearchParams({
                    email: resetEmail.value.trim(),
                    csrfToken: document.getElementById("CsrfToken").value
                })
            });
            const result = await response.json();
            if (!response.ok || !result.success) {
                resetError.textContent = result.message || "Unable to send your request. Please try again later.";
                resetError.hidden = false;
                return;
            }

            document.getElementById("resetFields").hidden = true;
            const confirmation = document.getElementById("resetConfirmation");
            confirmation.hidden = false;
            if (resetDialog.open) confirmation.focus();
        } catch {
            resetError.textContent = "We couldn't connect. Check your connection and try again in a minute.";
            resetError.hidden = false;
        } finally {
            window.clearTimeout(timeout);
            resetBusy = false;
            resetButton.disabled = false;
            resetButton.textContent = "Send Reset Link";
            resetDialog.removeAttribute("aria-busy");
        }
    }

    form.addEventListener("submit", event => {
        if (resetDialog.open) {
            event.preventDefault();
            sendResetLink();
            return;
        }
        if (submitting) {
            event.preventDefault();
            return;
        }
        const button = event.submitter;
        const action = button ? button.dataset.action : "login";
        if (action === "login") {
            const email = document.getElementById("Email");
            const password = document.getElementById("Password");
            const emailValid = Boolean(email.value.trim()) && email.validity.valid;
            const passwordValid = Boolean(password.value);
            document.getElementById("emailError").textContent = emailValid ? "" : "Enter a valid email address.";
            document.getElementById("passwordError").textContent = passwordValid ? "" : "Enter your password.";
            email.setAttribute("aria-invalid", String(!emailValid));
            password.setAttribute("aria-invalid", String(!passwordValid));
            if (!emailValid || !passwordValid) {
                event.preventDefault();
                (emailValid ? password : email).focus();
                return;
            }
        }
        if (action === "change") {
            updatePasswordFeedback();
            if (!Object.values(passwordRules()).every(Boolean) || newPassword.value !== confirmation.value) {
                event.preventDefault();
                (!Object.values(passwordRules()).every(Boolean) ? newPassword : confirmation).focus();
                return;
            }
        }
        if (action === "change" && isPasswordPreview) {
            event.preventDefault();
            submitting = true;
            saveChanges.disabled = true;
            saveChanges.value = "Previewing...";
            dialog.setAttribute("aria-busy", "true");
            window.setTimeout(() => {
                const status = document.getElementById("firstChangeStatus");
                status.textContent = "Preview complete. No password was changed.";
                status.classList.add("is-success");
                status.hidden = false;
                dialog.removeAttribute("aria-busy");
                saveChanges.value = "Preview Again";
                submitting = false;
                updatePasswordFeedback();
                status.focus();
            }, 450);
            return;
        }
        submitting = true;
        form.setAttribute("aria-busy", "true");
        if (button) {
            // Keep the successful submit control enabled so Web Forms receives its name.
            button.setAttribute("aria-disabled", "true");
            if (button.dataset.loading) button.value = button.dataset.loading;
        }
    });

    window.addEventListener("pageshow", () => {
        submitting = false;
        form.removeAttribute("aria-busy");
        document.querySelectorAll("[data-loading]").forEach(button => {
            button.removeAttribute("aria-disabled");
            button.value = button.dataset.label;
        });
        updatePasswordFeedback();
    });
})();


/* Explicit local demo identity selection. No real authentication or password storage. */
(function(){
    const demo=window.UimsDemo, mount=document.getElementById('loginContent');if(!demo||!mount)return;
    const section=document.createElement('section');section.className='auth-demo';
    section.innerHTML='<h2>Local frontend demo</h2><p>Not secure authentication. Select a test identity; passwords are validated for UI testing only and are never saved. Data stays in this browser.</p><label for="demoIdentity">Test identity</label><select id="demoIdentity"></select><button type="button" id="demoEnter">Enter demo workspace</button><p id="demoLoginError" role="alert"></p>';
    mount.append(section);
    const identities=section.querySelector('select'),error=section.querySelector('[role="alert"]'),dialog=document.getElementById('firstLoginDialog');
    try{demo.read().users.filter(u=>u.status!=='deleted').forEach(u=>{const option=document.createElement('option');option.value=u.id;option.textContent=u.name+' — '+u.role+' ('+u.status+')';identities.append(option);});}catch(e){error.textContent=e.message;}
    function redirect(user){location.href='../'+demo.dashboards[user.role];}
    function enter(){try{const user=demo.login(identities.value);if(!user.mustChangePassword){redirect(user);return;}document.getElementById('changeDescription').textContent='DEMO ONLY: validate a new password to test the first-login workflow. No password is stored or changed in Firebase.';document.getElementById('firstLoginPreviewNotice').hidden=false;document.getElementById('firstLoginPreviewNotice').textContent='Local demonstration. This is not secure authentication.';if(!dialog.open)dialog.showModal();document.getElementById('NewPassword').focus();}catch(e){error.textContent=e.message;}}
    section.querySelector('#demoEnter').addEventListener('click',enter);
    document.getElementById('SaveChanges').addEventListener('click',e=>{if(!demo.enabled())return;e.preventDefault();e.stopImmediatePropagation();try{demo.transaction('changePassword',{password:document.getElementById('NewPassword').value,confirm:document.getElementById('ConfirmPassword').value});redirect(demo.current());}catch(ex){const status=document.getElementById('firstChangeStatus');status.hidden=false;status.textContent=ex.message;}},true);
    document.getElementById('SendResetLink').addEventListener('click',e=>{e.preventDefault();e.stopImmediatePropagation();const email=document.getElementById('ResetEmail');if(!email.checkValidity()||!email.value){email.reportValidity();return;}document.getElementById('resetFields').hidden=true;const confirmation=document.getElementById('resetConfirmation');confirmation.hidden=false;confirmation.textContent='DEMO ONLY: no email was sent. In the connected system, if an account exists, password reset instructions will be sent.';},true);

    const loginForm=document.querySelector('.auth-layout');
    document.getElementById('ResetEmail').required=true;
    const loginNote=document.createElement('p');loginNote.className='auth-preview-note';loginNote.textContent='Frontend DEMO: use a listed test email and any nonempty sample password. No password is authenticated or stored. Never enter real credentials.';section.prepend(loginNote);
    loginForm.addEventListener('submit',e=>{
        if(new URLSearchParams(location.search).get('preview')==='first-login'&&!demo.enabled())return;
        e.preventDefault();e.stopImmediatePropagation();
        if(dialog.open&&demo.current()?.mustChangePassword){try{demo.transaction('changePassword',{password:document.getElementById('NewPassword').value,confirm:document.getElementById('ConfirmPassword').value});redirect(demo.current());}catch(ex){const feedback=document.getElementById('firstChangeStatus');feedback.hidden=false;feedback.textContent=ex.message;}return;}
        const reset=document.getElementById('forgotPasswordDialog');if(reset.open){document.getElementById('SendResetLink').click();return;}
        const email=document.getElementById('Email'),password=document.getElementById('Password');
        document.getElementById('emailError').textContent=email.value&&email.checkValidity()?'':'Enter a valid demo email.';
        document.getElementById('passwordError').textContent=password.value?'':'Enter a sample password for UI testing.';
        if(!email.value||!email.checkValidity()||!password.value)return;
        const account=demo.read().users.find(user=>user.email.toLowerCase()===email.value.trim().toLowerCase());
        if(!account||account.status!=='active'){error.textContent='This demo account is unavailable.';return;}
        identities.value=account.id;enter();
    },true);
    const current=demo.current();if(demo.enabled()&&current?.mustChangePassword){identities.value=current.id;enter();}
})();

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
