// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { useChangeStream } from '../useChangeStream';
import { FakeChangeStreamQuery, FakeItem } from './FakeChangeStreamQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { ChangeSet, QueryResult } from '@cratis/arc/queries';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when hook is disabled', () => {
    let capturedChangeSet: ChangeSet<FakeItem> = { added: [], replaced: [], removed: [] };

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    beforeEach(() => {
        FakeChangeStreamQuery.reset();
        capturedChangeSet = { added: [], replaced: [], removed: [] };
    });

    it('should return an empty change set', async () => {
        const TestComponent = () => {
            const changeSet = useChangeStream<FakeItem, FakeChangeStreamQuery>(
                FakeChangeStreamQuery,
                undefined,
                undefined,
                undefined,
                false
            );
            capturedChangeSet = changeSet;
            return React.createElement('div', null, 'Test');
        };

        render(
            React.createElement(
                ArcContext.Provider,
                { value: config },
                React.createElement(TestComponent)
            )
        );

        // Simulate incoming data even though the hook is disabled
        if (FakeChangeStreamQuery.subscribeCallbacks.length > 0) {
            await act(async () => {
                FakeChangeStreamQuery.subscribeCallbacks[0](new QueryResult({
                    data: [{ id: '1', name: 'First' }],
                    isSuccess: true,
                    isAuthorized: true,
                    isValid: true,
                    hasExceptions: false,
                    validationResults: [],
                    exceptionMessages: [],
                    exceptionStackTrace: '',
                    paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
                }, Object, true));
            });
        }

        capturedChangeSet.added.length.should.equal(0);
        capturedChangeSet.replaced.length.should.equal(0);
        capturedChangeSet.removed.length.should.equal(0);
    });
});
