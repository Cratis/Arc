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

function consumerRuns(args) {
    fs.mkdirSync(path.join(root, '.ai-work'), { recursive: true });
    const toolsRoot = fs.mkdtempSync(path.join(root, '.ai-work/consumer-tools-'));
    const baseScripts = path.join(toolsRoot, 'scripts');
    fs.mkdirSync(path.join(toolsRoot, 'node_modules/@cratis/fundamentals'), { recursive: true });
    fs.mkdirSync(path.join(toolsRoot, 'node_modules/typescript'), { recursive: true });
    fs.cpSync(path.join(__dirname, 'fixtures'), path.join(baseScripts, 'fixtures'), { recursive: true });
    const calls = [];
    vm.runInNewContext(consumers, {
        __dirname: baseScripts,
        process: { execPath: process.execPath, argv: ['node', 'guard.cjs', ...args], exit: code => { throw new Error(`exit ${code}`); } },
        console: { error: () => {}, log: () => {} },
        require: name => name === 'node:child_process'
            ? { spawnSync: (...call) => { calls.push(call); return { status: 0 }; } }
            : require(name)
    });
    return { calls, toolsRoot };
}

test('consumer guard defaults to built packages in its own repository', () => {
    const { calls, toolsRoot } = consumerRuns([]);
    for (const [, , options] of calls) {
        const arc = path.join(options.cwd, 'node_modules/@cratis/arc');
        assert.equal(fs.readlinkSync(arc), path.join(toolsRoot, 'Source/JavaScript/Arc'));
    }
});

test('consumer guard uses base compiler and types with head built packages', () => {
    const head = path.join(root, '.ai-work', 'head with spaces');
    const { calls, toolsRoot } = consumerRuns(['--root', head]);
    assert.equal(calls.length, 5);
    for (const [command, args, options] of calls) {
        assert.equal(command, process.execPath);
        assert.equal(args[0], path.join(toolsRoot, 'node_modules/typescript/bin/tsc'));
        assert.ok(args.includes('--ignoreConfig'));
        assert.ok(args.includes('--preserveSymlinks'));
        assert.ok(options.cwd.startsWith(path.join(toolsRoot, '.ai-work')));
        assert.equal(fs.readlinkSync(path.join(options.cwd, 'node_modules/@cratis/arc')), path.join(head, 'Source/JavaScript/Arc'));
        assert.equal(fs.readlinkSync(path.join(options.cwd, 'node_modules/typescript')), path.join(toolsRoot, 'node_modules/typescript'));
        assert.equal(fs.readlinkSync(path.join(options.cwd, 'node_modules/@cratis/fundamentals')), path.join(toolsRoot, 'node_modules/@cratis/fundamentals'));
        assert.equal(fs.readFileSync(args.at(-1), 'utf8'), fs.readFileSync(path.join(__dirname, 'fixtures/declaration-consumer', path.basename(args.at(-1))), 'utf8'));
    }
});

test('consumer guard retains the repo-root alias', () => {
    const head = path.join(root, '.ai-work', 'head with spaces');
    const { calls } = consumerRuns(['--repo-root', head]);
    assert.equal(fs.readlinkSync(path.join(calls[0][2].cwd, 'node_modules/@cratis/arc')), path.join(head, 'Source/JavaScript/Arc'));
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
    const result = spawnSync(process.execPath, [guard, '--root', head], { encoding: 'utf8' });
    assert.equal(result.status, 0, result.stdout + result.stderr);
    const aliasResult = spawnSync(process.execPath, [guard, '--repo-root', head], { encoding: 'utf8' });
    assert.equal(aliasResult.status, 0, aliasResult.stdout + aliasResult.stderr);
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
