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

describe('when committing a suspense observable resource in strict mode', () => {
    beforeEach(() => {
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.reset();
    });

    afterEach(() => {
        clearSuspenseObservableQueryCache();
    });

    it('should retain the subscription through the concurrent effect replay', async () => {
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
                React.StrictMode,
                null,
                React.createElement(
                    ArcContext.Provider,
                    { value: config },
                    React.createElement(
                        React.Suspense,
                        { fallback: React.createElement('div', null, 'Loading...') },
                        React.createElement(TestComponent),
                    ),
                ),
            ),
        );

        const callback = FakeSuspenseObservableQuery.subscribeCallbacks[0];
        await act(async () => {
            callback(createReadyResult('Initial'));
        });
        await act(async () => {
            callback(createReadyResult('Updated'));
        });

        (screen.getByTestId('content').textContent ?? '').should.equal('Updated');
        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(1);
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(0);

        await act(async () => {
            rendered.unmount();
        });
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(1);
    });
});
