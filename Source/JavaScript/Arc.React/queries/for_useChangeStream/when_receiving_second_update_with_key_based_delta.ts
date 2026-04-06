// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act } from '@testing-library/react';
import { useChangeStream } from '../useChangeStream';
import { FakeChangeStreamQuery, FakeItem } from './FakeChangeStreamQuery';
import { ArcContext, ArcConfiguration } from '../../ArcContext';
import { ChangeSet, QueryResult } from '@cratis/arc/queries';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when receiving second update with key-based delta', () => {
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

    it('should report new item as added and updated item as replaced', async () => {
        const TestComponent = () => {
            const changeSet = useChangeStream<FakeItem, FakeChangeStreamQuery>(
                FakeChangeStreamQuery,
                undefined,
                item => item.id
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

        const callback = FakeChangeStreamQuery.subscribeCallbacks[0];
        callback!.should.not.be.undefined;

        // First update
        await act(async () => {
            callback(new QueryResult({
                data: [
                    { id: '1', name: 'First' },
                    { id: '2', name: 'Second' },
                ],
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

        // Second update — item 2 name changed, item 3 added
        await act(async () => {
            callback(new QueryResult({
                data: [
                    { id: '1', name: 'First' },
                    { id: '2', name: 'Second Updated' },
                    { id: '3', name: 'Third' },
                ],
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

        capturedChangeSet.added.length.should.equal(1);
        capturedChangeSet.added[0].id.should.equal('3');
        capturedChangeSet.replaced.length.should.equal(1);
        capturedChangeSet.replaced[0].id.should.equal('2');
        capturedChangeSet.removed.length.should.equal(0);
    });
});
