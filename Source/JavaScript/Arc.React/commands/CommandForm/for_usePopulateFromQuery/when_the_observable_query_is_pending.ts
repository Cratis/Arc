// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache } from '@cratis/arc/queries';
import { type ArcConfiguration, ArcContext } from '../../../ArcContext';
import { QueryInstanceCacheContext } from '../../../queries/QueryInstanceCacheContext';
import { usePopulateFromObservableQuery } from '../usePopulateFromQuery';
import {
    FakeObservablePopulateQuery,
    type FakePopulateQueryResult,
} from './FakePopulateQuery';

describe('when the observable query is pending', () => {
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

    it('should not pass the non-null default to the initial value transform', () => {
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

        (result.current === undefined).should.equal(true);
        initialValue.called.should.equal(false);
    });
});
