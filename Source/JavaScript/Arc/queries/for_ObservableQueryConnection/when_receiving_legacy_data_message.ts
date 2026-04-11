// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../given';
import { an_observable_query_connection_with_websocket } from './given/an_observable_query_connection_with_websocket';
import { QueryResult } from '../QueryResult';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when receiving legacy data message without type field', given(an_observable_query_connection_with_websocket, context => {
    const queryResult = {
        data: [{ id: '1', name: 'Test' }],
        isSuccess: true,
        isAuthorized: true,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        paging: { page: 0, size: 0, totalItems: 1, totalPages: 1 }
    };

    let receivedData: QueryResult<unknown> | null = null;

    beforeEach(() => {
        receivedData = null;
        context.connection.connect((data) => {
            receivedData = data as QueryResult<unknown>;
        });
        context.simulateMessage(queryResult);
    });

    it('should pass the entire message as the query result', () => {
        receivedData!.should.not.be.null;
    });

    it('should have the correct data field', () => {
        (receivedData as any)!.data.should.deep.equal(queryResult.data);
    });

    it('should have the correct isSuccess field', () => {
        (receivedData as any)!.isSuccess.should.equal(true);
    });

    it('should have the correct paging field', () => {
        (receivedData as any)!.paging.should.deep.equal(queryResult.paging);
    });
}));
