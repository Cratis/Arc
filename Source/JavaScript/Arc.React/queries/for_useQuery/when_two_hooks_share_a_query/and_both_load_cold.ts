// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor, act } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../../useQuery.js';
import { FakeQuery, FakeQueryResult } from '../FakeQuery.js';
import { ArcContext, ArcConfiguration } from '../../../ArcContext.js';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext.js';

describe('when two hooks share a query and both load cold', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let firstResult: QueryResultWithState<unknown>;
    let secondResult: QueryResultWithState<unknown>;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const successfulResponse = {
        json: async () => ({
            data: [{ id: '1', name: 'shared' }],
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

        // The first request honors its abort signal, so the abort comes from the second hook superseding
        // it through the shared query instance - nothing here simulates it. Only the superseding request
        // ever completes.
        fetchStub.onFirstCall().callsFake((_url: URL, init: RequestInit) => new Promise<Response>((_resolve, reject) => {
            init.signal?.addEventListener('abort', () =>
                reject(Object.assign(new Error('The operation was aborted'), { name: 'AbortError' })));
        }));
        fetchStub.onSecondCall().resolves(successfulResponse);

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

        // A precondition, not an assertion: the hook that issued the superseding request has its payload.
        await waitFor(() => secondRenders[secondRenders.length - 1].isSuccess.should.be.true);
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, 0));
        });

        firstResult = firstRenders[firstRenders.length - 1];
        secondResult = secondRenders[secondRenders.length - 1];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should give the superseded hook the shared data', () => (firstResult.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'shared' }]));
    it('should report the superseded hook ready', () => firstResult.isReady.should.be.true);
    it('should stop the superseded hook performing', () => firstResult.isPerforming.should.be.false);
    it('should give the superseding hook the shared data', () => (secondResult.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'shared' }]));
    it('should stop the superseding hook performing', () => secondResult.isPerforming.should.be.false);
});
