// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import assert from 'node:assert/strict';
import { mkdirSync, mkdtempSync, readFileSync, rmSync, writeFileSync } from 'node:fs';
import { resolve } from 'node:path';
import { spawnSync } from 'node:child_process';
import test from 'node:test';

const checker = resolve('scripts/published-arccore-outcomes.mjs');
const issue = 'https://github.com/Cratis/Arc/issues/3595';

function trx(results = [['one', 'Passed'], ['two', 'Passed']], overrides = {}) {
    const counters = {
        total: results.length,
        executed: results.filter(([, outcome]) => ['Passed', 'Failed'].includes(outcome)).length,
        passed: results.filter(([, outcome]) => outcome === 'Passed').length,
        failed: results.filter(([, outcome]) => outcome === 'Failed').length,
        notExecuted: results.filter(([, outcome]) => !['Passed', 'Failed'].includes(outcome)).length,
        ...overrides
    };
    return `<?xml version="1.0"?><TestRun xmlns="http://microsoft.com/schemas/VisualStudio/TeamTest/2010"><Results>${results.map(([name, outcome]) => `<UnitTestResult testName="${name}" outcome="${outcome}"/>`).join('')}</Results><ResultSummary><Counters ${Object.entries(counters).map(([key, value]) => `${key}="${value}"`).join(' ')}/></ResultSummary></TestRun>`;
}

function check(options = {}) {
    const { xml = trx(), expectations = {}, baseExpectations, variant = 'NativeAot', facts = 2, summary = false, args = [] } = options;
    mkdirSync('.ai-work', { recursive: true });
    const root = mkdtempSync(resolve('.ai-work/published-outcomes-'));
    try {
        const input = resolve(root, 'acceptance.trx');
        const manifest = resolve(root, 'manifest.json');
        const summaryPath = resolve(root, 'summary.md');
        if (xml !== null) writeFileSync(input, xml);
        writeFileSync(manifest, JSON.stringify({ facts, variants: { Untrimmed: { expectedFailures: expectations }, NativeAot: { expectedFailures: expectations } } }));
        const baseArgs = [];
        if (baseExpectations !== undefined) {
            const baseManifest = resolve(root, 'base-manifest.json');
            if (baseExpectations !== null) writeFileSync(baseManifest, JSON.stringify({ facts, variants: { NativeAot: { expectedFailures: baseExpectations } } }));
            baseArgs.push('--base-manifest', baseManifest);
        }
        const env = { ...process.env };
        delete env.GITHUB_STEP_SUMMARY;
        if (summary) {
            writeFileSync(summaryPath, 'Prior summary\n\n');
            env.GITHUB_STEP_SUMMARY = summaryPath;
        }
        const result = spawnSync(process.execPath, [checker, '--trx', input, '--variant', variant, '--manifest', manifest, ...baseArgs, ...args], { encoding: 'utf8', env });
        assert.ifError(result.error);
        return { status: result.status, output: result.stdout + result.stderr, summary: summary ? readFileSync(summaryPath, 'utf8') : '' };
    } finally {
        rmSync(root, { recursive: true, force: true });
    }
}

test('all facts pass without expectations', () => {
    const result = check();
    assert.equal(result.status, 0, result.output);
    assert.match(result.output, /2 passed/);
});

test('listed failures match exactly', () => {
    const result = check({ xml: trx([['one', 'Failed'], ['two', 'Passed']]), expectations: { one: issue } });
    assert.equal(result.status, 0, result.output);
    assert.match(result.output, /1 failed as expected/);
});

test('expanding expected failures beyond the base manifest is rejected', () => {
    const result = check({ xml: trx([['one', 'Failed'], ['two', 'Failed']]), expectations: { one: issue, two: issue }, baseExpectations: { one: issue } });
    assert.equal(result.status, 1, result.output);
    assert.match(result.output, /Expected failure not present in base manifest: two/);
});

test('removing expected failures from the base manifest is allowed', () => {
    const result = check({ xml: trx([['one', 'Failed'], ['two', 'Passed']]), expectations: { one: issue }, baseExpectations: { one: issue, two: issue } });
    assert.equal(result.status, 0, result.output);
});

test('unchanged expected failures in the base manifest are allowed', () => {
    const result = check({ xml: trx([['one', 'Failed'], ['two', 'Passed']]), expectations: { one: issue }, baseExpectations: { one: issue } });
    assert.equal(result.status, 0, result.output);
});

test('a missing base manifest skips the comparison for first introduction', () => {
    const result = check({ xml: trx([['one', 'Failed'], ['two', 'Passed']]), expectations: { one: issue }, baseExpectations: null });
    assert.equal(result.status, 0, result.output);
});

test('an invalid base manifest cannot silently skip the comparison', () => {
    assert.equal(check({ baseExpectations: [] }).status, 2);
});

test('an unlisted failure names the fact', () => {
    const result = check({ xml: trx([['one', 'Failed'], ['two', 'Passed']]) });
    assert.equal(result.status, 1);
    assert.match(result.output, /Unexpected failure: one/);
});

test('a listed fact that passes must be removed', () => {
    const result = check({ expectations: { one: issue } });
    assert.equal(result.status, 1);
    assert.match(result.output, /one.*remove it from the manifest/);
});

for (const outcome of ['Skipped', 'NotExecuted', 'Inconclusive']) {
    test(`${outcome} is not accepted`, () => {
        const result = check({ xml: trx([['one', outcome], ['two', 'Passed']]), expectations: { one: issue } });
        assert.equal(result.status, 1);
        assert.match(result.output, /one/);
    });
}

test('the fact count is held by the manifest', () => {
    assert.equal(check({ facts: 3 }).status, 1);
});

test('counters must agree with individual results', () => {
    assert.equal(check({ xml: trx(undefined, { passed: 1, failed: 1 }) }).status, 1);
});

test('Untrimmed cannot carry expected failures', () => {
    const result = check({ variant: 'Untrimmed', xml: trx([['one', 'Failed'], ['two', 'Passed']]), expectations: { one: issue } });
    assert.equal(result.status, 1);
    assert.match(result.output, /Untrimmed/);
});

for (const url of ['', 'https://example.org/issues/1', 'https://github.com/Cratis/Arc/pull/3595', 'https://github.com/Cratis/Arc/issues/0']) {
    test(`an invalid issue URL is rejected: ${url}`, () => {
        assert.equal(check({ xml: trx([['one', 'Failed'], ['two', 'Passed']]), expectations: { one: url } }).status, 1);
    });
}

test('an expectation cannot silently disappear from the results', () => {
    assert.equal(check({ expectations: { missing: issue } }).status, 1);
});

test('duplicate result names cannot substitute for missing facts', () => {
    assert.equal(check({ xml: trx([['one', 'Passed'], ['one', 'Passed']]) }).status, 1);
});

test('namespaced elements and XML-encoded names are supported', () => {
    const xml = trx([['one &amp; &quot;two&quot;', 'Failed'], ['three', 'Passed']]).replace(/(<\/?)(TestRun|Results|UnitTestResult|ResultSummary|Counters)\b/g, '$1t:$2');
    assert.equal(check({ xml, expectations: { 'one & "two"': issue } }).status, 0);
});

for (const xml of [null, '', '<TestRun>', '<TestRun><Counters></TestRun>', trx().replace('<Counters ', '<Counters broken '), trx().replace('testName="one"', 'testName="one &bad;"'), '<TestRun/>', `${trx()}<TestRun/>`, `${trx()}garbage`, trx().replace('passed="2"', 'passed="invalid"')]) {
    test(`missing or malformed TRX cannot run: ${String(xml).slice(0, 35)}`, () => {
        assert.equal(check({ xml }).status, 2);
    });
}

test('an unknown variant cannot run', () => {
    assert.equal(check({ variant: 'Trimmed' }).status, 2);
});

test('unknown arguments cannot run', () => {
    assert.equal(check({ args: ['--unknown'] }).status, 2);
});

test('the step summary is appended without replacing prior content', () => {
    const result = check({ summary: true });
    assert.equal(result.status, 0, result.output);
    assert.match(result.summary, /^Prior summary\n\n/);
    assert.match(result.summary, /NativeAot/);
    assert.match(result.summary, /2 passed/);
});
