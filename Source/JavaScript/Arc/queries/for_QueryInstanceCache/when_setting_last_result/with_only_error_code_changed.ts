// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { expect } from 'chai';
import sinon from 'sinon';
import { PagingInfo } from '../../PagingInfo.js';
import { QueryInstanceCache } from '../../QueryInstanceCache.js';
import { QueryResultWithState } from '../../QueryResultWithState.js';

describe('when setting the last result with only the error code changed', () => {
    it('should retain the code when data is reconciled and notify when it clears', () => {
        const cache = new QueryInstanceCache();
        const key = 'MyQuery::';
        const listener = sinon.stub();
        const state = (code?: string) => new QueryResultWithState(
            ['a'], PagingInfo.noPaging, false, true, true, [], true, ['An unexpected error occurred.'], '', false, undefined, true, '', false, code,
        );
        cache.getOrCreate(key, () => ({}));
        cache.addListener<string[]>(key, listener);
        cache.setLastResult(key, state());
        listener.resetHistory();
        cache.setLastResult(key, state('namespace_closing'));
        listener.calledOnce.should.be.true;
        listener.firstCall.args[0].errorCode.should.equal('namespace_closing');
        listener.resetHistory();
        cache.setLastResult(key, state());
        listener.calledOnce.should.be.true;
        expect(listener.firstCall.args[0].errorCode).to.be.undefined;
    });
});
