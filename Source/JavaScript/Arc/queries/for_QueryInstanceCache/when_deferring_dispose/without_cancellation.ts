// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when deferring dispose without cancellation', () => {
    let cache: QueryInstanceCache;
    let teardownCalled: boolean;

    beforeEach(() => {
        vi.useFakeTimers();
        teardownCalled = false;
        cache = new QueryInstanceCache(true);
        cache.getOrCreate('MyQuery::', () => ({}));
        cache.acquire('MyQuery::');
        cache.setTeardown('MyQuery::', () => { teardownCalled = true; });

        cache.deferDispose();
        vi.advanceTimersByTime(0);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should call teardown', () => teardownCalled.should.be.true);
    it('should evict the entry', () => cache.has('MyQuery::').should.be.false);
});
