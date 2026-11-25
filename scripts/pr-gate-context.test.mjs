// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { checkCodeRun, createApi, latestCodeRun, latestMetaRun, WORKFLOW_PATH } from './pr-gate-context.mjs';

const sha = 'a'.repeat(40);
const repo = 'Cratis/Arc';
const run = (id, overrides = {}) => ({
    id, run_number: id, run_attempt: 1, workflow_id: 7, path: WORKFLOW_PATH,
    repository: { full_name: repo }, head_sha: sha, event: 'pull_request',
    display_title: 'A pull request [code]', status: 'completed', conclusion: 'success',
    created_at: `2026-11-22T12:${String(id).padStart(2, '0')}:00Z`, ...overrides
});

function apiFor(runs) {
    return {
        get: async path => path.endsWith('/pull-requests.yml') ? { id: 7, path: WORKFLOW_PATH } : runs.find(item => path.endsWith(`/${item.id}`)),
        pages: async () => runs
    };
}

test('selects the newest non-meta run, never an older green code run', () => {
    const runs = [run(1), run(2, { display_title: 'edit [meta]' }), run(3, { display_title: 'legacy' })];
    assert.equal(latestCodeRun(runs).id, 3);
    assert.equal(latestMetaRun(runs).id, 2);
});

test('a completed successful whole code run passes even after metadata edits', async () => {
    const result = await checkCodeRun(apiFor([run(1), run(2, { display_title: 'edit [meta]' })]), repo, sha);
    assert.equal(result.passed, true);
});

for (const status of ['queued', 'in_progress', 'waiting', 'pending']) {
    test(`the newest ${status} code run fails fast`, async () => {
        const result = await checkCodeRun(apiFor([run(1), run(3, { status, conclusion: null })]), repo, sha);
        assert.equal(result.passed, false);
        assert.match(result.message, /not finished; gate will be re-run when it completes/);
    });
}

for (const conclusion of ['failure', 'cancelled', 'timed_out', 'skipped', 'neutral', 'action_required', null, 'unknown']) {
    test(`a completed code run with ${conclusion} cannot reuse older success`, async () => {
        assert.equal((await checkCodeRun(apiFor([run(1), run(3, { conclusion })]), repo, sha)).passed, false);
    });
}

test('a missing code run fails fast with the recovery message', async () => {
    const result = await checkCodeRun(apiFor([]), repo, sha);
    assert.equal(result.passed, false);
    assert.match(result.message, /not finished; gate will be re-run when it completes/);
});

test('a newer unmarked run fails closed', async () => {
    const result = await checkCodeRun(apiFor([run(1), run(2, { display_title: 'legacy' })]), repo, sha);
    assert.equal(result.passed, false);
    assert.match(result.message, /not marked/);
});

test('a rerun uses the current attempt, not the list snapshot', async () => {
    const api = apiFor([run(1)]);
    const get = api.get;
    api.get = path => path.endsWith('/1') ? run(1, { run_attempt: 2, status: 'in_progress', conclusion: null }) : get(path);
    assert.equal((await checkCodeRun(api, repo, sha)).passed, false);
});

for (const overrides of [
    { repository: { full_name: 'other/repository' } }, { workflow_id: 9 },
    { path: '.github/workflows/other.yml' }, { event: 'push' }, { head_sha: 'b'.repeat(40) }
]) {
    test(`rejects unrelated API run identity ${JSON.stringify(overrides)}`, async () => {
        await assert.rejects(checkCodeRun(apiFor([run(1, overrides)]), repo, sha), /identity/);
    });
}

test('the API follows every page and refuses a truncated result', async () => {
    const requests = [];
    const api = createApi({ repository: repo, token: 'test-only', fetchImpl: async (url, options) => {
        requests.push([url, options]);
        return { ok: true, json: async () => ({ workflow_runs: Array(100).fill(run(1)) }) };
    } });
    await assert.rejects(api.pages('/actions/workflows/pull-requests.yml/runs?head_sha=' + sha, 'workflow_runs'), /pagination bound/);
    assert.equal(requests.length, 20);
    assert.match(requests[19][0], /page=20/);
    assert.equal(requests[0][1].redirect, 'error');
});

test('the API rejects outside destinations, malformed collections and HTTP errors', async () => {
    let calls = 0;
    const api = createApi({ repository: repo, token: 'test-only', fetchImpl: async () => {
        calls++;
        return { ok: false, status: 403 };
    } });
    await assert.rejects(api.get('https://example.com/'), /path/);
    assert.equal(calls, 0);
    await assert.rejects(api.get('/actions/runs/1'), /403/);
    const malformed = createApi({ repository: repo, token: 'test-only', fetchImpl: async () => ({ ok: true, json: async () => ({}) }) });
    await assert.rejects(malformed.pages('/actions/runs', 'workflow_runs'), /collection/);
});

test('the API handles a second page, including an empty final page', async () => {
    let calls = 0;
    const api = createApi({ repository: repo, token: 'test-only', fetchImpl: async () => ({
        ok: true, json: async () => ({ workflow_runs: ++calls === 1 ? Array(100).fill(run(1)) : [] })
    }) });
    assert.equal((await api.pages('/actions/runs', 'workflow_runs')).length, 100);
    assert.equal(calls, 2);
});

test('API failures propagate instead of becoming a green gate', async () => {
    await assert.rejects(checkCodeRun({ get: async () => { throw new Error('unavailable'); } }, repo, sha), /unavailable/);
});
