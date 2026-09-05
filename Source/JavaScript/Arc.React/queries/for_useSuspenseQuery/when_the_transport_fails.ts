// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, screen, act } from '@testing-library/react';
import sinon from 'sinon';
import { useSuspenseQuery, clearSuspenseQueryCache } from '../useSuspenseQuery';
import { QueryFailed } from '../QueryFailed';
import { FakeSuspenseQuery } from './FakeSuspenseQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';

describe('when the transport fails', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let originalConsoleError: typeof console.error;
    let capturedError: Error | undefined;
    let renderedFallback: HTMLElement | null;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    class TestErrorBoundary extends React.Component<{ children: React.ReactNode }, { error: Error | undefined }> {
        constructor(props: { children: React.ReactNode }) {
            super(props);
            this.state = { error: undefined };
        }

        static getDerivedStateFromError(error: Error) {
            return { error };
        }

        componentDidCatch(error: Error) {
            capturedError = error;
        }

        render() {
            if (this.state.error) {
                return React.createElement('div', { 'data-testid': 'error' }, 'Error occurred');
            }
            return this.props.children as React.ReactElement;
        }
    }

    beforeEach(async () => {
        originalConsoleError = console.error;
        console.error = () => { /* suppress React ErrorBoundary output */ };

        capturedError = undefined;

        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();

        // A dead network, a CORS rejection or a DNS failure - the request never reaches the server, so
        // there is no response for the hook to inspect, only a rejected fetch.
        fetchStub.rejects(new Error('Network error'));

        const TestComponent = () => {
            useSuspenseQuery(FakeSuspenseQuery);
            return React.createElement('div', { 'data-testid': 'content' }, 'content');
        };

        await act(async () => {
            render(
                React.createElement(
                    ArcContext.Provider,
                    { value: config },
                    React.createElement(
                        TestErrorBoundary,
                        null,
                        React.createElement(
                            React.Suspense,
                            { fallback: React.createElement('div', { 'data-testid': 'loading' }, 'Loading...') },
                            React.createElement(TestComponent)
                        )
                    )
                )
            );
        });

        await act(async () => { });

        renderedFallback = screen.queryByTestId('loading');
    });

    afterEach(() => {
        fetchHelper.restore();
        console.error = originalConsoleError;
        clearSuspenseQueryCache();
    });

    it('should propagate a QueryFailed error to the error boundary', () => capturedError!.should.be.instanceOf(QueryFailed));

    it('should carry the transport error message', () => (capturedError as unknown as QueryFailed).exceptionMessages.should.deep.equal(['Network error']));

    it('should not leave the component suspended on its fallback', () => (renderedFallback === null).should.be.true);
});
