// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { rollup } from '../../../rollup.config.mjs';

import pkg from './package.json' with { type: 'json' };

import { copyFileSync, existsSync, mkdirSync, readdirSync, readFileSync } from 'fs';
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

/** Matches the specifier of a stylesheet import or require in emitted JavaScript. */
const stylesheetSpecifier = /(?:from\s*|import\s*|require\s*\(\s*)['"]([^'"]+\.css)['"]/g;

/**
 * Collects the emitted JavaScript that makes up the package's runtime surface.
 *
 * Spec output is skipped - the same `for_*` folders the swc plugin already excludes. A spec quoting an
 * import in an assertion is not an import a consumer will ever resolve, and treating it as one turns the
 * check into noise.
 * @param {string} directory Directory to walk.
 * @returns {string[]} Absolute paths of the JavaScript files found.
 */
function collectEmittedJavaScript(directory) {
    return readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
        const entryPath = path.join(directory, entry.name);
        if (entry.isDirectory()) {
            return entry.name.startsWith('for_') ? [] : collectEmittedJavaScript(entryPath);
        }
        return entry.name.endsWith('.js') ? [entryPath] : [];
    });
}

/**
 * Rollup plugin that keeps stylesheet imports in the generated JS exactly as they were written.
 *
 * The plain `external` option would send them through Rollup's absolute-path normalization, which
 * rebases them against the root shared by every input module and lands `./stories.css` on
 * `../Arc.React/stories/stories.css` - a path that resolves to nothing. Resolving them here as
 * `'relative'` says the file sits next to the chunk that imports it, which is precisely what
 * `preserveModules` plus `copyStylesheets` arrange for.
 * @returns {import('rollup').Plugin} The plugin.
 */
function externalizeStylesheets() {
    return {
        name: 'externalize-stylesheets',
        resolveId(source) {
            return source.endsWith('.css') ? { id: source, external: 'relative' } : null;
        }
    };
}

/**
 * Rollup plugin that copies the package's stylesheets into both output directories.
 *
 * Rollup keeps `import './stories.css'` verbatim in the generated JS because the config marks
 * stylesheets external, so the file has to sit next to the JS that imports it - otherwise the
 * package publishes an import that resolves to nothing in every consumer. Nothing else in the
 * build touches CSS, which is exactly how a stylesheet stops shipping without anyone noticing,
 * so this fails the build rather than emitting a package that is quietly missing its styling.
 * @param {string} sourceDirectory Package root to collect stylesheets from.
 * @param {string} cjsDirectory CommonJS output directory.
 * @param {string} esmDirectory ES module output directory.
 * @returns {import('rollup').Plugin} The plugin.
 */
function copyStylesheets(sourceDirectory, cjsDirectory, esmDirectory) {
    let hasRun = false;
    return {
        name: 'copy-stylesheets',
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

            for (const outputDirectory of [cjsDirectory, esmDirectory]) {
                for (const emitted of collectEmittedJavaScript(outputDirectory)) {
                    const code = readFileSync(emitted, 'utf-8');
                    for (const [, specifier] of code.matchAll(stylesheetSpecifier)) {
                        const target = path.resolve(path.dirname(emitted), specifier);
                        if (!existsSync(target)) {
                            this.error(
                                `'${path.relative(sourceDirectory, emitted)}' imports '${specifier}', ` +
                                `which does not exist in the built package. A consumer resolving that ` +
                                `import would get nothing, so the story kit would render unstyled.`);
                        }
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

    // Stylesheets are side-effect imported by the components that need them. They stay external so
    // the import survives into the published JS, where the consumer's bundler resolves it against
    // the copies `copyStylesheets` puts next to that JS.
    plugins: [
        externalizeStylesheets(),
        ...config.plugins,
        copyStylesheets(import.meta.dirname, cjsPath, esmPath)
    ]
};
