// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { waitFor } from '@testing-library/react';
import { QueryResultWithState } from '@cratis/arc/queries';
import { useQuery } from '../../useQuery.js';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersResult } from '../FakeQueryWithRequiredParameters.js';
import { a_shared_query_instance_cache } from '../given/a_shared_query_instance_cache.js';
import { given } from '../../../given.js';

describe('when two hooks share a query and the arguments differ', given(a_shared_query_instance_cache, context => {
    let firstResult: QueryResultWithState<unknown>;
    let secondResult: QueryResultWithState<unknown>;
    let signals: AbortSignal[];

    beforeEach(async () => {
        context.establish();
        signals = [];

        context.fetchStub.callsFake(async (url: URL, requestInit: RequestInit) => {
            signals.push(requestInit.signal!);
            await context.nextTurn();
            const userId = url.toString().includes('userId=a') ? 'a' : 'b';
            return context.successfulResponseWith([{ id: userId, name: `for ${userId}` }]);
        });

        const firstRenders: QueryResultWithState<unknown>[] = [];
        const secondRenders: QueryResultWithState<unknown>[] = [];

        const First = () => {
            const [result] = useQuery(FakeQueryWithRequiredParameters, { userId: 'a', category: 'x' });
            firstRenders.push(result);
            return null;
        };

        const Second = () => {
            const [result] = useQuery(FakeQueryWithRequiredParameters, { userId: 'b', category: 'x' });
            secondRenders.push(result);
            return null;
        };

        context.render(
            React.createElement(React.Fragment, null,
                React.createElement(First),
                React.createElement(Second)));

        await waitFor(() => context.fetchStub.should.have.been.calledTwice);
        await waitFor(() => {
            firstRenders[firstRenders.length - 1].isReady.should.be.true;
            secondRenders[secondRenders.length - 1].isReady.should.be.true;
        });
        await context.letPendingWorkSettle();

        firstResult = firstRenders[firstRenders.length - 1];
        secondResult = secondRenders[secondRenders.length - 1];
    });

    afterEach(() => context.cleanup());

    it('should not abort either request', () => signals.some(signal => signal.aborted).should.be.false);
    it('should give the first hook its own data', () => (firstResult.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'a', name: 'for a' }]));
    it('should give the second hook its own data', () => (secondResult.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'b', name: 'for b' }]));
}));
