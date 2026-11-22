// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { pathToFileURL } from 'node:url';

export const WORKFLOW_PATH = '.github/workflows/pull-requests.yml';
export const AGGREGATE_STEP = 'Every needed job passed or was skipped';
export const CODE_STEP = "Head commit's code run passed";
const workflowRuns = '/actions/workflows/pull-requests.yml/runs';
const validRepository = value => /^[\w.-]+\/[\w.-]+$/.test(value);
const positiveInteger = value => Number.isSafeInteger(value) && value > 0;

function apiRequest(repository, token, fetchImpl) {
    return async (path, method = 'GET') => {
        if (!/^\/(actions|pulls)(\/|\?)/.test(path) || path.includes('..') || path.includes('#')) throw new Error('Invalid API path.');
        const response = await fetchImpl(`https://api.github.com/repos/${repository}${path}`, {
            method, redirect: 'error', signal: AbortSignal.timeout(30_000),
            headers: { Authorization: `Bearer ${token}`, Accept: 'application/vnd.github+json', 'X-GitHub-Api-Version': '2022-11-28' }
        });
        if (!response.ok) throw new Error(`GitHub API ${response.status} for ${method} ${path}.`);
        return method === 'POST' ? undefined : response.json();
    };
}

function lastPage(body, key, count, batchSize) {
    if (batchSize === 100) return false;
    return key ? count >= (body.total_count ?? count) : true;
}

async function readPages(request, path, key) {
    const items = [];
    // Match the old action's 20-page bound, but never silently accept a truncated list.
    for (let page = 1; page <= 20; page++) {
        const body = await request(`${path}${path.includes('?') ? '&' : '?'}per_page=100&page=${page}`);
        const batch = key ? body[key] : body;
        if (!Array.isArray(batch)) throw new Error('Invalid GitHub API collection.');
        items.push(...batch);
        if (lastPage(body, key, items.length, batch.length)) return items;
    }
    throw new Error('GitHub API pagination bound exceeded; refusing partial evidence.');
}

export function createApi({ repository, token, fetchImpl = fetch }) {
    if (!validRepository(repository) || !token) throw new Error('Missing or invalid repository/token configuration.');
    const request = apiRequest(repository, token, fetchImpl);
    return {
        get: path => request(path),
        post: path => request(path, 'POST'),
        pages: (path, key) => readPages(request, path, key)
    };
}

export async function workflow(api) {
    const result = await api.get('/actions/workflows/pull-requests.yml');
    if (!positiveInteger(result?.id) || result.path !== WORKFLOW_PATH) throw new Error('Invalid workflow identity.');
    return result;
}

export function assertRun(run, expected) {
    const identity = {
        repository: run?.repository?.full_name, sha: run?.head_sha, workflowId: run?.workflow_id,
        event: run?.event, path: run?.path
    };
    const fields = { ...expected, event: 'pull_request', path: WORKFLOW_PATH };
    const validFields = Object.entries(fields).every(([key, value]) => identity[key] === value);
    const validNumbers = [run?.id, run?.run_attempt].every(positiveInteger);
    if (!validFields || !validNumbers) throw new Error('Invalid Pull Requests run identity.');
    return run;
}

export async function runsForHead(api, repository, sha, workflowId) {
    if (!/^[a-f0-9]{40}$/.test(sha)) throw new Error('Invalid head SHA.');
    const runs = await api.pages(`${workflowRuns}?head_sha=${sha}&event=pull_request`, 'workflow_runs');
    return runs.map(run => assertRun(run, { repository, sha, workflowId }));
}

const marked = (run, marker) => run.display_title?.endsWith(` [${marker}]`) === true;
export const isCodeRun = run => marked(run, 'code');
export const isMetaRun = run => marked(run, 'meta');

function newest(runs) {
    return [...runs].sort((left, right) => left.created_at.localeCompare(right.created_at) || left.id - right.id).at(-1);
}

// Like find_code_run in the former polling action, a newer unmarked non-meta run blocks older success.
export const latestCodeRun = runs => newest(runs.filter(run => !isMetaRun(run)));
export const latestMetaRun = runs => newest(runs.filter(isMetaRun));

export async function currentRun(api, run, expected) {
    return assertRun(await api.get(`/actions/runs/${run.id}`), expected);
}

export async function checkCodeRun(api, repository, sha) {
    const { id: workflowId } = await workflow(api);
    const selected = latestCodeRun(await runsForHead(api, repository, sha, workflowId));
    const run = selected && await currentRun(api, selected, { repository, sha, workflowId });
    if (run && !isCodeRun(run)) return { passed: false, message: `The latest run for ${sha} is not marked as a code run; push a commit or re-run the pull request workflow.` };
    if (!run || run.status !== 'completed') {
        return { passed: false, message: `code run for ${sha} not finished; gate will be re-run when it completes` };
    }
    return {
        passed: run.conclusion === 'success',
        message: `Code run ${run.id} attempt ${run.run_attempt} for ${sha} ended ${run.conclusion}; ${run.conclusion === 'success' ? 'gate can pass.' : 'fix it or re-run it.'}`
    };
}

if (process.argv[1] && import.meta.url === pathToFileURL(process.argv[1]).href) {
    try {
        const result = await checkCodeRun(createApi({ repository: process.env.GITHUB_REPOSITORY, token: process.env.GH_TOKEN }), process.env.GITHUB_REPOSITORY, process.env.HEAD_SHA);
        console.log(result.message);
        if (!result.passed) process.exitCode = 1;
    } catch (error) {
        console.error(error.message);
        process.exitCode = 1;
    }
}
