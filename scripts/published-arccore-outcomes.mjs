// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { appendFileSync, readFileSync } from 'node:fs';
import { parseArgs } from 'node:util';

function decodeXml(value) {
    const entities = { amp: '&', quot: '"', apos: "'", lt: '<', gt: '>' };
    return value.replace(/&([^;\s]*);|&/g, (entity, name) => {
        if (Object.hasOwn(entities, name)) return entities[name];
        if (/^#(?:[0-9]+|x[0-9a-fA-F]+)$/.test(name)) {
            const code = name.startsWith('#x') ? Number.parseInt(name.slice(2), 16) : Number(name.slice(1));
            if (isXmlCodePoint(code)) return String.fromCodePoint(code);
        }
        throw new Error(`Invalid XML entity: ${entity}`);
    });
}

function isXmlCodePoint(code) {
    if (code >= 0xd800 && code <= 0xdfff) return false;
    return code > 0 && code <= 0x10ffff;
}

function attributes(text) {
    const values = Object.create(null);
    const pattern = /\s+([\w:.-]+)\s*=\s*(?:"([^"<]*)"|'([^'<]*)')/y;
    let position = 0;
    while (position < text.length && text.slice(position).trim()) {
        pattern.lastIndex = position;
        const match = pattern.exec(text);
        if (!match || Object.hasOwn(values, match[1])) throw new Error('Invalid or duplicate XML attribute');
        values[match[1]] = decodeXml(match[2] ?? match[3]);
        position = pattern.lastIndex;
    }
    return values;
}

function closeElement(name, body, selfClosing, stack) {
    if (body.trim() || selfClosing) throw new Error('Invalid TRX XML closing element');
    if (stack.pop() !== name) throw new Error('Mismatched TRX XML element');
}

function validateRoot(state, localName) {
    if (state.stack.length) return;
    state.roots++;
    if (state.roots !== 1 || localName !== 'TestRun') throw new Error('Expected one TRX TestRun');
}

function visitElement(token, state, visit) {
    const [, closing, name, body, selfClosing] = /^<(\/?)([\w:.-]+)([\s\S]*?)(\/?)>$/.exec(token);
    if (closing) return closeElement(name, body, selfClosing, state.stack);
    const localName = name.split(':').at(-1);
    validateRoot(state, localName);
    visit(localName, attributes(body));
    if (!selfClosing) state.stack.push(name);
}

function visitToken(token, state, visit) {
    if (/^(?:<\?xml\b|<!--)/.test(token)) return;
    if (token.startsWith('<![CDATA[')) {
        if (!state.stack.length) throw new Error('CDATA outside TRX root');
        return;
    }
    if (token.startsWith('<')) return visitElement(token, state, visit);
    if (!state.stack.length && token.trim()) throw new Error('Text outside TRX root');
    decodeXml(token);
}

function visitXml(xml, visit) {
    const tokens = /<\?xml\b[^?]*\?>|<!--[\s\S]*?-->|<!\[CDATA\[[\s\S]*?\]\]>|<\/?[\w:.-]+(?:\s+(?:[^<>"']|"[^"]*"|'[^']*')*)?\s*\/?>|[^<]+/g;
    const state = { stack: [], roots: 0 };
    let position = 0;
    for (const match of xml.matchAll(tokens)) {
        if (match.index !== position) throw new Error('Malformed TRX XML');
        position += match[0].length;
        visitToken(match[0], state, visit);
    }
    if (position !== xml.length) throw new Error('Malformed TRX XML');
    if (state.stack.length || state.roots !== 1) throw new Error('Incomplete TRX XML');
}

function collectTrxElement(name, attrs, trx) {
    if (name === 'Counters') {
        if (trx.counters) throw new Error('Duplicate TRX Counters');
        trx.counters = attrs;
        return;
    }
    if (name !== 'UnitTestResult') return;
    if (!attrs.testName || !attrs.outcome) throw new Error('TRX result lacks testName or outcome');
    trx.results.push({ name: attrs.testName, outcome: attrs.outcome });
}

function readTrx(path) {
    const trx = { counters: null, results: [] };
    visitXml(readFileSync(path, 'utf8').replace(/^\uFEFF/, ''), (name, attrs) => collectTrxElement(name, attrs, trx));
    if (!trx.counters) throw new Error('Missing TRX Counters');
    for (const key of ['total', 'executed', 'passed', 'failed', 'notExecuted']) {
        if (!/^\d+$/.test(trx.counters[key])) throw new Error(`Missing or invalid TRX counter: ${key}`);
        trx.counters[key] = Number(trx.counters[key]);
    }
    return trx;
}

function readExpectations(path, variant) {
    const manifest = JSON.parse(readFileSync(path, 'utf8'));
    if (!Number.isSafeInteger(manifest.facts) || manifest.facts <= 0) throw new Error('Manifest facts must be a positive integer');
    if (!Object.hasOwn(manifest.variants ?? {}, variant)) throw new Error(`Unknown variant: ${variant}`);
    const expected = manifest.variants[variant]?.expectedFailures;
    if (!isRecord(expected)) throw new Error('Manifest expectedFailures must be an object');
    return { facts: manifest.facts, expected };
}

function isRecord(value) {
    return value !== null && typeof value === 'object' && !Array.isArray(value);
}

function validateExpectations(variant, expected, results) {
    const defects = [];
    if (variant === 'Untrimmed' && Object.keys(expected).length) defects.push('Untrimmed must not carry expected failures');
    for (const [name, issue] of Object.entries(expected)) {
        if (typeof issue !== 'string' || !/^https:\/\/github\.com\/[\w.-]+\/[\w.-]+\/issues\/[1-9]\d*$/.test(issue)) defects.push(`Invalid tracking issue for ${name}`);
        if (!results.some(result => result.name === name)) defects.push(`Expected failure missing from TRX: ${name}`);
    }
    return defects;
}

function checkResult({ name, outcome }, expected) {
    if (outcome === 'Passed') {
        console.log(`Passed: ${name}`);
        return Object.hasOwn(expected, name) ? [`Unexpected pass: ${name}; remove it from the manifest`] : [];
    }
    if (outcome !== 'Failed') return [`Unexpected outcome: ${name}: ${outcome} (skips are not allowed)`];
    if (!Object.hasOwn(expected, name)) return [`Unexpected failure: ${name}`];
    console.log(`Failed as expected: ${name} (${expected[name]})`);
    return [];
}

function classifyResults(results, expected) {
    const defects = [];
    const names = new Set();
    for (const result of results) {
        if (names.has(result.name)) defects.push(`Duplicate fact: ${result.name}`);
        names.add(result.name);
        defects.push(...checkResult(result, expected));
    }
    const passed = results.filter(result => result.outcome === 'Passed').length;
    const failures = results.filter(result => result.outcome === 'Failed');
    const expectedFailures = failures.filter(result => Object.hasOwn(expected, result.name)).length;
    return { passed, failed: failures.length, expectedFailures, defects };
}

function validateCounters(counters, results, facts, { passed, failed }) {
    const expected = { total: results.length, executed: passed + failed, passed, failed, notExecuted: results.length - passed - failed };
    const defects = [];
    for (const [key, value] of Object.entries(expected)) {
        if (counters[key] !== value) defects.push(`TRX counter ${key}=${counters[key]} disagrees with results (${value})`);
    }
    if (results.length !== facts || counters.total !== facts) defects.push(`Expected ${facts} facts; found ${results.length} results and total=${counters.total}`);
    if (counters.executed !== facts || counters.notExecuted !== 0) defects.push(`Expected ${facts} executed facts without skips`);
    return defects;
}

function main() {
    const { values } = parseArgs({ options: { trx: { type: 'string' }, variant: { type: 'string' }, manifest: { type: 'string', default: 'Samples/ArcCore.Specs/published-outcomes.json' } } });
    if (!values.trx || !values.variant) throw new Error('Usage: node scripts/published-arccore-outcomes.mjs --trx <file> --variant <name> [--manifest <file>]');
    const { facts, expected } = readExpectations(values.manifest, values.variant);
    const { counters, results } = readTrx(values.trx);
    const classified = classifyResults(results, expected);
    const defects = [
        ...validateExpectations(values.variant, expected, results),
        ...classified.defects,
        ...validateCounters(counters, results, facts, classified)
    ];
    const summary = `${values.variant}: ${results.length} facts, ${classified.passed} passed, ${classified.expectedFailures} failed as expected, ${defects.length} defects`;
    console.log(summary);
    for (const defect of defects) console.error(defect);
    if (process.env.GITHUB_STEP_SUMMARY) appendFileSync(process.env.GITHUB_STEP_SUMMARY, `### Published ArcCore acceptance\n\n${summary}\n\n`);
    return defects.length ? 1 : 0;
}

try {
    process.exitCode = main();
} catch (error) {
    console.error(`Could not check published ArcCore outcomes: ${error.message}`);
    process.exitCode = 2;
}
