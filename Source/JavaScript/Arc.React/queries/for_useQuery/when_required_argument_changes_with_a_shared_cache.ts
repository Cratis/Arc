// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../useQuery.js';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersResult } from './FakeQueryWithRequiredParameters.js';
import { ArcContext, ArcConfiguration } from '../../ArcContext.js';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext.js';

describe('when a required argument changes with a shared cache', () => {
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let queryCache: QueryInstanceCache;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const responseFor = (userId: string) => ({
        json: async () => ({
            data: [{ id: userId, name: `for ${userId}` }],
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
        })
    } as Response);

    const keyFor = (userId: string) => queryCache.buildKey(FakeQueryWithRequiredParameters.name, { userId, category: 'x' });

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        const fetchStub = fetchHelper.stubFetch();
        fetchStub.callsFake(async (url: URL) => responseFor(url.toString().includes('userId=b') ? 'b' : 'a'));

        queryCache = new QueryInstanceCache();
        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        const { result, rerender } = renderHook(
            ({ userId }: { userId: string }) => useQuery(FakeQueryWithRequiredParameters, { userId, category: 'x' }),
            { wrapper, initialProps: { userId: 'a' } });

        await waitFor(() => (result.current[0].data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'a', name: 'for a' }]));
        rerender({ userId: 'b' });
        await waitFor(() => (result.current[0].data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'b', name: 'for b' }]));
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should store the new result under the key for the new arguments', () => (queryCache.getLastResult(keyFor('b'))!.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'b', name: 'for b' }]));
    it('should keep the previous result under the key for the previous arguments', () => (queryCache.getLastResult(keyFor('a'))!.data as FakeQueryWithRequiredParametersResult[]).should.deep.equal([{ id: 'a', name: 'for a' }]));
});
