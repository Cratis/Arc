// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { useQuery } from '../../useQuery';
import { FakeNullishRejectingQuery } from '../FakeNullishRejectingQuery';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext';

describe('and the rejection has no message', () => {
    let queryResult: QueryResultWithState<unknown>;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    beforeEach(async () => {
        const queryCache = new QueryInstanceCache();
        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        const { result } = renderHook(() => useQuery(FakeNullishRejectingQuery), { wrapper });

        // Wait for the hook to settle on something other than its initial state, rather than on any of
        // the fields asserted below - so a wrong field cannot be hidden by the wait that produced it.
        const initialResult = result.current[0];
        await waitFor(() => (result.current[0] !== initialResult).should.be.true);

        queryResult = result.current[0];
    });

    it('should stop performing', () => queryResult.isPerforming.should.be.false);

    it('should report that it has exceptions', () => queryResult.hasExceptions.should.be.true);

    it('should describe the rejection it could not read a message from', () => queryResult.exceptionMessages.should.deep.equal(['undefined']));
});
