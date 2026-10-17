// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor, act } from '@testing-library/react';
import { QueryInstanceCache, QueryResult, QueryResultWithState } from '@cratis/arc/queries';
import { useQuery } from '../../useQuery.js';
import { FakeQuery, FakeQueryResult } from '../FakeQuery.js';
import { ArcContext, ArcConfiguration } from '../../../ArcContext.js';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext.js';

let performCount = 0;
let abortFirstRequest: (error: Error) => void = () => { };

/**
 * A query whose first request stays in flight until the second aborts it, and whose second request
 * rejects outright rather than resolving to an unsuccessful result.
 */
class FakeRejectingOnSupersedeQuery extends FakeQuery {
    override perform(): Promise<QueryResult<FakeQueryResult[]>> {
        performCount++;
        if (performCount === 1) {
            return new Promise((_resolve, reject) => abortFirstRequest = reject);
        }
        abortFirstRequest(Object.assign(new Error('The operation was aborted'), { name: 'AbortError' }));
        return Promise.reject(new Error('Interceptor failed'));
    }
}

describe('when two hooks share a query and the superseding request rejects', () => {
    let firstResult: QueryResultWithState<unknown>;
    let secondResult: QueryResultWithState<unknown>;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    beforeEach(async () => {
        performCount = 0;
        const queryCache = new QueryInstanceCache();
        const firstRenders: QueryResultWithState<unknown>[] = [];
        const secondRenders: QueryResultWithState<unknown>[] = [];

        const First = () => {
            const [result] = useQuery(FakeRejectingOnSupersedeQuery);
            firstRenders.push(result);
            return null;
        };

        const Second = () => {
            const [result] = useQuery(FakeRejectingOnSupersedeQuery);
            secondRenders.push(result);
            return null;
        };

        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        render(
            React.createElement(React.Fragment, null,
                React.createElement(First),
                React.createElement(Second)),
            { wrapper });

        await waitFor(() => secondRenders[secondRenders.length - 1].hasExceptions.should.be.true);
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, 0));
        });

        firstResult = firstRenders[firstRenders.length - 1];
        secondResult = secondRenders[secondRenders.length - 1];
    });

    it('should report the rejection on the superseded hook', () => firstResult.exceptionMessages.should.deep.equal(['Interceptor failed']));
    it('should settle the superseded hook as unsuccessful', () => firstResult.isSuccess.should.be.false);
    it('should stop the superseded hook performing', () => firstResult.isPerforming.should.be.false);
    it('should make the superseded hook ready', () => firstResult.isReady.should.be.true);
    it('should report the rejection on the superseding hook', () => secondResult.exceptionMessages.should.deep.equal(['Interceptor failed']));
    it('should make the superseding hook ready', () => secondResult.isReady.should.be.true);
});
