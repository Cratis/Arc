// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../../QueryInstanceCache';
import { QueryResultWithState } from '../../../QueryResultWithState';

describe('when getting the last result for a key that has a stored result', () => {
    let cache: QueryInstanceCache;
    let stored: QueryResultWithState<string[]>;
    let retrieved: QueryResultWithState<string[]> | undefined;

    beforeEach(() => {
        cache = new QueryInstanceCache();
        cache.getOrCreate('MyQuery::', () => ({}));

        stored = QueryResultWithState.empty<string[]>();
        cache.setLastResult('MyQuery::', stored);

        retrieved = cache.getLastResult<string[]>('MyQuery::');
    });

    it('should return the stored result', () => retrieved!.should.equal(stored));
});
