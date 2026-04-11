// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when disposing with active subscriptions', () => {
    let cache: QueryInstanceCache;
    let firstTeardownCalled: boolean;
    let secondTeardownCalled: boolean;

    beforeEach(() => {
        firstTeardownCalled = false;
        secondTeardownCalled = false;
        cache = new QueryInstanceCache();
        cache.getOrCreate('QueryA::', () => ({}));
        cache.acquire('QueryA::');
        cache.setTeardown('QueryA::', () => { firstTeardownCalled = true; });

        cache.getOrCreate('QueryB::', () => ({}));
        cache.acquire('QueryB::');
        cache.setTeardown('QueryB::', () => { secondTeardownCalled = true; });

        cache.dispose();
    });

    it('should call teardown for the first entry', () => firstTeardownCalled.should.be.true);
    it('should call teardown for the second entry', () => secondTeardownCalled.should.be.true);
    it('should evict the first entry', () => cache.has('QueryA::').should.be.false);
    it('should evict the second entry', () => cache.has('QueryB::').should.be.false);
});
