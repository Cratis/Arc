// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const { test } = require('node:test');
const vm = require('node:vm');

const root = path.resolve(__dirname, '..');
const consumers = fs.readFileSync(path.join(__dirname, 'check-declaration-consumers.cjs'), 'utf8');
const baseScripts = path.join(root, 'gate-tools', 'scripts');

function consumerRuns(args) {
    const calls = [];
    const output = [];
    vm.runInNewContext(consumers, {
        __dirname: baseScripts,
        process: { argv: ['node', 'guard.cjs', ...args], exit: code => { throw new Error(`exit ${code}`); } },
        console: { error: message => output.push(message), log: () => {} },
        require: name => name === 'node:child_process'
            ? { spawnSync: (...call) => { calls.push(call); return { status: 0 }; } }
            : require(name)
    });
    return calls;
}

test('consumer guard defaults to its own repository', () => {
    for (const [, , options] of consumerRuns([])) {
        assert.equal(options.cwd, path.dirname(baseScripts));
    }
});

test('consumer guard uses head dependencies but keeps base fixtures', () => {
    const head = path.join(root, '.ai-work', 'head with spaces');
    const calls = consumerRuns(['--repo-root', head]);
    assert.equal(calls.length, 5);
    for (const [, args, options] of calls) {
        assert.equal(options.cwd, head);
        assert.ok(args.at(-1).startsWith(path.join(baseScripts, 'fixtures')));
    }
});

test('consumer guard rejects an incomplete root option', () => {
    assert.throws(() => consumerRuns(['--repo-root']), /exit 2/);
});

function declarationFixture() {
    const parent = path.join(root, '.ai-work');
    fs.mkdirSync(parent, { recursive: true });
    const fixture = fs.mkdtempSync(path.join(parent, 'declaration-roots-'));
    const scripts = path.join(fixture, 'base', 'scripts');
    const head = path.join(fixture, 'head with spaces');
    fs.mkdirSync(scripts, { recursive: true });
    const guard = path.join(scripts, 'check-declaration-imports.cjs');
    fs.copyFileSync(path.join(__dirname, 'check-declaration-imports.cjs'), guard);
    for (const name of ['Arc', 'Arc.React', 'Arc.React.MVVM', 'Arc.Vite']) {
        const folder = path.join(head, 'Source', 'JavaScript', name);
        const exports = {};
        for (const [branch, format] of [['import', 'esm'], ['require', 'cjs']]) {
            const dist = path.join(folder, 'dist', format);
            fs.mkdirSync(dist, { recursive: true });
            fs.writeFileSync(path.join(dist, 'index.d.ts'), 'export interface Thing {}\n');
            fs.writeFileSync(path.join(dist, 'index.js'), '');
            exports[branch] = { types: `./dist/${format}/index.d.ts`, default: `./dist/${format}/index.js` };
        }
        fs.writeFileSync(path.join(folder, 'package.json'), JSON.stringify({ name, exports: { '.': exports } }));
    }
    return { guard, head };
}

test('import guard reads explicit head output rather than base output', () => {
    const { guard, head } = declarationFixture();
    const result = spawnSync(process.execPath, [guard, '--repo-root', head], { encoding: 'utf8' });
    assert.equal(result.status, 0, result.stdout + result.stderr);
    const defaultResult = spawnSync(process.execPath, [guard], { encoding: 'utf8' });
    assert.equal(defaultResult.status, 1);
    assert.match(defaultResult.stderr, /Cannot find module/);
});

test('import guard still rejects invalid declarations in the head tree', () => {
    const { guard, head } = declarationFixture();
    fs.writeFileSync(path.join(head, 'Source/JavaScript/Arc/dist/esm/index.d.ts'), "export type Thing = import('./missing').Thing;\n");
    const result = spawnSync(process.execPath, [guard, '--repo-root', head], { encoding: 'utf8' });
    assert.equal(result.status, 1);
    assert.match(result.stderr, /extensionless relative import/);
});
