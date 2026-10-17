// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, waitFor, act } from '@testing-library/react';
import sinon from 'sinon';
import { QueryInstanceCache, QueryResultWithState } from '@cratis/arc/queries';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { PerformQuery, useQuery } from '../../useQuery.js';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersArguments } from '../FakeQueryWithRequiredParameters.js';
import { ArcContext, ArcConfiguration } from '../../../ArcContext.js';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext.js';
import { QueryScopeContext } from '../../QueryScopeContext.js';
import { QueryScopeImplementation } from '../../QueryScopeImplementation.js';

describe('when two hooks share a query and the superseded hook performs again without reaching the server', () => {
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let supersededResult: QueryResultWithState<unknown>;
    let stateAfterLocalPerform: QueryResultWithState<unknown>;
    let supersededScope: QueryScopeImplementation;
    let supersededScopeStarts: sinon.SinonSpy;
    let supersededScopeCompletions: sinon.SinonSpy;
    let supersededScopePerformingAfterLocalPerform: boolean;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    const successfulResponse = {
        json: async () => ({
            data: [{ id: '1', name: 'shared' }],
            isSuccess: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
        })
    } as Response;

    beforeEach(async () => {
        fetchHelper = createFetchHelper();
        const fetchStub = fetchHelper.stubFetch();

        fetchStub.onFirstCall().callsFake((_url: URL, init: RequestInit) => new Promise<Response>((_resolve, reject) => {
            init.signal?.addEventListener('abort', () =>
                reject(Object.assign(new Error('The operation was aborted'), { name: 'AbortError' })));
        }));

        // The superseding request is held open while the superseded hook performs again.
        let completeSupersedingRequest: (response: Response) => void = () => { };
        fetchStub.onSecondCall().returns(new Promise<Response>(resolve => {
            completeSupersedingRequest = resolve;
        }));

        // The hooks share a cache entry but belong to different query scopes.
        const queryCache = new QueryInstanceCache();
        supersededScope = new QueryScopeImplementation();
        supersededScopeStarts = sinon.spy(supersededScope, 'notifyPerformingStarted');
        supersededScopeCompletions = sinon.spy(supersededScope, 'notifyPerformingCompleted');
        const supersedingScope = new QueryScopeImplementation();
        const supersededRenders: QueryResultWithState<unknown>[] = [];
        let performSuperseded: PerformQuery<FakeQueryWithRequiredParametersArguments> = async () => { };

        const Superseded = () => {
            const [result, perform] = useQuery(FakeQueryWithRequiredParameters, { userId: 'a', category: 'x' });
            supersededRenders.push(result);
            performSuperseded = perform;
            return null;
        };

        const Superseding = () => {
            useQuery(FakeQueryWithRequiredParameters, { userId: 'a', category: 'x' });
            return null;
        };

        const wrapper = ({ children }: { children: React.ReactNode }) =>
            React.createElement(QueryInstanceCacheContext.Provider, { value: queryCache },
                React.createElement(ArcContext.Provider, { value: config }, children));

        render(
            React.createElement(React.Fragment, null,
                React.createElement(QueryScopeContext.Provider, { value: supersededScope }, React.createElement(Superseded)),
                React.createElement(QueryScopeContext.Provider, { value: supersedingScope }, React.createElement(Superseding))),
            { wrapper });

        await waitFor(() => fetchStub.should.have.been.calledTwice);

        // Missing a required argument, so the query settles locally without a request and without aborting
        // the superseding one the hook's first request is still waiting on.
        await act(async () => {
            await performSuperseded({ userId: 'a' } as FakeQueryWithRequiredParametersArguments);
        });
        stateAfterLocalPerform = supersededRenders[supersededRenders.length - 1];
        supersededScopePerformingAfterLocalPerform = supersededScope.isPerforming;

        await act(async () => {
            completeSupersedingRequest(successfulResponse);
            await new Promise(resolve => setTimeout(resolve, 0));
        });
        await act(async () => {
            await new Promise(resolve => setTimeout(resolve, 0));
        });

        supersededResult = supersededRenders[supersededRenders.length - 1];
    });

    afterEach(() => {
        fetchHelper.restore();
    });

    it('should have settled the newer local perform as unsuccessful', () => stateAfterLocalPerform.isSuccess.should.be.false);
    it('should keep the state of the newer local perform', () => supersededResult.should.equal(stateAfterLocalPerform));
    it('should not leave the superseded hooks scope performing after the newer local perform', () => supersededScopePerformingAfterLocalPerform.should.be.false);
    it('should complete performing on the superseded hooks scope once for every start', () => supersededScopeCompletions.callCount.should.equal(supersededScopeStarts.callCount));
    it('should keep the superseded hooks scope not performing', () => supersededScope.isPerforming.should.be.false);
});
