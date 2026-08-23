// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, screen } from '@testing-library/react';
import { QueryResult } from '@cratis/arc/queries';
import { type ArcConfiguration, ArcContext } from '../../ArcContext';
import {
    clearSuspenseObservableQueryCache,
    useSuspenseObservableQuery,
} from '../useSuspenseObservableQuery';
import {
    FakeSuspenseObservableQuery,
    type FakeSuspenseObservableQueryResult,
} from './FakeSuspenseObservableQuery';

const config: ArcConfiguration = {
    microservice: 'test-microservice',
    apiBasePath: '/api',
    origin: 'https://example.com',
};

function createReadyResult(): QueryResult<FakeSuspenseObservableQueryResult[]> {
    return new QueryResult<FakeSuspenseObservableQueryResult[]>(
        {
            data: [{ id: '1', name: 'Ready' }],
            isSuccess: true,
            isReady: true,
            isAuthorized: true,
            isValid: true,
            hasExceptions: false,
            validationResults: [],
            exceptionMessages: [],
            exceptionStackTrace: '',
            paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 },
        },
        Object,
        true,
    );
}

describe('when a suspense observable query remains not ready for a long time', () => {
    beforeEach(() => {
        vi.useFakeTimers();
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.reset();
    });

    afterEach(() => {
        clearSuspenseObservableQueryCache();
        vi.useRealTimers();
    });

    it('should keep the resource until it becomes ready', async () => {
        const TestComponent = () => {
            const [result] = useSuspenseObservableQuery<
                FakeSuspenseObservableQueryResult[],
                FakeSuspenseObservableQuery
            >(FakeSuspenseObservableQuery);
            return React.createElement(
                'div',
                { 'data-testid': 'content' },
                result.data[0]?.name,
            );
        };

        const rendered = render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(
                    React.Suspense,
                    {
                        fallback: React.createElement(
                            'div',
                            { 'data-testid': 'loading' },
                            'Loading...',
                        ),
                    },
                    React.createElement(TestComponent),
                ),
            ),
        );

        await act(async () => {
            vi.advanceTimersByTime(60000);
        });

        screen.getByTestId('loading');
        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(1);
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(0);

        await act(async () => {
            FakeSuspenseObservableQuery.subscribeCallbacks[0](createReadyResult());
        });

        (screen.getByTestId('content').textContent ?? '').should.equal('Ready');
        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(1);
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(0);

        await act(async () => {
            rendered.unmount();
        });
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(1);
    });
});
