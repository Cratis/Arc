// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

// Reads the JSON report linkinator wrote and decides whether the link check failed.
//
// GitHub answers bursts of requests for github.com/<org>/<repo>/blob/... pages with 429 and 5xx
// responses that have nothing to do with the link. linkinator already retries them with backoff;
// when github.com still refuses after the retries, the link is reported as a warning, not a failure.
// Every other broken link fails the check, including a 404 from github.com: that one is a real
// broken link.
//
// Usage: node verify-links-results.mjs <linkinator-json-report>

import { readFileSync } from 'node:fs';

const TRANSIENT_HOSTS = new Set(['github.com', 'www.github.com']);

const isTransientGitHubRefusal = link => {
    const status = Number(link.status);
    if (status !== 429 && status < 500) {
        return false;
    }
    try {
        return TRANSIENT_HOSTS.has(new URL(link.url).hostname);
    } catch {
        return false;
    }
};

const describe = link => `  [${link.status}] ${link.url}${link.parent ? `\n        in ${link.parent}` : ''}`;

const reportPath = process.argv[2];
let report;
try {
    report = JSON.parse(readFileSync(reportPath, 'utf8'));
} catch (error) {
    console.log('✗ Link verification could not read the linkinator report.');
    console.log(`  ${error.message}`);
    console.log('  linkinator produced no scan result - the file globs most likely matched nothing.');
    process.exit(1);
}

const links = Array.isArray(report.links) ? report.links : [];
if (links.length === 0) {
    console.log('✗ Link verification scanned zero links.');
    console.log('  Nothing was checked, so this run proves nothing about the links in Documentation.');
    process.exit(1);
}

const broken = links.filter(link => link.state === 'BROKEN');
const transient = broken.filter(isTransientGitHubRefusal);
const failed = broken.filter(link => !isTransientGitHubRefusal(link));

if (transient.length > 0) {
    console.log(`⚠ ${transient.length} github.com link(s) were still refused by GitHub after retries (429/5xx).`);
    console.log('  GitHub throttles bursts of requests; these are reported as warnings, not failures:');
    transient.forEach(link => console.log(describe(link)));
    console.log('');
}

if (failed.length > 0) {
    console.log(`✗ Link verification failed - ${failed.length} broken link(s) out of ${links.length} scanned:`);
    failed.forEach(link => console.log(describe(link)));
    process.exit(1);
}

console.log(`✓ Link verification passed - ${links.length} links scanned.`);
