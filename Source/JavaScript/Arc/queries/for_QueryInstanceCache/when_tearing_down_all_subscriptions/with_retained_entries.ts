// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache.js';

describe.each([false, true])('when reconnecting a released entry with a subscription: %s', (hasSubscription) => {
    let cache: QueryInstanceCache;
    let teardown: ReturnType<typeof vi.fn>;

    beforeEach(() => {
        vi.useFakeTimers();
        cache = new QueryInstanceCache(1000);
        teardown = vi.fn();
        cache.getOrCreate('RetainedQuery::', () => ({}));
        cache.acquire('RetainedQuery::');
        if (hasSubscription) {
            cache.setTeardown('RetainedQuery::', teardown);
        }
        cache.release('RetainedQuery::');
        vi.advanceTimersByTime(400);
        cache.teardownAllSubscriptions();
    });

    afterEach(() => {
        cache.dispose();
        vi.useRealTimers();
    });

    it('should preserve the original expiry deadline across repeated reconnects', () => {
        vi.advanceTimersByTime(300);
        cache.teardownAllSubscriptions();
        vi.advanceTimersByTime(299);
        cache.has('RetainedQuery::').should.be.true;
        vi.advanceTimersByTime(1);
        cache.has('RetainedQuery::').should.be.false;
        teardown.mock.calls.length.should.equal(hasSubscription ? 1 : 0);
    });

    it('should cancel expiry when a consumer returns', () => {
        cache.acquire('RetainedQuery::');
        vi.advanceTimersByTime(1000);
        cache.has('RetainedQuery::').should.be.true;
        cache.release('RetainedQuery::');
        vi.advanceTimersByTime(999);
        cache.has('RetainedQuery::').should.be.true;
        vi.advanceTimersByTime(1);
        cache.has('RetainedQuery::').should.be.false;
    });
});
