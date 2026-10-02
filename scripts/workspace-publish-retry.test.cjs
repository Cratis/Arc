// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const assert = require('node:assert/strict');
const test = require('node:test');
const runWorkspaces = require('./fixtures/workspace-runner.cjs');

const packages = [
    { name: 'consumer', dependencies: { core: '*' } },
    { name: 'core' }
];
const exists = { status: 0, stdout: '22.32.1\n' };
const notFound = { status: 1, stderr: 'npm error code E404' };
const unknown = { status: 1, stderr: 'npm error code E401' };
const publishedNames = result => result.calls.map(call => call.name);

test('does not republish an already-published dependency or block its dependents', () => {
    const result = runWorkspaces(packages, { viewResults: { core: exists } });
    assert.equal(result.status, 0);
    assert.deepEqual(publishedNames(result), ['consumer']);
    assert.deepEqual(result.viewCalls[0].args, ['view', 'core@22.32.1', 'version']);
    assert.equal(result.manifests[1].version, '22.32.1');
    assert.equal(result.calls[0].manifest.dependencies.core, '22.32.1');
});

for (const [name, response] of [['E404', notFound], ['another error', unknown]]) {
    test(`attempts publish normally after npm view returns ${name}`, () => {
        const result = runWorkspaces(packages, { viewResults: { core: response } });
        assert.equal(result.status, 0);
        assert.deepEqual(publishedNames(result), ['core', 'consumer']);
        assert.deepEqual(result.viewCalls[0].args, ['view', 'core@22.32.1', 'version']);
        assert.equal(result.viewCalls[0].cwd, result.calls[0].cwd);
        assert.equal(result.viewCalls.filter(call => call.name === 'core').length, 1);
    });
}

test('satisfies a failed publish if one re-check confirms the exact version exists', () => {
    const result = runWorkspaces(packages, {
        viewResults: { core: [notFound, exists] }, results: { core: { status: 1 } }
    });
    assert.equal(result.status, 0);
    assert.deepEqual(publishedNames(result), ['core', 'consumer']);
    assert.equal(result.viewCalls.filter(call => call.name === 'core').length, 2);
});

for (const [name, response] of [['E404', notFound], ['another error', unknown], ['a different version', { status: 0, stdout: '22.32.0\n' }]]) {
    test(`blocks dependents after a publish failure and one re-check returns ${name}`, () => {
        const result = runWorkspaces(packages, {
            viewResults: { core: [notFound, response] }, results: { core: { status: 1 } }
        });
        assert.equal(result.status, 1);
        assert.deepEqual(publishedNames(result), ['core']);
        assert.equal(result.viewCalls.filter(call => call.name === 'core').length, 2);
    });
}

test('does not mistake another version for the requested published version', () => {
    const result = runWorkspaces(packages, { viewResults: { core: { status: 0, stdout: '22.32.0\n' } } });
    assert.equal(result.status, 0);
    assert.deepEqual(publishedNames(result), ['core', 'consumer']);
});

test('checks the publishConfig registry from the publish cwd before and after a failed publish', () => {
    const registry = 'https://registry.example.test/';
    const result = runWorkspaces([{ name: '@example/core', publishConfig: { registry } }], {
        results: { '@example/core': { status: 1 } },
        viewResults: { '@example/core': [notFound, exists] }
    });
    assert.equal(result.status, 0);
    assert.equal(result.viewCalls.length, 2);
    for (const call of result.viewCalls) {
        assert.deepEqual(call.args, ['view', '@example/core@22.32.1', 'version', '--registry', registry]);
        assert.equal(call.cwd, result.calls[0].cwd);
    }
});
