// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when releasing the only subscriber outside development mode', () => {
    let cache: QueryInstanceCache;
    let teardownCalled: boolean;

    beforeEach(() => {
        vi.useFakeTimers();
        teardownCalled = false;
        cache = new QueryInstanceCache(false);
        cache.getOrCreate('MyQuery::', () => ({}));
        cache.acquire('MyQuery::');
        cache.setTeardown('MyQuery::', () => { teardownCalled = true; });
        cache.release('MyQuery::');
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should call teardown synchronously', () => teardownCalled.should.be.true);

    describe('and the deferred timer fires', () => {
        beforeEach(() => {
            vi.advanceTimersByTime(0);
        });

        it('should evict the entry', () => cache.has('MyQuery::').should.be.false);
    });
});
