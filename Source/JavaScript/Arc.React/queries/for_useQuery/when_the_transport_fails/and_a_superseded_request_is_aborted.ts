// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor, act } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../../useQuery';
import { FakeQuery, FakeQueryResult } from '../FakeQuery';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext';

describe('and a superseded request is aborted', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let queryResult: QueryResultWithState<unknown>;

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

        // The stub does not honor the abort signal, so the ordering that makes this scenario dangerous
        // is forced by hand: the superseded request is held open until after the newer one has settled.
        let rejectSupersededRequest: (error: Error) => void = () => { };
        fetchStub.onFirstCall().returns(new Promise<Response>((_, reject) => {
            rejectSupersededRequest = reject;
        }));
        fetchStub.onSecondCall().resolves(successfulResponse);

        const queryCache = new QueryInstanceCache();
        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        const { result } = renderHook(() => useQuery(FakeQuery), { wrapper });

        await waitFor(() => fetchStub.should.have.been.calledOnce);

        await act(async () => {
            await result.current[1]();
        });

        await waitFor(() => result.current[0].hasData.should.be.true);

        await act(async () => {
            rejectSupersededRequest(Object.assign(new Error('The operation was aborted'), { name: 'AbortError' }));
            await new Promise(resolve => setTimeout(resolve, 0));
        });

        queryResult = result.current[0];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should keep the data from the newer request', () => (queryResult.data as FakeQueryResult[]).should.deep.equal([{ id: '2', name: 'newer' }]));

    it('should not report the abort as an exception', () => queryResult.hasExceptions.should.be.false);
});
