// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { useLayoutEffect } from 'react';
import { act, renderHook, waitFor } from '@testing-library/react';
import { Paging } from '@cratis/arc/queries';
import { useObservableQueryWithPaging } from '../useObservableQuery.js';
import { ObservableQueryWithArguments } from './given/ObservableQueryWithArguments.js';
import { an_observable_query_cache } from './given/an_observable_query_cache.js';
import { given } from '../../given.js';

describe('when the observable page changes', given(an_observable_query_cache, context => {
    beforeEach(() => context.establish());
    afterEach(() => context.cleanup());

    it('should reset before committing and subscribe with the new page', async () => {
        const committed: string[][] = [];
        const { result } = renderHook(() => {
            const hook = useObservableQueryWithPaging<string[], ObservableQueryWithArguments>(ObservableQueryWithArguments, new Paging(0, 10));
            useLayoutEffect(() => { committed.push(hook[0].data); });
            return hook;
        }, { wrapper: context.wrapper });
        await context.push(0, ['first page']);
        committed.length = 0;

        await act(async () => { await result.current[2](1); });

        committed[0].should.deep.equal([]);
        result.current[0].data.should.deep.equal([]);
        result.current[0].isReady.should.be.false;
        result.current[0].isPerforming.should.be.true;
        context.connectStub.should.have.been.calledTwice;
        context.connectStub.secondCall.args[1].should.deep.equal({ page: 1, pageSize: 10 });
        await waitFor(() => context.disconnectStub.should.have.been.calledOn(context.connectStub.firstCall.thisValue));

        await context.push(1, ['second page']);
        result.current[0].data.should.deep.equal(['second page']);
        result.current[0].isReady.should.be.true;
    });
}));
