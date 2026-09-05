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

describe('when replacing with an existing not-ready subscription', () => {
    beforeEach(() => {
        clearSuspenseObservableQueryCache();
        FakeSuspenseObservableQuery.reset();
    });

    afterEach(() => {
        clearSuspenseObservableQueryCache();
    });

    it('should reuse and settle the existing subscription', async () => {
        let setSorting: SetSorting | undefined;
        const nameSorting = new Sorting('name', SortDirection.ascending);

        const UnsortedComponent = () => {
            const [result, changeSorting] = useSuspenseObservableQuery<
                FakeSuspenseObservableQueryResult[],
                FakeSuspenseObservableQuery
            >(FakeSuspenseObservableQuery);
            setSorting = changeSorting;
            return React.createElement(
                'div',
                { 'data-testid': 'unsorted-content' },
                result.data[0]?.name,
            );
        };
        const SortedComponent = () => {
            const [result] = useSuspenseObservableQuery<
                FakeSuspenseObservableQueryResult[],
                FakeSuspenseObservableQuery
            >(FakeSuspenseObservableQuery, undefined, nameSorting);
            return React.createElement(
                'div',
                { 'data-testid': 'sorted-content' },
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
                            { 'data-testid': 'unsorted-loading' },
                            'Loading...',
                        ),
                    },
                    React.createElement(UnsortedComponent),
                ),
                React.createElement(
                    React.Suspense,
                    {
                        fallback: React.createElement(
                            'div',
                            { 'data-testid': 'sorted-loading' },
                            'Loading...',
                        ),
                    },
                    React.createElement(SortedComponent),
                ),
            ),
        );

        const unsortedCallback = FakeSuspenseObservableQuery.subscribeCallbacks[0];
        const sortedCallback = FakeSuspenseObservableQuery.subscribeCallbacks[1];
        await act(async () => {
            unsortedCallback(createResult(true, 'Unsorted'));
            sortedCallback(createResult(false, 'Not ready'));
        });

        screen.getByTestId('unsorted-content').textContent!.should.equal('Unsorted');
        screen.getByTestId('sorted-loading');

        await act(async () => {
            await setSorting!(nameSorting);
        });

        FakeSuspenseObservableQuery.subscribeCallbacks.should.have.lengthOf(2);

        await act(async () => {
            sortedCallback(createResult(true, 'Shared ready'));
        });

        screen.getByTestId('unsorted-content').textContent!.should.equal('Shared ready');
        screen.getByTestId('sorted-content').textContent!.should.equal('Shared ready');
        FakeSuspenseObservableQuery.unsubscribeCallCount.should.equal(1);
    });
});
