// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { waitFor, act } from '@testing-library/react';
import sinon from 'sinon';
import { QueryResultWithState } from '@cratis/arc/queries';
import { PerformQuery, useQuery } from '../../useQuery.js';
import { FakeQueryWithRequiredParameters, FakeQueryWithRequiredParametersArguments } from '../FakeQueryWithRequiredParameters.js';
import { QueryScopeContext } from '../../QueryScopeContext.js';
import { QueryScopeImplementation } from '../../QueryScopeImplementation.js';
import { a_shared_query_instance_cache } from '../given/a_shared_query_instance_cache.js';
import { given } from '../../../given.js';

describe('when two hooks share a query and the superseded hook performs again without reaching the server', given(a_shared_query_instance_cache, context => {
    let supersededResult: QueryResultWithState<unknown>;
    let stateAfterLocalPerform: QueryResultWithState<unknown>;
    let supersededScope: QueryScopeImplementation;
    let supersededScopeStarts: sinon.SinonSpy;
    let supersededScopeCompletions: sinon.SinonSpy;
    let supersededScopePerformingAfterLocalPerform: boolean;

    beforeEach(async () => {
        context.establish();

        context.fetchStub.onFirstCall().callsFake((url: URL, requestInit: RequestInit) => context.untilAborted(requestInit));

        // The superseding request is held open while the superseded hook performs again.
        let completeSupersedingRequest: (response: Response) => void = () => { };
        context.fetchStub.onSecondCall().returns(new Promise<Response>(resolve => {
            completeSupersedingRequest = resolve;
        }));

        // The hooks share a cache entry but belong to different query scopes.
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

        context.render(
            React.createElement(React.Fragment, null,
                React.createElement(QueryScopeContext.Provider, { value: supersededScope }, React.createElement(Superseded)),
                React.createElement(QueryScopeContext.Provider, { value: supersedingScope }, React.createElement(Superseding))));

        await waitFor(() => context.fetchStub.should.have.been.calledTwice);

        // Missing a required argument, so the query settles locally without a request and without aborting
        // the superseding one the hook's first request is still waiting on.
        await act(async () => {
            await performSuperseded({ userId: 'a' } as FakeQueryWithRequiredParametersArguments);
        });
        stateAfterLocalPerform = supersededRenders[supersededRenders.length - 1];
        supersededScopePerformingAfterLocalPerform = supersededScope.isPerforming;

        await act(async () => {
            completeSupersedingRequest(context.successfulResponseWith([{ id: '1', name: 'shared' }]));
            await context.nextTurn();
        });
        await context.letPendingWorkSettle();

        supersededResult = supersededRenders[supersededRenders.length - 1];
    });

    afterEach(() => context.cleanup());

    it('should have settled the newer local perform as unsuccessful', () => stateAfterLocalPerform.isSuccess.should.be.false);
    it('should keep the state of the newer local perform', () => supersededResult.should.equal(stateAfterLocalPerform));
    it('should not leave the superseded hooks scope performing after the newer local perform', () => supersededScopePerformingAfterLocalPerform.should.be.false);
    it('should complete performing on the superseded hooks scope once for every start', () => supersededScopeCompletions.callCount.should.equal(supersededScopeStarts.callCount));
    it('should keep the superseded hooks scope not performing', () => supersededScope.isPerforming.should.be.false);
}));
