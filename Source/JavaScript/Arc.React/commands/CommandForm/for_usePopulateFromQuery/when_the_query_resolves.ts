// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { QueryInstanceCache } from '@cratis/arc/queries';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { QueryInstanceCacheContext } from '../../../queries/QueryInstanceCacheContext';
import { usePopulateFromQuery } from '../usePopulateFromQuery';
import { FakePopulateQuery, FakePopulateQueryResult } from './FakePopulateQuery';

describe('when the query resolves', () => {
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let queryCache: QueryInstanceCache;

    beforeEach(() => {
        queryCache = new QueryInstanceCache();
        fetchHelper = createFetchHelper();
        fetchHelper.stubFetch().resolves({
            json: async () => ({
                data: { name: 'Jane Austen', email: 'jane@example.com' },
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
            }),
        } as Response);
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    const wrapper = ({ children }: { children: React.ReactNode }) =>
        React.createElement(
            QueryInstanceCacheContext.Provider,
            { value: queryCache },
            React.createElement(ArcContext.Provider, { value: config }, children),
        );

    it('should return data from a compatibility envelope through the initial value transform', async () => {
        const initialValue = sinon
            .stub()
            .callsFake((source: FakePopulateQueryResult) => source.name.toUpperCase());
        const { result } = renderHook(
            () => {
                const population = usePopulateFromQuery<
                    FakePopulateQueryResult,
                    FakePopulateQuery
                >(FakePopulateQuery);
                return population === undefined ? undefined : initialValue(population);
            },
            { wrapper },
        );

        await waitFor(() => (result.current !== undefined).should.be.true);
        if (result.current === undefined) {
            throw new Error('Expected query population data');
        }
        result.current.should.equal('JANE AUSTEN');
        initialValue.calledOnce.should.equal(true);
    });
});
