// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { waitFor } from '@testing-library/react';
import { QueryResultWithState } from '@cratis/arc/queries';
import { useQuery } from '../../useQuery.js';
import { FakeQuery, FakeQueryResult } from '../FakeQuery.js';
import { a_shared_query_instance_cache } from '../given/a_shared_query_instance_cache.js';
import { given } from '../../../given.js';

describe('when two hooks share a query and both load cold', given(a_shared_query_instance_cache, context => {
    let firstResult: QueryResultWithState<unknown>;
    let secondResult: QueryResultWithState<unknown>;

    beforeEach(async () => {
        context.establish();

        // Only the superseding request ever completes; the first is aborted by it.
        context.fetchStub.onFirstCall().callsFake((url: URL, requestInit: RequestInit) => context.untilAborted(requestInit));
        context.fetchStub.onSecondCall().resolves(context.successfulResponseWith([{ id: '1', name: 'shared' }]));

        const firstRenders: QueryResultWithState<unknown>[] = [];
        const secondRenders: QueryResultWithState<unknown>[] = [];

        const First = () => {
            const [result] = useQuery(FakeQuery);
            firstRenders.push(result);
            return null;
        };

        const Second = () => {
            const [result] = useQuery(FakeQuery);
            secondRenders.push(result);
            return null;
        };

        context.render(
            React.createElement(React.Fragment, null,
                React.createElement(First),
                React.createElement(Second)));

        await waitFor(() => context.fetchStub.should.have.been.calledTwice);

        // A precondition, not an assertion: the hook that issued the superseding request has its payload.
        await waitFor(() => secondRenders[secondRenders.length - 1].isSuccess.should.be.true);
        await context.letPendingWorkSettle();

        firstResult = firstRenders[firstRenders.length - 1];
        secondResult = secondRenders[secondRenders.length - 1];
    });

    afterEach(() => context.cleanup());

    it('should give the superseded hook the shared data', () => (firstResult.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'shared' }]));
    it('should report the superseded hook ready', () => firstResult.isReady.should.be.true);
    it('should stop the superseded hook performing', () => firstResult.isPerforming.should.be.false);
    it('should give the superseding hook the shared data', () => (secondResult.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'shared' }]));
    it('should stop the superseding hook performing', () => secondResult.isPerforming.should.be.false);
}));
