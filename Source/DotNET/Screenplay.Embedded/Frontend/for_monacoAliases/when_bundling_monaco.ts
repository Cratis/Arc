// @vitest-environment node
// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { existsSync, readFileSync } from 'node:fs';
import { join } from 'node:path';
import { fileURLToPath } from 'node:url';
import { describe, expect, it } from 'vitest';
import { vendoredDomPurify } from '../../monaco.aliases';

// Monaco's export map does not expose its own files, so both packages are read from where npm installs them.
const nodeModules = fileURLToPath(new URL('../../node_modules/', import.meta.url));
const monacoRoot = join(nodeModules, 'monaco-editor');
const domPurifyRoot = join(nodeModules, 'dompurify');
const vendoredImport = /from '(\.\/dompurify\/dompurify\.js)'/;

describe('when bundling monaco', () => {
    // If Monaco moves or renames its copy, the alias would quietly stop applying and the pinned copy would ship.
    it('still finds the DOMPurify copy Monaco imports', () => {
        const sanitizer = readFileSync(join(monacoRoot, 'esm/vs/base/browser/domSanitize.js'), 'utf8');
        const specifier = sanitizer.match(vendoredImport)?.[1];

        expect(specifier).toBeDefined();
        expect((vendoredDomPurify.find as RegExp).test(specifier!)).toBe(true);
        expect(existsSync(join(monacoRoot, 'esm/vs/base/browser', specifier!))).toBe(true);
    });

    it('resolves it to a DOMPurify at least as new as the copy Monaco carries', () => {
        const vendored = readFileSync(join(monacoRoot, 'esm/vs/base/browser/dompurify/dompurify.js'), 'utf8');
        const vendoredVersion = vendored.match(/DOMPurify (\d+\.\d+\.\d+)/)![1];
        const used = (JSON.parse(readFileSync(join(domPurifyRoot, 'package.json'), 'utf8')) as { version: string }).version;
        const parts = (version: string) => version.split('.').map(Number);
        const [a, b] = [parts(used), parts(vendoredVersion)];
        const newer = a[0] - b[0] || a[1] - b[1] || a[2] - b[2];

        expect(vendoredDomPurify.replacement).toBe('dompurify');
        expect(newer).toBeGreaterThanOrEqual(0);
    });
});
