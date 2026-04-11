// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { useObservableQuery } from '../useObservableQuery';
import { FakeObservableQuery } from './FakeObservableQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { QueryResult, QueryInstanceCache } from '@cratis/arc/queries';
import { QueryInstanceCacheContext } from '../QueryInstanceCacheContext';

describe('when server returns single item for collection', () => {
    let capturedData: unknown = undefined;
    let renderCount = 0;

    beforeEach(() => {
        FakeObservableQuery.reset();
        capturedData = undefined;
        renderCount = 0;
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    it('should have data as an array', async () => {
        const TestComponent = () => {
            const [result] = useObservableQuery(FakeObservableQuery);
            renderCount++;

            if (renderCount === 2) {
                capturedData = result.data;
            }

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
                data: [{ id: '1', name: 'Single Item' }],
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

        Array.isArray(capturedData).should.be.true;
        (capturedData as unknown[]).should.have.lengthOf(1);
    });
});
