// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, renderHook, waitFor } from '@testing-library/react';
import { Paging } from '@cratis/arc/queries';
import { useObservableQueryWithPaging } from '../useObservableQuery.js';
import { ObservableQueryWithArguments } from './given/ObservableQueryWithArguments.js';
import { an_observable_query_cache } from './given/an_observable_query_cache.js';
import { given } from '../../given.js';

describe('when the observable page size changes', given(an_observable_query_cache, context => {
    beforeEach(() => context.establish());
    afterEach(() => context.cleanup());

    it('should reset and subscribe with the new page size', async () => {
        const { result } = renderHook(() => useObservableQueryWithPaging<string[], ObservableQueryWithArguments>(ObservableQueryWithArguments, new Paging(0, 10)), { wrapper: context.wrapper });
        await context.push(0, ['old size']);

        await act(async () => { await result.current[3](20); });

        result.current[0].data.should.deep.equal([]);
        result.current[0].isReady.should.be.false;
        result.current[0].isPerforming.should.be.true;
        context.connectStub.should.have.been.calledTwice;
        context.connectStub.secondCall.args[1].should.deep.equal({ page: 0, pageSize: 20 });
        await waitFor(() => context.disconnectStub.should.have.been.calledOn(context.connectStub.firstCall.thisValue));
        await context.push(1, ['new size']);
        result.current[0].data.should.deep.equal(['new size']);
    });
}));
