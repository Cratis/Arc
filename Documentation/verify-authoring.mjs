// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdir, readFile, stat } from 'node:fs/promises';
import path from 'node:path';
import { fileURLToPath } from 'node:url';

const documentationRoot = path.dirname(fileURLToPath(import.meta.url));
const validAsideVariants = new Set(['note', 'tip', 'caution', 'danger']);
const errors = [];

async function filesBelow(directory) {
    const files = [];
    for (const entry of await readdir(directory, { withFileTypes: true })) {
        const entryPath = path.join(directory, entry.name);
        if (entry.isDirectory()) {
            files.push(...await filesBelow(entryPath));
        } else if (/\.mdx?$/i.test(entry.name)) {
            files.push(entryPath);
        }
    }

    return files;
}

function validateContent(file, content) {
    const isMarkdown = path.extname(file).toLowerCase() === '.md';
    let fence;

    for (const [index, line] of content.split('\n').entries()) {
        const fenceMatch = line.match(/^\s*(`{3,}|~{3,})/);
        if (fenceMatch) {
            const marker = fenceMatch[1];
            if (!fence) {
                fence = { character: marker[0], length: marker.length };
            } else if (marker[0] === fence.character && marker.length >= fence.length) {
                fence = undefined;
            }
            continue;
        }

        if (fence) continue;

        const asideMatch = line.match(/^\s*:::(\w[\w-]*)/);
        if (asideMatch && !validAsideVariants.has(asideMatch[1])) {
            errors.push(`${relative(file)}:${index + 1}: Unknown Starlight aside variant '${asideMatch[1]}'. Use note, tip, caution, or danger.`);
        }

        if (isMarkdown && /^\s*import\s+(?:.+\s+from\s+)?['"]/.test(line)) {
            errors.push(`${relative(file)}:${index + 1}: Imports require .mdx; in .md they render as visible prose.`);
        }

        if (isMarkdown) {
            const componentMatch = line.match(/^\s*<\/?([A-Z][A-Za-z0-9.]*)\b/);
            if (componentMatch) {
                errors.push(`${relative(file)}:${index + 1}: <${componentMatch[1]}> requires .mdx; in .md it renders as an inert element.`);
            }
        }
    }
}

async function validateLandingCollisions(files) {
    for (const file of files) {
        const extension = path.extname(file);
        const possibleDirectory = file.slice(0, -extension.length);
        let directoryStats;
        try {
            directoryStats = await stat(possibleDirectory);
        } catch {
            continue;
        }

        if (!directoryStats.isDirectory()) continue;

        const entries = await readdir(possibleDirectory);
        if (entries.some(entry => /^index\.mdx?$/i.test(entry))) {
            errors.push(`${relative(file)}: Conflicts with ${relative(possibleDirectory)}/index.md[x]. The site demotes the directory index to /overview/ and can orphan it; keep one landing page for the route.`);
        }
    }
}

function relative(file) {
    return path.relative(path.dirname(documentationRoot), file).split(path.sep).join('/');
}

const files = await filesBelow(documentationRoot);
for (const file of files) {
    validateContent(file, await readFile(file, 'utf8'));
}
await validateLandingCollisions(files);

if (errors.length > 0) {
    console.error('Documentation authoring validation failed:');
    for (const error of errors) console.error(`  - ${error}`);
    process.exit(1);
}

const markdownCount = files.filter(file => path.extname(file).toLowerCase() === '.md').length;
const mdxCount = files.length - markdownCount;
console.log(`Documentation authoring validation passed for ${files.length} files (${markdownCount} .md, ${mdxCount} .mdx).`);
