// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../../QueryInstanceCache';

describe('when releasing the only subscriber with a retention timeout, after the timeout elapses', () => {
    let cache: QueryInstanceCache;
    let teardownCalled: boolean;

    beforeEach(() => {
        vi.useFakeTimers();
        teardownCalled = false;
        cache = new QueryInstanceCache(5000);
        cache.getOrCreate('MyQuery::', () => ({}));
        cache.acquire('MyQuery::');
        cache.setTeardown('MyQuery::', () => { teardownCalled = true; });
        cache.release('MyQuery::');

        vi.advanceTimersByTime(5000);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should call teardown', () => teardownCalled.should.be.true);
    it('should evict the entry from the cache', () => cache.has('MyQuery::').should.be.false);
});
