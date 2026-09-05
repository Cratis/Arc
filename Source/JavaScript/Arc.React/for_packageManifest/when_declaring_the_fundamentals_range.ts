// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * This package imports fundamentals across twenty files, so it declares it rather than resolving it by
 * hoisting through `@cratis/arc` - a dependency reached only because something else happened to put it
 * within reach is one package manager away from not being reachable at all.
 *
 * The range's lower bound is a compatibility claim; the exact devDependency pin is the only version this
 * package is ever compiled and tested against. When they disagree the range claims support for versions
 * nothing verifies, and nothing fails until a consumer resolves one of them.
 */
const here = dirname(fileURLToPath(import.meta.url));

const read = (path: string) => JSON.parse(readFileSync(path, 'utf-8')) as {
    dependencies?: Record<string, string>;
    peerDependencies?: Record<string, string>;
    devDependencies?: Record<string, string>;
};

describe('when declaring the fundamentals range', () => {
    const manifest = read(join(here, '..', 'package.json'));
    const arcManifest = read(join(here, '..', '..', 'Arc', 'package.json'));

    const dependency = manifest.dependencies?.['@cratis/fundamentals'];
    const pinned = manifest.devDependencies?.['@cratis/fundamentals'];

    it('should declare fundamentals as an ordinary dependency', () => dependency!.should.not.be.undefined);

    it('should pin one concrete version to build against', () => pinned!.should.match(/^\d+\.\d+\.\d+$/));

    it('should admit no version below the one it builds against', () => dependency!.should.equal(`^${pinned}`));

    /**
     * Not a peer, and `react` sitting next to it in this package's peers is what makes the distinction
     * worth pinning. A peer is for a singleton the consuming application owns and has a view on: the
     * application picks its React version, and everything in the tree has to agree with that choice.
     *
     * Fundamentals is the other kind. The application does import it - the proxy generator emits
     * `import { Guid } from '@cratis/fundamentals'` straight into application code - but it never chose
     * it and has no view on which version is right. Arc does, because Arc's own generated output is
     * what has to compile against it. So Arc carries it and the version is decided in one place,
     * instead of landing on the consumer as a warning to silence rather than a choice to make.
     */
    it('should not require the consumer to declare it', () =>
        (manifest.peerDependencies?.['@cratis/fundamentals'] === undefined).should.be.true);

    /**
     * Both packages ship in the same application and both import fundamentals. Let the two ranges drift
     * and an application can resolve one copy for `@cratis/arc` and a different one for this package -
     * and a second copy brings its own converter registry and its own `Guid` class object, so a
     * converter registered on one is invisible to the other and `instanceof` across them is false.
     * Nothing throws; values simply stop being recognized.
     */
    it('should declare the same range as arc', () =>
        dependency!.should.equal(arcManifest.dependencies?.['@cratis/fundamentals']));
});
