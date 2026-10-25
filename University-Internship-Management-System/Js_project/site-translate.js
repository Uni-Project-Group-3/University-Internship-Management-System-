/* English / Arabic switch on every page, using Google's website translator.
   The choice is kept in Google's googtrans cookie, so later pages open already translated. */
(function () {
    'use strict';
    if (window.uimsSiteTranslate) return;
    window.uimsSiteTranslate = true;

    const cookieName = 'googtrans';
    const current = () => (document.cookie.match(/(?:^|;\s*)googtrans=\/[a-z-]+\/([a-z-]+)/i) || [])[1] || 'en';
    function setCookie(lang) {
        const host = location.hostname, value = lang === 'en' ? '' : '/en/' + lang;
        const expires = lang === 'en' ? '; expires=Thu, 01 Jan 1970 00:00:00 GMT' : '; max-age=31536000';
        // Google reads the cookie for the exact host and for the parent domain; write and clear both.
        document.cookie = cookieName + '=' + value + '; path=/' + expires;
        if (host.includes('.') && !/^\d+(\.\d+){3}$/.test(host)) document.cookie = cookieName + '=' + value + '; path=/; domain=.' + host.split('.').slice(-2).join('.') + expires;
    }

    const style = document.createElement('style');
    style.textContent = `
.goog-te-banner-frame,iframe.skiptranslate,#goog-gt-tt,.goog-te-balloon-frame,.goog-tooltip,.goog-te-spinner-pos{display:none!important}
body{top:0!important;position:static}
.goog-text-highlight{background:none!important;box-shadow:none!important}
#uimsGoogleTranslate{position:absolute;width:1px;height:1px;overflow:hidden;clip:rect(0 0 0 0)}
.uims-lang-switch{position:fixed;right:18px;bottom:18px;z-index:2500;display:inline-flex;padding:4px;border:1px solid #d3dae6;border-radius:999px;background:#fff;box-shadow:0 8px 24px rgba(20,32,60,.16);font:700 13px/1 Inter,Arial,sans-serif}
.uims-lang-switch button{border:0;background:none;color:#354c7a;padding:9px 14px;border-radius:999px;cursor:pointer;font:inherit}
.uims-lang-switch button[aria-pressed="true"]{background:#1f294d;color:#fff}
.uims-lang-switch button:focus-visible{outline:2px solid #6689bb;outline-offset:2px}
html.uims-lang-ar body :is(p,li,td,th,dd,dt,label,h1,h2,h3,h4,h5,h6,span,strong,small,a,button,option){unicode-bidi:plaintext}
@media print{.uims-lang-switch{display:none}}
`;
    document.head.appendChild(style);

    function apply(lang) {
        document.documentElement.classList.toggle('uims-lang-ar', lang === 'ar');
        document.querySelectorAll('.uims-lang-switch button').forEach(b => b.setAttribute('aria-pressed', String(b.dataset.lang === lang)));
    }
    // Switching always reloads: English pages never load Google's script, so the system's own pages and logic run unchanged.
    function choose(lang) {
        if (lang === current()) return;
        setCookie(lang);
        location.reload();
    }

    function build() {
        const host = document.createElement('div'); host.id = 'uimsGoogleTranslate'; host.className = 'notranslate'; host.setAttribute('aria-hidden', 'true');
        const bar = document.createElement('div'); bar.className = 'uims-lang-switch notranslate'; bar.setAttribute('translate', 'no'); bar.setAttribute('role', 'group'); bar.setAttribute('aria-label', 'Language');
        for (const [lang, label, title] of [['en', 'English', 'Show the page in English'], ['ar', 'العربية', 'عرض الصفحة بالعربية']]) {
            const button = document.createElement('button'); button.type = 'button'; button.dataset.lang = lang; button.textContent = label; button.title = title; button.lang = lang;
            button.addEventListener('click', () => choose(lang)); bar.append(button);
        }
        document.body.append(host, bar); apply(current());
        if (current() !== 'ar') return;
        window.uimsGoogleTranslateInit = function () {
            try { new window.google.translate.TranslateElement({ pageLanguage: 'en', includedLanguages: 'en,ar', autoDisplay: false }, 'uimsGoogleTranslate'); } catch (_) { /* The page stays in English if Google is unreachable. */ }
        };
        const script = document.createElement('script');
        script.src = 'https://translate.google.com/translate_a/element.js?cb=uimsGoogleTranslateInit'; script.async = true;
        document.head.appendChild(script);
    }
    if (document.readyState === 'loading') document.addEventListener('DOMContentLoaded', build, { once: true }); else build();
}());
