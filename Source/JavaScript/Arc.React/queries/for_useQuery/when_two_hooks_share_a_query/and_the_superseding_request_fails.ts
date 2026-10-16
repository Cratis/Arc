// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor, act } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../../useQuery.js';
import { FakeQuery } from '../FakeQuery.js';
import { ArcContext, ArcConfiguration } from '../../../ArcContext.js';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext.js';

describe('when two hooks share a query and the superseding request fails', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let firstResult: QueryResultWithState<unknown>;
    let secondResult: QueryResultWithState<unknown>;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();

        fetchStub.onFirstCall().callsFake((_url: URL, init: RequestInit) => new Promise<Response>((_resolve, reject) => {
            init.signal?.addEventListener('abort', () =>
                reject(Object.assign(new Error('The operation was aborted'), { name: 'AbortError' })));
        }));
        fetchStub.onSecondCall().rejects(new TypeError('Failed to fetch'));

        const queryCache = new QueryInstanceCache();
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

        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        render(
            React.createElement(React.Fragment, null,
                React.createElement(First),
                React.createElement(Second)),
            { wrapper });

        await waitFor(() => fetchStub.should.have.been.calledTwice);

        // A precondition, not an assertion: the hook that issued the failing request has settled.
        await waitFor(() => secondRenders[secondRenders.length - 1].hasExceptions.should.be.true);
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, 0));
        });

        firstResult = firstRenders[firstRenders.length - 1];
        secondResult = secondRenders[secondRenders.length - 1];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should settle the superseded hook as unsuccessful', () => firstResult.isSuccess.should.be.false);
    it('should report the failure on the superseded hook', () => firstResult.exceptionMessages.should.deep.equal(['Failed to fetch']));
    it('should stop the superseded hook performing', () => firstResult.isPerforming.should.be.false);
    it('should settle the superseding hook as unsuccessful', () => secondResult.isSuccess.should.be.false);
    it('should stop the superseding hook performing', () => secondResult.isPerforming.should.be.false);
});
