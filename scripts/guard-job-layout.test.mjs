// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
import assert from 'node:assert/strict';
import fs from 'node:fs';
import path from 'node:path';
import { spawnSync } from 'node:child_process';
import { fileURLToPath } from 'node:url';
import { after, test } from 'node:test';

const root = path.resolve(path.dirname(fileURLToPath(import.meta.url)), '..');
const workflow = name => fs.readFileSync(path.join(root, '.github/workflows', name), 'utf8');
const policies = [['dotnet-build.yml', 'test-shard-policy'], ['javascript-build.yml', 'declaration-guards']];
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

test('declaration evaluation uses an artifact and tooling installed and invoked in the trusted tree', () => {
    const build = job('javascript-build.yml', 'build');
    const guards = job('javascript-build.yml', 'declaration-guards');
    assert.match(build, /name: Upload declaration inputs/);
    assert.doesNotMatch(build, /name: Verify declarations with base guards/);
    assert.match(guards, /needs: build/);
    assert.match(guards, /working-directory: \$\{\{ env.GATE_TOOLS \}\}\n        run: yarn install --immutable\n/);
    assert.match(guards, /path: \$\{\{ runner.temp \}\}\/declaration-data\/Source\/JavaScript/);
    assert.match(guards, /name: Verify declarations with base guards\n        working-directory: \$\{\{ env.GATE_TOOLS \}\}/);
    const decision = stepScript(guards, 'Verify declarations with base guards');
    assert.doesNotMatch(decision, /yarn|npm|GITHUB_WORKSPACE|bootstrap|prefix/);
    for (const name of ['imports', 'consumers']) {
        assert.ok(decision.includes(`node scripts/check-declaration-${name}.cjs --root "$RUNNER_TEMP/declaration-data"`));
    }
    const extraction = stepScript(guards, 'Extract base tools outside the head tree');
    // Archive all workspace sources, not links back into the PR checkout, for Yarn's workspace dependencies.
    assert.match(extraction, /archive "\$revision" \| tar -x -C "\$tools"/);
    assert.doesNotMatch(extraction, /ln -s|node_modules/);
});

test('no checkout places gate tools inside the workspace', () => {
    for (const file of fs.readdirSync(path.join(root, '.github/workflows')).filter(name => name.endsWith('.yml'))) {
        for (const step of workflow(file).split(/(?=^\s*- (?:name:|uses:))/m)) {
            if (step.includes('uses: actions/checkout@')) assert.doesNotMatch(step, /path:.*gate-tools/, file);
        }
    }
    for (const [file, name] of policies) {
        const checkout = job(file, name).split(/^      - name: /m).find(step => step.startsWith('Checkout head data\n'));
        assert.doesNotMatch(checkout, /\n          path:/);
    }
});

test('the pull request gate includes both callers and non-PR events never select github.sha as tooling', () => {
    const gate = job('pull-requests.yml', 'gate');
    assert.match(gate, /needs: \[[^\n]*dotnet-build, javascript-build,/);
    for (const [file, name] of policies) {
        const policy = job(file, name);
        assert.doesNotMatch(policy, /continue-on-error/);
        assert.match(policy, /BASE_SHA: \$\{\{ github.event_name == 'pull_request' && github.event.pull_request.base.sha \|\| '' \}\}/);
        assert.match(policy, /permissions:\n      contents: read/);
        assert.match(policy, /persist-credentials: false/);
        assert.doesNotMatch(stepScript(policy, 'Extract base tools outside the head tree'), /github.sha|GITHUB_SHA|FETCH_HEAD/);
    }
});

const guardScripts = ['scripts/ci-test-shards.py', 'scripts/check-declaration-imports.cjs', 'scripts/check-declaration-consumers.cjs'];
const guardFiles = ['.github/test-shards.json', 'scripts/declaration-guard-paths.cjs',
    'scripts/fixtures/declaration-consumer/consumer.mts', 'scripts/fixtures/declaration-consumer/consumer.cts',
    'package.json', 'yarn.lock', '.yarnrc.yml', 'Source/JavaScript/Arc/package.json'];

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
    after(() => fs.rmSync(fixture, { recursive: true, force: true }));
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
    fs.rmSync(path.join(head, 'Source'), { recursive: true });
    fs.symlinkSync(path.join(root, 'Source'), path.join(head, 'Source'));
    fs.symlinkSync(path.join(root, 'Samples'), path.join(head, 'Samples'));
    return { fixture, head, base, trusted };
}
function selectionRun(fixture, file, name, overrides) {
    const temp = path.join(fixture.fixture, name);
    fs.mkdirSync(temp);
    const env = { ...process.env, GITHUB_WORKSPACE: fixture.head, RUNNER_TEMP: temp, BASE_SHA: fixture.base,
        DEFAULT_BRANCH: 'main', GITHUB_OUTPUT: path.join(temp, 'output.txt'), GITHUB_ENV: path.join(temp, 'env.txt'),
        GUARD_SCRIPTS: name === 'test-shard-policy' ? guardScripts[0] : guardScripts.slice(1).join(' '),
        GUARD_FILES: name === 'test-shard-policy' ? guardFiles[0] : guardFiles.slice(1).join(' '), ...overrides };
    const result = spawnSync('bash', ['-c', stepScript(job(file, name), 'Extract base tools outside the head tree')], {
        cwd: temp, env, encoding: 'utf8'
    });
    return { ...result, env, temp };
}
function checkExtractedTools(fixture, name, run) {
    assert.equal(fs.readlinkSync(path.join(fixture.head, 'gate-tools')), '.');
    const tools = fs.readFileSync(run.env.GITHUB_ENV, 'utf8').trim().replace(/^GATE_TOOLS=/, '');
    assert.ok(tools.startsWith(path.join(run.temp, 'gate-tools.')));
    assert.equal(fs.lstatSync(tools).isSymbolicLink(), false);
    const selected = name === 'test-shard-policy' ? guardScripts[0] : guardScripts[1];
    assert.equal(fs.readFileSync(path.join(tools, selected), 'utf8'), fs.readFileSync(path.join(root, selected), 'utf8'));
    return tools;
}
function checkTrustedSelection(fixture, expected, overrides = {}) {
    for (const [file, name] of policies) {
        const run = selectionRun(fixture, file, name, overrides);
        assert.equal(run.status, 0, run.stdout + run.stderr);
        assert.ok(run.stdout.includes(`Selected ${expected}`), run.stdout);
        assert.ok(run.stdout.includes(fixture.trusted));
        const tools = checkExtractedTools(fixture, name, run);
        if (name === 'test-shard-policy') {
            command('bash', ['-c', stepScript(job(file, name), 'Verify the test shard assignment')], run.temp, { ...run.env, GATE_TOOLS: tools });
            assert.match(fs.readFileSync(run.env.GITHUB_OUTPUT, 'utf8'), /proxy-shards=/);
        } else {
            const workspace = path.join(tools, 'Source/JavaScript/Arc');
            assert.equal(fs.realpathSync(workspace), workspace);
            assert.equal(fs.readFileSync(path.join(workspace, 'package.json'), 'utf8'), fs.readFileSync(path.join(root, 'Source/JavaScript/Arc/package.json'), 'utf8'));
        }
    }
}
function checkClosedSelection(fixture, overrides = {}) {
    for (const [file, name] of policies) {
        const run = selectionRun(fixture, file, name, overrides);
        assert.equal(run.status, 1, run.stdout + run.stderr);
        assert.match(run.stdout, /::error::Neither base nor default branch provides required gate tool: scripts\//);
        assert.equal(fs.readdirSync(run.temp).some(entry => entry.startsWith('gate-tools.')), false);
    }
}

test('a committed head gate-tools symlink cannot redirect either base extraction', () => {
    checkTrustedSelection(archiveFixture(), 'base revision');
});

test('a base without tools uses default-branch tools, never head tools or head policy', () => {
    checkTrustedSelection(archiveFixture(false, true), 'default branch main revision');
});

test('missing tools on both trusted revisions fail closed despite head-provided scripts', () => {
    checkClosedSelection(archiveFixture(false, false));
});

test('dispatch, schedule and workflow_run use default-branch tools, not GITHUB_SHA', () => {
    for (const event of ['workflow_dispatch', 'schedule', 'workflow_run']) {
        const fixture = archiveFixture(false, true);
        checkTrustedSelection(fixture, 'default branch main revision', {
            BASE_SHA: '', GITHUB_SHA: command('git', ['rev-parse', 'HEAD'], fixture.head), GITHUB_EVENT_NAME: event
        });
    }
});

test('invalid or unavailable base SHAs fall back to the default branch', () => {
    for (const base of ['main', 'a'.repeat(40)]) {
        checkTrustedSelection(archiveFixture(false, true), 'default branch main revision', { BASE_SHA: base });
    }
});

test('an unavailable default branch fails closed with a named missing tool', () => {
    checkClosedSelection(archiveFixture(false, true), { DEFAULT_BRANCH: 'missing' });
});

function remoteResponse(fixture, response) {
    const bin = path.join(fixture.fixture, 'bin');
    fs.mkdirSync(bin);
    const git = command('which', ['git'], root);
    fs.writeFileSync(path.join(bin, 'git'), `#!/bin/bash\nfor arg in "$@"; do\n  if [ "$arg" = ls-remote ]; then printf '%s\\n' "$REMOTE_RESPONSE"; exit 0; fi\ndone\nexec "${git}" "$@"\n`, { mode: 0o755 });
    return { PATH: `${bin}${path.delimiter}${process.env.PATH}`, REMOTE_RESPONSE: response };
}

test('default branch lookup accepts only an exact ref with a 40-hex SHA', () => {
    for (const response of ['wrong-ref', 'invalid-sha', 'ambiguous']) {
        const fixture = archiveFixture(false, true);
        const rows = {
            'wrong-ref': `${fixture.trusted}\trefs/heads/main-extra`,
            'invalid-sha': 'not-a-sha\trefs/heads/main',
            ambiguous: `${fixture.trusted}\trefs/heads/main\n${fixture.base}\trefs/heads/main`
        };
        checkClosedSelection(fixture, remoteResponse(fixture, rows[response]));
    }
});

test('default branch lookup ignores additional non-matching refs', () => {
    const fixture = archiveFixture(false, true);
    const response = `${fixture.base}\trefs/heads/main-extra\n${fixture.trusted}\trefs/heads/main`;
    checkTrustedSelection(fixture, 'default branch main revision', remoteResponse(fixture, response));
});
