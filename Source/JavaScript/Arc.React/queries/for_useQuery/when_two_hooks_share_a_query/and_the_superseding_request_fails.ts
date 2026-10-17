// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { waitFor } from '@testing-library/react';
import { QueryResultWithState } from '@cratis/arc/queries';
import { useQuery } from '../../useQuery.js';
import { FakeQuery } from '../FakeQuery.js';
import { a_shared_query_instance_cache } from '../given/a_shared_query_instance_cache.js';
import { given } from '../../../given.js';

describe('when two hooks share a query and the superseding request fails', given(a_shared_query_instance_cache, context => {
    let firstResult: QueryResultWithState<unknown>;
    let secondResult: QueryResultWithState<unknown>;

    beforeEach(async () => {
        context.establish();

        context.fetchStub.onFirstCall().callsFake((url: URL, requestInit: RequestInit) => context.untilAborted(requestInit));
        context.fetchStub.onSecondCall().rejects(new TypeError('Failed to fetch'));

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

        // A precondition, not an assertion: the hook that issued the failing request has settled.
        await waitFor(() => secondRenders[secondRenders.length - 1].hasExceptions.should.be.true);
        await context.letPendingWorkSettle();

        firstResult = firstRenders[firstRenders.length - 1];
        secondResult = secondRenders[secondRenders.length - 1];
    });

    afterEach(() => context.cleanup());

    it('should settle the superseded hook as unsuccessful', () => firstResult.isSuccess.should.be.false);
    it('should report the failure on the superseded hook', () => firstResult.exceptionMessages.should.deep.equal(['Failed to fetch']));
    it('should report an exception on the superseded hook', () => firstResult.hasExceptions.should.be.true);
    it('should report the superseded hook ready', () => firstResult.isReady.should.be.true);
    it('should stop the superseded hook performing', () => firstResult.isPerforming.should.be.false);
    it('should settle the superseding hook as unsuccessful', () => secondResult.isSuccess.should.be.false);
    it('should stop the superseding hook performing', () => secondResult.isPerforming.should.be.false);
}));
