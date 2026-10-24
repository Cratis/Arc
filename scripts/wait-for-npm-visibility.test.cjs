// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const assert = require('node:assert/strict');
const path = require('node:path');
const test = require('node:test');
const { discoverPackages, waitForVisibility } = require('./wait-for-npm-visibility.cjs');

const version = '22.53.0';
const packages = [{ name: '@cratis/core' }, { name: '@cratis/react' }];
const metadata = versions => ({ ok: true, json: async () => ({ versions }) });
const visible = () => metadata({ [version]: {} });
const missing = () => metadata({ '22.52.0': {} });

function polling(respond = visible) {
    let elapsed = 0;
    const calls = [];
    const sleeps = [];
    const logs = [];
    const options = {
        fetch: async (url, options) => {
            calls.push({ url, ...options });
            return respond(url, elapsed);
        },
        now: () => elapsed,
        sleep: async milliseconds => { sleeps.push(milliseconds); elapsed += milliseconds; },
        log: message => logs.push(message),
        intervalMs: 10,
        deadlineMs: 30
    };
    return { options, calls, sleeps, logs };
}

function discovery(manifests) {
    const root = path.resolve(__dirname, '..');
    const locations = manifests.map((_, index) => path.join('packages', String(index), 'package.json'));
    const definitions = ['packages/*', '!**/dist/**', '!**/bin/**', '!**/obj/**'];
    const calls = [];
    const files = new Map(locations.map((location, index) => [path.join(root, location), manifests[index]]));
    files.set(path.join(root, 'package.json'), { workspaces: definitions });
    const found = discoverPackages(root, {
        glob: (pattern, options) => { calls.push({ pattern, ...options }); return locations; },
        readFileSync: location => Buffer.from(JSON.stringify(files.get(location)))
    });
    return { found, calls, root };
}

test('succeeds immediately when every exact version is visible', async () => {
    const result = polling();
    await waitForVisibility(packages, version, result.options);
    assert.equal(result.calls.length, 2);
    assert.deepEqual(result.sleeps, []);
    assert.deepEqual(result.logs, packages.map(_ => `${_.name}@${version} visible after 0.0s`));
});

test('waits for a delayed package and does not recheck visible packages', async () => {
    const result = polling((url, elapsed) => url.endsWith('react') && elapsed < 20 ? missing() : visible());
    await waitForVisibility(packages, version, result.options);
    assert.deepEqual(result.sleeps, [10, 10]);
    assert.equal(result.calls.filter(_ => _.url.endsWith('core')).length, 1);
    assert.equal(result.calls.filter(_ => _.url.endsWith('react')).length, 3);
    assert.equal(result.logs.length, 2);
});

test('fails at the deadline naming only the packages still missing', async () => {
    const result = polling(url => url.endsWith('react') ? missing() : visible());
    await assert.rejects(waitForVisibility(packages, version, result.options), error => {
        assert.match(error.message, /Deadline.*22\.53\.0.*@cratis\/react/);
        assert.doesNotMatch(error.message, /@cratis\/core/);
        return true;
    });
    assert.deepEqual(result.sleeps, [10, 10, 10]);
});

test('bounds the final sleep by the remaining deadline', async () => {
    const result = polling(missing);
    await assert.rejects(waitForVisibility(packages, version, { ...result.options, deadlineMs: 25 }), /still missing/);
    assert.deepEqual(result.sleeps, [10, 10, 5]);
});

test('ignores private workspaces during discovery and polling', async () => {
    const { found } = discovery([...packages, { name: 'private', private: true }]);
    const result = polling();
    await waitForVisibility(found, version, result.options);
    assert.deepEqual(found, packages);
    assert.equal(result.calls.length, 2);
});

test('uses the runner workspace globs and exclusion patterns', () => {
    const { found, calls, root } = discovery(packages);
    assert.deepEqual(found, packages);
    assert.deepEqual(calls, [{
        pattern: path.join('packages/*', '**', 'package.json'),
        cwd: root,
        ignore: ['**/dist/**', '**/bin/**', '**/obj/**', `**${path.sep}dist${path.sep}**`, '**/node_modules/**']
    }]);
});

for (const status of [404, 401, 500]) {
    test(`keeps polling after HTTP ${status} even if its body lists the version`, async () => {
        const result = polling((_, elapsed) => elapsed === 0 ? { ...visible(), ok: false, status } : visible());
        await waitForVisibility(packages, version, result.options);
        assert.deepEqual(result.sleeps, [10]);
        assert.equal(result.logs.length, 2);
    });
}

test('keeps polling after fetch throws', async () => {
    const result = polling((_, elapsed) => {
        if (elapsed === 0) throw new Error('Network unavailable');
        return visible();
    });
    await waitForVisibility(packages, version, result.options);
    assert.deepEqual(result.sleeps, [10]);
});

test('keeps polling after invalid install metadata', async () => {
    const result = polling((_, elapsed) => elapsed === 0 ? { ok: true, json: async () => { throw new Error('Invalid JSON'); } } : visible());
    await waitForVisibility(packages, version, result.options);
    assert.deepEqual(result.sleeps, [10]);
});

test('requests abbreviated install metadata with an encoded package name', async () => {
    const result = polling();
    await waitForVisibility([packages[0]], version, result.options);
    assert.equal(result.calls[0].url, 'https://registry.npmjs.org/%40cratis%2Fcore');
    assert.deepEqual(result.calls[0].headers, { Accept: 'application/vnd.npm.install-v1+json' });
    assert.ok(result.calls[0].signal instanceof AbortSignal);
});

test('honors publishConfig.registry, preserving its path without duplicate slashes', async () => {
    const registry = 'https://registry.example.test/npm/';
    const { found } = discovery([{ ...packages[0], publishConfig: { registry } }]);
    const result = polling();
    await waitForVisibility(found, version, result.options);
    assert.equal(result.calls[0].url, 'https://registry.example.test/npm/%40cratis%2Fcore');
});

test('refuses an empty package train rather than silently succeeding', async () => {
    await assert.rejects(waitForVisibility([], version), /No non-private workspace packages/);
});

test('requires a release version', async () => {
    await assert.rejects(waitForVisibility(packages, ''), /release version is required/);
});

for (const value of [0, -1, NaN, Infinity]) {
    test(`refuses invalid polling durations: ${value}`, async () => {
        await assert.rejects(waitForVisibility(packages, version, { intervalMs: value }), /positive milliseconds/);
        await assert.rejects(waitForVisibility(packages, version, { deadlineMs: value }), /positive milliseconds/);
    });
}
