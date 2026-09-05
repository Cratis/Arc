// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, screen } from '@testing-library/react';
import { QueryResult } from '@cratis/arc/queries';
import { type ArcConfiguration, ArcContext } from '../../ArcContext';
import { QueryUnauthorized } from '../QueryUnauthorized';
import {
    clearSuspenseObservableQueryCache,
    useSuspenseObservableQuery,
} from '../useSuspenseObservableQuery';
import {
    FakeSuspenseObservableQuery,
    type FakeSuspenseObservableQueryResult,
} from './FakeSuspenseObservableQuery';

let capturedError: Error | null = null;

class TestErrorBoundary extends React.Component<
    { children: React.ReactNode },
    { error: Error | null }
> {
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
            return React.createElement(
                'div',
                { 'data-testid': 'error' },
                'Error occurred',
            );
        }
        return this.props.children as React.ReactElement;
    }
}

describe('when observable query is unauthorized', () => {
    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    let originalConsoleError: typeof console.error;

    beforeEach(() => {
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.reset();
        capturedError = null;
        originalConsoleError = console.error;
        console.error = () => {};
    });

    afterEach(() => {
        console.error = originalConsoleError;
        clearSuspenseObservableQueryCache();
    });

    it('should propagate a QueryUnauthorized error to the error boundary', async () => {
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
                        React.createElement(TestComponent),
                    ),
                ),
            ),
        );

        const callback = FakeSuspenseObservableQuery.subscribeCallbacks[0];
        if (!callback) {
            throw new Error('Expected the observable query to subscribe');
        }

        await act(async () => {
            callback(
                new QueryResult<FakeSuspenseObservableQueryResult[]>(
                    {
                        data: [],
                        isSuccess: false,
                        isReady: true,
                        isAuthorized: false,
                        isValid: true,
                        hasExceptions: false,
                        validationResults: [],
                        exceptionMessages: [],
                        exceptionStackTrace: '',
                        paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 },
                    },
                    Object,
                    true,
                ),
            );
        });

        screen.getByTestId('error');
        (capturedError instanceof QueryUnauthorized).should.equal(true);
    });
});
