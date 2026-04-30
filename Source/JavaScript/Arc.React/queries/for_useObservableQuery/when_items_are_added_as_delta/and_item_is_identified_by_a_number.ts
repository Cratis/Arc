// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { useObservableQuery } from '../../useObservableQuery';
import { FakeNumericObservableQuery, FakeNumericObservableQueryResult } from '../FakeNumericObservableQuery';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { QueryResult, QueryResultWithState, QueryInstanceCache } from '@cratis/arc/queries';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext';

describe('when items are added as delta and item is identified by a number', () => {
    let capturedResult: QueryResultWithState<FakeNumericObservableQueryResult[]> | undefined;

    beforeEach(() => {
        FakeNumericObservableQuery.reset();
        capturedResult = undefined;
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    it('should append the added item', async () => {
        const TestComponent = () => {
            const [result] = useObservableQuery<FakeNumericObservableQueryResult[], FakeNumericObservableQuery>(FakeNumericObservableQuery);
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

        const callback = FakeNumericObservableQuery.subscribeCallbacks[0];

        await act(async () => {
            callback(new QueryResult({
                data: [{ id: 1, name: 'First' }],
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
                    added: [{ id: 2, name: 'Second' }],
                    replaced: [],
                    removed: [],
                }
            } as unknown as QueryResult<FakeNumericObservableQueryResult[]>);
        });

        capturedResult!.data.should.deep.equal([
            { id: 1, name: 'First' },
            { id: 2, name: 'Second' },
        ]);
    });
});