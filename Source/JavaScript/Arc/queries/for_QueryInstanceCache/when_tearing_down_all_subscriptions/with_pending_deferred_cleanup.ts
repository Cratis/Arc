// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when tearing down all subscriptions with pending deferred cleanup', () => {
    let cache: QueryInstanceCache;
    let teardownCalled: boolean;

    beforeEach(() => {
        vi.useFakeTimers();
        teardownCalled = false;
        cache = new QueryInstanceCache(true);
        cache.getOrCreate('MyQuery::', () => ({}));
        cache.acquire('MyQuery::');
        cache.setTeardown('MyQuery::', () => { teardownCalled = true; });

        // Release the only subscriber — in dev mode this defers cleanup.
        cache.release('MyQuery::');

        // Tear down all before the deferred timer fires.
        cache.teardownAllSubscriptions();
        vi.advanceTimersByTime(0);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should call teardown', () => teardownCalled.should.be.true);
    it('should keep the entry', () => cache.has('MyQuery::').should.be.true);
    it('should mark the entry as not subscribed', () => cache.isSubscribed('MyQuery::').should.be.false);
});
