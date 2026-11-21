// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { createHash } from 'node:crypto';
import fs from 'node:fs';
import os from 'node:os';
import path from 'node:path';
import { test } from 'node:test';
import { fileURLToPath } from 'node:url';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const fixtures = path.join(root, 'scripts/fixtures/guard-tooling');
const workflow = fs.readFileSync(path.join(root, '.github/workflows/javascript-build.yml'), 'utf8');
function workflowStep(name) {
    const step = workflow.split(/^      - name: /m).find(value => value.startsWith(`${name}\n`));
    assert.ok(step, `Missing workflow step: ${name}`);
    return step;
}
function stepScript(step) {
    return step.split('        run: |\n')[1].split('\n').map(line => line.replace(/^          /, '')).join('\n');
}
function command(executable, args, cwd, options = {}) {
    const result = spawnSync(executable, args, { cwd, timeout: 30000, encoding: 'utf8', maxBuffer: 8 * 1024 * 1024, ...options });
    assert.ifError(result.error);
    return result;
}
function succeeds(result) {
    assert.equal(result.status, 0, result.stdout + result.stderr);
    return result.stdout.trim();
}
function extractTools(temp) {
    const revision = succeeds(command('git', ['rev-parse', 'HEAD'], root));
    const step = workflowStep('Extract base tools outside the head tree');
    const env = { ...process.env, BASE_SHA: revision, GITHUB_WORKSPACE: root,
        RUNNER_TEMP: temp, GITHUB_ENV: path.join(temp, 'github-env'), DEFAULT_BRANCH: 'main' };
    for (const name of ['GUARD_SCRIPTS', 'GUARD_FILES']) {
        env[name] = step.match(new RegExp(`^          ${name}: (.+)$`, 'm'))[1];
    }
    const output = succeeds(command('bash', ['-c', stepScript(step)], temp, { env }));
    assert.ok(output.includes(`Selected base revision ${revision}.`), output);
    const tools = fs.readFileSync(env.GITHUB_ENV, 'utf8').trim().replace(/^GATE_TOOLS=/, '');
    assert.ok(tools.startsWith(path.join(temp, 'gate-tools.')));
    assert.equal(fs.lstatSync(tools).isSymbolicLink(), false);
    checkYarnFiles(tools, revision);
    return tools;
}
function checkYarnFiles(tools, revision) {
    const config = fs.readFileSync(path.join(tools, '.yarnrc.yml'), 'utf8');
    const files = [...config.matchAll(/^\s*(?:- path:|yarnPath:)\s+(\.yarn\/\S+)/gm)].map(match => match[1]);
    assert.ok(files.some(file => file.startsWith('.yarn/plugins/')));
    assert.ok(files.some(file => file.startsWith('.yarn/releases/')));
    for (const file of files) {
        assert.equal(fs.lstatSync(path.join(tools, file)).isFile(), true);
        assert.equal(fs.readFileSync(path.join(tools, file), 'utf8').trim(),
            succeeds(command('git', ['show', `${revision}:${file}`], root)));
    }
}
function writeFile(directory, relative, content) {
    const file = path.join(directory, relative);
    fs.mkdirSync(path.dirname(file), { recursive: true });
    fs.writeFileSync(file, content);
}
function buildSample(directory) {
    const packages = JSON.parse(fs.readFileSync(path.join(fixtures, 'packages.json'), 'utf8'));
    for (const [name, sample] of Object.entries(packages)) {
        const folder = path.join(directory, 'Source/JavaScript', name);
        const conditions = {};
        for (const [branch, format] of [['import', 'esm'], ['require', 'cjs']]) {
            writeFile(folder, `dist/${format}/index.d.ts`, sample.declaration);
            writeFile(folder, `dist/${format}/index.js`, '');
            conditions[branch] = { types: `./dist/${format}/index.d.ts`, default: `./dist/${format}/index.js` };
        }
        const exports = Object.fromEntries(['.', ...sample.subpaths.map(subpath => `./${subpath}`)]
            .map(subpath => [subpath, conditions]));
        writeFile(folder, 'package.json', JSON.stringify({ name: `@cratis/${name.toLowerCase()}`, exports }));
    }
}
function snapshot(directory) {
    return ['', ...fs.readdirSync(directory, { recursive: true }).sort()].map(relative => {
        const file = path.join(directory, relative);
        const stat = fs.lstatSync(file);
        let content = 'directory';
        if (stat.isFile()) content = createHash('sha256').update(fs.readFileSync(file)).digest('hex');
        if (stat.isSymbolicLink()) content = fs.readlinkSync(file);
        return [relative, stat.mode, stat.mtimeMs, content];
    });
}
function checkSample(tools, directory, expectedStatus) {
    const before = snapshot(directory);
    const results = {};
    try {
        for (const name of ['imports', 'consumers']) {
            results[name] = command(process.execPath, [`scripts/check-declaration-${name}.cjs`, '--root', directory], tools);
            assert.equal(results[name].status, expectedStatus, results[name].stdout + results[name].stderr);
        }
    } finally {
        assert.deepEqual(snapshot(directory), before, 'Guards must not modify the checked tree');
    }
    return results;
}

test('archived base tooling installs once and evaluates checked declarations without modifying them', async context => {
    const temp = fs.realpathSync(fs.mkdtempSync(path.join(process.env.RUNNER_TEMP || os.tmpdir(), 'guard-tooling-')));
    context.after(() => fs.rmSync(temp, { recursive: true, force: true }));
    const tools = extractTools(temp);
    const install = workflowStep('Install base tooling').match(/^        run: (.+)$/m)[1];
    succeeds(command('bash', ['-c', install], tools, { timeout: 180000 }));
    const valid = path.join(temp, 'valid checked tree');
    buildSample(valid);
    await context.test('valid declarations pass both real guards in every consumer resolution mode', () => {
        const results = checkSample(tools, valid, 0);
        assert.match(results.imports.stdout, /relative imports resolve/);
        assert.equal((results.consumers.stdout.match(/consumer type-check passed/g) || []).length, 5);
    });
    await context.test('invalid declarations are rejected by both real guards', () => {
        const invalid = path.join(temp, 'invalid checked tree');
        fs.cpSync(valid, invalid, { recursive: true });
        for (const format of ['esm', 'cjs']) {
            fs.copyFileSync(path.join(fixtures, 'invalid.txt'), path.join(invalid, 'Source/JavaScript/Arc/dist', format, 'index.d.ts'));
        }
        const results = checkSample(tools, invalid, 1);
        assert.match(results.imports.stderr, /extensionless relative import \.\/missing/);
        assert.match(results.consumers.stdout, /has no exported member 'joinPaths'/);
        assert.match(results.consumers.stderr, /consumer type-check failed/);
    });
});
