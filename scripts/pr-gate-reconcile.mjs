// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { pathToFileURL } from 'node:url';
import {
    AGGREGATE_STEP, CODE_STEP, assertRun, createApi, currentRun, isCodeRun, isMetaRun,
    latestCodeRun, latestMetaRun, runsForHead, workflow
} from './pr-gate-context.mjs';

const MAX_META_ATTEMPTS = 3;
const sameAttempt = (left, right) => left?.id === right?.id && left?.run_attempt === right?.run_attempt;
const successful = run => isCodeRun(run) && run.status === 'completed' && run.conclusion === 'success';

function matchesHead(pull, run) {
    return [pull.state === 'open', pull.head?.sha === run.head_sha, pull.head?.repo?.full_name === run.head_repository.full_name].every(Boolean);
}

const isValidRepository = repository => /^[\w.-]+\/[\w.-]+$/.test(repository);

function assertHeadReference(run) {
    if (!isValidRepository(run.head_repository?.full_name) || !run.head_branch) throw new Error('Invalid head repository/branch.');
}

async function hasCurrentPullRequest(api, run) {
    assertHeadReference(run);
    const repository = run.head_repository.full_name;
    // workflow_run.pull_requests may be empty, including for forks. Read current PR heads instead.
    const head = encodeURIComponent(`${repository.split('/')[0]}:${run.head_branch}`);
    const pulls = await api.pages(`/pulls?state=open&head=${head}`);
    return pulls.some(pull => matchesHead(pull, run));
}

function singleGate(jobs) {
    const gates = jobs.filter(job => job.name === 'gate');
    if (gates.length > 1) throw new Error('Multiple gate jobs in the current attempt.');
    return gates[0];
}

const isCurrentFailure = (gate, meta) =>
    [gate.run_id === meta.id, gate.run_attempt === meta.run_attempt, gate.status === 'completed', gate.conclusion === 'failure'].every(Boolean);

const stepHas = (gate, name, conclusion) => gate.steps?.some(step => step.name === name && step.conclusion === conclusion);

const failedAtCodeLookup = gate => stepHas(gate, AGGREGATE_STEP, 'success') && stepHas(gate, CODE_STEP, 'failure');

function failedCodeLookup(jobs, meta) {
    const gate = singleGate(jobs);
    if (!gate || !isCurrentFailure(gate, meta)) return undefined;
    return failedAtCodeLookup(gate) ? gate : undefined;
}

const isUsableSource = (source, hint) =>
    sameAttempt(source, hint) && source.status === 'completed' && (isMetaRun(source) || successful(source));

async function completedSource(api, repository, event, expected) {
    if (event.repository?.full_name !== repository) throw new Error('Unexpected event repository.');
    const hint = assertRun(event.workflow_run, expected);
    const source = await currentRun(api, hint, expected);
    return isUsableSource(source, hint) ? source : undefined;
}

async function currentPair(api, expected) {
    const { repository, sha, workflowId } = expected;
    const runs = await runsForHead(api, repository, sha, workflowId);
    const candidates = [latestCodeRun(runs), latestMetaRun(runs)];
    if (candidates.some(run => !run)) return undefined;
    // Copy attempt identities: later API reads must not silently move the selected attempts.
    const pair = await Promise.all(candidates.map(async run => ({ ...await currentRun(api, run, expected) })));
    if (!pair.every((run, index) => sameAttempt(run, candidates[index]))) return undefined;
    return { code: pair[0], meta: pair[1] };
}

function ready(pair) {
    if (!pair) return false;
    return successful(pair.code) && pair.meta.status === 'completed';
}

function unchanged(before, after) {
    if (!ready(after)) return false;
    return sameAttempt(before.code, after.code) && sameAttempt(before.meta, after.meta);
}

async function rerunGate(api, expected, pair, gate) {
    // A newer metadata revision, head or rerun supersedes the selected pair. Never poll or fall back.
    const fresh = await currentPair(api, expected);
    if (!unchanged(pair, fresh)) return 'Inputs changed before rerun.';
    if (!await hasCurrentPullRequest(api, fresh.code)) return 'Pull request head changed before rerun.';
    await api.post(`/actions/jobs/${gate.id}/rerun`);
    return `Re-ran gate job ${gate.id} for metadata run ${pair.meta.id} attempt ${pair.meta.run_attempt}; code run ${pair.code.id} attempt ${pair.code.run_attempt} succeeded for ${expected.sha}.`;
}

export async function reconcile(api, repository, event) {
    const { id: workflowId } = await workflow(api);
    const expected = { repository, sha: event.workflow_run?.head_sha, workflowId };
    if (!await completedSource(api, repository, event, expected)) return 'Ignored stale, unfinished or unsuccessful completion.';
    // Completion events are wake-ups: coalescing can retain an older run's event.
    const pair = await currentPair(api, expected);
    if (!ready(pair)) return 'Current code/metadata attempts are not ready.';
    if (pair.meta.run_attempt >= MAX_META_ATTEMPTS) return `Skipped metadata run ${pair.meta.id} attempt ${pair.meta.run_attempt}: reached the rerun attempt cap of ${MAX_META_ATTEMPTS}.`;
    if (!await hasCurrentPullRequest(api, pair.code)) return 'No open pull request still uses this head.';
    const jobs = await api.pages(`/actions/runs/${pair.meta.id}/attempts/${pair.meta.run_attempt}/jobs`, 'jobs');
    const gate = failedCodeLookup(jobs, pair.meta);
    if (!gate) return 'Latest metadata gate does not need a code-lookup rerun.';
    return rerunGate(api, expected, pair, gate);
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    try {
        if (process.env.GITHUB_EVENT_NAME !== 'workflow_run') throw new Error('Expected a workflow_run event.');
        const event = JSON.parse(readFileSync(process.env.GITHUB_EVENT_PATH, 'utf8'));
        console.log(await reconcile(createApi({ repository: process.env.GITHUB_REPOSITORY, token: process.env.GH_TOKEN }), process.env.GITHUB_REPOSITORY, event));
    } catch (error) {
        console.error(error.message);
        process.exitCode = 1;
    }
}
