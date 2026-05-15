// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';
import { QueryResultWithState } from '../../QueryResultWithState';

describe('when setting the last result with identical data as the previous result', () => {
    let cache: QueryInstanceCache;
    let listenerCallCount: number;

    beforeEach(() => {
        listenerCallCount = 0;
        cache = new QueryInstanceCache();
        cache.getOrCreate('MyQuery::', () => ({}));
        cache.addListener<string[]>('MyQuery::', () => listenerCallCount++);

        const first = QueryResultWithState.empty<string[]>(['a', 'b']);
        cache.setLastResult('MyQuery::', first);
        listenerCallCount = 0; // reset after initial push

        // Second push with data that serializes identically
        const second = QueryResultWithState.empty<string[]>(['a', 'b']);
        cache.setLastResult('MyQuery::', second);
    });

    it('should not notify listeners', () => listenerCallCount.should.equal(0));
});
