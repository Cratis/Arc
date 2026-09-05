// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, renderHook, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResult } from '@cratis/arc/queries';
import { type ArcConfiguration, ArcContext } from '../../../ArcContext';
import { QueryInstanceCacheContext } from '../../../queries/QueryInstanceCacheContext';
import { usePopulateFromObservableQuery } from '../usePopulateFromQuery';
import {
    FakeObservablePopulateQuery,
    type FakePopulateQueryResult,
} from './FakePopulateQuery';

describe('when the observable query resolves', () => {
    let queryCache: QueryInstanceCache;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    beforeEach(() => {
        queryCache = new QueryInstanceCache();
        FakeObservablePopulateQuery.reset();
    });

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
                const population = usePopulateFromObservableQuery<
                    FakePopulateQueryResult,
                    FakeObservablePopulateQuery
                >(FakeObservablePopulateQuery);
                return population === undefined ? undefined : initialValue(population);
            },
            { wrapper },
        );

        const compatibilityResult = new QueryResult<FakePopulateQueryResult>(
            {
                data: { name: 'Jane Austen', email: 'jane@example.com' },
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
            },
            Object,
            false,
        );
        delete (compatibilityResult as { isReady?: boolean }).isReady;

        await act(async () => {
            FakeObservablePopulateQuery.subscribeCallbacks[0](compatibilityResult);
        });
        await waitFor(() => (result.current !== undefined).should.equal(true));

        if (result.current === undefined) {
            throw new Error('Expected observable population data');
        }
        result.current.should.equal('JANE AUSTEN');
        initialValue.calledOnce.should.equal(true);
    });
});
