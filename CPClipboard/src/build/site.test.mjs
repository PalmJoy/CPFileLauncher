import {readFileSync} from 'node:fs';
import vm from 'node:vm';
import test from 'node:test';
import assert from 'node:assert/strict';
import {selectRelease} from '../../../Common/Web/release-selection.js';
const code=readFileSync(new URL('../../../docs/CPClipboard/assets/site.js',import.meta.url),'utf8').replace(/^import .*;\n/, '');
async function run(response,lang='en'){
  const status={textContent:'unreleased'},downloads={children:[],appendChild(a){this.children.push(a);}};
  const document={documentElement:{lang},querySelector:s=>s==='[data-release-status]'?status:downloads,createElement:()=>({})};
  await vm.runInNewContext(code,{document,URL,selectRelease,fetch:async()=>response});
  return {status:status.textContent,links:downloads.children};
}
test('unpublished, draft and prerelease never expose downloads',async()=>{
  for(const response of [{status:404},{ok:true,json:async()=>([{draft:true}])},{ok:true,json:async()=>([{prerelease:true}])}]){
    assert.deepEqual((await run(response)).links,[]);
  }
});
test('only known archives from the expected GitHub release path are linked',async()=>{
  const result=await run({ok:true,json:async()=>([{draft:false,prerelease:false,tag_name:'cpclipboard-v0.1.0',assets:[
    {name:'CPClipboard-win-x64.zip',browser_download_url:'https://github.com/PalmJoy/CPFileLauncher/releases/download/cpclipboard-v0.1.0/CPClipboard-win-x64.zip'},
    {name:'CPClipboard-win-arm64.zip',browser_download_url:'https://example.com/untrusted.zip'},
    {name:'unrelated.zip',browser_download_url:'https://github.com/PalmJoy/CPFileLauncher/releases/download/cpclipboard-v0.1.0/unrelated.zip'}]}])});
  assert.equal(result.links.length,1);assert.equal(result.links[0].textContent,'Windows · x64 ↓ ZIP');
  assert.equal(result.status,'Latest release: cpclipboard-v0.1.0');
});
test('network failure gives a localized fallback without download links',async()=>{
  const result=await run({ok:false,status:500},'ja');
  assert.equal(result.links.length,0);assert.match(result.status,/GitHub/);
});
