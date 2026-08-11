// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor, act } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../../useQuery';
import { FakeQuery } from '../FakeQuery';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext';

describe('and a previous result was settled', () => {
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
            data: [{ id: '1', name: 'first' }],
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
        fetchStub.resolves(successfulResponse);

        const queryCache = new QueryInstanceCache();
        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        const { result } = renderHook(() => useQuery(FakeQuery), { wrapper });

        await waitFor(() => result.current[0].hasData.should.be.true);

        fetchStub.resetBehavior();
        fetchStub.rejects(new Error('Network error'));

        await act(async () => {
            await result.current[1]();
        });

        queryResult = result.current[0];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should stop performing', () => queryResult.isPerforming.should.be.false);

    it('should no longer be successful', () => queryResult.isSuccess.should.be.false);

    it('should report that it has exceptions', () => queryResult.hasExceptions.should.be.true);

    it('should carry the transport error message', () => queryResult.exceptionMessages.should.deep.equal(['Network error']));
});
