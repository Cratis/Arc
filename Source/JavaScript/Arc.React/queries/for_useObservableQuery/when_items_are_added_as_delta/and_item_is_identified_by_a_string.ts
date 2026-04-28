// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { useObservableQuery } from '../../useObservableQuery';
import { FakeObservableQuery, FakeObservableQueryResult } from '../FakeObservableQuery';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { QueryResult, QueryResultWithState, QueryInstanceCache } from '@cratis/arc/queries';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext';

describe('when items are added as delta and item is identified by a string', () => {
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

    it('should append the added item', async () => {
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
            callback(new QueryResult({
                data: [{ id: 'first', name: 'First' }],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
            }, Object, true));
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
                paging: { page: 0, size: 0, totalItems: 2, totalPages: 1 },
                changeSet: {
                    added: [{ id: 'second', name: 'Second' }],
                    replaced: [],
                    removed: [],
                }
            } as unknown as QueryResult<FakeObservableQueryResult[]>);
        });

        capturedResult!.data.should.deep.equal([
            { id: 'first', name: 'First' },
            { id: 'second', name: 'Second' },
        ]);
    });
});