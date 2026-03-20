// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryInstanceCache } from '../../../QueryInstanceCache';

describe('when getting or creating an entry that already exists', () => {
    let cache: QueryInstanceCache;
    const factoryResult = { id: 42 };
    let firstInstance: object;
    let secondInstance: object;
    let isNewFirst: boolean;
    let isNewSecond: boolean;

    beforeEach(() => {
        cache = new QueryInstanceCache();
        const first = cache.getOrCreate('MyQuery::', () => factoryResult);
        firstInstance = first.instance;
        isNewFirst = first.isNew;

        const second = cache.getOrCreate('MyQuery::', () => ({ id: 99 }));
        secondInstance = second.instance;
        isNewSecond = second.isNew;
    });

    it('should return the same instance', () => secondInstance.should.equal(firstInstance));
    it('should not call factory a second time', () => secondInstance.should.not.deep.equal({ id: 99 }));
    it('should report the first entry as new', () => isNewFirst.should.be.true);
    it('should report the second entry as not new', () => isNewSecond.should.be.false);
});
