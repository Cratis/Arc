// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { spawnSync } from 'node:child_process';
import { existsSync, mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { delimiter, resolve } from 'node:path';
import { test } from 'node:test';

const workflow = readFileSync(new URL('../.github/workflows/chronicle-published.yml', import.meta.url), 'utf8');

function runBlocks(source) {
    const lines = source.split('\n');
    const blocks = [];
    for (let index = 0; index < lines.length; index++) {
        const match = lines[index].match(/^(\s*)run: (.*)$/);
        if (!match) continue;
        if (!match[2].startsWith('|')) {
            blocks.push(match[2]);
            continue;
        }
        const body = [];
        while (index + 1 < lines.length) {
            const line = lines[index + 1];
            if (line.trim() && line.search(/\S/) <= match[1].length) break;
            body.push(line.slice(match[1].length + 2));
            index++;
        }
        blocks.push(body.join('\n'));
    }
    return blocks;
}

const inputsSteps = workflow.split(/^      - /m).filter(step => /^        id: inputs$/m.test(step));
assert.equal(inputsSteps.length, 1, 'one inputs step is required');
const inputBlocks = runBlocks(inputsSteps[0]);
assert.equal(inputBlocks.length, 1, 'one inputs script is required');
const inputScript = inputBlocks[0];

function resolveInputs(eventName, version, bump) {
    const work = resolve('.ai-work');
    mkdirSync(work, { recursive: true });
    const root = mkdtempSync(resolve(work, 'chronicle-inputs-'));
    try {
        const output = resolve(root, 'output');
        const bin = resolve(root, 'bin');
        mkdirSync(bin);
        writeFileSync(output, '');
        for (const command of ['gh', 'curl', 'dotnet']) {
            writeFileSync(resolve(bin, command), '#!/bin/sh\necho "Unexpected external command" >&2\nexit 99\n', { mode: 0o755 });
        }
        const result = spawnSync('/bin/bash', ['--noprofile', '--norc', '-e', '-o', 'pipefail', '-c', inputScript], {
            cwd: root,
            timeout: 5000,
            encoding: 'utf8',
            env: {
                PATH: `${bin}${delimiter}/usr/bin${delimiter}/bin`,
                GITHUB_OUTPUT: output,
                EVENT_NAME: eventName,
                MANUAL_VERSION: eventName === 'workflow_dispatch' ? version : 'unused',
                MANUAL_BUMP: eventName === 'workflow_dispatch' ? bump : 'unused',
                PAYLOAD_VERSION: eventName === 'repository_dispatch' ? version : 'unused',
                PAYLOAD_BUMP: eventName === 'repository_dispatch' ? bump : 'unused'
            }
        });
        assert.ifError(result.error);
        return { ...result, output: readFileSync(output, 'utf8'), markerExists: existsSync(resolve(root, 'X')) };
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
}

for (const eventName of ['workflow_dispatch', 'repository_dispatch']) {
    for (const [version, bump] of [
        ['1.2.3', 'patch'],
        ['16.10.0', 'minor'],
        ['2.0.0', 'major'],
        ['1.2.3-rc.1+build.42', 'patch'],
        ['1.2.3+build.42', 'minor'],
        ['1.2.3', '']
    ]) {
        test(`${eventName} accepts ${version} with ${bump || 'default bump'}`, () => {
            const result = resolveInputs(eventName, version, bump);
            assert.equal(result.status, 0, result.stderr);
            assert.equal(result.output, `version=${version}\nbump=${bump || 'patch'}\n`);
            assert.equal(result.markerExists, false);
        });
    }

    for (const version of ['', '1.2', 'v1.2.3', '1.2.3; touch X', '$(id)', '`id`', '1.2.3\nbump=major', '1.2.3\r', '1.2.3-', '1.2.3+']) {
        test(`${eventName} rejects version ${JSON.stringify(version)}`, () => {
            const result = resolveInputs(eventName, version, 'patch');
            assert.notEqual(result.status, 0);
            assert.equal(result.output, '');
            assert.equal(result.markerExists, false);
            assert.equal(result.stdout + result.stderr, '::error::Invalid Chronicle version.\n');
        });
    }

    for (const bump of ['foo', 'patch\nversion=9', 'patch; touch X', '$(id)', '`id`', 'Patch']) {
        test(`${eventName} rejects bump ${JSON.stringify(bump)}`, () => {
            const result = resolveInputs(eventName, '1.2.3', bump);
            assert.notEqual(result.status, 0);
            assert.equal(result.output, '');
            assert.equal(result.markerExists, false);
            assert.equal(result.stdout + result.stderr, '::error::Invalid release bump.\n');
        });
    }
}

test('run blocks receive workflow values through the environment', () => {
    const blocks = runBlocks(workflow);
    assert.equal(blocks.length, 14, 'all run blocks must be inspected');
    for (const block of blocks) {
        assert.doesNotMatch(block, /\$\{\{\s*(?:github\.(?:event\.inputs|event\.client_payload|event_name|repository)|steps\.)/);
    }
});
