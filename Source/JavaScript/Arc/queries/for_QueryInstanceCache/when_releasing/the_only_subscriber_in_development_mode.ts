// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when releasing the only subscriber in development mode', () => {
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
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should not call teardown synchronously', () => teardownCalled.should.be.false);
    it('should keep the entry before the timer fires', () => cache.has('MyQuery::').should.be.true);
    it('should still report as subscribed before the timer fires', () => cache.isSubscribed('MyQuery::').should.be.true);

    describe('and the deferred timer fires', () => {
        beforeEach(() => {
            vi.advanceTimersByTime(0);
        });

        it('should call teardown', () => teardownCalled.should.be.true);
        it('should evict the entry', () => cache.has('MyQuery::').should.be.false);
    });
});
