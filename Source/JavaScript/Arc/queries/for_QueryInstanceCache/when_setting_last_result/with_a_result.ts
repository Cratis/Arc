// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';
import { QueryResultWithState } from '../../QueryResultWithState';

describe('when setting the last result', () => {
    let cache: QueryInstanceCache;
    let retrieved: QueryResultWithState<number> | undefined;
    const stored = QueryResultWithState.empty<number>(0);

    beforeEach(() => {
        cache = new QueryInstanceCache();
        cache.getOrCreate('MyQuery::', () => ({}));
        cache.setLastResult('MyQuery::', stored);
        retrieved = cache.getLastResult<number>('MyQuery::');
    });

    it('should persist the result so subsequent getLastResult returns it', () => retrieved!.should.equal(stored));
});
