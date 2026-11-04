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
const publishCalls = (result, name) => result.calls.filter(call => call.name === name);
const tlogConflict = {
    status: 1,
    stderr: 'npm error code TLOG_CREATE_ENTRY_ERROR\nnpm error error creating tlog entry - (409) an equivalent entry already exists in the transparency log with UUID example\n'
};

function assertProvenanceAttempts(result, name) {
    const calls = publishCalls(result, name);
    for (const call of calls) {
        assert.equal(call.command, 'npm');
        assert.deepEqual(call.args, ['publish', '--provenance']);
        assert.equal(call.cwd, calls[0].cwd);
    }
}

for (const format of ['error', 'ERR!']) {
    test(`retries a transparency log conflict with npm ${format} and then publishes dependents`, () => {
        const conflict = { ...tlogConflict, stderr: tlogConflict.stderr.replaceAll('npm error', `npm ${format}`) };
        const result = runWorkspaces(packages, {
            results: { core: [conflict, { status: 0 }] }, viewResults: { core: notFound }
        });
        assert.equal(result.status, 0);
        assert.deepEqual(publishedNames(result), ['core', 'core', 'consumer']);
        assert.deepEqual(result.delays, [5000]);
        assert.equal(result.viewCalls.filter(call => call.name === 'core').length, 1);
        assertProvenanceAttempts(result, 'core');
    });
}

test('publishes successfully on the third identical provenance attempt', () => {
    const result = runWorkspaces(packages, {
        results: { core: [tlogConflict, tlogConflict, { status: 0 }] }, viewResults: { core: notFound }
    });
    assert.equal(result.status, 0);
    assert.deepEqual(publishedNames(result), ['core', 'core', 'core', 'consumer']);
    assert.deepEqual(result.delays, [5000, 10000]);
    assert.equal(result.viewCalls.filter(call => call.name === 'core').length, 1);
    assertProvenanceAttempts(result, 'core');
});

test('stops after three conflicts, blocks dependents and continues independent workspaces', () => {
    const result = runWorkspaces([...packages, { name: 'independent' }], {
        results: { core: [tlogConflict, tlogConflict, tlogConflict] },
        viewResults: { core: [notFound, notFound] }
    });
    assert.equal(result.status, 1);
    assert.deepEqual(publishedNames(result), ['core', 'core', 'core', 'independent']);
    assert.deepEqual(result.delays, [5000, 10000]);
    assert.equal(result.viewCalls.filter(call => call.name === 'core').length, 2);
    assertProvenanceAttempts(result, 'core');
});

test('accepts exhausted conflicts only when the final exact-version check confirms', () => {
    const result = runWorkspaces(packages, {
        results: { core: [tlogConflict, tlogConflict, tlogConflict] },
        viewResults: { core: [notFound, exists] }
    });
    assert.equal(result.status, 0);
    assert.deepEqual(publishedNames(result), ['core', 'core', 'core', 'consumer']);
    assert.deepEqual(result.delays, [5000, 10000]);
    assert.equal(result.viewCalls.filter(call => call.name === 'core').length, 2);
    assertProvenanceAttempts(result, 'core');
});

test('stops immediately on a nonqualifying failure after the first conflict', () => {
    const result = runWorkspaces(packages, {
        results: { core: [tlogConflict, unknown, { status: 0 }] },
        viewResults: { core: [notFound, notFound] }
    });
    assert.equal(result.status, 1);
    assert.deepEqual(publishedNames(result), ['core', 'core']);
    assert.deepEqual(result.delays, [5000]);
    assert.equal(result.viewCalls.filter(call => call.name === 'core').length, 2);
    assertProvenanceAttempts(result, 'core');
});

for (const [name, response] of [
    ['the tlog code without 409', { status: 1, stderr: 'npm error code TLOG_CREATE_ENTRY_ERROR\nnpm error (500) internal error\n' }],
    ['a generic 409 without the tlog code', { status: 1, stderr: 'npm error code E409\nnpm error (409) conflict\n' }],
    ['the tlog code only as a substring', { status: 1, stderr: `npm error prefix code TLOG_CREATE_ENTRY_ERROR\n${tlogConflict.stderr.split('\n')[1]}\n` }],
    ['a code with a suffix', { ...tlogConflict, stderr: tlogConflict.stderr.replace('TLOG_CREATE_ENTRY_ERROR', 'TLOG_CREATE_ENTRY_ERROR_OTHER') }],
    ['the tlog code and 409 without equivalent-entry text', { status: 1, stderr: 'npm error code TLOG_CREATE_ENTRY_ERROR\nnpm error (409) conflict\n' }],
    ['equivalent-entry text outside an npm error line', { ...tlogConflict, stderr: tlogConflict.stderr.replace('npm error error creating', 'error creating') }],
    ['409 and equivalent-entry text on separate lines', { status: 1, stderr: 'npm error code TLOG_CREATE_ENTRY_ERROR\nnpm error (409) conflict\nnpm error an equivalent entry already exists in the transparency log\n' }],
    ['a spawn error', { status: null, error: new Error('spawn npm ENOENT'), stderr: tlogConflict.stderr }],
    ['a successful exit with conflict text', { ...tlogConflict, status: 0 }]
]) {
    test(`does not retry ${name}`, () => {
        const result = runWorkspaces(packages, {
            results: { core: response }, viewResults: { core: [notFound, notFound] }
        });
        assert.equal(result.status, response.status === 0 ? 0 : 1);
        assert.deepEqual(publishedNames(result), response.status === 0 ? ['core', 'consumer'] : ['core']);
        assert.deepEqual(result.delays, []);
        assert.equal(result.viewCalls.filter(call => call.name === 'core').length, response.status === 0 ? 1 : 2);
    });
}

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
