// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when releasing one of two subscribers', () => {
    let cache: QueryInstanceCache;

    beforeEach(() => {
        cache = new QueryInstanceCache();
        cache.getOrCreate('MyQuery::', () => ({}));
        cache.getOrCreate('MyQuery::', () => ({})); // second subscriber — reuses entry
        cache.release('MyQuery::');
    });

    it('should keep the entry because one subscriber remains', () => cache.has('MyQuery::').should.be.true);
});
