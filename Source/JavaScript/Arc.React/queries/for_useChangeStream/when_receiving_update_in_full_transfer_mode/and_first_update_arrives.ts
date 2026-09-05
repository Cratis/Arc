// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import React from 'react';
import { render, act, waitFor } from '@testing-library/react';
import { useChangeStream } from '../../useChangeStream';
import { FakeChangeStreamQueryBase, FakeItem } from '../FakeChangeStreamQuery';
import { ArcContext, ArcConfiguration } from '../../../ArcContext';
import { ChangeSet, QueryResult } from '@cratis/arc/queries';
import { Globals, ObservableQueryTransferMode } from '@cratis/arc';

/* eslint-disable @typescript-eslint/no-explicit-any */

class FakeChangeStreamQuery extends FakeChangeStreamQueryBase {
    readonly route = '/api/when-receiving-update-in-full-transfer-mode-and-first-update-arrives';
    readonly queryName = 'when-receiving-update-in-full-transfer-mode-and-first-update-arrives';
}

describe('when receiving first update in full transfer mode', () => {
    let capturedChangeSet: ChangeSet<FakeItem> = { added: [], replaced: [], removed: [] };
    const originalMode = Globals.observableQueryTransferMode;

    const config: ArcConfiguration = {
        microservice: 'test-microservice',
        apiBasePath: '/api',
        origin: 'https://example.com',
    };

    beforeEach(() => {
        FakeChangeStreamQuery.reset();
        capturedChangeSet = { added: [], replaced: [], removed: [] };
        Globals.observableQueryTransferMode = ObservableQueryTransferMode.Full;
    });

    afterEach(() => {
        Globals.observableQueryTransferMode = originalMode;
    });

    it('should report all items as added', async () => {
        const TestComponent = () => {
            const changeSet = useChangeStream<FakeItem, FakeChangeStreamQuery>(FakeChangeStreamQuery);
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

        await waitFor(() => expect(FakeChangeStreamQuery.subscribeCallbacks[0]).toBeDefined());
        const callback = FakeChangeStreamQuery.subscribeCallbacks[0]!;

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

        capturedChangeSet.added.length.should.equal(2);
        capturedChangeSet.replaced.length.should.equal(0);
        capturedChangeSet.removed.length.should.equal(0);
    });
});
