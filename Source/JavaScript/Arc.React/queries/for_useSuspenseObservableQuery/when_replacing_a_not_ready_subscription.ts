// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { act, render, screen } from '@testing-library/react';
import { QueryResult, SortDirection, Sorting } from '@cratis/arc/queries';
import { type ArcConfiguration, ArcContext } from '../../ArcContext';
import type { SetSorting } from '../SetSorting';
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

function createResult(
    isReady: boolean,
    name: string,
): QueryResult<FakeSuspenseObservableQueryResult[]> {
    return new QueryResult<FakeSuspenseObservableQueryResult[]>(
        {
            data: [{ id: '1', name }],
            isSuccess: true,
            isReady,
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

describe('when replacing a subscription that is not ready', () => {
    beforeEach(() => {
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.reset();
    });

    afterEach(() => {
        clearSuspenseObservableQueryCache();
    });

    it('should settle from the replacement ready response', async () => {
        let setSorting: SetSorting | undefined;

        const TestComponent = () => {
            const [result, changeSorting] = useSuspenseObservableQuery<
                FakeSuspenseObservableQueryResult[],
                FakeSuspenseObservableQuery
            >(FakeSuspenseObservableQuery);
            setSorting = changeSorting;
            return React.createElement(
                'div',
                { 'data-testid': 'content' },
                result.data[0]?.name,
            );
        };

        render(
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

        const firstCallback = FakeSuspenseObservableQuery.subscribeCallbacks[0];
        await act(async () => {
            firstCallback(createResult(true, 'Initial'));
        });
        screen.getByTestId('content').textContent!.should.equal('Initial');

        await act(async () => {
            await setSorting!(new Sorting('name', SortDirection.ascending));
        });

        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(2);
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(0);
        screen.getByTestId('loading');

        const replacementCallback = FakeSuspenseObservableQuery.subscribeCallbacks[1];
        await act(async () => {
            replacementCallback(createResult(false, 'Replacement not ready'));
        });
        screen.getByTestId('loading');

        await act(async () => {
            replacementCallback(createResult(true, 'Replacement ready'));
        });

        screen.getByTestId('content').textContent!.should.equal('Replacement ready');
        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(2);
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(1);
    });
});
