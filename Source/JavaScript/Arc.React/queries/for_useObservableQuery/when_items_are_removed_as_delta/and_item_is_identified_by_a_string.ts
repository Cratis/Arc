// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { useObservableQuery } from '../../useObservableQuery';
import { FakeObservableQuery, FakeObservableQueryResult } from '../FakeObservableQuery';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { QueryResult, QueryResultWithState, QueryInstanceCache } from '@cratis/arc/queries';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext';

describe('when items are removed as delta and item is identified by a string', () => {
    let capturedResult: QueryResultWithState<FakeObservableQueryResult[]> | undefined;

    beforeEach(() => {
        FakeObservableQuery.reset();
        capturedResult = undefined;
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    it('should remove the matching item', async () => {
        const TestComponent = () => {
            const [result] = useObservableQuery<FakeObservableQueryResult[], FakeObservableQuery>(FakeObservableQuery);
            capturedResult = result;
            return React.createElement('div', null, 'Test');
        };

        render(
            React.createElement(
                QueryInstanceCacheContext.Provider,
                { value: new QueryInstanceCache() },
                React.createElement(
                    ArcContext.Provider,
                    { value: config },
                    React.createElement(TestComponent)
                )
            )
        );

        const callback = FakeObservableQuery.subscribeCallbacks[0];

        await act(async () => {
            callback({
                data: [
                    { id: 'first', name: 'First' },
                    { id: 'second', name: 'Second' },
                ],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 2, totalPages: 1 }
            } as unknown as QueryResult<FakeObservableQueryResult[]>);
        });

        await act(async () => {
            callback({
                data: [],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 },
                changeSet: {
                    added: [],
                    replaced: [],
                    removed: [{ id: 'first', name: 'First' }],
                }
            } as unknown as QueryResult<FakeObservableQueryResult[]>);
        });

        capturedResult!.data.should.deep.equal([
            { id: 'second', name: 'Second' },
        ]);
    });
});