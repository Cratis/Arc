// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { QueryInstanceCache } from '../../QueryInstanceCache';
import { QueryResultWithState } from '../../QueryResultWithState';

type Item = { id: string; name: string };

describe('when a re-delivered snapshot changed only one of its items', () => {
    const key = 'MyQuery::';
    let cache: QueryInstanceCache;
    let listener: sinon.SinonStub;
    let stored: QueryResultWithState<Item[]>;

    const first = QueryResultWithState.empty<Item[]>([
        { id: 'a', name: 'First' },
        { id: 'b', name: 'Second' }
    ]);

    const second = QueryResultWithState.empty<Item[]>([
        { id: 'a', name: 'First' },
        { id: 'b', name: 'Second changed' }
    ]);

    beforeEach(() => {
        cache = new QueryInstanceCache();
        cache.getOrCreate(key, () => ({}));
        listener = sinon.stub();
        cache.addListener(key, listener);

        cache.setLastResult(key, first);
        cache.setLastResult(key, second);
        stored = cache.getLastResult<Item[]>(key)!;
    });

    afterEach(() => sinon.restore());

    it('should notify for both results', () => listener.calledTwice.should.be.true);
    it('should keep the reference of the unchanged item', () => stored.data[0].should.equal(first.data[0]));
    it('should take the new reference for the changed item', () => stored.data[1].should.equal(second.data[1]));
    it('should carry the changed value', () => stored.data[1].name.should.equal('Second changed'));
});
