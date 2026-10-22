/* Contact Us: sends the message to the internship coordinator (saved in Firebase by Contact.aspx.cs). */
(function () {
    const button = document.querySelector('button[aria-describedby="contactStatus"]');
    const status = document.getElementById('contactStatus');
    if (!button || !status || !window.UimsDemo) return;

    button.disabled = false;
    button.innerHTML = 'Send Message <span aria-hidden="true">&#8599;</span>';
    status.innerHTML = '<strong>Your message goes to the internship coordinator.</strong><p>Fill in all fields, then press Send Message.</p>';

    button.addEventListener('click', () => {
        const fields = ['contactName', 'contactEmail', 'contactSubject', 'contactMessage'].map(id => document.getElementById(id));
        const invalid = fields.find(el => !el.checkValidity());
        if (invalid) { invalid.reportValidity(); return; }

        try {
            UimsDemo.transaction('contact', { name: fields[0].value, email: fields[1].value, subject: fields[2].value, message: fields[3].value });
            status.innerHTML = '<strong>Message sent.</strong><p>The internship coordinator will review it soon.</p>';
            fields.forEach(el => el.value = '');
        } catch (e) {
            status.textContent = e.message;
        }
    });
})();

(() => { const form = document.getElementById("contactForm"); const button = form?.querySelector("button[aria-describedby=contactStatus]"); form?.addEventListener("submit", event => { event.preventDefault(); button?.click(); }); })();