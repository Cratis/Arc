// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, renderHook } from '@testing-library/react';
import { Paging } from '@cratis/arc/queries';
import { useObservableQueryWithPaging } from '../useObservableQuery.js';
import { ObservableQueryWithArguments } from './given/ObservableQueryWithArguments.js';
import { an_observable_query_cache } from './given/an_observable_query_cache.js';
import { given } from '../../given.js';

describe('when the observable page changes back to a retained subscription', given(an_observable_query_cache, context => {
    beforeEach(() => context.establish(30_000));
    afterEach(() => context.cleanup());

    it('should show the retained result without subscribing again', async () => {
        const { result } = renderHook(() => useObservableQueryWithPaging<string[], ObservableQueryWithArguments>(ObservableQueryWithArguments, new Paging(0, 10)), { wrapper: context.wrapper });
        await context.push(0, ['page zero']);
        await act(async () => { await result.current[2](1); });
        await context.push(1, ['page one']);
        await context.push(0, ['page zero updated while away']);
        result.current[0].data.should.deep.equal(['page one']);

        await act(async () => { await result.current[2](0); });

        result.current[0].data.should.deep.equal(['page zero updated while away']);
        result.current[0].isReady.should.be.true;
        result.current[0].isPerforming.should.be.false;
        context.connectStub.should.have.been.calledTwice;
        context.disconnectStub.should.not.have.been.called;
        await context.push(1, ['late page one']);
        result.current[0].data.should.deep.equal(['page zero updated while away']);
    });
}));
