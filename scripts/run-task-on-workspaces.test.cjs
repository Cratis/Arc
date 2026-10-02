// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const assert = require('node:assert/strict');
const test = require('node:test');
const runWorkspaces = require('./fixtures/workspace-runner.cjs');

const publishedNames = result => result.calls.map(call => call.name);

for (const field of ['dependencies', 'peerDependencies', 'optionalDependencies']) {
    test(`publishes internal ${field} before dependents regardless of version syntax`, () => {
        const result = runWorkspaces([
            { name: 'consumer', [field]: { core: 'workspace:*', external: '^3.0.0' } },
            { name: 'core' }
        ]);
        assert.equal(result.status, 0);
        assert.deepEqual(publishedNames(result), ['core', 'consumer']);
        assert.equal(result.calls[1].manifest[field].core, '22.32.1');
        assert.equal(result.calls[1].manifest[field].external, '^3.0.0');
        for (const call of result.calls) {
            assert.equal(call.command, 'npm');
            assert.deepEqual(call.args, ['publish', '--provenance']);
            assert.equal(call.manifest.version, '22.32.1');
        }
    });
}

test('publishes an Arc-shaped diamond graph only once per workspace', () => {
    const result = runWorkspaces([
        { name: '@cratis/arc.react.mvvm', dependencies: { '@cratis/arc': '1.0.0', '@cratis/arc.react': '1.0.0' } },
        { name: '@cratis/arc.vite', dependencies: { '@cratis/arc': '1.0.0' } },
        { name: '@cratis/arc.react', dependencies: { '@cratis/arc': '1.0.0' }, peerDependencies: { '@cratis/arc': '*' } },
        { name: '@cratis/arc' },
        { name: '@cratis/eslint-plugin-arc' }
    ]);
    assert.equal(result.status, 0);
    assert.deepEqual(publishedNames(result), [
        '@cratis/arc', '@cratis/arc.react', '@cratis/arc.react.mvvm', '@cratis/arc.vite', '@cratis/eslint-plugin-arc'
    ]);
});

test('retains discovery order for independent packages and ignores development dependency cycles', () => {
    const result = runWorkspaces([
        { name: 'b', devDependencies: { a: '*' }, dependencies: { external: '*' } },
        { name: 'a', devDependencies: { b: '*' } },
        { name: 'c' }
    ]);
    assert.equal(result.status, 0);
    assert.deepEqual(publishedNames(result), ['b', 'a', 'c']);
    assert.equal(result.calls[0].manifest.devDependencies.a, '22.32.1');
});

test('skips private workspaces and does not include them in publish cycles', () => {
    const result = runWorkspaces([
        { name: 'private', private: true, dependencies: { public: '*' } },
        { name: 'public', dependencies: { private: '*' } }
    ]);
    assert.equal(result.status, 0);
    assert.deepEqual(publishedNames(result), ['public']);
    assert.equal(result.manifests[0].version, '1.0.0');
});

for (const graph of [
    [{ name: 'a', dependencies: { b: '*' } }, { name: 'b', optionalDependencies: { c: '*' } }, { name: 'c', peerDependencies: { a: '*' } }],
    [{ name: 'a', dependencies: { a: '*' } }]
]) {
    test(`rejects ${graph.length === 1 ? 'self' : 'multi-package'} cycles before mutating or publishing any workspace`, () => {
        const result = runWorkspaces([{ name: 'independent' }, ...graph]);
        assert.equal(result.status, 1);
        assert.deepEqual(result.calls, []);
        assert.ok(result.manifests.every(manifest => manifest.version === '1.0.0'));
        assert.match(result.output, graph.length === 1
            ? /Workspace publish dependency cycle: a -> a/
            : /Workspace publish dependency cycle: a -> b -> c -> a/);
    });
}

for (const field of ['dependencies', 'peerDependencies', 'optionalDependencies']) {
    test(`does not publish direct or transitive ${field} dependents of a failed package`, () => {
        const result = runWorkspaces([
            { name: 'leaf', dependencies: { middle: '*' } },
            { name: 'middle', [field]: { core: '*' } },
            { name: 'core' },
            { name: 'independent' },
            { name: 'independent-consumer', dependencies: { independent: '*' } }
        ], { results: { core: { status: 1 } } });
        assert.equal(result.status, 1);
        assert.deepEqual(publishedNames(result), ['core', 'independent', 'independent-consumer']);
        assert.equal(result.manifests[0].version, '1.0.0');
        assert.equal(result.manifests[1].version, '1.0.0');
    });
}

test('treats a failed npm spawn as a publish failure and still publishes independent workspaces', () => {
    const result = runWorkspaces([
        { name: 'consumer', dependencies: { core: '*' } }, { name: 'core' }, { name: 'independent' }
    ], { results: { core: { status: null, stdout: null, stderr: null, error: new Error('spawn npm ENOENT') } } });
    assert.equal(result.status, 1);
    assert.deepEqual(publishedNames(result), ['core', 'independent']);
});

test('handles an empty workspace set without publishing', () => {
    const result = runWorkspaces([]);
    assert.equal(result.status, 0);
    assert.deepEqual(result.calls, []);
});

for (const task of ['clean', 'build', 'lint', 'test', 'ci', 'up']) {
    test(`retains discovery order, private/script skipping and no manifest edits for ${task}`, () => {
        const result = runWorkspaces([
            { name: 'consumer', dependencies: { core: '*' }, scripts: { [task]: 'stub' } },
            { name: 'private', private: true, scripts: { [task]: 'stub' } },
            { name: 'no-script' },
            { name: 'core', dependencies: { consumer: '*' }, scripts: { [task]: 'stub' } }
        ], { task, args: ['ignored'] });
        assert.equal(result.status, 0);
        assert.deepEqual(publishedNames(result), ['consumer', 'core']);
        for (const call of result.calls) {
            assert.equal(call.command, 'yarn');
            assert.deepEqual(call.args, [task]);
        }
        assert.ok(result.manifests.every(manifest => manifest.version === '1.0.0'));
        assert.equal(result.manifests[0].dependencies.core, '*');
    });
}

test('retains fail-fast behavior for non-publish tasks', () => {
    const result = runWorkspaces([
        { name: 'first', scripts: { build: 'stub' } }, { name: 'second', scripts: { build: 'stub' } }
    ], { task: 'build', results: { first: { status: 1 } } });
    assert.equal(result.status, 1);
    assert.deepEqual(publishedNames(result), ['first']);
});

for (const args of [[], ['1.0.0', 'extra']]) {
    test(`retains no-op behavior for publishing with ${args.length} version arguments`, () => {
        const result = runWorkspaces([{ name: 'a', dependencies: { a: '*' } }], { args });
        assert.equal(result.status, 0);
        assert.deepEqual(result.calls, []);
        assert.equal(result.manifests[0].version, '1.0.0');
    });
}
