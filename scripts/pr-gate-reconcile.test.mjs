// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { test } from 'node:test';
import { AGGREGATE_STEP, CODE_STEP, WORKFLOW_PATH, checkCodeRun } from './pr-gate-context.mjs';
import { reconcile } from './pr-gate-reconcile.mjs';

const repository = 'Cratis/Arc';
const sha = 'a'.repeat(40);

function fixture() {
    const code = {
        id: 10, run_number: 10, run_attempt: 1, workflow_id: 7, path: WORKFLOW_PATH,
        repository: { full_name: repository }, head_repository: { full_name: repository }, head_branch: 'feature',
        head_sha: sha, event: 'pull_request', display_title: 'PR [code]', status: 'completed', conclusion: 'success',
        created_at: '2026-11-22T12:00:00Z', pull_requests: []
    };
    const meta = { ...code, id: 20, run_number: 20, display_title: 'PR [meta]', conclusion: 'failure', created_at: '2026-11-22T12:01:00Z' };
    const gate = {
        id: 200, name: 'gate', run_id: 20, run_attempt: 1, status: 'completed', conclusion: 'failure',
        steps: [{ name: AGGREGATE_STEP, conclusion: 'success' }, { name: CODE_STEP, conclusion: 'failure' }]
    };
    const state = {
        code, meta, gate, runs: [meta, code], jobs: [gate],
        pulls: [{ state: 'open', head: { sha, repo: { full_name: repository } } }], posts: [], reads: [],
        event: { repository: { full_name: repository }, workflow_run: { ...code } }
    };
    state.api = {
        get: async path => {
            state.reads.push(path);
            if (path.endsWith('/pull-requests.yml')) return { id: 7, path: WORKFLOW_PATH };
            return state.runs.find(run => path.endsWith(`/runs/${run.id}`));
        },
        pages: async (path, key) => {
            state.reads.push(path);
            if (key === 'workflow_runs') return state.runs;
            if (key === 'jobs') return state.jobs;
            return state.pulls;
        },
        post: async path => {
            state.posts.push(path);
            state.meta.status = 'queued';
        }
    };
    return state;
}

const execute = state => reconcile(state.api, repository, state.event);

test('metadata-first: a successful code completion reruns only the gate', async () => {
    const state = fixture();
    assert.match(await execute(state), /Re-ran/);
    assert.deepEqual(state.posts, ['/actions/jobs/200/rerun']);
    assert.ok(state.reads.includes('/actions/runs/20/attempts/1/jobs'));
});

test('code-first: metadata passes its one-shot check without a rerun', async () => {
    const state = fixture();
    assert.equal((await checkCodeRun(state.api, repository, sha)).passed, true);
    state.gate.conclusion = 'success';
    state.meta.conclusion = 'success';
    state.event.workflow_run = { ...state.meta };
    await execute(state);
    assert.equal(state.posts.length, 0);
});

test('code completing just before the metadata failure is repaired by metadata completion', async () => {
    const state = fixture();
    state.meta.status = 'in_progress';
    await execute(state);
    assert.equal(state.posts.length, 0);
    state.meta.status = 'completed';
    state.event.workflow_run = { ...state.meta };
    await execute(state);
    assert.equal(state.posts.length, 1);
});

test('duplicate completion deliveries do not rerun a queued attempt again', async () => {
    const state = fixture();
    await execute(state);
    await execute(state);
    assert.equal(state.posts.length, 1);
});

test('the latest metadata revision supersedes earlier failed metadata', async () => {
    const state = fixture();
    const older = { ...state.meta, id: 15, created_at: '2026-11-22T12:00:30Z' };
    state.runs.push(older);
    await execute(state);
    assert.deepEqual(state.posts, ['/actions/jobs/200/rerun']);
});

test('a delayed superseded metadata event does not repair an obsolete run', async () => {
    const state = fixture();
    const older = { ...state.meta, id: 15, created_at: '2026-11-22T12:00:30Z' };
    state.runs.push(older);
    state.event.workflow_run = older;
    await execute(state);
    assert.equal(state.posts.length, 0);
});

for (const change of [
    state => { state.pulls[0].head.sha = 'b'.repeat(40); },
    state => { state.pulls = []; },
    state => { state.pulls[0].head.repo.full_name = 'other/Arc'; }
]) {
    test('an old head, a closed PR or a different head repository is ignored', async () => {
        const state = fixture();
        change(state);
        await execute(state);
        assert.equal(state.posts.length, 0);
    });
}

test('empty run PR associations work for an open fork PR', async () => {
    const state = fixture();
    state.code.head_repository = { full_name: 'contributor/Arc' };
    state.meta.head_repository = { full_name: 'contributor/Arc' };
    state.pulls[0].head.repo.full_name = 'contributor/Arc';
    await execute(state);
    assert.equal(state.posts.length, 1);
});

for (const change of [
    state => { state.code.run_attempt = 2; },
    state => { state.code.status = 'in_progress'; state.code.conclusion = null; },
    state => { state.runs.push({ ...state.code, id: 30, created_at: '2026-11-22T12:02:00Z', status: 'in_progress', conclusion: null }); },
    state => { state.code.conclusion = 'failure'; },
    state => { state.code.conclusion = 'cancelled'; },
    state => { state.code.display_title = 'legacy'; },
    state => { state.runs = [state.code]; }
]) {
    test('stale, pending, failed, cancelled, unmarked or missing evidence never reruns metadata', async () => {
        const state = fixture();
        change(state);
        await execute(state);
        assert.equal(state.posts.length, 0);
    });
}

test('a stale metadata attempt is ignored', async () => {
    const state = fixture();
    state.event.workflow_run = { ...state.meta };
    state.meta.run_attempt = 2;
    await execute(state);
    assert.equal(state.posts.length, 0);
});

for (const change of [
    state => { state.gate.steps[0].conclusion = 'failure'; },
    state => { state.gate.steps[1].conclusion = 'skipped'; },
    state => { state.gate.conclusion = 'cancelled'; },
    state => { state.gate.conclusion = 'skipped'; },
    state => { state.gate.steps = []; },
    state => { state.jobs = []; },
    state => { state.meta.run_attempt = 2; }
]) {
    test('only a gate that failed the code lookup with passing own dependencies can be rerun', async () => {
        const state = fixture();
        change(state);
        await execute(state);
        assert.equal(state.posts.length, 0);
    });
}

test('partial reruns accept only current-attempt gate evidence', async () => {
    const state = fixture();
    state.meta.run_attempt = 2;
    state.gate.run_attempt = 2;
    await execute(state);
    assert.equal(state.posts.length, 1);
});

for (const change of [
    state => { state.pulls[0].head.sha = 'b'.repeat(40); },
    state => { state.code.run_attempt++; },
    state => { state.meta.run_attempt++; },
    state => { state.runs.push({ ...state.meta, id: 25, created_at: '2026-11-22T12:02:00Z' }); }
]) {
    test('revalidates the current head and latest attempts immediately before the rerun', async () => {
        const state = fixture();
        const pages = state.api.pages;
        state.api.pages = async (path, key) => {
            const result = await pages(path, key);
            if (key === 'jobs') change(state);
            return result;
        };
        await execute(state);
        assert.equal(state.posts.length, 0);
    });
}

test('rejects duplicate gate identities rather than guessing a job', async () => {
    const state = fixture();
    state.jobs.push({ ...state.gate, id: 201 });
    await assert.rejects(execute(state), /Multiple gate/);
    assert.equal(state.posts.length, 0);
});

test('API/permission errors are visible and do not trigger fallback writes', async () => {
    const state = fixture();
    state.api.post = async () => { throw new Error('API 403'); };
    await assert.rejects(execute(state), /403/);
});

test('rejects an unrelated workflow or repository', async () => {
    const state = fixture();
    state.code.workflow_id = 99;
    await assert.rejects(execute(state), /identity/);
    state.event.repository.full_name = 'other/Arc';
    await assert.rejects(execute(state), /repository/);
});
