// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';
import { QueryResultWithState } from '../../QueryResultWithState';

describe('when setting the last result without a previous result', () => {
    let cache: QueryInstanceCache;
    let listenerCallCount: number;

    beforeEach(() => {
        listenerCallCount = 0;
        cache = new QueryInstanceCache();
        cache.getOrCreate('MyQuery::', () => ({}));
        cache.addListener<string[]>('MyQuery::', () => listenerCallCount++);

        const result = QueryResultWithState.empty<string[]>(['a', 'b']);
        cache.setLastResult('MyQuery::', result);
    });

    it('should notify listeners', () => listenerCallCount.should.equal(1));
});
