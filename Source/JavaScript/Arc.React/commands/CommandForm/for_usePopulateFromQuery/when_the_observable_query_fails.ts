// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, renderHook } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResult } from '@cratis/arc/queries';
import { type ArcConfiguration, ArcContext } from '../../../ArcContext';
import { QueryInstanceCacheContext } from '../../../queries/QueryInstanceCacheContext';
import { usePopulateFromObservableQuery } from '../usePopulateFromQuery';
import {
    FakeObservablePopulateQuery,
    type FakePopulateQueryResult,
    pendingDefaultValue,
} from './FakePopulateQuery';

describe('when the observable query fails', () => {
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

    it('should not pass the terminal failed default to the initial value transform', async () => {
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

        await act(async () => {
            FakeObservablePopulateQuery.subscribeCallbacks[0](
                new QueryResult(
                    {
                        data: pendingDefaultValue,
                        isSuccess: false,
                        isReady: true,
                        isAuthorized: true,
                        isValid: true,
                        hasExceptions: true,
                        validationResults: [],
                        exceptionMessages: ['Query failed'],
                        exceptionStackTrace: '',
                        paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
                    },
                    Object,
                    false,
                ),
            );
        });

        (result.current === undefined).should.equal(true);
        initialValue.called.should.equal(false);
    });
});
