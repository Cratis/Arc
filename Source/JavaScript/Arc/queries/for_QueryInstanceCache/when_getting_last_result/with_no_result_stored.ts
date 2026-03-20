// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when getting the last result for a key that has no result yet', () => {
    let cache: QueryInstanceCache;
    let result: unknown;

    beforeEach(() => {
        cache = new QueryInstanceCache();
        cache.getOrCreate('MyQuery::', () => ({}));
        result = cache.getLastResult('MyQuery::');
    });

    it('should return undefined', () => (result === undefined).should.be.true);
});
