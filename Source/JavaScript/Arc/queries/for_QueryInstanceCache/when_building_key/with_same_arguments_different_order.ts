// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when building key for a query with the same arguments in different order', () => {
    let cache: QueryInstanceCache;
    let keyA: string;
    let keyB: string;

    beforeEach(() => {
        cache = new QueryInstanceCache();
        keyA = cache.buildKey('MyQuery', { z: 1, a: 2 });
        keyB = cache.buildKey('MyQuery', { a: 2, z: 1 });
    });

    it('should produce identical keys regardless of property order', () => keyA.should.equal(keyB));
});
