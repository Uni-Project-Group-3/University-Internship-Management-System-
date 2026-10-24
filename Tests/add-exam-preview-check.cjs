const fs=require('fs'),p='Tests/FinalEvaluation/TaskWorkspace.browser.cjs';let s=fs.readFileSync(p,'utf8');s=s.replace("        await page.screenshot({ path: path.join(__dirname, 'task-exam-builder-mobile.png'), animations: 'disabled' });",`        await page.getByRole('button',{name:'Preview examination',exact:true}).click();
        await page.locator('dialog[open]').getByRole('heading',{name:'Examination preview',exact:true}).waitFor();
        assert((await page.locator('dialog[open]').textContent()).includes('The statement is true.'));
        await page.locator('dialog[open]').getByRole('button',{name:'Done',exact:true}).click();
        console.log('PASS company examination preview renders configured questions without publishing');
        if(process.argv.includes('--builder-only'))return;
        await page.screenshot({ path: path.join(__dirname, 'task-exam-builder-mobile.png'), animations: 'disabled' });`);fs.writeFileSync(p,s);
