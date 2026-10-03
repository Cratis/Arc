// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Use an existing Playwright installation; this optional production check installs nothing.
const { chromium } = await import(process.env.PLAYWRIGHT_MODULE || 'playwright');
import { createServer } from 'node:http';
import { readFile } from 'node:fs/promises';
import { resolve, extname } from 'node:path';
import assert from 'node:assert/strict';
const prefix = '/nested/.cratis/event-model/';
const root = resolve('Source/DotNET/Screenplay.Embedded/wwwroot');
const hierarchy = [{id:'Acme.Orders',name:'Acme.Orders',documents:[
 {id:'Acme.Orders',title:'Acme.Orders',namespace:'Acme.Orders',kind:'assembly',parentId:null,resourceName:'Acme.Orders.play'},
 {id:'Acme.Orders.Ordering',title:'Ordering',namespace:'Acme.Orders.Ordering',kind:'module',parentId:'Acme.Orders',resourceName:'Ordering.play'},
 {id:'Acme.Orders.Ordering.Checkout',title:'Checkout',namespace:'Acme.Orders.Ordering.Checkout',kind:'feature',parentId:'Acme.Orders.Ordering',resourceName:'Checkout.play'}]}];
let releaseAssembly;
const deferred = new Promise(resolve => releaseAssembly = resolve);
const requests = [];
const server = createServer(async (request,response) => {
 try {
  const path = new URL(request.url,'http://localhost').pathname;
  requests.push(path);
  if (path.endsWith('/hierarchy')) {
   response.writeHead(200,{'Content-Type':'application/json'}); response.end(JSON.stringify(hierarchy)); return;
  }
  if (path.includes('/documents/')) {
   const checkout = path.includes('Checkout');
   if (!checkout) await deferred;
   if (path.endsWith('/source')) {
    response.writeHead(200,{'Content-Type':'text/plain'}); response.end(checkout ? 'feature Checkout {}' : 'assembly Acme.Orders {}');
   } else {
    response.writeHead(200,{'Content-Type':'application/json'}); response.end(JSON.stringify({id:'00000000-0000-4000-8000-000000000001',name:'Acme.Orders',collections:[],stickyNotes:[],links:[]}));
   }
   return;
  }
  assert.ok(path.startsWith(prefix));
  const relative = path.slice(prefix.length) || 'index.html';
  const content = await readFile(resolve(root,relative));
  response.writeHead(200,{'Content-Type':({'.js':'text/javascript','.css':'text/css','.html':'text/html','.ttf':'font/ttf','.svg':'image/svg+xml'})[extname(relative)] || 'application/octet-stream'}); response.end(content);
 } catch {response.writeHead(404);response.end();}
});
await new Promise(resolve => server.listen(19101,'127.0.0.1',resolve));
const browser = await chromium.launch({headless:true,executablePath:process.env.PLAYWRIGHT_CHROMIUM_EXECUTABLE});
const errors=[];
try {
 const page=await browser.newPage();
 page.on('pageerror',error=>errors.push(error.stack));
 page.on('console',message=>{if(message.type()==='error'||message.type()==='warning') errors.push(message.text());});
 await page.goto('http://127.0.0.1:19101'+prefix);
 await page.waitForTimeout(500);
 assert.deepEqual(errors,[]);
 await page.getByRole('treeitem',{name:/Checkout Feature/}).click();
 await page.locator('canvas').first().waitFor();
 await page.getByRole('tab',{name:'Source',exact:true}).click();
 await page.locator('.monaco-editor').waitFor();
 await page.waitForFunction(()=>document.querySelector('.monaco-editor')?.textContent.includes('Checkout'));
 releaseAssembly();
 await page.waitForTimeout(500);
 assert.equal(await page.getByRole('treeitem',{name:/Checkout Feature/}).getAttribute('aria-selected'),'true');
 assert.equal(await page.getByRole('treeitem',{name:/Acme\.Orders Assembly/}).getAttribute('aria-selected'),'false');
 assert.ok(await page.locator('.monaco-editor').innerText().then(text=>text.includes('Checkout')));
 // Exercise Monaco's real worker proxy, not merely the emitted worker filename.
 const workerResult=await page.evaluate(async()=>{
  const worker=self.MonacoEnvironment.getWorker();
  return await new Promise((resolve,reject)=>{
   const timeout=setTimeout(()=>{worker.terminate();reject(new Error('Monaco worker did not initialize'));},10000);
   worker.onerror=event=>{clearTimeout(timeout);reject(new Error(event.message));};
   worker.onmessage=event=>{clearTimeout(timeout);worker.terminate();resolve(event.data);};
   worker.postMessage('vs/editor/common/services/editorSimpleWorker');
   worker.postMessage({vsWorker:1,type:0,channel:'default',req:'1',method:'$initialize',args:[1]});
  });
 });
 assert.equal(workerResult.type,1);
 assert.equal(workerResult.seq,'1');
 assert.equal(workerResult.err,undefined);
 console.log('Worker response',JSON.stringify(workerResult));
 assert.ok(requests.some(path=>/editor\.worker-.*\.js$/.test(path)));
 assert.deepEqual(errors,[]);
 console.log('PASS production viewer: nested PathBase, real Monaco editor, selected feature survives late assembly response, bundled worker loads and responds, no browser diagnostics');
} catch (error) { console.log('Browser errors', errors); console.log('Requests', requests); throw error; } finally {releaseAssembly();await browser.close();await new Promise(resolve=>server.close(resolve));}
