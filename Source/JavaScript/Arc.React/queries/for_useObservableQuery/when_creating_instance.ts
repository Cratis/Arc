// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { useObservableQuery } from '../useObservableQuery';
import { FakeObservableQuery } from './FakeObservableQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryResult } from '@cratis/arc/queries';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when creating instance', () => {
    let capturedIsPerformingInitial: boolean | null = null;
    let capturedIsPerformingAfterResponse: boolean | null = null;
    let renderCount = 0;

    beforeEach(() => {
        FakeObservableQuery.reset();
        capturedIsPerformingInitial = null;
        capturedIsPerformingAfterResponse = null;
        renderCount = 0;
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    it('should have isPerforming true initially before receiving data', async () => {
        const TestComponent = () => {
            const [result] = useObservableQuery(FakeObservableQuery);
            renderCount++;

            if (renderCount === 1) {
                capturedIsPerformingInitial = result.isPerforming;
            }

            return React.createElement('div', null, 'Test');
        };

        render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(TestComponent)
            )
        );

        capturedIsPerformingInitial!.should.be.true;
    });

    it('should have isPerforming false after receiving data', async () => {
        const TestComponent = () => {
            const [result] = useObservableQuery(FakeObservableQuery);
            renderCount++;

            if (renderCount === 1) {
                capturedIsPerformingInitial = result.isPerforming;
            } else if (renderCount === 2) {
                capturedIsPerformingAfterResponse = result.isPerforming;
            }

            return React.createElement('div', null, 'Test');
        };

        render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(TestComponent)
            )
        );

        // Simulate receiving data from WebSocket
        const callback = FakeObservableQuery.subscribeCallbacks[0];
        callback!.should.not.be.undefined;

        await act(async () => {
            callback({
                data: [{ id: '1', name: 'Test' }],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
            } as QueryResult<any[]>);
        });

        capturedIsPerformingInitial!.should.be.true;
        capturedIsPerformingAfterResponse!.should.be.false;
    });
});
