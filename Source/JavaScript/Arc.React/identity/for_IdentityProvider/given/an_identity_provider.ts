// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, RenderResult } from '@testing-library/react';
import sinon from 'sinon';
import { IdentityProvider } from '../../IdentityProvider';
import { useIdentity } from '../../useIdentity';
import { IIdentity } from '@cratis/arc/identity';
import { ArcContext } from '../../../ArcContext';
import { IdentityProvider as RootIdentityProvider } from '@cratis/arc/identity';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';
import { Constructor } from '@cratis/fundamentals';

export class an_identity_provider {
    capturedIdentity: IIdentity | null = null;
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
        this.fetchStub.resolves({
            json: async () => ({ id, name, details })
        } as Response);
    }

    setupFailedIdentityFetch() {
        this.fetchStub = this.fetchHelper.stubFetch();
        this.fetchStub.rejects(new Error('Failed to fetch'));
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
        await new Promise(resolve => setTimeout(resolve, 200));
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
}