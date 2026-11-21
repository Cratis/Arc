// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { test } from 'node:test';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const workflow = name => fs.readFileSync(path.join(root, '.github/workflows', name), 'utf8');
function job(file, name) {
    return workflow(file).split(`\n  ${name}:\n`)[1].split(/\n  [\w-]+:\n/)[0];
}
function stepScript(text, name) {
    const step = text.split(/^      - name: /m).find(value => value.startsWith(`${name}\n`));
    return step.split('        run: |\n')[1].split('\n').map(line => line.replace(/^          /, '')).join('\n');
}
function command(executable, args, cwd, env = process.env) {
    const result = spawnSync(executable, args, { cwd, env, encoding: 'utf8' });
    assert.equal(result.status, 0, result.stdout + result.stderr);
    return result.stdout.trim();
}

test('shard selection is separate from head script tests and supplies both matrices', () => {
    const policy = job('dotnet-build.yml', 'test-shard-policy');
    assert.doesNotMatch(policy, /--self-test|yarn|npm|dotnet /);
    assert.match(policy, /python3 -I "\$script" check/);
    assert.match(policy, /python3 -I "\$script" matrix/);
    assert.match(job('dotnet-build.yml', 'dotnet-build'), /scripts\/ci-test-shards.py --self-test/);
    for (const [name, output] of [['specs', 'spec-shards'], ['proxy-specs', 'proxy-shards']]) {
        const consumer = job('dotnet-build.yml', name);
        assert.match(consumer, /needs: \[dotnet-build, test-shard-policy\]/);
        assert.ok(consumer.includes(`needs.test-shard-policy.outputs.${output}`));
        assert.match(consumer, /This checks completeness of the run, not trust\./);
    }
});

test('declaration evaluation uses an artifact and base-installed tooling in a separate job', () => {
    const build = job('javascript-build.yml', 'build');
    const guards = job('javascript-build.yml', 'declaration-guards');
    assert.match(build, /name: Upload declaration inputs/);
    assert.doesNotMatch(build, /name: Verify declarations with base guards/);
    assert.match(guards, /needs: build/);
    assert.match(guards, /working-directory: \$\{\{ runner.temp \}\}\/gate-tools\n        run: yarn install --immutable --mode=skip-build/);
    assert.match(guards, /path: \$\{\{ runner.temp \}\}\/declaration-data\/Source\/JavaScript/);
    const decision = stepScript(guards, 'Verify declarations with base guards');
    assert.doesNotMatch(decision, /yarn|npm/);
    assert.match(decision, /node "\$tools\/check-declaration-consumers.cjs" --root/);
    assert.doesNotMatch(decision, /GITHUB_WORKSPACE|bootstrap|prefix/);
});

test('no checkout places gate tools inside the workspace', () => {
    for (const file of fs.readdirSync(path.join(root, '.github/workflows')).filter(name => name.endsWith('.yml'))) {
        for (const step of workflow(file).split(/(?=^\s*- (?:name:|uses:))/m)) {
            if (step.includes('uses: actions/checkout@')) assert.doesNotMatch(step, /path:.*gate-tools/, file);
        }
    }
});

test('the pull request gate includes both reusable workflow callers', () => {
    const gate = job('pull-requests.yml', 'gate');
    assert.match(gate, /needs: \[[^\n]*dotnet-build, javascript-build,/);
    for (const [file, name] of [['dotnet-build.yml', 'test-shard-policy'], ['javascript-build.yml', 'declaration-guards']]) {
        assert.doesNotMatch(job(file, name), /continue-on-error/);
        assert.match(job(file, name), /BASE_SHA: \$\{\{ github.event_name == 'pull_request' && github.event.pull_request.base.sha \|\| github.sha \}\}/);
        assert.doesNotMatch(workflow(file), /path: gate-tools/);
    }
});

const guardScripts = ['scripts/ci-test-shards.py', 'scripts/check-declaration-imports.cjs', 'scripts/check-declaration-consumers.cjs'];
const guardFiles = ['.github/test-shards.json', 'scripts/fixtures/declaration-consumer/consumer.mts',
    'scripts/fixtures/declaration-consumer/consumer.cts', 'package.json', 'yarn.lock', '.yarnrc.yml'];

function copyFixtureFile(head, file) {
    fs.mkdirSync(path.dirname(path.join(head, file)), { recursive: true });
    fs.copyFileSync(path.join(root, file), path.join(head, file));
}
function commitFixture(head, message) {
    command('git', ['add', '.'], head);
    command('git', ['-c', 'user.name=Guard specs', '-c', 'user.email=guard-specs@example.invalid', 'commit', '-qm', message], head);
    return command('git', ['rev-parse', 'HEAD'], head);
}
function archiveFixture(baseHasTools = true, defaultHasTools = true) {
    fs.mkdirSync(path.join(root, '.ai-work'), { recursive: true });
    const fixture = fs.mkdtempSync(path.join(root, '.ai-work/guard-archive-'));
    const head = path.join(fixture, 'head with spaces');
    fs.mkdirSync(head);
    command('git', ['init', '-qb', 'main', head], root);
    for (const file of guardFiles) copyFixtureFile(head, file);
    if (baseHasTools) for (const file of guardScripts) copyFixtureFile(head, file);
    const base = commitFixture(head, 'Base fixture');
    let trusted = base;
    if (!baseHasTools && defaultHasTools) {
        for (const file of guardScripts) copyFixtureFile(head, file);
        trusted = commitFixture(head, 'Default branch fixture');
    }
    command('git', ['checkout', '-qb', 'pr-data', base], head);
    for (const file of guardScripts) {
        fs.mkdirSync(path.dirname(path.join(head, file)), { recursive: true });
        fs.writeFileSync(path.join(head, file), 'head fixture is not a trusted guard\n');
    }
    fs.writeFileSync(path.join(head, '.github/test-shards.json'), 'head policy is not selected\n');
    fs.symlinkSync('.', path.join(head, 'gate-tools'));
    commitFixture(head, 'Head fixture');
    command('git', ['remote', 'add', 'origin', head], head);
    fs.symlinkSync(path.join(root, 'Source'), path.join(head, 'Source'));
    fs.symlinkSync(path.join(root, 'Samples'), path.join(head, 'Samples'));
    return { fixture, head, base, trusted };
}
function selectionEnvironment(fixture, name, temp) {
    return { ...process.env, GITHUB_WORKSPACE: fixture.head, RUNNER_TEMP: temp, BASE_SHA: fixture.base,
        DEFAULT_BRANCH: 'main', GITHUB_OUTPUT: path.join(temp, 'output.txt'),
        GUARD_SCRIPTS: name === 'test-shard-policy' ? guardScripts[0] : guardScripts.slice(1).join(' '),
        GUARD_FILES: name === 'test-shard-policy' ? guardFiles[0] : guardFiles.slice(1).join(' ') };
}
function checkTrustedSelection(fixture, expected) {
    for (const [file, name] of [['dotnet-build.yml', 'test-shard-policy'], ['javascript-build.yml', 'declaration-guards']]) {
        const temp = path.join(fixture.fixture, name);
        fs.mkdirSync(temp);
        const env = selectionEnvironment(fixture, name, temp);
        const policy = job(file, name);
        const output = command('bash', ['-c', stepScript(policy, 'Extract base tools outside the head tree')], temp, env);
        assert.ok(output.includes(`Selected ${expected}`));
        assert.ok(output.includes(fixture.trusted));
        assert.equal(fs.readlinkSync(path.join(fixture.head, 'gate-tools')), '.');
        const selected = name === 'test-shard-policy' ? guardScripts[0] : guardScripts[1];
        assert.equal(fs.readFileSync(path.join(temp, 'gate-tools', selected), 'utf8'), fs.readFileSync(path.join(root, selected), 'utf8'));
        if (name === 'test-shard-policy') {
            command('bash', ['-c', stepScript(policy, 'Verify the test shard assignment')], temp, env);
            assert.match(fs.readFileSync(env.GITHUB_OUTPUT, 'utf8'), /proxy-shards=/);
        }
    }
}

test('a committed head gate-tools symlink cannot redirect either base extraction', () => {
    checkTrustedSelection(archiveFixture(), 'base revision');
});

test('a base without tools uses default-branch tools, never head tools or head policy', () => {
    checkTrustedSelection(archiveFixture(false, true), 'default branch main revision');
});

test('missing tools on both trusted revisions fail closed despite head-provided scripts', () => {
    const fixture = archiveFixture(false, false);
    for (const [file, name] of [['dotnet-build.yml', 'test-shard-policy'], ['javascript-build.yml', 'declaration-guards']]) {
        const temp = path.join(fixture.fixture, name);
        fs.mkdirSync(temp);
        const result = spawnSync('bash', ['-c', stepScript(job(file, name), 'Extract base tools outside the head tree')], {
            cwd: temp, env: selectionEnvironment(fixture, name, temp), encoding: 'utf8'
        });
        assert.equal(result.status, 1, result.stdout + result.stderr);
        assert.match(result.stdout, /::error::Neither base nor default branch provides required gate tool: scripts\//);
        assert.equal(fs.existsSync(path.join(temp, 'gate-tools')), false);
    }
});
