// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
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

/** Strips comments, so prose mentioning a class name is never mistaken for a rule defining one. */
const withoutComments = (css: string) => css.replace(/\/\*[\s\S]*?\*\//g, '');

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
     * Side-effect imported rather than left to the consumer to remember: a bundler resolving
     * `@cratis/arc.react/stories` pulls the stylesheet in on its own, which is what makes the doc
     * comments' "automatically" true. This is how `@cratis/components` ships its component CSS too.
     */
    it('should be imported by the component that renders the classes', () =>
        readText('stories', 'StoryContainer.tsx').should.contain(`import './stories.css'`));

    /**
     * Ordering CSS is the consumer's business - it may need the kit's tokens loaded before its own
     * theme, or want the stylesheet in `.storybook/preview` and nowhere else. That needs a subpath
     * it can name, and the subpath has to point at the built copy, not the source.
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
