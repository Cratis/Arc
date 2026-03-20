// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when building key for a query with an empty arguments object', () => {
    let cache: QueryInstanceCache;
    let result: string;

    beforeEach(() => {
        cache = new QueryInstanceCache();
        result = cache.buildKey('MyQuery', {});
    });

    it('should produce the same key as no arguments', () => result.should.equal('MyQuery::'));
});
