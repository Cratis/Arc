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

function createReadyResult(
    name: string,
): QueryResult<FakeSuspenseObservableQueryResult[]> {
    return new QueryResult<FakeSuspenseObservableQueryResult[]>(
        {
            data: [{ id: '1', name }],
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

function renderTestComponent() {
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

    return render(
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
}

describe('when abandoning a not-ready subscription', () => {
    beforeEach(() => {
        vi.useFakeTimers();
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.reset();
    });

    afterEach(() => {
        clearSuspenseObservableQueryCache();
        vi.useRealTimers();
    });

    it('should unsubscribe and remove the resource after the grace period', async () => {
        const firstRender = renderTestComponent();
        firstRender.unmount();

        await act(async () => {
            vi.advanceTimersByTime(5000);
        });

        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(1);

        renderTestComponent();
        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(2);
    });

    it('should ignore a late callback from the disposed resource', async () => {
        const firstRender = renderTestComponent();
        const disposedCallback = FakeSuspenseObservableQuery.subscribeCallbacks[0];
        firstRender.unmount();

        await act(async () => {
            vi.advanceTimersByTime(5000);
        });

        renderTestComponent();
        const activeCallback = FakeSuspenseObservableQuery.subscribeCallbacks[1];

        await act(async () => {
            disposedCallback(createReadyResult('Disposed'));
        });

        screen.getByTestId('loading');
        (screen.queryByTestId('content') === null).should.be.true;

        await act(async () => {
            activeCallback(createReadyResult('Active'));
        });

        (screen.getByTestId('content').textContent ?? '').should.equal('Active');
    });
});
