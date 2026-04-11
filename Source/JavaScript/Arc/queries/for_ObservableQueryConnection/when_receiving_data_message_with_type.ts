// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../given';
import { an_observable_query_connection_with_websocket } from './given/an_observable_query_connection_with_websocket';
import { QueryResult } from '../QueryResult';
import { WebSocketMessageType } from '../WebSocketMessage';

/* eslint-disable @typescript-eslint/no-explicit-any */

describe('when receiving data message with Data type', given(an_observable_query_connection_with_websocket, context => {
    const innerQueryResult = {
        data: [{ id: '2', name: 'Item' }],
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
        context.simulateMessage({
            type: WebSocketMessageType.Data,
            data: innerQueryResult
        });
    });

    it('should pass the inner data payload as the query result', () => {
        receivedData!.should.not.be.null;
    });

    it('should have the correct data field', () => {
        (receivedData as any)!.data.should.deep.equal(innerQueryResult.data);
    });

    it('should have the correct isSuccess field', () => {
        (receivedData as any)!.isSuccess.should.equal(true);
    });
}));
