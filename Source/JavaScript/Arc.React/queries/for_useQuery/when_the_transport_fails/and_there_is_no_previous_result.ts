// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { renderHook, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { useQuery } from '../../useQuery';
import { FakeQuery } from '../FakeQuery';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext';
import { QueryScopeContext } from '../../QueryScope';
import { IQueryScope } from '../../IQueryScope';

describe('and there is no previous result', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let notifyPerformingCompleted: sinon.SinonSpy;
    let queryResult: QueryResultWithState<unknown>;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.rejects(new Error('Network error'));

        notifyPerformingCompleted = sinon.spy();
        const queryScope: IQueryScope = {
            parent: undefined,
            isPerforming: false,
            addChildScope: sinon.spy(),
            notifyPerformingStarted: sinon.spy(),
            notifyPerformingCompleted
        };
        const queryCache = new QueryInstanceCache();

        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config },
                    React.createElement(QueryScopeContext.Provider, { value: queryScope }, children)));

        const { result } = renderHook(() => useQuery(FakeQuery), { wrapper });

        // Wait for the hook to settle on something other than its initial state, rather than on any of
        // the fields asserted below - so a wrong field cannot be hidden by the wait that produced it.
        const initialResult = result.current[0];
        await waitFor(() => (result.current[0] !== initialResult).should.be.true);

        queryResult = result.current[0];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should stop performing', () => queryResult.isPerforming.should.be.false);

    it('should not be successful', () => queryResult.isSuccess.should.be.false);

    it('should report that it has exceptions', () => queryResult.hasExceptions.should.be.true);

    it('should carry the transport error message', () => queryResult.exceptionMessages.should.deep.equal(['Network error']));

    it('should notify the query scope that performing completed', () => notifyPerformingCompleted.should.have.been.called);
});
