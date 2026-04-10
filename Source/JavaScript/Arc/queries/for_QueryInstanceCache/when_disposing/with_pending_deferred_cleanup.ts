// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when disposing with pending deferred cleanup', () => {
    let cache: QueryInstanceCache;
    let teardownCalled: number;

    beforeEach(() => {
        vi.useFakeTimers();
        teardownCalled = 0;
        cache = new QueryInstanceCache(true);
        cache.getOrCreate('MyQuery::', () => ({}));
        cache.acquire('MyQuery::');
        cache.setTeardown('MyQuery::', () => { teardownCalled++; });
        cache.release('MyQuery::');

        // At this point a deferred cleanup is pending. Dispose should
        // tear down immediately and cancel the deferred cleanup.
        cache.dispose();
        vi.advanceTimersByTime(0);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should call teardown exactly once', () => teardownCalled.should.equal(1));
    it('should evict the entry', () => cache.has('MyQuery::').should.be.false);
});
