// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import { QueryInstanceCache, QueryResult, QueryResultWithState } from '@cratis/arc/queries';
import { useQuery } from '../useQuery';
import { FakeQuery, FakeQueryResult } from './FakeQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';

/**
 * A query whose own perform rejects - the hook accepts any performer, so it has to settle on a
 * terminal result for one that rejects rather than stay on its initial, permanently performing state.
 */
class RejectingQuery extends FakeQuery {
    override async perform(): Promise<QueryResult<FakeQueryResult[]>> {
        throw new Error('Performer failure');
    }
}

describe('when performing rejects', () => {
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

        const { result } = renderHook(() => useQuery(RejectingQuery), { wrapper });

        // Wait for the hook to settle on something other than its initial state, rather than on any of
        // the fields asserted below.
        const initialResult = result.current[0];
        await waitFor(() => (result.current[0] !== initialResult).should.be.true);

        queryResult = result.current[0];
    });

    it('should stop performing', () => queryResult.isPerforming.should.be.false);

    it('should not be successful', () => queryResult.isSuccess.should.be.false);

    it('should report that it has exceptions', () => queryResult.hasExceptions.should.be.true);

    it('should carry the error message', () => queryResult.exceptionMessages.should.deep.equal(['Performer failure']));
});
