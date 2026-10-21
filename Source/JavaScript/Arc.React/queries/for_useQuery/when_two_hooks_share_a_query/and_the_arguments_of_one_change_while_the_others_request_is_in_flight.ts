// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { waitFor, act } from '@testing-library/react';
import { QueryResultWithState } from '@cratis/arc/queries';
import { useQuery } from '../../useQuery.js';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersResult } from '../FakeQueryWithRequiredParameters.js';
import { a_shared_query_instance_cache } from '../given/a_shared_query_instance_cache.js';
import { given } from '../../../given.js';

describe('when two hooks share a query and the arguments of one change while the other\'s request is in flight', given(a_shared_query_instance_cache, context => {
    let changingResult: QueryResultWithState<unknown>;
    let stayingResult: QueryResultWithState<unknown>;
    let requests: { url: string; signal: AbortSignal }[];

    beforeEach(async () => {
        context.establish();
        requests = [];

        // Every request honors its abort signal and is held until released, so the argument change happens
        // while the staying hook's request is still in flight.
        let releaseRequests: () => void = () => { };
        const released = new Promise<void>(resolve => releaseRequests = resolve);
        context.fetchStub.callsFake((url: URL, requestInit: RequestInit) => new Promise<Response>((resolve, reject) => {
            requests.push({ url: url.toString(), signal: requestInit.signal! });
            requestInit.signal?.addEventListener('abort', () => reject(context.abortError()));
            const userId = url.toString().includes('userId=b') ? 'b' : 'a';
            released.then(() => resolve(context.successfulResponseWith([{ id: userId, name: `for ${userId}` }])));
        }));

        const changingRenders: QueryResultWithState<unknown>[] = [];
        const stayingRenders: QueryResultWithState<unknown>[] = [];

        const Changing = ({ userId }: { userId: string }) => {
            const [result] = useQuery(FakeQueryWithRequiredParameters, { userId, category: 'x' });
            changingRenders.push(result);
            return null;
        };

        const Staying = () => {
            const [result] = useQuery(FakeQueryWithRequiredParameters, { userId: 'a', category: 'x' });
            stayingRenders.push(result);
            return null;
        };

        const Page = ({ userId }: { userId: string }) =>
            React.createElement(React.Fragment, null,
                React.createElement(Changing, { userId }),
                React.createElement(Staying));

        const { rerender } = context.render(React.createElement(Page, { userId: 'a' }));
        await waitFor(() => context.fetchStub.should.have.been.calledTwice);

        rerender(React.createElement(Page, { userId: 'b' }));
        await waitFor(() => context.fetchStub.should.have.been.calledThrice);

        await act(async () => {
            releaseRequests();
            await context.nextTurn();
        });
        await waitFor(() => {
            changingRenders[changingRenders.length - 1].isReady.should.be.true;
            stayingRenders[stayingRenders.length - 1].isReady.should.be.true;
        });

        changingResult = changingRenders[changingRenders.length - 1];
        stayingResult = stayingRenders[stayingRenders.length - 1];
    });

    afterEach(() => context.cleanup());

    it('should not abort the staying hook\'s request', () => requests[1].signal.aborted.should.be.false);
    it('should give the staying hook the data for its own arguments', () => (stayingResult.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'a', name: 'for a' }]));
    it('should give the changing hook the data for its new arguments', () => (changingResult.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'b', name: 'for b' }]));
}));
