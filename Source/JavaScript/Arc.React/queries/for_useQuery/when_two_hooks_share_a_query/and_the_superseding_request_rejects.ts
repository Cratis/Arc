// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { QueryResult, QueryResultWithState } from '@cratis/arc/queries';
import { useQuery } from '../../useQuery.js';
import { FakeQuery, FakeQueryResult } from '../FakeQuery.js';
import { a_shared_query_instance_cache } from '../given/a_shared_query_instance_cache.js';
import { given } from '../../../given.js';

describe('when two hooks share a query and the superseding request rejects', given(a_shared_query_instance_cache, context => {
    let perform: sinon.SinonStub;
    let firstResult: QueryResultWithState<unknown>;
    let secondResult: QueryResultWithState<unknown>;

    beforeEach(async () => {
        context.establish();

        // The query's own perform is replaced rather than the network: the first request stays in flight until
        // the second aborts it, and the second rejects outright instead of resolving to an unsuccessful result.
        let abortFirstRequest: (error: Error) => void = () => { };
        perform = sinon.stub(FakeQuery.prototype, 'perform');
        perform.onFirstCall().callsFake(() => new Promise<QueryResult<FakeQueryResult[]>>((resolve, reject) => abortFirstRequest = reject));
        perform.onSecondCall().callsFake(() => {
            abortFirstRequest(context.abortError());
            return Promise.reject(new Error('Interceptor failed'));
        });

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

        await waitFor(() => secondRenders[secondRenders.length - 1].hasExceptions.should.be.true);
        await context.letPendingWorkSettle();

        firstResult = firstRenders[firstRenders.length - 1];
        secondResult = secondRenders[secondRenders.length - 1];
    });

    afterEach(() => {
        perform.restore();
        context.cleanup();
    });

    it('should report the rejection on the superseded hook', () => firstResult.exceptionMessages.should.deep.equal(['Interceptor failed']));
    it('should settle the superseded hook as unsuccessful', () => firstResult.isSuccess.should.be.false);
    it('should stop the superseded hook performing', () => firstResult.isPerforming.should.be.false);
    it('should make the superseded hook ready', () => firstResult.isReady.should.be.true);
    it('should report the rejection on the superseding hook', () => secondResult.exceptionMessages.should.deep.equal(['Interceptor failed']));
    it('should make the superseding hook ready', () => secondResult.isReady.should.be.true);
}));
