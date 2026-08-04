// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { readFileSync } from 'node:fs';
import { dirname, join } from 'node:path';
import { fileURLToPath } from 'node:url';

/**
 * The peer range's lower bound is a compatibility claim; the exact devDependency pin is the only version this
 * package is ever compiled and tested against. When they disagree the range claims support for versions nothing
 * verifies, and nothing fails until a consumer resolves one of them.
 *
 * That is what happened here: a `^7.16.0` dependency became a `^7` peer range, widening it by sixteen minor
 * versions while the pin stayed at 7.16.0. Tying the bound to the pin is what stops the two drifting again.
 */
describe('when declaring the fundamentals peer range', () => {
    const manifest = JSON.parse(
        readFileSync(join(dirname(fileURLToPath(import.meta.url)), '..', 'package.json'), 'utf-8')) as {
            peerDependencies?: Record<string, string>;
            devDependencies?: Record<string, string>;
        };

    const peer = manifest.peerDependencies?.['@cratis/fundamentals'];
    const pinned = manifest.devDependencies?.['@cratis/fundamentals'];

    it('should declare fundamentals as a peer dependency', () => peer!.should.not.be.undefined);

    it('should pin one concrete version to build against', () => pinned!.should.match(/^\d+\.\d+\.\d+$/));

    it('should admit no version below the one it builds against', () => peer!.should.equal(`^${pinned}`));
});
