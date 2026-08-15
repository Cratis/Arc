// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readdirSync, readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

import { renderToStaticMarkup } from 'react-dom/server';

import { StoryBadge, StoryContainer, StoryDivider, StoryGrid, StorySection } from '../stories';
import type { BadgeVariant, StoryContainerProps } from '../stories';

/**
 * The story kit renders class names and nothing else - every visual promise it makes is a rule in a
 * stylesheet. Up to 21.14.2 it shipped the class names and left the stylesheet behind in this
 * repository's `.storybook/` folder, so a consumer installing the package got the whole API, the doc
 * comments promising "consistent spacing and styling", and not one line of CSS to make any of it
 * true. The kit rendered as unstyled block elements everywhere it was used.
 *
 * Nothing failed when that happened, which is why it lasted: the package built, the types resolved,
 * the components rendered. These specs are the thing that would have failed. They hold the two
 * halves of the contract against each other - the classes the components actually render, and the
 * stylesheet the package actually publishes - so that neither half can move without the other.
 *
 * The first attempt at the fix side-effect imported the stylesheet from `StoryContainer`, so that a
 * bundler would pull it in with no work from the consumer. That broke the build in a subtler way and
 * is why `should not be imported by any TypeScript in the package` exists. This package has two
 * builders: `yarn build` runs `tsc -b` and then Rollup, and only Rollup copies assets. But a
 * TypeScript project reference - `Arc.React.MVVM` holds one, and so can any consuming app - makes
 * `tsc -b` build this project directly, with no package script in sight. The emitted
 * `import './stories.css'` resolved under one builder and dangled under the other, and MVVM's specs
 * died loading it. Emitted JavaScript must not depend on an asset only one builder produces.
 */
const here = dirname(fileURLToPath(import.meta.url));
const packageRoot = join(here, '..');

/** Where the stylesheet lives in the source tree, relative to the package root. */
const stylesheetPath = 'stories/stories.css';

/** The subpath a consumer imports when it needs the stylesheet on its own terms. */
const stylesheetExport = './stories/styles.css';

const readText = (...segments: string[]) => readFileSync(join(packageRoot, ...segments), 'utf-8');

const manifest = JSON.parse(readText('package.json')) as {
    files: string[];
    exports: Record<string, string | undefined>;
};

/** Strips block comments, so prose mentioning a rule or an import is never mistaken for one. */
const withoutComments = (source: string) => source.replace(/\/\*[\s\S]*?\*\//g, '');

/** Directories under the package root that hold this repository's tooling rather than the package. */
const notPartOfThePackage = ['node_modules', 'dist', '.storybook', 'storybook-static'];

/** Matches an import, re-export or require of a stylesheet. Not global - `test` would carry state. */
const stylesheetImport = /(?:from|import|require\s*\()\s*['"][^'"]*\.css['"]/;

/**
 * Every TypeScript file the package compiles, as `[package-relative path, contents]`.
 * @param directory Directory to walk.
 * @param prefix Path of that directory relative to the package root.
 */
const typeScriptIn = (directory: string, prefix = ''): [string, string][] =>
    readdirSync(directory, { withFileTypes: true }).flatMap(entry => {
        const relativePath = prefix ? `${prefix}/${entry.name}` : entry.name;
        if (entry.isDirectory()) {
            return notPartOfThePackage.includes(entry.name)
                ? []
                : typeScriptIn(join(directory, entry.name), relativePath);
        }
        return /\.tsx?$/.test(entry.name)
            ? [[relativePath, readFileSync(join(directory, entry.name), 'utf-8')] as [string, string]]
            : [];
    });

/** The class names a stylesheet defines a rule for. */
const classesDefinedIn = (css: string) => new Set(
    withoutComments(css)
        .split('}')
        .map(block => block.split('{')[0])
        .flatMap(selector => [...selector.matchAll(/\.([a-zA-Z][\w-]*)/g)].map(match => match[1])));

/** The class names a rendered markup fragment carries. */
const classesRenderedIn = (markup: string) => [...markup.matchAll(/class="([^"]*)"/g)]
    .flatMap(match => match[1].split(' '))
    .filter(className => className.length > 0);

const sizes: StoryContainerProps['size'][] = ['sm', 'md', 'lg', 'full'];
const variants: BadgeVariant[] = ['success', 'warning', 'error', 'info'];

/**
 * Every visual state the kit can render. A class name that only appears for one prop value is still
 * a class name the stylesheet has to cover, so the specs render the axes rather than reading the
 * source for string literals - `story-badge-${variant}` is not a literal anywhere.
 */
const everythingTheKitRenders = [
    ...sizes.flatMap(size => [
        renderToStaticMarkup(<StoryContainer size={size}>content</StoryContainer>),
        renderToStaticMarkup(<StoryContainer size={size} asCard>content</StoryContainer>)
    ]),
    renderToStaticMarkup(<StorySection>content</StorySection>),
    renderToStaticMarkup(<StoryGrid>content</StoryGrid>),
    renderToStaticMarkup(<StoryDivider />),
    ...variants.map(variant => renderToStaticMarkup(<StoryBadge variant={variant}>label</StoryBadge>))
];

describe('when shipping the story kit stylesheet', () => {
    const stylesheet = readText(stylesheetPath);
    const rules = withoutComments(stylesheet);
    const defined = classesDefinedIn(stylesheet);
    const rendered = [...new Set(everythingTheKitRenders.flatMap(classesRenderedIn))];

    const declaredProperties = new Set([...rules.matchAll(/(--[\w-]+)\s*:/g)].map(match => match[1]));
    const usedProperties = [...rules.matchAll(/var\(\s*(--[\w-]+)/g)].map(match => match[1]);

    it('should render at least one class name per component', () => rendered.should.have.length.greaterThan(4));

    /**
     * The failure the whole file exists for. Add a class name to a component, rename one, or drop a
     * rule, and the half that moved is named here instead of shipping as a silently unstyled element.
     */
    it('should define a rule for every class name the components render', () =>
        rendered.filter(className => !defined.has(className)).should.be.empty);

    /**
     * A rule that reads a variable nothing declares computes to nothing - the same unstyled result as
     * shipping no rule at all, arrived at one step later. The stylesheet has to carry its own tokens
     * rather than borrow them from whatever happens to surround it in this repository's Storybook.
     */
    it('should declare every custom property its own rules read', () =>
        usedProperties.filter(property => !declaredProperties.has(property)).should.be.empty);

    /**
     * The regression that turned CI red. A stylesheet import in the source becomes a stylesheet import
     * in the emitted JavaScript, and the two things that build this package do not agree about whether
     * that import resolves: Rollup copies the asset next to the JS, a bare `tsc -b` does not - and
     * `tsc -b` is what runs when another TypeScript project references this one, with no package script
     * involved. So the kit is reached through an export subpath and nothing in the emitted JavaScript
     * points at a stylesheet at all.
     *
     * This is the same defect class as Cratis/Components#118, and `@cratis/components` reaches its own
     * component CSS the same fragile way through `copy-css.sh`; that hole is simply not exercised there
     * yet. Convenience is not worth an artifact that only works when the right builder happened to run.
     */
    it('should not be imported by any TypeScript in the package', () =>
        typeScriptIn(packageRoot)
            .filter(([, source]) => stylesheetImport.test(withoutComments(source)))
            .map(([path]) => path).should.be.empty);

    /**
     * What a contributor sees locally has to be produced the same way a consumer produces it, or this
     * repository stops being evidence about the package. The Storybook chrome loads the shipped
     * stylesheet - the in-repo spelling of the one import the documentation asks a consumer for.
     */
    it('should be loaded by this repository\'s own Storybook', () =>
        readText('.storybook', 'stories.css').should.contain(`@import '../${stylesheetPath}'`));

    /**
     * The only way in. Nothing in the emitted JavaScript points at the stylesheet, so if this subpath
     * goes missing the styling is unreachable no matter how correct the CSS is - and it has to name the
     * built copy, because the source tree is not what a consumer installs.
     */
    it('should expose the stylesheet as an export subpath', () =>
        manifest.exports[stylesheetExport]!.should.equal(`./dist/esm/${stylesheetPath}`));

    /**
     * An export subpath pointing outside `files` is an export pointing at nothing once published -
     * the manifest promises a path npm never put in the tarball.
     */
    it('should publish the directory the export subpath points into', () =>
        manifest.files.should.contain(manifest.exports[stylesheetExport]!.replace('./', '').split('/')[0]));

    /**
     * One source of truth. This repository's own Storybook renders through the same stylesheet the
     * package ships, so what a contributor sees locally is what a consumer gets - and a rule tweaked
     * for the local preview cannot quietly stop matching the published one.
     */
    it('should be the only place the kit\'s classes are defined', () =>
        [...classesDefinedIn(readText('.storybook', 'stories.css'))]
            .filter(className => className.startsWith('story-')).should.be.empty);
});
