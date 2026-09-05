// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import sinon from 'sinon';
import { PagingInfo } from '../../PagingInfo';
import { QueryInstanceCache } from '../../QueryInstanceCache';
import { QueryResultWithState } from '../../QueryResultWithState';

describe('when setting the last result with only readiness changed', () => {
    const key = 'MyQuery::';
    const data = ['a', 'b'];
    let listener: sinon.SinonStub;

    beforeEach(() => {
        const cache = new QueryInstanceCache();
        cache.getOrCreate(key, () => ({}));
        listener = sinon.stub();
        cache.addListener<string[]>(key, listener);

        cache.setLastResult(
            key,
            new QueryResultWithState(
                data,
                PagingInfo.noPaging,
                false,
                true,
                true,
                [],
                false,
                [],
                '',
                false,
                undefined,
                false,
            ),
        );
        listener.resetHistory();

        cache.setLastResult(
            key,
            new QueryResultWithState(
                data,
                PagingInfo.noPaging,
                false,
                true,
                true,
                [],
                false,
                [],
                '',
                false,
                undefined,
                true,
            ),
        );
    });

    afterEach(() => sinon.restore());

    it('should notify the listener', () => listener.calledOnce.should.be.true);
    it('should provide the changed readiness', () =>
        listener.firstCall.args[0].isReady.should.be.true);
});
