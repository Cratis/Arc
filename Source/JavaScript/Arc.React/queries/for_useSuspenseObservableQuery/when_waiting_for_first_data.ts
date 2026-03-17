// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, screen, act } from '@testing-library/react';
import { useSuspenseObservableQuery, clearSuspenseObservableQueryCache } from '../useSuspenseObservableQuery';
import { FakeSuspenseObservableQuery } from './FakeSuspenseObservableQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryResult } from '@cratis/arc/queries';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when waiting for first data', () => {
    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com'
    };

    beforeEach(() => {
        FakeSuspenseObservableQuery.reset();
        clearSuspenseObservableQueryCache();
    });

    it('should initially show suspense fallback before first data arrives', () => {
        const TestComponent = () => {
            useSuspenseObservableQuery(FakeSuspenseObservableQuery);
            return React.createElement('div', { 'data-testid': 'content' }, 'loaded');
        };

        render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(
                    React.Suspense,
                    { fallback: React.createElement('div', { 'data-testid': 'loading' }, 'Loading...') },
                    React.createElement(TestComponent)
                )
            )
        );

        screen.getByTestId('loading');
    });

    it('should render with data after first observable emission', async () => {
        let capturedResult: any = null;

        const TestComponent = () => {
            const [result] = useSuspenseObservableQuery(FakeSuspenseObservableQuery);
            capturedResult = result;
            return React.createElement('div', { 'data-testid': 'content' }, 'loaded');
        };

        render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(
                    React.Suspense,
                    { fallback: React.createElement('div', null, 'Loading...') },
                    React.createElement(TestComponent)
                )
            )
        );

        const callback = FakeSuspenseObservableQuery.subscribeCallbacks[0];
        callback!.should.not.be.undefined;

        await act(async () => {
            callback(new QueryResult({
                data: [{ id: '1', name: 'Test' }],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
            }, Array, true));
        });

        screen.getByTestId('content');

        capturedResult!.should.not.be.null;
        capturedResult!.isPerforming.should.be.false;
        capturedResult!.isSuccess.should.be.true;
    });
});

