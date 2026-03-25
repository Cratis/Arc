// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when releasing the only subscriber', () => {
    let cache: QueryInstanceCache;

    beforeEach(() => {
        vi.useFakeTimers();
        cache = new QueryInstanceCache();
        cache.getOrCreate('MyQuery::', () => ({}));
        cache.acquire('MyQuery::');
        cache.release('MyQuery::');
        vi.advanceTimersByTime(0);
    });

    afterEach(() => {
        vi.useRealTimers();
    });

    it('should evict the entry', () => cache.has('MyQuery::').should.be.false);
});
