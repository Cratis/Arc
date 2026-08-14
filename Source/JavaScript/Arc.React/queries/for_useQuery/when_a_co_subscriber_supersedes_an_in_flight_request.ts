// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor, act } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../useQuery';
import { FakeQuery, FakeQueryResult } from './FakeQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';

describe('when a co subscriber supersedes an in flight request', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let supersededResult: QueryResultWithState<unknown>;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const successfulResponse = {
        json: async () => ({
            data: [{ id: '2', name: 'newer' }],
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
        })
    } as Response;

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();

        // The first request honors its abort signal instead of being rejected by hand, so nothing here
        // simulates the abort - it is produced by the second component superseding the first through the
        // shared query instance. If the two components ever stopped sharing that instance, no abort would
        // be raised at all and this spec would fail rather than quietly prove nothing.
        fetchStub.onFirstCall().callsFake((_url: URL, init: RequestInit) => new Promise<Response>((_resolve, reject) => {
            init.signal?.addEventListener('abort', () =>
                reject(Object.assign(new Error('The operation was aborted'), { name: 'AbortError' })));
        }));
        fetchStub.onSecondCall().resolves(successfulResponse);

        // A single cache plus one query type without arguments gives both components the same cache key,
        // and therefore one shared QueryFor instance - a page header and a table on the same query.
        const queryCache = new QueryInstanceCache();

        const supersededRenders: QueryResultWithState<unknown>[] = [];
        const supersedingRenders: QueryResultWithState<unknown>[] = [];

        const SupersededSubscriber = () => {
            const [result] = useQuery(FakeQuery);
            supersededRenders.push(result);
            return null;
        };

        const SupersedingSubscriber = () => {
            const [result] = useQuery(FakeQuery);
            supersedingRenders.push(result);
            return null;
        };

        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        render(
            React.createElement(React.Fragment, null,
                React.createElement(SupersededSubscriber),
                React.createElement(SupersedingSubscriber)),
            { wrapper });

        // Both components issued a request of their own - without that, there is nothing to supersede.
        await waitFor(() => fetchStub.should.have.been.calledTwice);

        // Waiting on the superseding component settling with its payload, rather than on anything
        // asserted below, so no assertion is proven by the wait that produced it. The abort is raised
        // before that point, so the superseded component has had every opportunity to settle when read.
        // This is a precondition and deliberately not an assertion: nothing in the abort path can reach
        // the superseding component's own state, so asserting on it here would prove nothing.
        await waitFor(() => (supersedingRenders[supersedingRenders.length - 1].data as FakeQueryResult[]).should.deep.equal([{ id: '2', name: 'newer' }]));
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, 0));
        });

        supersededResult = supersededRenders[supersededRenders.length - 1];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should stop the superseded component performing', () => supersededResult.isPerforming.should.be.false);

    it('should not report the abort as an exception on the superseded component', () => supersededResult.hasExceptions.should.be.false);
});
