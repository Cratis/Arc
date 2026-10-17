// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { waitFor } from '@testing-library/react';
import { Paging, QueryResultWithState } from '@cratis/arc/queries';
import { useQueryWithPaging } from '../useQuery.js';
import { FakeQuery, FakeQueryResult } from '../for_useQuery/FakeQuery.js';
import { a_shared_query_instance_cache } from '../for_useQuery/given/a_shared_query_instance_cache.js';
import { given } from '../../given.js';

describe('when two hooks share a query', given(a_shared_query_instance_cache, context => {
    let supersededResult: QueryResultWithState<unknown>;

    beforeEach(async () => {
        context.establish();

        context.fetchStub.onFirstCall().callsFake((url: URL, requestInit: RequestInit) => context.untilAborted(requestInit));
        context.fetchStub.onSecondCall().resolves(context.successfulResponseWith(
            [{ id: '1', name: 'shared' }],
            { page: 0, size: 10, totalItems: 1, totalPages: 1 }));

        const supersededRenders: QueryResultWithState<unknown>[] = [];
        const supersedingRenders: QueryResultWithState<unknown>[] = [];

        const Superseded = () => {
            const [result] = useQueryWithPaging(FakeQuery, new Paging(0, 10));
            supersededRenders.push(result);
            return null;
        };

        const Superseding = () => {
            const [result] = useQueryWithPaging(FakeQuery, new Paging(0, 10));
            supersedingRenders.push(result);
            return null;
        };

        context.render(
            React.createElement(React.Fragment, null,
                React.createElement(Superseded),
                React.createElement(Superseding)));

        await waitFor(() => context.fetchStub.should.have.been.calledTwice);
        await waitFor(() => supersedingRenders[supersedingRenders.length - 1].isReady.should.be.true);
        await context.letPendingWorkSettle();

        supersededResult = supersededRenders[supersededRenders.length - 1];
    });

    afterEach(() => context.cleanup());

    it('should give the superseded hook the shared data', () => (supersededResult.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'shared' }]));
    it('should give the superseded hook the shared paging', () => supersededResult.paging.totalItems.should.equal(1));
    it('should report the superseded hook ready', () => supersededResult.isReady.should.be.true);
    it('should stop the superseded hook performing', () => supersededResult.isPerforming.should.be.false);
}));
