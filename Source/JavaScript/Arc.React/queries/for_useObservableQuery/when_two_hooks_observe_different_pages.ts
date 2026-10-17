// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { act, renderHook } from '@testing-library/react';
import { Paging } from '@cratis/arc/queries';
import { useObservableQueryWithPaging } from '../useObservableQuery.js';
import { ObservableQueryWithArguments } from './given/ObservableQueryWithArguments.js';
import { an_observable_query_cache } from './given/an_observable_query_cache.js';
import { given } from '../../given.js';

describe('when two hooks observe different pages', given(an_observable_query_cache, context => {
    beforeEach(() => context.establish());
    afterEach(() => context.cleanup());

    it('should keep each page independent and share only matching subscriptions', async () => {
        const { result } = renderHook(() => [
            useObservableQueryWithPaging<string[], ObservableQueryWithArguments>(ObservableQueryWithArguments, new Paging(0, 10)),
            useObservableQueryWithPaging<string[], ObservableQueryWithArguments>(ObservableQueryWithArguments, new Paging(1, 10))
        ], { wrapper: context.wrapper });
        context.connectStub.should.have.been.calledTwice;
        context.connectStub.firstCall.args[1].should.deep.equal({ page: 0, pageSize: 10 });
        context.connectStub.secondCall.args[1].should.deep.equal({ page: 1, pageSize: 10 });
        await context.push(0, ['zero']);
        await context.push(1, ['one']);
        result.current[0][0].data.should.deep.equal(['zero']);
        result.current[1][0].data.should.deep.equal(['one']);

        await act(async () => { await result.current[0][2](1); });
        context.connectStub.should.have.been.calledTwice;
        result.current[0][0].data.should.deep.equal(['one']);
        await context.push(1, ['one updated']);
        result.current[0][0].data.should.deep.equal(['one updated']);
        result.current[1][0].data.should.deep.equal(['one updated']);
    });
}));
