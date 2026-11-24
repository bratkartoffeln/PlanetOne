// Exercise the actual inline workflow code without publishing or contacting GitHub.
const fs = require('fs');
const path = require('path');
const crypto = require('crypto');
const assert = require('assert/strict');
const source = fs.readFileSync(path.join(__dirname, '../../.github/workflows/release.yml'), 'utf8');
const scripts = [...source.matchAll(/          script: \|\r?\n((?:            .*\r?\n?)*)/g)]
  .map(match => match[1].split(/\r?\n/).map(line => line.slice(12)).join('\n'));
assert.equal(scripts.length, 2);
const AsyncFunction = Object.getPrototypeOf(async function () {}).constructor;
const sha = 'a'.repeat(40);
function fixture(mode) {
  const info = {version: '1.0.4', commit: mode === 'wrong-commit' ? 'b'.repeat(40) : sha};
  const files = {'PlanetOne-1.0.4-Windows-x64.zip': Buffer.from('zip'), 'PlanetOne-1.0.4-Windows-x64-Setup.exe': Buffer.from('exe'), 'build-info.json': Buffer.from(JSON.stringify(info))};
  files['SHA256SUMS.txt'] = Buffer.from(Object.entries(files).map(([name, data]) => crypto.createHash('sha256').update(data).digest('hex') + '  ' + name).join('\n'));
  if (mode === 'corrupt') files['PlanetOne-1.0.4-Windows-x64.zip'] = Buffer.from('modified');
  const calls = [];
  const github = {rest: {
    repos: {
      getBranch: async () => ({data: {commit: {sha: mode === 'main-moved' ? 'c'.repeat(40) : sha}}}),
      createRelease: async data => { calls.push(['create', data]); return {data: {id: 1, html_url: 'test'}}; },
      uploadReleaseAsset: async data => { if (mode === 'upload-failed') throw new Error('Upload failed'); calls.push(['upload', data]); },
      updateRelease: async data => calls.push(['publish', data])
    },
    git: {getRef: async () => { if (mode !== 'existing') throw Object.assign(new Error('Not found'), {status: 404}); }},
    actions: {listWorkflowRuns: async () => ({data: {workflow_runs: mode === 'no-build' ? [] : [{id: 12, head_sha: sha, conclusion: 'success'}]}})}
  }};
  const requireMock = name => name === 'fs' ? {readFileSync: (file, encoding) => {
    const data = files[file.replace('distribution/', '')];
    if (!data) throw new Error('Missing file');
    return encoding ? data.toString(encoding) : data;
  }} : require(name);
  return {calls, invoke: index => new AsyncFunction('github', 'context', 'core', 'require', 'process', scripts[index])(
    github, {repo: {owner: 'test', repo: 'test'}, sha}, {setOutput() {}, notice() {}}, requireMock,
    {env: {BUILD_SHA: sha, PRERELEASE: 'true'}})};
}
(async () => {
  for (const mode of ['wrong-commit', 'corrupt', 'main-moved', 'existing', 'upload-failed']) {
    const test = fixture(mode);
    await assert.rejects(test.invoke(1));
    assert.equal(test.calls.filter(([name]) => name === 'publish').length, 0);
  }
  await assert.rejects(fixture('no-build').invoke(0));
  await assert.rejects(fixture('main-moved').invoke(0));
  const success = fixture('success');
  await success.invoke(0);
  await success.invoke(1);
  assert.deepEqual(success.calls.map(([name]) => name), ['create', 'upload', 'upload', 'upload', 'upload', 'publish']);
  assert.equal(success.calls[0][1].draft, true);
  assert.equal(success.calls[0][1].target_commitish, sha);
  assert.equal(success.calls[0][1].prerelease, true);
  assert.equal(success.calls[5][1].draft, false);
  console.log('PASS Release: success and seven failure scenarios; no external publication.');
})().catch(error => { console.error(error); process.exitCode = 1; });
