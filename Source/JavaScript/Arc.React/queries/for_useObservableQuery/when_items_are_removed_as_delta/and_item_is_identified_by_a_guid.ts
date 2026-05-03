// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { useObservableQuery } from '../../useObservableQuery';
import { FakeGuidObservableQuery, FakeGuidItem } from '../FakeGuidObservableQuery';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { QueryResult, QueryResultWithState, QueryInstanceCache } from '@cratis/arc/queries';
import { Guid } from '@cratis/fundamentals';
import { QueryInstanceCacheContext } from '../../QueryInstanceCacheContext';

describe('when items are removed as delta and item is identified by a guid', () => {
    let capturedResult: QueryResultWithState<FakeGuidItem[]> | undefined;

    beforeEach(() => {
        FakeGuidObservableQuery.reset();
        capturedResult = undefined;
    });

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    it('should remove the matching item', async () => {
        const firstId = Guid.create();
        const secondId = Guid.create();

        const TestComponent = () => {
            const [result] = useObservableQuery<FakeGuidItem[], FakeGuidObservableQuery>(FakeGuidObservableQuery);
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

        const callback = FakeGuidObservableQuery.subscribeCallbacks[0];

        await act(async () => {
            callback({
                data: [
                    { id: firstId.toString(), name: 'First' },
                    { id: secondId.toString(), name: 'Second' },
                ],
                isSuccess: true,
                isAuthorized: true,
                isValid: true,
                hasExceptions: false,
                validationResults: [],
                exceptionMessages: [],
                exceptionStackTrace: '',
                paging: { page: 0, size: 0, totalItems: 2, totalPages: 1 }
            } as unknown as QueryResult<FakeGuidItem[]>);
        });

        capturedResult!.data.length.should.equal(2);
        capturedResult!.data[0].id.equals(firstId).should.be.true;
        capturedResult!.data[1].id.equals(secondId).should.be.true;

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
                    removed: [{ id: firstId.toString(), name: 'First' }],
                }
            } as unknown as QueryResult<FakeGuidItem[]>);
        });

        capturedResult!.data.length.should.equal(1);
        capturedResult!.data[0].id.equals(secondId).should.be.true;
    });
});