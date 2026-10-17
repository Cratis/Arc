// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { waitFor, act } from '@testing-library/react';
import { QueryResultWithState } from '@cratis/arc/queries';
import { useQuery } from '../../useQuery.js';
import { FakeQuery, FakeQueryResult } from '../FakeQuery.js';
import { a_shared_query_instance_cache } from '../given/a_shared_query_instance_cache.js';
import { given } from '../../../given.js';

describe('when two hooks share a query and the superseding hook unmounts while its request is in flight', given(a_shared_query_instance_cache, context => {
    let remainingResult: QueryResultWithState<unknown>;

    beforeEach(async () => {
        context.establish();

        context.fetchStub.onFirstCall().callsFake((url: URL, requestInit: RequestInit) => context.untilAborted(requestInit));

        // The superseding request is held open until its hook has unmounted.
        let completeSupersedingRequest: (response: Response) => void = () => { };
        context.fetchStub.onSecondCall().returns(new Promise<Response>(resolve => {
            completeSupersedingRequest = resolve;
        }));

        const remainingRenders: QueryResultWithState<unknown>[] = [];

        const Remaining = () => {
            const [result] = useQuery(FakeQuery);
            remainingRenders.push(result);
            return null;
        };

        const Superseding = () => {
            useQuery(FakeQuery);
            return null;
        };

        const Page = ({ showSuperseding }: { showSuperseding: boolean }) =>
            React.createElement(React.Fragment, null,
                React.createElement(Remaining),
                showSuperseding ? React.createElement(Superseding) : null);

        const { rerender } = context.render(React.createElement(Page, { showSuperseding: true }));

        await waitFor(() => context.fetchStub.should.have.been.calledTwice);

        rerender(React.createElement(Page, { showSuperseding: false }));

        await act(async () => {
            completeSupersedingRequest(context.successfulResponseWith([{ id: '1', name: 'shared' }]));
            await context.nextTurn();
        });

        await waitFor(() => remainingRenders[remainingRenders.length - 1].isReady.should.be.true);

        remainingResult = remainingRenders[remainingRenders.length - 1];
    });

    afterEach(() => context.cleanup());

    it('should give the remaining hook the shared data', () => (remainingResult.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'shared' }]));
    it('should stop the remaining hook performing', () => remainingResult.isPerforming.should.be.false);
}));
