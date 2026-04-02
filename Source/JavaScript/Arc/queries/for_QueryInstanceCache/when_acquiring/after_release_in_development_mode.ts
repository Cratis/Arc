// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when acquiring after release in development mode', () => {
    let cache: QueryInstanceCache;
    let teardownCalled: boolean;

    beforeEach(() => {
        vi.useFakeTimers();
        teardownCalled = false;
        cache = new QueryInstanceCache(true);
        cache.getOrCreate('MyQuery::', () => ({}));
        cache.acquire('MyQuery::');
        cache.setTeardown('MyQuery::', () => { teardownCalled = true; });
        cache.release('MyQuery::');

        // Re-acquire before the deferred timer fires (simulates StrictMode re-mount).
        cache.acquire('MyQuery::');
        vi.advanceTimersByTime(0);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should not call teardown', () => teardownCalled.should.be.false);
    it('should keep the entry', () => cache.has('MyQuery::').should.be.true);
    it('should still report as subscribed', () => cache.isSubscribed('MyQuery::').should.be.true);
});
