// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, RenderResult, waitFor } from '@testing-library/react';
import sinon from 'sinon';
import { IdentityProvider } from '../../IdentityProvider';
import { useIdentity } from '../../useIdentity';
import { IIdentityContext } from '../../IIdentityContext';
import { ArcContext } from '../../../ArcContext';
import { IdentityProvider as RootIdentityProvider } from '@cratis/arc/identity';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { Constructor } from '@cratis/fundamentals';

export class an_identity_provider {
    capturedIdentity: IIdentityContext | null = null;
    renderCount = 0;
    renderResult!: RenderResult;
    originalApiBasePath = '';
    originalOrigin = '';
    private originalConsoleError?: typeof console.error;
    fetchHelper: ReturnType<typeof createFetchHelper>;
    fetchStub!: sinon.SinonStub;

    constructor() {
        this.originalApiBasePath = RootIdentityProvider.apiBasePath;
        this.originalOrigin = RootIdentityProvider.origin;

        RootIdentityProvider.setOrigin('https://example.com');
        RootIdentityProvider.setApiBasePath('https://example.com/api');

        this.fetchHelper = createFetchHelper();
    }

    setupSuccessfulIdentityFetch(id: string, name: string, details: object = {}) {
        this.fetchStub = this.fetchHelper.stubFetch();
        this.answerFetchWith(id, name, details);
    }

    setupFailedIdentityFetch() {
        this.fetchStub = this.fetchHelper.stubFetch();
        this.fetchStub.rejects(new Error('Failed to fetch'));
    }

    /**
     * Points the already stubbed fetch at a different identity, so a refresh can bring back something
     * other than what the initial load did.
     */
    answerFetchWith(id: string, name: string, details: object = {}, roles: string[] = []) {
        this.fetchStub.resolves({
            ok: true,
            json: async () => ({ id, name, roles, details })
        } as Response);
    }

    /**
     * Fails every fetch from here on, the way a network error does.
     */
    failEveryFetch() {
        this.fetchStub.rejects(new Error('Failed to fetch'));
    }

    /**
     * Holds the next fetch open, so a refresh can be observed while it is still in flight.
     */
    holdNextFetchOpen() {
        this.fetchStub.returns(new Promise<Response>(() => { /* never answered */ }));
    }

    createTestComponent() {
        return () => {
            this.renderCount++;
            this.capturedIdentity = useIdentity();
            return React.createElement('div', null, 'Test');
        };
    }

    renderProvider(detailsType?: Constructor) {
        const arcContext = {
            microservice: 'test-microservice',
            apiBasePath: '/api',
            origin: 'http://localhost'
        };

        const TestComponent = this.createTestComponent();
        this.renderResult = render(
            React.createElement(
                ArcContext.Provider,
                { value: arcContext },
                React.createElement(
                    IdentityProvider,
                    { detailsType },
                    React.createElement(TestComponent)
                )
            )
        );
    }

    async waitForAsyncUpdates() {
        await this.flush();
        await waitFor(() => {
            expect(this.capturedIdentity).not.toBeNull();
        }, { timeout: 1000 });
    }

    /**
     * Refreshes the identity and waits for the round-trip to complete.
     * @returns The error the refresh rejected with, or null when it succeeded.
     */
    async refreshIdentity(): Promise<unknown> {
        let rejection: unknown = null;
        await act(async () => {
            await this.capturedIdentity!.refresh().catch(error => rejection = error);
        });
        return rejection;
    }

    /**
     * Starts a refresh and leaves it in flight.
     */
    async beginRefresh() {
        this.holdNextFetchOpen();
        await act(async () => {
            this.capturedIdentity!.refresh().catch(() => { /* left in flight on purpose */ });
            await this.drainPromises();
        });
    }

    async clearIdentity() {
        await act(async () => {
            this.capturedIdentity!.clearIdentity();
        });
    }

    /**
     * Drains the promise chain the answered fetch feeds and lets React commit whatever came of it.
     *
     * Nothing about the outcome is assumed here - the specs are what observe it.
     */
    async flush() {
        await act(async () => {
            await this.drainPromises();
        });
    }

    suppressConsoleErrors() {
        if (!this.originalConsoleError) {
            this.originalConsoleError = console.error;
            console.error = () => { /* Suppressed during test */ };
        }
    }

    restoreConsole() {
        if (this.originalConsoleError) {
            console.error = this.originalConsoleError;
            this.originalConsoleError = undefined;
        }
    }

    cleanup() {
        this.restoreConsole();
        this.fetchHelper.restore();
        RootIdentityProvider.setApiBasePath(this.originalApiBasePath);
        RootIdentityProvider.setOrigin(this.originalOrigin);
        if (this.renderResult) {
            this.renderResult.unmount();
        }
    }

    private drainPromises() {
        return new Promise(resolve => setTimeout(resolve, 0));
    }
}
