// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Raises central Cratis.* package pins that a restore reported as NU1109 downgrades.
//
// Usage: node scripts/raise-central-cratis-pins.mjs --log <restore.log> --props <Directory.Packages.props>
// Exit codes: 0 = at least one pin raised (restore again), 2 = nothing this script may fix, 1 = usage or I/O error.

import { readFileSync, writeFileSync } from 'node:fs';
import { parseArgs } from 'node:util';
import { pathToFileURL } from 'node:url';

const downgradeLinePattern = /Detected package downgrade:[^\r\n]*/g;
const centralDowngradePattern = /^Detected package downgrade: (\S+) from (\S+) to centrally defined (\S+?)\.?(?=\s|$)/;
const packageIdPattern = /^[A-Za-z0-9_][A-Za-z0-9_.-]*$/;
const versionPattern = /^\d+(\.\d+){1,3}(-[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?(\+[0-9A-Za-z-]+(\.[0-9A-Za-z-]+)*)?$/;

export function isVersion(value) {
    return versionPattern.test(value);
}

// Every downgrade in the log is returned. One this script cannot reason about - a non-central
// downgrade (NU1605) or a malformed package id or version - comes back with `unsupported` set.
export function parseDowngrades(log) {
    const required = new Map();
    const unsupported = [];
    for (const [line] of log.matchAll(downgradeLinePattern)) {
        const match = centralDowngradePattern.exec(line);
        if (!match || !packageIdPattern.test(match[1]) || !isVersion(match[2]) || !isVersion(match[3])) {
            unsupported.push({ name: match?.[1] ?? 'unknown', required: match?.[2] ?? 'unknown', pinned: match?.[3] ?? 'unknown', unsupported: true });
            continue;
        }
        const [, name, minimum, pinned] = match;
        const key = name.toLowerCase();
        const existing = required.get(key);
        if (!existing || compareVersions(minimum, existing.required) > 0) {
            required.set(key, { name, required: minimum, pinned });
        }
    }
    return [...required.values(), ...unsupported];
}

function splitVersion(version) {
    if (!isVersion(version)) throw new Error(`Unsupported version: ${version}`);
    const [core, ...rest] = version.split('+')[0].split('-');
    const numbers = core.split('.').map(part => {
        if (!/^\d+$/.test(part)) throw new Error(`Unsupported version: ${version}`);
        return Number(part);
    });
    // NuGet compares prerelease labels case-insensitively.
    const prerelease = rest.length ? rest.join('-').toLowerCase().split('.') : [];
    return { numbers, prerelease };
}

function comparePrereleaseIdentifier(left, right) {
    const leftNumeric = /^\d+$/.test(left);
    const rightNumeric = /^\d+$/.test(right);
    if (leftNumeric && rightNumeric) return Math.sign(Number(left) - Number(right));
    if (leftNumeric) return -1;
    if (rightNumeric) return 1;
    return left < right ? -1 : left > right ? 1 : 0;
}

export function compareVersions(left, right) {
    const a = splitVersion(left);
    const b = splitVersion(right);
    for (let index = 0; index < Math.max(a.numbers.length, b.numbers.length); index++) {
        const difference = (a.numbers[index] ?? 0) - (b.numbers[index] ?? 0);
        if (difference !== 0) return Math.sign(difference);
    }
    if (!a.prerelease.length || !b.prerelease.length) {
        return Math.sign(b.prerelease.length - a.prerelease.length);
    }
    for (let index = 0; index < Math.max(a.prerelease.length, b.prerelease.length); index++) {
        if (index >= a.prerelease.length) return -1;
        if (index >= b.prerelease.length) return 1;
        const result = comparePrereleaseIdentifier(a.prerelease[index], b.prerelease[index]);
        if (result !== 0) return result;
    }
    return 0;
}

function escapeRegExp(value) {
    return value.replace(/[.*+?^${}()|[\]\\]/g, '\\$&');
}

function pinPattern(name) {
    return new RegExp(`(<PackageVersion\\s+Include="${escapeRegExp(name)}"\\s+Version=")([^"]+)(")`, 'gi');
}

// Returns { props, raised, refused }. `refused` lists conflicts this script must not fix:
// non-Cratis packages and Cratis packages without a central pin to raise.
export function raisePins(props, downgrades) {
    const raised = [];
    const refused = [];
    let updated = props;
    for (const downgrade of downgrades) {
        if (downgrade.unsupported) {
            refused.push({ ...downgrade, reason: 'not a central downgrade this script can read' });
            continue;
        }
        if (!/^Cratis\./i.test(downgrade.name)) {
            refused.push({ ...downgrade, reason: 'not a Cratis package' });
            continue;
        }
        const matches = [...updated.matchAll(pinPattern(downgrade.name))];
        if (matches.length !== 1) {
            refused.push({ ...downgrade, reason: matches.length ? 'pinned more than once' : 'no central pin' });
            continue;
        }
        const current = matches[0][2];
        if (compareVersions(downgrade.required, current) <= 0) continue;
        updated = updated.replace(pinPattern(downgrade.name), (_, start, __, end) => `${start}${downgrade.required}${end}`);
        raised.push({ name: downgrade.name, from: current, to: downgrade.required });
    }
    return { props: updated, raised, refused };
}

function main() {
    const { values } = parseArgs({ options: { log: { type: 'string' }, props: { type: 'string' } } });
    if (!values.log || !values.props) {
        console.error('Usage: raise-central-cratis-pins.mjs --log <restore.log> --props <Directory.Packages.props>');
        return 1;
    }
    const downgrades = parseDowngrades(readFileSync(values.log, 'utf8'));
    const { props, raised, refused } = raisePins(readFileSync(values.props, 'utf8'), downgrades);
    for (const conflict of refused) {
        console.log(`Cannot raise ${conflict.name} (${conflict.reason}): requires ${conflict.required}, pinned ${conflict.pinned}.`);
    }
    if (refused.length || !raised.length) return 2;
    writeFileSync(values.props, props);
    for (const pin of raised) console.log(`Raised ${pin.name} from ${pin.from} to ${pin.to}.`);
    return 0;
}

if (import.meta.url === pathToFileURL(process.argv[1]).href) {
    try {
        process.exitCode = main();
    } catch (error) {
        console.error(error.message);
        process.exitCode = 1;
    }
}
