// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when tearing down all subscriptions with active subscriptions', () => {
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

        cache.teardownAllSubscriptions();
    });

    it('should call teardown for the first entry', () => firstTeardownCalled.should.be.true);
    it('should call teardown for the second entry', () => secondTeardownCalled.should.be.true);
    it('should keep the first entry', () => cache.has('QueryA::').should.be.true);
    it('should keep the second entry', () => cache.has('QueryB::').should.be.true);
    it('should mark the first entry as not subscribed', () => cache.isSubscribed('QueryA::').should.be.false);
    it('should mark the second entry as not subscribed', () => cache.isSubscribed('QueryB::').should.be.false);
});
