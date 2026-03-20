// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../QueryInstanceCache';

describe('when getting or creating an entry that does not exist yet', () => {
    let cache: QueryInstanceCache;
    let instance: object;
    let isNew: boolean;

    beforeEach(() => {
        cache = new QueryInstanceCache();
        const result = cache.getOrCreate('MyQuery::', () => ({ id: 1 }));
        instance = result.instance;
        isNew = result.isNew;
    });

    it('should create a new instance', () => instance.should.deep.equal({ id: 1 }));
    it('should report the entry as new', () => isNew.should.be.true);
    it('should record the entry in the cache', () => cache.has('MyQuery::').should.be.true);
});
