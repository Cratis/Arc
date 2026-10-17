// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { waitFor, act } from '@testing-library/react';
import sinon from 'sinon';
import { QueryResultWithState } from '@cratis/arc/queries';
import { useQuery } from '../../useQuery.js';
import { FakeQuery, FakeQueryResult } from '../FakeQuery.js';
import { QueryScopeContext } from '../../QueryScopeContext.js';
import { QueryScopeImplementation } from '../../QueryScopeImplementation.js';
import { a_shared_query_instance_cache } from '../given/a_shared_query_instance_cache.js';
import { given } from '../../../given.js';

// Records every state update together with the component whose render created the state, so a state
// update reaching a hook after its component has unmounted is observable. React no longer warns about it.
const stateUpdates = vi.hoisted(() => ({ renderingComponent: undefined as string | undefined, updatedComponents: [] as string[] }));

vi.mock('react', async (importOriginal) => {
    const actual = await importOriginal<typeof import('react')>();
    const useState = ((initial: unknown) => {
        const [state, setState] = actual.useState(initial);
        const component = stateUpdates.renderingComponent;
        return [state, (next: unknown) => {
            if (component !== undefined) {
                stateUpdates.updatedComponents.push(component);
            }
            setState(next);
        }];
    }) as typeof actual.useState;
    return { ...actual, useState, default: { ...actual, useState } };
});

describe('when two hooks share a query and the superseded hook unmounts while the superseding request is in flight', given(a_shared_query_instance_cache, context => {
    let supersededScope: QueryScopeImplementation;
    let supersededScopeCompletions: sinon.SinonSpy;
    let supersededScopePerformingAfterUnmount: boolean;
    let supersededUpdatesAfterUnmount: string[];
    let supersedingResult: QueryResultWithState<unknown>;

    beforeEach(async () => {
        context.establish();
        stateUpdates.updatedComponents = [];

        context.fetchStub.onFirstCall().callsFake((url: URL, requestInit: RequestInit) => context.untilAborted(requestInit));

        // The superseding request is held open until the superseded hook has unmounted.
        let completeSupersedingRequest: (response: Response) => void = () => { };
        context.fetchStub.onSecondCall().returns(new Promise<Response>(resolve => {
            completeSupersedingRequest = resolve;
        }));

        // The hooks share a cache entry but belong to different query scopes.
        supersededScope = new QueryScopeImplementation();
        supersededScopeCompletions = sinon.spy(supersededScope, 'notifyPerformingCompleted');
        const supersedingScope = new QueryScopeImplementation();
        const supersedingRenders: QueryResultWithState<unknown>[] = [];

        const Superseded = () => {
            stateUpdates.renderingComponent = 'superseded';
            useQuery(FakeQuery);
            stateUpdates.renderingComponent = undefined;
            return null;
        };

        const Superseding = () => {
            const [result] = useQuery(FakeQuery);
            supersedingRenders.push(result);
            return null;
        };

        const Page = ({ showSuperseded }: { showSuperseded: boolean }) =>
            React.createElement(React.Fragment, null,
                showSuperseded
                    ? React.createElement(QueryScopeContext.Provider, { value: supersededScope }, React.createElement(Superseded))
                    : null,
                React.createElement(QueryScopeContext.Provider, { value: supersedingScope }, React.createElement(Superseding)));

        const { rerender } = context.render(React.createElement(Page, { showSuperseded: true }));

        await waitFor(() => context.fetchStub.should.have.been.calledTwice);

        rerender(React.createElement(Page, { showSuperseded: false }));
        supersededScopePerformingAfterUnmount = supersededScope.isPerforming;
        const updatesBeforeCompletion = stateUpdates.updatedComponents.length;

        await act(async () => {
            completeSupersedingRequest(context.successfulResponseWith([{ id: '1', name: 'shared' }]));
            await context.nextTurn();
        });

        await waitFor(() => supersedingRenders[supersedingRenders.length - 1].isReady.should.be.true);

        // The superseded hook's wait on the superseding request continues independently of the superseding
        // hook's render, so it is given several turns to reach - or not reach - the unmounted hook's state.
        for (let turn = 0; turn < 5; turn++) {
            await context.letPendingWorkSettle();
        }

        supersededUpdatesAfterUnmount = stateUpdates.updatedComponents.slice(updatesBeforeCompletion);
        supersedingResult = supersedingRenders[supersedingRenders.length - 1];
    });

    afterEach(() => context.cleanup());

    it('should stop the superseded hooks scope performing when it unmounts', () => supersededScopePerformingAfterUnmount.should.be.false);
    it('should complete performing on the superseded hooks scope exactly once', () => supersededScopeCompletions.should.have.been.calledOnce);
    it('should keep the superseded hooks scope not performing', () => supersededScope.isPerforming.should.be.false);
    it('should not update the state of the unmounted superseded hook', () => supersededUpdatesAfterUnmount.should.be.empty);
    it('should give the superseding hook the shared data', () => (supersedingResult.data as FakeQueryResult[]).should.deep.equal([{ id: '1', name: 'shared' }]));
    it('should make the superseding hook ready', () => supersedingResult.isReady.should.be.true);
}));
