// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.
const assert = require('node:assert/strict');
const fs = require('node:fs');
const os = require('node:os');
const path = require('node:path');
const { spawnSync } = require('node:child_process');
const { after, test } = require('node:test');
const vm = require('node:vm');

const root = os.tmpdir();
const consumers = fs.readFileSync(path.join(__dirname, 'check-declaration-consumers.cjs'), 'utf8');

function consumerRuns(args, expectedExit) {
    const toolsRoot = fs.mkdtempSync(path.join(root, 'consumer-tools-'));
    after(() => fs.rmSync(toolsRoot, { recursive: true, force: true }));
    const baseScripts = path.join(toolsRoot, 'scripts');
    fs.mkdirSync(path.join(toolsRoot, 'node_modules/@cratis/fundamentals'), { recursive: true });
    fs.mkdirSync(path.join(toolsRoot, 'node_modules/typescript'), { recursive: true });
    fs.cpSync(path.join(__dirname, 'fixtures'), path.join(baseScripts, 'fixtures'), { recursive: true });
    buildPackages(toolsRoot);
    const calls = [];
    const handlers = new Map();
    const messages = [];
    const run = () => vm.runInNewContext(consumers, {
        __dirname: baseScripts,
        process: {
            execPath: process.execPath,
            argv: ['node', 'guard.cjs', ...args],
            on: (event, handler) => {
                handlers.set(event, handler);
                if (event === 'exit') after(handler);
            },
            exit: code => { throw new Error(`exit ${code}`); }
        },
        console: { error: message => messages.push(message), log: () => {} },
        require: name => name === 'node:child_process'
            ? { spawnSync: (...call) => { calls.push(call); return { status: 0 }; } }
            : require(name)
    });
    if (expectedExit === undefined) run();
    else assert.throws(run, new RegExp(`exit ${expectedExit}$`));
    return { calls, toolsRoot, handlers, messages };
}

function assertPackageCopy(copy, source) {
    assert.equal(fs.lstatSync(copy).isDirectory(), true);
    assert.equal(fs.lstatSync(copy).isSymbolicLink(), false);
    for (const file of ['package.json', 'dist/esm/index.d.ts', 'dist/cjs/index.d.ts']) {
        assert.equal(fs.readFileSync(path.join(copy, file), 'utf8'), fs.readFileSync(path.join(source, file), 'utf8'));
    }
}

test('consumer guard defaults to built packages in its own repository', () => {
    const { calls, toolsRoot } = consumerRuns([]);
    assert.equal(calls.length, 5);
    for (const [, , options] of calls) {
        const arc = path.join(options.cwd, 'node_modules/@cratis/arc');
        assertPackageCopy(arc, path.join(toolsRoot, 'Source/JavaScript/Arc'));
    }
});

test('consumer guard uses the tools compiler and types with checked built packages', () => {
    const { head } = declarationFixture();
    fs.writeFileSync(path.join(head, 'Source/JavaScript/Arc/checked-marker'), 'checked tree');
    const { calls, toolsRoot } = consumerRuns(['--root', head]);
    assert.equal(calls.length, 5);
    for (const [command, args, options] of calls) {
        assert.equal(command, process.execPath);
        assert.equal(args[0], path.join(toolsRoot, 'node_modules/typescript/bin/tsc'));
        assert.ok(args.includes('--ignoreConfig'));
        assert.ok(args.includes('--preserveSymlinks'));
        assert.ok(options.cwd.startsWith(path.join(os.tmpdir(), 'declaration-consumers-')));
        const arc = path.join(options.cwd, 'node_modules/@cratis/arc');
        assertPackageCopy(arc, path.join(head, 'Source/JavaScript/Arc'));
        assert.equal(fs.readFileSync(path.join(arc, 'checked-marker'), 'utf8'), 'checked tree');
        assert.equal(fs.readlinkSync(path.join(options.cwd, 'node_modules/typescript')), path.join(toolsRoot, 'node_modules/typescript'));
        assert.equal(fs.readlinkSync(path.join(options.cwd, 'node_modules/@cratis/fundamentals')), path.join(toolsRoot, 'node_modules/@cratis/fundamentals'));
        assert.equal(fs.readFileSync(args.at(-1), 'utf8'), fs.readFileSync(path.join(__dirname, 'fixtures/declaration-consumer', path.basename(args.at(-1))), 'utf8'));
    }
});

test('consumer guard retains the repo-root alias', () => {
    const { head } = declarationFixture();
    const { calls } = consumerRuns(['--repo-root', head]);
    assertPackageCopy(path.join(calls[0][2].cwd, 'node_modules/@cratis/arc'), path.join(head, 'Source/JavaScript/Arc'));
});

test('consumer guard rejects an incomplete root option', () => {
    assert.throws(() => consumerRuns(['--repo-root']), /exit 2/);
});

function declarationFixture() {
    const fixture = fs.mkdtempSync(path.join(root, 'declaration-roots-'));
    after(() => fs.rmSync(fixture, { recursive: true, force: true }));
    const scripts = path.join(fixture, 'base', 'scripts');
    const head = path.join(fixture, 'head with spaces');
    fs.mkdirSync(scripts, { recursive: true });
    fs.symlinkSync(path.resolve(__dirname, '../node_modules'), path.join(fixture, 'base', 'node_modules'));
    const guard = path.join(scripts, 'check-declaration-imports.cjs');
    fs.copyFileSync(path.join(__dirname, 'check-declaration-imports.cjs'), guard);
    fs.copyFileSync(path.join(__dirname, 'declaration-guard-paths.cjs'), path.join(scripts, 'declaration-guard-paths.cjs'));
    buildPackages(head);
    return { guard, head };
}

function buildPackages(repository) {
    for (const name of ['Arc', 'Arc.React', 'Arc.React.MVVM', 'Arc.Vite']) {
        const folder = path.join(repository, 'Source', 'JavaScript', name);
        const exports = {};
        for (const [branch, format] of [['import', 'esm'], ['require', 'cjs']]) {
            const dist = path.join(folder, 'dist', format);
            fs.mkdirSync(dist, { recursive: true });
            fs.writeFileSync(path.join(dist, 'index.d.ts'), 'export interface Thing {}\n');
            fs.writeFileSync(path.join(dist, 'index.js'), '');
            exports[branch] = { types: `./dist/${format}/index.d.ts`, default: `./dist/${format}/index.js` };
        }
        fs.writeFileSync(path.join(folder, 'package.json'), JSON.stringify({
            name,
            types: './dist/esm/index.d.ts',
            exports: { './package.json': './package.json', '.': exports }
        }));
    }
}

test('import guard reads explicit head output rather than base output', () => {
    const { guard, head } = declarationFixture();
    const result = spawnSync(process.execPath, [guard, '--root', head], { encoding: 'utf8' });
    assert.equal(result.status, 0, result.stdout + result.stderr);
    const aliasResult = spawnSync(process.execPath, [guard, '--repo-root', head], { encoding: 'utf8' });
    assert.equal(aliasResult.status, 0, aliasResult.stdout + aliasResult.stderr);
    const defaultResult = spawnSync(process.execPath, [guard], { encoding: 'utf8' });
    assert.equal(defaultResult.status, 2);
    assert.match(defaultResult.stderr, /Invalid checked path/);
});

for (const option of ['--root', '--repo-root']) {
    for (const [label, value] of [
        ['missing', () => undefined],
        ['empty', () => ''],
        ['nonexistent', head => path.join(head, 'missing')],
        ['file', head => path.join(head, 'Source/JavaScript/Arc/package.json')],
        ['symlinked', head => {
            const link = path.join(head, '..', 'linked-root');
            fs.symlinkSync(head, link);
            return link;
        }]
    ]) {
        test(`both guards refuse a ${label} ${option}`, () => {
            const { guard, head } = declarationFixture();
            const selected = value(head);
            const args = selected === undefined ? [option] : [option, selected];
            const result = spawnSync(process.execPath, [guard, ...args], { encoding: 'utf8' });
            assert.equal(result.status, 2, result.stdout + result.stderr);
            assert.match(result.stderr, /Usage:|Invalid checked root/);
            const { calls, messages } = consumerRuns(args, 2);
            assert.equal(calls.length, 0);
            assert.match(messages.join('\n'), /Usage:|Invalid checked root/);
        });
    }
}

test('both guards refuse a symlinked manifest without executing its JavaScript target', () => {
    const { guard, head } = declarationFixture();
    const marker = path.join(head, 'executed');
    const payload = path.join(head, 'payload.js');
    fs.writeFileSync(payload, `require('node:fs').writeFileSync(${JSON.stringify(marker)}, 'ran'); module.exports = {};\n`);
    const manifest = path.join(head, 'Source/JavaScript/Arc/package.json');
    fs.unlinkSync(manifest);
    fs.symlinkSync(payload, manifest);
    const result = spawnSync(process.execPath, [guard, '--root', head], { encoding: 'utf8' });
    assert.equal(result.status, 2, result.stdout + result.stderr);
    assert.match(result.stderr, /symlink is not allowed/);
    assert.equal(fs.existsSync(marker), false);
    const { calls, messages } = consumerRuns(['--root', head], 2);
    assert.equal(calls.length, 0);
    assert.match(messages.join('\n'), /symlink is not allowed/);
    assert.equal(fs.existsSync(marker), false);
});

test('both guards refuse declaration directories linked outside the checked root', () => {
    const { guard, head } = declarationFixture();
    const dist = path.join(head, 'Source/JavaScript/Arc/dist');
    const outside = path.join(head, '..', 'outside-dist');
    fs.renameSync(dist, outside);
    fs.symlinkSync(outside, dist);
    const result = spawnSync(process.execPath, [guard, '--root', head], { encoding: 'utf8' });
    assert.equal(result.status, 2, result.stdout + result.stderr);
    assert.match(result.stderr, /symlink is not allowed/);
    const { calls, messages } = consumerRuns(['--root', head], 2);
    assert.equal(calls.length, 0);
    assert.match(messages.join('\n'), /symlink is not allowed/);
});

function changeManifest(head, change) {
    const file = path.join(head, 'Source/JavaScript/Arc/package.json');
    const manifest = JSON.parse(fs.readFileSync(file, 'utf8'));
    change(manifest);
    fs.writeFileSync(file, JSON.stringify(manifest));
}

function assertBothRefuse(guard, head, message) {
    const result = spawnSync(process.execPath, [guard, '--root', head], { encoding: 'utf8' });
    assert.equal(result.status, 2, result.stdout + result.stderr);
    assert.match(result.stderr, message);
    const { calls, messages } = consumerRuns(['--root', head], 2);
    assert.equal(calls.length, 0);
    assert.match(messages.join('\n'), message);
}

function assertBothPass(guard, head) {
    const result = spawnSync(process.execPath, [guard, '--root', head], { encoding: 'utf8' });
    assert.equal(result.status, 0, result.stdout + result.stderr);
    const run = consumerRuns(['--root', head]);
    assert.equal(run.calls.length, 5);
    return run;
}

test('both guards refuse a string export into a symlinked external directory', () => {
    const { guard, head } = declarationFixture();
    const outside = path.join(head, '..', 'external');
    fs.mkdirSync(outside);
    fs.writeFileSync(path.join(outside, 'index.d.ts'), 'export interface External {}\n');
    fs.symlinkSync(outside, path.join(head, 'Source/JavaScript/Arc/external'));
    changeManifest(head, manifest => { manifest.exports = { '.': './external/index.d.ts' }; });
    assertBothRefuse(guard, head, /symlink is not allowed/);
});

for (const [label, change] of [
    ['types', manifest => { manifest.types = './outside.d.ts'; }],
    ['typings', manifest => { manifest.typings = './outside.d.ts'; }],
    ['declaration main', manifest => { manifest.main = './outside.d.ts'; }],
    ['string export', manifest => { manifest.exports = { '.': './outside.d.ts' }; }],
    ['root string export', manifest => { manifest.exports = './outside.d.ts'; }],
    ['nested condition export', manifest => { manifest.exports['.'].import.browser = { development: './outside.d.ts' }; }],
    ['array export', manifest => { manifest.exports['.'].require.fallback = [null, ['./outside.d.ts']]; }],
    ['sibling package declaration', manifest => { manifest.types = '../Arc.React/dist/esm/index.d.ts'; }],
    ['normalized traversal', manifest => { manifest.types = './dist/../outside.d.ts'; }],
    ['disguised metadata export', manifest => { manifest.exports['./package.json'] = { types: './package.json' }; }],
    ['metadata at another subpath', manifest => { manifest.exports['./other.json'] = './package.json'; }]
]) {
    test(`both guards refuse ${label} outside the package dist`, () => {
        const { guard, head } = declarationFixture();
        fs.writeFileSync(path.join(head, 'Source/JavaScript/Arc/outside.d.ts'), 'export interface Outside {}\n');
        changeManifest(head, change);
        assertBothRefuse(guard, head, /under package dist/);
    });
}

for (const location of ['unreferenced-link', 'src/deep/unreferenced-link', 'dist/esm/unreferenced-link']) {
    test(`both guards refuse an unreferenced package symlink at ${location}`, () => {
        const { guard, head } = declarationFixture();
        const link = path.join(head, 'Source/JavaScript/Arc', location);
        fs.mkdirSync(path.dirname(link), { recursive: true });
        fs.symlinkSync(path.join(head, 'Source/JavaScript/Arc.React/dist'), link);
        assertBothRefuse(guard, head, /symlink is not allowed/);
    });
}

for (const [label, exports] of [
    ['string', { '.': './dist/esm/index.d.ts' }],
    ['root string', './dist/esm/index.d.ts'],
    ['root array', [null, { types: './dist/esm/index.d.ts' }, './dist/esm/index.js']],
    ['nested conditions and arrays', {
        './package.json': './package.json',
        '.': {
            import: [{ types: './dist/esm/index.d.ts', default: './dist/esm/index.js' }],
            require: { node: { types: './dist/cjs/index.d.ts', default: './dist/cjs/index.js' } }
        }
    }]
]) {
    test(`both guards accept valid ${label} exports and declaration entrypoints under dist`, () => {
        const { guard, head } = declarationFixture();
        changeManifest(head, manifest => {
            manifest.exports = exports;
            manifest.typings = './dist/esm/index.d.ts';
            manifest.main = './dist/cjs/index.d.ts';
        });
        assertBothPass(guard, head);
    });
}

for (const kind of ['directory', 'symlink']) {
    test(`consumer copies omit checked node_modules (${kind}) at every depth`, () => {
        const { guard, head } = declarationFixture();
        const arc = path.join(head, 'Source/JavaScript/Arc');
        const outside = path.join(head, '..', 'external-types');
        fs.mkdirSync(outside);
        fs.writeFileSync(path.join(outside, 'index.d.ts'), 'export interface Untrusted {}\n');
        if (kind === 'directory') {
            fs.mkdirSync(path.join(arc, 'node_modules'));
            fs.symlinkSync(outside, path.join(arc, 'node_modules/unchecked'));
        } else fs.symlinkSync(outside, path.join(arc, 'node_modules'));
        fs.mkdirSync(path.join(arc, 'src'));
        fs.symlinkSync(outside, path.join(arc, 'src/node_modules'));
        fs.symlinkSync(outside, path.join(arc, 'dist/esm/node_modules'));
        const { calls, toolsRoot } = assertBothPass(guard, head);
        for (const [, , options] of calls) {
            const copy = path.join(options.cwd, 'node_modules/@cratis/arc');
            assertPackageCopy(copy, arc);
            for (const location of ['node_modules', 'src/node_modules', 'dist/esm/node_modules']) {
                assert.equal(fs.lstatSync(path.join(copy, location), { throwIfNoEntry: false }), undefined);
            }
            assert.equal(fs.readlinkSync(path.join(options.cwd, 'node_modules/typescript')), path.join(toolsRoot, 'node_modules/typescript'));
        }
        assert.equal(fs.existsSync(path.join(arc, 'node_modules')), true);
    });
}

for (const [signal, status] of [['SIGINT', 130], ['SIGTERM', 143]]) {
    test(`consumer guard cleans its fixture directory on ${signal}`, () => {
        const { calls, handlers } = consumerRuns([]);
        const fixtures = calls[0][2].cwd;
        assert.equal(fs.existsSync(fixtures), true);
        assert.throws(() => handlers.get(signal)(), new RegExp(`exit ${status}$`));
        assert.equal(fs.existsSync(fixtures), false);
    });
}

test('import guard still rejects invalid declarations in the head tree', () => {
    const { guard, head } = declarationFixture();
    fs.writeFileSync(path.join(head, 'Source/JavaScript/Arc/dist/esm/index.d.ts'), "export type Thing = import('./missing').Thing;\n");
    const result = spawnSync(process.execPath, [guard, '--repo-root', head], { encoding: 'utf8' });
    assert.equal(result.status, 1);
    assert.match(result.stderr, /extensionless relative import/);
});
