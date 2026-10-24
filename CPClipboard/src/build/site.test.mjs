import {readFileSync} from 'node:fs';
import vm from 'node:vm';
import test from 'node:test';
import assert from 'node:assert/strict';
import {selectRelease} from '../../../Common/Web/release-selection.js';
const code=readFileSync(new URL('../../../docs/CPClipboard/assets/site.js',import.meta.url),'utf8').replace(/^import .*;\n/, '');
async function run(response,lang='en',navigator={}){
  const status={textContent:'unreleased'},downloads={children:[],appendChild(a){this.children.push(a);}};
  const hero={href:'#download'},heroLabel={textContent:'Download'},other={hidden:true};
  const elements={'[data-release-status]':status,'[data-downloads]':downloads,'[data-hero-download]':hero,'[data-hero-label]':heroLabel,'[data-hero-other]':other};
  function node(tag) { return {tag,children:[],setAttribute(){},appendChild(child){this.children.push(child);}}; }
  const document={documentElement:{lang},querySelector:s=>elements[s]??null,querySelectorAll:()=>[],getElementById:()=>null,createElement:node};
  await vm.runInNewContext(code,{document,navigator,URL,selectRelease,fetch:async()=>response});
  const links=[];
  function visit(node){if(node.tag==='a')links.push(node);for(const child of node.children??[])visit(child);}
  visit(downloads);
  return {status:status.textContent,links,cards:downloads.children,hero,heroLabel,other};
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
  assert.equal(result.links.length,1);assert.equal(result.links[0].children[0].textContent,'↓ Download');
  assert.equal(result.cards[0].className,'dl-item');
  assert.equal(result.status,'Latest release: cpclipboard-v0.1.0');
});
test('network failure gives a localized fallback without download links',async()=>{
  const result=await run({ok:false,status:500},'ja');
  assert.equal(result.links.length,0);assert.match(result.status,/GitHub/);
});

const responseFor=(names)=>({ok:true,json:async()=>[
  {tag_name:'v9.0.0',draft:false,prerelease:false,assets:[]},
  {tag_name:'cpclipboard-v1.0.0',draft:false,prerelease:false,assets:names.map(name=>({name,browser_download_url:`https://github.com/PalmJoy/CPFileLauncher/releases/download/cpclipboard-v1.0.0/${name}`}))}
]});
test('hero downloads the matching published Clipboard asset on Windows and Mac',async()=>{
  for(const [navigator,rid,label] of [
    [{platform:'Win32'},'win-x64',/Windows/],
    [{userAgentData:{platform:'Windows',getHighEntropyValues:async()=>({architecture:'arm'})}},'win-arm64',/ARM/],
    [{platform:'Win32',userAgentData:{getHighEntropyValues:async()=>{throw Error('denied');}}},'win-x64',/Windows/],
    [{platform:'MacIntel'},'macos-arm64',/Apple silicon/]
  ]){
    const name=`CPClipboard-${rid}.zip`;
    const result=await run(responseFor([name]),'en',navigator);
    assert.ok(result.hero.href.endsWith('/'+name));assert.match(result.heroLabel.textContent,label);assert.equal(result.other.hidden,false);
  }
});
test('unknown platforms, missing assets and network failure retain the platform chooser',async()=>{
  for(const [response,navigator] of [
    [responseFor(['CPClipboard-win-x64.zip']),{platform:'Linux'}],
    [responseFor(['CPClipboard-macos-arm64.zip']),{platform:'Win32'}],
    [responseFor(['CPClipboard-macos-arm64.zip']),{platform:'MacIntel',userAgent:'iPad'}],
    [{ok:false,status:500},{platform:'Win32'}]
  ]){
    const result=await run(response,'ja',navigator);assert.equal(result.hero.href,'#download');assert.equal(result.other.hidden,true);
  }
});
