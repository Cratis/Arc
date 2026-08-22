// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook } from '@testing-library/react';
import sinon from 'sinon';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { QueryInstanceCache } from '@cratis/arc/queries';
import { type ArcConfiguration, ArcContext } from '../../../ArcContext';
import { QueryInstanceCacheContext } from '../../../queries/QueryInstanceCacheContext';
import { usePopulateFromQuery } from '../usePopulateFromQuery';
import { FakePopulateQuery, type FakePopulateQueryResult } from './FakePopulateQuery';

describe('when the query is pending', () => {
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let queryCache: QueryInstanceCache;

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

    beforeEach(() => {
        queryCache = new QueryInstanceCache();
        fetchHelper = createFetchHelper();
        fetchHelper.stubFetch().returns(new Promise(() => {}));
    });

    afterEach(() => fetchHelper.restore());

    it('should not pass the non-null default to the initial value transform', () => {
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

        (result.current === undefined).should.equal(true);
        initialValue.called.should.equal(false);
    });
});
