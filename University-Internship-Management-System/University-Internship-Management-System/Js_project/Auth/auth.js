/* UI behavior only. Firebase credentials and role resolution are handled by C#. */
(() => {
    'use strict';
    const by=id=>document.getElementById(id),form=document.querySelector('.auth-layout');
    if(!form)return;
    const result=JSON.parse(by('loginResult').textContent),key='uims.tab.ticket';
    let ticket=sessionStorage.getItem(key)||'';
    if(result.ticket){ticket=result.ticket;sessionStorage.setItem(key,ticket);}
    let field=document.createElement('input');field.type='hidden';field.name='UimsTabTicket';field.value=ticket;form.append(field);
    document.querySelectorAll('[data-toggle]').forEach(button=>button.addEventListener('click',()=>{
        const input=by(button.dataset.toggle),show=input.type==='password';input.type=show?'text':'password';
        button.setAttribute('aria-pressed',String(show));button.setAttribute('aria-label',show?'Hide password':'Show password');
    }));
    const dialog=by('firstLoginDialog');
    dialog.addEventListener('cancel',event=>event.preventDefault());
    let saving=false;
    function validateChange(){
        const password=by('NewPassword').value,confirmation=by('ConfirmPassword').value;
        const rules={length:password.length>=12&&password.length<=128,case:/[a-z]/.test(password)&&/[A-Z]/.test(password),number:/[0-9]/.test(password),symbol:/[^a-zA-Z0-9\s]/.test(password)};
        Object.entries(rules).forEach(([rule,valid])=>document.querySelector('[data-rule="'+rule+'"]').classList.toggle('is-valid',valid));
        const count=Object.values(rules).filter(Boolean).length,meter=by('strengthMeter');
        meter.setAttribute('aria-valuenow',String(count));meter.classList.toggle('is-strong',count===4);
        [...meter.children].forEach((bar,index)=>bar.classList.toggle('is-filled',index<count));
        by('strengthText').textContent=count===4?'Strong password':'Complete all password requirements';
        const match=password.length>0&&password===confirmation;
        by('matchText').textContent=confirmation?(match?'Passwords match':'Passwords do not match'):'';
        by('matchText').classList.toggle('is-valid',match);by('matchText').classList.toggle('is-invalid',!!confirmation&&!match);
        by('SaveChanges').disabled=saving||(!result.completionPending&&(!Object.values(rules).every(Boolean)||!match||!by('CurrentPassword').value||password===by('CurrentPassword').value));
    }
    if(result.restricted){
        by('firstChangeStatus').hidden=false;
        by('firstChangeStatus').textContent=result.completionPending?'Your password changed. Retry saving completion without entering another password.':'This updates your Firebase password. If completion storage is denied, keep this tab open and retry completion after its scoped rule is published.';
        if(result.completionPending){
            ['CurrentPassword','NewPassword','ConfirmPassword'].forEach(id=>by(id).disabled=true);
            by('SaveChanges').value='Retry completion';
        }
        ['CurrentPassword','NewPassword','ConfirmPassword'].forEach(id=>by(id).addEventListener('input',validateChange));
        validateChange();dialog.showModal();
        dialog.addEventListener('keydown',event=>{if(event.key==='Enter'&&event.target.tagName==='INPUT'){event.preventDefault();if(!by('SaveChanges').disabled)by('SaveChanges').click();}});
    }else if(result.ticket&&result.dashboard){
        const url=new URL(result.dashboard,location.href);if(url.origin!==location.origin)return;
        const nav=document.createElement('form');nav.method='POST';nav.action=url.href;
        const input=document.createElement('input');input.type='hidden';input.name='UimsTabTicket';input.value=ticket;nav.append(input);document.body.append(nav);nav.submit();
    }
    const reset=by('forgotPasswordDialog');
    by('ForgotPassword').addEventListener('click',()=>{by('resetFields').hidden=true;by('resetConfirmation').hidden=false;by('resetConfirmation').textContent='Password recovery is outside Phase 2. Contact your administrator.';reset.showModal();});
    document.querySelectorAll('[data-close-reset]').forEach(button=>button.addEventListener('click',()=>reset.close()));
    reset.addEventListener('close',()=>by('ForgotPassword').focus());
    form.addEventListener('submit',event=>{
        if(dialog.open){
            validateChange();
            if(saving||by('SaveChanges').disabled){event.preventDefault();return;}
            saving=true;by('SaveChanges').value=result.completionPending?'Saving completion...':'Changing password...';
            by('SaveChanges').setAttribute('aria-busy','true');return;
        }
        by('SignIn').value='Signing in...';
    });
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
