import test from 'node:test';
import assert from 'node:assert/strict';
import {mkdtempSync,mkdirSync,writeFileSync,rmSync} from 'node:fs';
import {tmpdir} from 'node:os';
import {join} from 'node:path';
import {fileURLToPath} from 'node:url';
import {spawnSync} from 'node:child_process';
const script=fileURLToPath(new URL('./resolve-mac-signing.sh',import.meta.url));
const first='A'.repeat(40), second='B'.repeat(40);
function resolve({identities=`  1) ${first} "FileLauncher Dev"\n  2) ${second} "Apple Development: Test"`,pin,explicit}={}){
  const root=mkdtempSync(join(tmpdir(),'cpcb-sign-'));
  try{
    mkdirSync(join(root,'work'));mkdirSync(join(root,'tools'));
    writeFileSync(join(root,'identities'),identities);
    writeFileSync(join(root,'tools','security'),'#!/bin/sh\ncat "$ROOT/identities"\n',{mode:0o755});
    if(pin!==undefined)writeFileSync(join(root,'work','macos-signing.identity'),pin+'\n');
    const env={...process.env,ROOT:root,PATH:join(root,'tools')+':'+process.env.PATH};
    delete env.CODESIGN_IDENTITY;delete env.NOTARYTOOL_PROFILE;
    if(explicit!==undefined)env.CODESIGN_IDENTITY=explicit;
    return spawnSync('zsh',['-c','set -euo pipefail; source "$1"; print -r -- "$IDENTITY|$IDENTITY_NAME|${SIGN_OPTIONS[*]}"','test',script],{env,encoding:'utf8'});
  }finally{rmSync(root,{recursive:true,force:true});}
}
test('explicit identity takes priority over the local pin',()=>{
  const r=resolve({pin:first,explicit:second});assert.equal(r.status,0,r.stderr);assert.ok(r.stdout.startsWith(second+'|Apple Development: Test|'));
});
test('local fingerprint is used, and the fixed name is the last default',()=>{
  assert.ok(resolve({pin:second}).stdout.startsWith(second+'|'));
  assert.ok(resolve().stdout.startsWith(first+'|FileLauncher Dev|'));
});
test('missing or ambiguous identities fail without choosing another certificate',()=>{
  for(const args of [{pin:''},{pin:'C'.repeat(40)},{identities:`  1) ${second} "Apple Development: Test"`},{explicit:'FileLauncher Dev',identities:`1) ${first} "FileLauncher Dev"\n2) ${second} "FileLauncher Dev"`}]){
    const r=resolve(args);assert.notEqual(r.status,0);assert.equal(r.stdout,'');
  }
});
test('ad-hoc is available only through an explicit dash',()=>{
  const r=resolve({identities:'',explicit:'-'});assert.equal(r.status,0,r.stderr);assert.ok(r.stdout.startsWith('-|-|'));
});
test('Developer ID fingerprint keeps the hardened runtime signing options',()=>{
  const r=resolve({identities:`1) ${first} "Developer ID Application: Test (TEAM)"`,pin:first});
  assert.equal(r.status,0,r.stderr);assert.ok(r.stdout.includes('--timestamp --options runtime'));
});
