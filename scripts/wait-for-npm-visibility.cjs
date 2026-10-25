// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

const fs = require('node:fs');
const path = require('node:path');

function discoverPackages(root = process.cwd(), { glob = require('glob').sync, readFileSync = fs.readFileSync } = {}) {
    const rootPackage = JSON.parse(readFileSync(path.join(root, 'package.json')).toString());
    const ignore = rootPackage.workspaces.filter(_ => _.startsWith('!')).map(_ => _.substring(1));
    const packages = new Map();
    for (const workspace of rootPackage.workspaces) {
        if (workspace.startsWith('!')) continue;
        const locations = glob(path.join(workspace, '**', 'package.json'), {
            cwd: root,
            ignore: [...ignore, `**${path.sep}dist${path.sep}**`, '**/node_modules/**']
        });
        for (const location of locations) {
            const manifest = JSON.parse(readFileSync(path.resolve(root, location)).toString());
            if (manifest.private !== true) packages.set(manifest.name, manifest);
        }
    }
    return [...packages.values()];
}

async function isVisible(manifest, version, fetch, timeoutMs) {
    const registry = manifest.publishConfig?.registry ?? 'https://registry.npmjs.org';
    const url = `${registry.replace(/\/+$/, '')}/${encodeURIComponent(manifest.name)}`;
    try {
        const response = await fetch(url, {
            headers: { Accept: 'application/vnd.npm.install-v1+json' },
            signal: AbortSignal.timeout(Math.max(1, Math.ceil(timeoutMs)))
        });
        if (!response.ok) return false;
        const metadata = await response.json();
        return Object.hasOwn(metadata.versions ?? {}, version);
    } catch {
        return false;
    }
}

async function waitForVisibility(packages, version, {
    fetch = globalThis.fetch,
    now = Date.now,
    sleep = milliseconds => new Promise(resolve => setTimeout(resolve, milliseconds)),
    log = console.log,
    intervalMs = 15_000,
    deadlineMs = 75 * 60_000
} = {}) {
    if (!version) throw new Error('A release version is required.');
    if (packages.length === 0) throw new Error('No non-private workspace packages found.');
    for (const value of [intervalMs, deadlineMs]) {
        if (!Number.isFinite(value) || value <= 0) throw new Error('Polling interval and deadline must be positive milliseconds.');
    }
    const start = now();
    let pending = packages;
    while (now() - start < deadlineMs) {
        const timeoutMs = Math.min(intervalMs, deadlineMs - (now() - start));
        const visible = await Promise.all(pending.map(manifest => isVisible(manifest, version, fetch, timeoutMs)));
        pending = pending.filter((manifest, index) => {
            if (!visible[index]) return true;
            log(`${manifest.name}@${version} visible after ${((now() - start) / 1000).toFixed(1)}s`);
            return false;
        });
        if (pending.length === 0) return;
        const remaining = deadlineMs - (now() - start);
        if (remaining > 0) await sleep(Math.min(intervalMs, remaining));
    }
    throw new Error(`Deadline waiting for npm visibility of ${version}; still missing: ${pending.map(_ => _.name).join(', ')}`);
}

async function main() {
    await waitForVisibility(discoverPackages(), process.argv[2], {
        intervalMs: Number(process.env.NPM_VISIBILITY_INTERVAL_MS ?? 15_000),
        deadlineMs: Number(process.env.NPM_VISIBILITY_DEADLINE_MS ?? 75 * 60_000)
    });
}

if (require.main === module) {
    main().catch(error => {
        console.error(error.message);
        process.exitCode = 1;
    });
}

module.exports = { discoverPackages, waitForVisibility };
