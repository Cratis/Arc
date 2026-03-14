// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, screen, act } from '@testing-library/react';
import { useSuspenseObservableQuery, clearSuspenseObservableQueryCache } from '../useSuspenseObservableQuery';
import { QueryFailed } from '../QueryFailed';
import { FakeSuspenseObservableQuery } from './FakeSuspenseObservableQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryResult } from '@cratis/arc/queries';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when observable query returns exceptions', () => {
    let originalConsoleError: typeof console.error;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    beforeEach(() => {
        originalConsoleError = console.error;
        console.error = () => { /* suppress React ErrorBoundary output */ };
        FakeSuspenseObservableQuery.reset();
        clearSuspenseObservableQueryCache();
    });

    afterEach(() => {
        console.error = originalConsoleError;
        clearSuspenseObservableQueryCache();
    });

    it('should propagate a QueryFailed error to the error boundary', async () => {
        let capturedError: Error | null = null;

        class TestErrorBoundary extends React.Component<{ children: React.ReactNode }, { error: Error | null }> {
            constructor(props: { children: React.ReactNode }) {
                super(props);
                this.state = { error: null };
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

        const TestComponent = () => {
            useSuspenseObservableQuery(FakeSuspenseObservableQuery);
            return React.createElement('div', { 'data-testid': 'content' }, 'content');
        };

        render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(
                    TestErrorBoundary,
                    null,
                    React.createElement(
                        React.Suspense,
                        { fallback: React.createElement('div', null, 'Loading...') },
                        React.createElement(TestComponent)
                    )
                )
            )
        );

        const callback = FakeSuspenseObservableQuery.subscribeCallbacks[0];
        callback!.should.not.be.undefined;

        await act(async () => {
            callback(new QueryResult({
                data: null,
                isSuccess: false,
                isAuthorized: true,
                isValid: true,
                hasExceptions: true,
                validationResults: [],
                exceptionMessages: ['Unexpected error occurred'],
                exceptionStackTrace: 'at line 42',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
            }, Array, true));
        });

        screen.getByTestId('error');

        capturedError!.should.not.be.null;
        capturedError!.should.be.instanceOf(QueryFailed);
        (capturedError as unknown as QueryFailed).exceptionMessages.should.deep.equal(['Unexpected error occurred']);
    });
});

