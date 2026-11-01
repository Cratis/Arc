// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { execFileSync } from 'node:child_process';
import { readFileSync, readdirSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

const packageRoot = join(dirname(fileURLToPath(import.meta.url)), '..');

/** Specs and test declarations are part of the build, but they are not what consumers compile against. */
const declarationFiles = (directory: string): string[] => readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
    const path = join(directory, entry.name);
    if (entry.isDirectory()) return entry.name.startsWith('for_') ? [] : declarationFiles(path);
    return entry.name.endsWith('.d.ts') ? [path] : [];
});

describe('when shipping public declarations', () => {
    it('should not publish the test-only global declarations', () => {
        const output = execFileSync('npm', ['pack', '--dry-run', '--json', '--ignore-scripts'], { cwd: packageRoot, encoding: 'utf-8' });
        const reports = Object.values(JSON.parse(output)) as { files: { path: string }[] }[];
        const files = reports.flatMap(report => report.files.map(file => file.path));
        files.filter(file => file.endsWith('global.d.ts')).should.deep.equal([]);
    }, 60000);

    it('should not reference test packages or augment the global JSX namespace', () => {
        const offending = ['esm', 'cjs'].flatMap(format => declarationFiles(join(packageRoot, 'dist', format))
            .filter(file => /vitest\/globals|["']chai["']|sinon-chai|namespace JSX/.test(readFileSync(file, 'utf-8'))));
        offending.should.deep.equal([]);
    });

    it('should not use the global JSX namespace in published declarations', () => {
        const offending = ['esm', 'cjs'].flatMap(format => declarationFiles(join(packageRoot, 'dist', format))
            .filter(file => /(?<![.\w])JSX\./.test(readFileSync(file, 'utf-8'))));
        offending.should.deep.equal([]);
    });
});
