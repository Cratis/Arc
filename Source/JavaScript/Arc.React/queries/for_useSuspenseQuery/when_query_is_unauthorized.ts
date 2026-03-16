// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, screen, act } from '@testing-library/react';
import sinon from 'sinon';
import { useSuspenseQuery, clearSuspenseQueryCache } from '../useSuspenseQuery';
import { QueryUnauthorized } from '../QueryUnauthorized';
import { FakeSuspenseQuery } from './FakeSuspenseQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { createFetchHelper } from '@cratis/arc/helpers/fetchHelper';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when query is unauthorized', () => {
    let fetchStub: sinon.SinonStub;
    let fetchHelper: { stubFetch: () => sinon.SinonStub; restore: () => void };
    let originalConsoleError: typeof console.error;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    beforeEach(() => {
        originalConsoleError = console.error;
        console.error = () => { /* suppress React ErrorBoundary output */ };

        fetchHelper = createFetchHelper();
        fetchStub = fetchHelper.stubFetch();
        fetchStub.resolves({
            json: async () => ({
                data: null,
                isSuccess: false,
                isAuthorized: false,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
            })
        } as Response);
    });

    afterEach(() => {
        fetchHelper.restore();
        console.error = originalConsoleError;
        clearSuspenseQueryCache();
    });

    it('should propagate a QueryUnauthorized error to the error boundary', async () => {
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
                            { fallback: React.createElement('div', null, 'Loading...') },
                            React.createElement(TestComponent)
                        )
                    )
                )
            );
        });

        await act(async () => {});

        screen.getByTestId('error');

        capturedError!.should.not.be.null;
        capturedError!.should.be.instanceOf(QueryUnauthorized);
    });
});
