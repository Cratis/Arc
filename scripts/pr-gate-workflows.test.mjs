// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { readFileSync } from 'node:fs';
import { test } from 'node:test';
import { AGGREGATE_STEP, CODE_STEP } from './pr-gate-context.mjs';

const read = path => readFileSync(new URL(`../${path}`, import.meta.url), 'utf8');
const pull = read('.github/workflows/pull-requests.yml');
const handler = read('.github/workflows/reconcile-pr-gate.yml');
const lint = read('.github/workflows/workflow-lint.yml');
const gate = pull.slice(pull.indexOf('\n  gate:'));
const metaCondition = "contains(fromJSON('[\"edited\",\"labeled\",\"unlabeled\"]'), github.event.action)";

test('the existing Actions gate remains the sole required check producer', () => {
    assert.equal((pull.match(/^    name: gate$/gm) || []).length, 1);
    assert.match(gate, /if: \$\{\{ always\(\) \}\}/);
    assert.match(gate, /timeout-minutes: 5/);
    assert.doesNotMatch(pull + handler, /pr-code-run-gate|sleep |checks: write|pull_request_target|95/);
    assert.match(gate, /run: node scripts\/pr-gate-context.mjs/);
    assert.match(gate, /actions: read/);
    assert.ok(gate.includes(`- name: ${AGGREGATE_STEP}`));
    assert.ok(gate.includes(`- name: ${CODE_STEP}`));
    assert.equal((gate.split(metaCondition).length - 1), 3);
});

test('the gate still covers every validation and requires changes to succeed', () => {
    const jobs = [...pull.slice(pull.indexOf('\njobs:')).matchAll(/^  ([\w-]+):$/gm)].map(match => match[1]).filter(name => name !== 'gate');
    const needs = gate.match(/needs: \[([^\]]+)\]/)[1].split(',').map(name => name.trim());
    assert.deepEqual(needs.sort(), jobs.sort());
    assert.equal(jobs.length, 9);
    assert.ok(gate.includes(".changes.result"));
    assert.ok(gate.includes('IN("success","skipped")'));
    for (const job of ['aot-ratchet', 'dotnet-build', 'javascript-build', 'markdown-verification', 'package-graph', 'prerelease', 'workflow-lint']) {
        const block = pull.split(`\n  ${job}:\n`)[1].split(/^  [\w-]+:/m)[0];
        assert.ok(block.includes(`!${metaCondition}`), job);
    }
    assert.match(pull, /cancel-in-progress: true/);
    assert.ok(pull.includes("&& '-meta' || ''"));
});

test('the reconciler has only completion events, trusted source and minimal permissions', () => {
    assert.match(handler, /workflow_run:[^\n]*\n    workflows: \[Pull Requests\]\n    types: \[completed\]/);
    assert.match(handler, /ref: \$\{\{ github.workflow_sha \}\}/);
    assert.match(handler, /persist-credentials: false/);
    assert.match(handler, /permissions: \{\}/);
    const permissions = handler.match(/^    permissions:\n((?:      .+\n)+)/m)[1].trim().split('\n').map(line => line.trim()).sort();
    assert.deepEqual(permissions, ['actions: write', 'contents: read']);
    assert.match(handler, /timeout-minutes: 5/);
    assert.match(handler, /group: pr-gate-reconcile-\$\{\{ github.event.workflow_run.head_sha \}\}/);
    assert.match(handler, /cancel-in-progress: false/);
    assert.match(handler, /run: node scripts\/pr-gate-reconcile.mjs/);
    assert.doesNotMatch(handler, /pull_request_target|workflow_dispatch|head_sha.*\n.*ref:|secrets: inherit|download-artifact|\n\s+cache:|npm |yarn |checks:|github-script/);
    const uses = [...handler.matchAll(/uses: (\S+)/g)].map(match => match[1]);
    assert.equal(uses.length, 2);
    for (const use of uses) assert.match(use, /@[a-f0-9]{40}$/);
});

test('helper-only edits select workflow lint and CI runs all five Node spec files', () => {
    assert.ok(pull.includes('- "scripts/pr-gate-*.mjs"'));
    const expected = 'node --test scripts/chronicle-published-inputs.test.mjs scripts/raise-central-cratis-pins.test.mjs scripts/pr-gate-context.test.mjs scripts/pr-gate-reconcile.test.mjs scripts/pr-gate-workflows.test.mjs';
    assert.ok(lint.includes(`run: ${expected}`));
});
