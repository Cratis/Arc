// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor, act } from '@testing-library/react';
import sinon from 'sinon';
import { Paging, QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQueryWithPaging } from '../useQuery.js';
import { FakeQuery, FakeQueryResult } from '../for_useQuery/FakeQuery.js';
import { ArcContext, ArcConfiguration } from '../../ArcContext.js';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext.js';

describe('when two hooks share a query', () => {
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
            data: [{ id: '1', name: 'shared' }],
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: 10, totalItems: 1, totalPages: 1 }
        })
    } as Response;

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();

        fetchStub.onFirstCall().callsFake((_url: URL, init: RequestInit) => new Promise<Response>((_resolve, reject) => {
            init.signal?.addEventListener('abort', () =>
                reject(Object.assign(new Error('The operation was aborted'), { name: 'AbortError' })));
        }));
        fetchStub.onSecondCall().resolves(successfulResponse);

        const queryCache = new QueryInstanceCache();
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

        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        render(
            React.createElement(React.Fragment, null,
                React.createElement(Superseded),
                React.createElement(Superseding)),
            { wrapper });

        await waitFor(() => fetchStub.should.have.been.calledTwice);
        await waitFor(() => supersedingRenders[supersedingRenders.length - 1].isReady.should.be.true);
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, 0));
        });

        supersededResult = supersededRenders[supersededRenders.length - 1];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should give the superseded hook the shared data', () => (supersededResult.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'shared' }]));
    it('should give the superseded hook the shared paging', () => supersededResult.paging.totalItems.should.equal(1));
    it('should report the superseded hook ready', () => supersededResult.isReady.should.be.true);
    it('should stop the superseded hook performing', () => supersededResult.isPerforming.should.be.false);
});
