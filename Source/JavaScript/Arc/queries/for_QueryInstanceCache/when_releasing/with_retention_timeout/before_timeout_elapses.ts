// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../../QueryInstanceCache';

describe('when releasing the only subscriber with a retention timeout, before the timeout elapses', () => {
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

        // Advance to just before the retention window expires.
        vi.advanceTimersByTime(4999);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should not call teardown', () => teardownCalled.should.be.false);
    it('should keep the entry in the cache', () => cache.has('MyQuery::').should.be.true);
    it('should still report as subscribed', () => cache.isSubscribed('MyQuery::').should.be.true);
});
