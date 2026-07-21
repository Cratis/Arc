// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { QueryInstanceCache } from '../../QueryInstanceCache';
import { QueryResultWithState } from '../../QueryResultWithState';

type Item = { id: string; name: string };

describe('when the server re-delivers an identical snapshot after re-subscribing', () => {
    const key = 'MyQuery::';
    let cache: QueryInstanceCache;
    let listener: sinon.SinonStub;

    const first = QueryResultWithState.empty<Item[]>([{ id: 'a', name: 'First' }]);

    // A fresh deserialization of the same payload — equal in value, all-new references.
    const second = QueryResultWithState.empty<Item[]>([{ id: 'a', name: 'First' }]);

    beforeEach(() => {
        cache = new QueryInstanceCache();
        cache.getOrCreate(key, () => ({}));
        listener = sinon.stub();
        cache.addListener(key, listener);

        cache.setLastResult(key, first);
        cache.setLastResult(key, second);
    });

    afterEach(() => sinon.restore());

    it('should notify only for the first result', () => listener.calledOnce.should.be.true);
    it('should keep holding the original data reference', () => cache.getLastResult<Item[]>(key)!.data.should.equal(first.data));
});
