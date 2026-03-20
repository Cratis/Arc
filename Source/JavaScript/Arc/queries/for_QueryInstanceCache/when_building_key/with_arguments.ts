// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../../QueryInstanceCache';

describe('when building key for a query with arguments', () => {
    let cache: QueryInstanceCache;
    let result: string;

    beforeEach(() => {
        cache = new QueryInstanceCache();
        result = cache.buildKey('MyQuery', { id: '42', category: 'books' });
    });

    it('should return key containing the query name', () => result.should.contain('MyQuery::'));
    it('should serialise arguments as sorted JSON', () => result.should.equal('MyQuery::{"category":"books","id":"42"}'));
});
