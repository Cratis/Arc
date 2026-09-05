// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { rollup } from '../../../rollup.config.mjs';

import pkg from './package.json' with { type: 'json' };

import { copyFileSync, existsSync, mkdirSync, readdirSync } from 'fs';
import path from "path";

const cjsPath = path.dirname(pkg.main);
const esmPath = path.dirname(pkg.module);
const tsconfigPath = path.join(import.meta.dirname, "tsconfig.json");

/** Directories that hold stylesheets belonging to this repository rather than to the package. */
const notPartOfThePackage = ['node_modules', 'dist', '.storybook', 'storybook-static'];

/**
 * Collects every stylesheet that belongs to the package, relative to its root.
 * @param {string} directory Directory to walk.
 * @param {string} prefix Path of that directory relative to the package root.
 * @returns {string[]} Package-relative paths of the stylesheets found.
 */
function collectStylesheets(directory, prefix = '') {
    return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
        const relativePath = prefix ? `${prefix}/${entry.name}` : entry.name;
        if (entry.isDirectory()) {
            return notPartOfThePackage.includes(entry.name)
                ? []
                : collectStylesheets(path.join(directory, entry.name), relativePath);
        }
        return entry.name.endsWith('.css') ? [relativePath] : [];
    });
}

/** Why no module in this package may import a stylesheet, appended to whichever guard catches it. */
const whyStylesheetImportsAreBanned =
    ` Emitted JavaScript must not import stylesheets: a bare 'tsc -b', which is what builds this ` +
    `package when another TypeScript project references it, copies no assets, so the import resolves ` +
    `to nothing there even though Rollup can make it resolve here. That is what broke CI once already. ` +
    `Reach the stylesheet through the './stories/styles.css' export subpath instead.`;

/**
 * Rollup plugin that copies the package's stylesheets into both output directories, so the paths the
 * `exports` map points at exist in the published artifact.
 *
 * It also enforces the rule that keeps this honest: **no emitted JavaScript may import a stylesheet.**
 *
 * This package is built two ways. `yarn build` runs `tsc -b` and then Rollup, and Rollup is the only
 * one of the two that copies an asset. But when another TypeScript project references this one - which
 * `Arc.React.MVVM` does, and any consuming app can - `tsc -b` builds this project on its own and no
 * package script runs at all. A stylesheet import in the emitted JS therefore resolves in one build and
 * dangles in the other, which is exactly how CI broke: MVVM's specs loaded a `StoryContainer.js` whose
 * `import './stories.css'` pointed at a file `tsc` never copied.
 *
 * So the stylesheet is reached through the `./stories/styles.css` export subpath, which nothing in the
 * emitted JavaScript depends on, and this check fails the build if an import ever creeps back in.
 * @param {string} sourceDirectory Package root to collect stylesheets from.
 * @param {string} cjsDirectory CommonJS output directory.
 * @param {string} esmDirectory ES module output directory.
 * @returns {import('rollup').Plugin} The plugin.
 */
function copyStylesheets(sourceDirectory, cjsDirectory, esmDirectory) {
    let hasRun = false;
    return {
        name: 'copy-stylesheets',

        /**
         * Catches the import at the moment it is resolved, which is the only place the message can name
         * the file that wrote it. Left to itself Rollup either parses the stylesheet as JavaScript and
         * reports `Expression expected` on a `:root` selector, or - if some plugin externalizes it -
         * emits it happily; neither tells you what is actually wrong.
         * @param {string} source The import specifier.
         * @param {string | undefined} importer The module importing it.
         * @returns {null} Never resolves anything; it either throws or defers.
         */
        resolveId(source, importer) {
            if (source.endsWith('.css')) {
                this.error(
                    `'${importer ? path.relative(sourceDirectory, importer) : source}' imports the ` +
                    `stylesheet '${source}'.${whyStylesheetImportsAreBanned}`);
            }
            return null;
        },

        /**
         * Reads the import graph Rollup parsed, rather than grepping the emitted text - a doc comment
         * showing a consumer the import they should write is not an import the module performs, and
         * only the parsed graph can tell the two apart.
         * @param {import('rollup').NormalizedOutputOptions} _options Output options.
         * @param {import('rollup').OutputBundle} bundle The emitted bundle.
         */
        writeBundle(_options, bundle) {
            for (const [fileName, output] of Object.entries(bundle)) {
                if (output.type !== 'chunk') continue;
                const stylesheets = [...output.imports, ...output.dynamicImports]
                    .filter(imported => imported.endsWith('.css'));
                if (stylesheets.length > 0) {
                    this.error(
                        `'${fileName}' imports the stylesheet '${stylesheets[0]}'.` +
                        whyStylesheetImportsAreBanned);
                }
            }
        },

        closeBundle() {
            if (hasRun) return;
            hasRun = true;

            const stylesheets = collectStylesheets(sourceDirectory);
            if (stylesheets.length === 0) {
                this.error('No stylesheets found to copy - the story kit ships one, so this means it was lost.');
            }

            for (const stylesheet of stylesheets) {
                for (const outputDirectory of [cjsDirectory, esmDirectory]) {
                    const target = path.join(outputDirectory, stylesheet);
                    mkdirSync(path.dirname(target), { recursive: true });
                    copyFileSync(path.join(sourceDirectory, stylesheet), target);
                    if (!existsSync(target)) {
                        this.error(`Failed to copy stylesheet '${stylesheet}' to '${outputDirectory}'.`);
                    }
                }
            }

            console.log(`✓ Copied ${stylesheets.length} stylesheet(s) to CJS and ESM outputs`);
        }
    };
}

const config = rollup(cjsPath, esmPath, tsconfigPath, pkg);

export default {
    ...config,

    // First, so its `resolveId` guard sees a stylesheet specifier before any plugin can externalize it
    // or hand Rollup the file to parse as JavaScript.
    plugins: [copyStylesheets(import.meta.dirname, cjsPath, esmPath), ...config.plugins]
};
