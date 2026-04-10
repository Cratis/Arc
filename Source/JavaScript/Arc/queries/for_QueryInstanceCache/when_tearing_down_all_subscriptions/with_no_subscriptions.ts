// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when tearing down all subscriptions with no subscriptions', () => {
    let cache: QueryInstanceCache;

    beforeEach(() => {
        cache = new QueryInstanceCache();
        cache.getOrCreate('MyQuery::', () => ({}));

        cache.teardownAllSubscriptions();
    });

    it('should keep the entry', () => cache.has('MyQuery::').should.be.true);
    it('should still report as not subscribed', () => cache.isSubscribed('MyQuery::').should.be.false);
});
