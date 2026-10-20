(() => {
    "use strict";

    const form = document.querySelector(".auth-layout");
    if (!form) return;
    const dialog = document.getElementById("firstLoginDialog");
    const newPassword = document.getElementById("NewPassword");
    const confirmation = document.getElementById("ConfirmPassword");
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
    }

    newPassword.addEventListener("input", updatePasswordFeedback);
    confirmation.addEventListener("input", updatePasswordFeedback);
    dialog.addEventListener("cancel", event => event.preventDefault());

    if (form.dataset.passwordRequired === "true") {
        if (typeof dialog.showModal === "function") {
            dialog.showModal();
        } else {
            dialog.setAttribute("open", "");
            document.getElementById("loginContent").setAttribute("inert", "");
        }
        newPassword.focus();
    }

    form.addEventListener("submit", event => {
        if (submitting) {
            event.preventDefault();
            return;
        }
        const button = event.submitter;
        const action = button ? button.dataset.action : "login";
        if (action === "login" || action === "reset") {
            const email = document.getElementById("Email");
            const password = document.getElementById("Password");
            const emailValid = Boolean(email.value.trim()) && email.validity.valid;
            const passwordValid = action === "reset" || Boolean(password.value);
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
    });
})();
