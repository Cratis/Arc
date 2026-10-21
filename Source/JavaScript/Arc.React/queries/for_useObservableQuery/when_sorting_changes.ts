// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, renderHook, waitFor } from '@testing-library/react';
import { Sorting, SortDirection } from '@cratis/arc/queries';
import { useObservableQuery } from '../useObservableQuery.js';
import { ObservableQueryWithArguments } from './given/ObservableQueryWithArguments.js';
import { an_observable_query_cache } from './given/an_observable_query_cache.js';
import { given } from '../../given.js';

describe('when observable sorting changes', given(an_observable_query_cache, context => {
    beforeEach(() => context.establish());
    afterEach(() => context.cleanup());

    it('should reset and subscribe with the new sort order', async () => {
        const { result } = renderHook(() => useObservableQuery<string[], ObservableQueryWithArguments>(ObservableQueryWithArguments), { wrapper: context.wrapper });
        await context.push(0, ['old order']);

        await act(async () => { await result.current[1](new Sorting('name', SortDirection.descending)); });

        result.current[0].data.should.deep.equal([]);
        result.current[0].isReady.should.be.false;
        result.current[0].isPerforming.should.be.true;
        context.connectStub.should.have.been.calledTwice;
        context.connectStub.secondCall.args[1].should.deep.equal({ sortBy: 'name', sortDirection: 'desc' });
        await waitFor(() => context.disconnectStub.should.have.been.calledOn(context.connectStub.firstCall.thisValue));
        await context.push(1, ['new order']);
        result.current[0].data.should.deep.equal(['new order']);
    });
}));
