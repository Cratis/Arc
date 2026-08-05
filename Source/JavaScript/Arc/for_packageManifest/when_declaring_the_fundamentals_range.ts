// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * The range's lower bound is a compatibility claim; the exact devDependency pin is the only version this package is
 * ever compiled and tested against. When they disagree the range claims support for versions nothing verifies, and
 * nothing fails until a consumer resolves one of them.
 *
 * That is what happened once already: a `^7.16.0` dependency became a `^7` range, widening it by sixteen minor
 * versions while the pin stayed at 7.16.0. Tying the bound to the pin is what stops the two drifting again.
 */
describe('when declaring the fundamentals range', () => {
    const manifest = JSON.parse(
        readFileSync(join(dirname(fileURLToPath(import.meta.url)), '..', 'package.json'), 'utf-8')) as {
            dependencies?: Record<string, string>;
            peerDependencies?: Record<string, string>;
            devDependencies?: Record<string, string>;
        };

    const dependency = manifest.dependencies?.['@cratis/fundamentals'];
    const pinned = manifest.devDependencies?.['@cratis/fundamentals'];

    it('should declare fundamentals as an ordinary dependency', () => dependency!.should.not.be.undefined);

    it('should pin one concrete version to build against', () => pinned!.should.match(/^\d+\.\d+\.\d+$/));

    it('should admit no version below the one it builds against', () => dependency!.should.equal(`^${pinned}`));

    /**
     * Not a peer. A peer dependency is for a singleton the consuming application owns and has a view on - React is
     * the example this package itself gets right. Fundamentals is an implementation detail of Arc: the application
     * never imports it, so requiring it to name a version makes it declare something it has no opinion about - and
     * a peer does not reach it transitively through `@cratis/arc.react` anyway, so the declaration lands on the
     * consumer as a warning to silence rather than a choice to make.
     */
    it('should not require the consumer to declare it', () =>
        (manifest.peerDependencies?.['@cratis/fundamentals'] === undefined).should.be.true);
});
