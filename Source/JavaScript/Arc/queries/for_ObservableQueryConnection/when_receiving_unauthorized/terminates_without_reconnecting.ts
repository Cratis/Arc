// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { given } from '../../../given';
import { an_observable_query_connection_with_a_reconnect_policy } from '../given/an_observable_query_connection_with_a_reconnect_policy';
import { QueryResult } from '../../QueryResult';
import { WebSocketMessageType } from '../../WebSocketMessage';

describe('when receiving unauthorized it terminates without reconnecting', given(an_observable_query_connection_with_a_reconnect_policy, context => {
    const unauthorizedResult = {
        data: null,
        isSuccess: false,
        isAuthorized: false,
        isValid: true,
        hasExceptions: false,
        validationResults: [],
        exceptionMessages: [],
        exceptionStackTrace: '',
        paging: { page: 0, size: 0, totalItems: 0, totalPages: 0 }
    };

    let receivedResults: QueryResult<unknown>[] = [];

    beforeEach(() => {
        receivedResults = [];
        context.connection.connect((data) => {
            receivedResults.push(data as QueryResult<unknown>);
        });
        context.simulateMessage({
            type: WebSocketMessageType.Data,
            data: unauthorizedResult
        });

        // The server closes the stream right after the terminal result.
        context.simulateClose();
    });

    it('should deliver the unauthorized result', () => {
        receivedResults.should.have.lengthOf(1);
    });

    it('should deliver it as not authorized', () => {
        (receivedResults[0] as unknown as { isAuthorized: boolean }).isAuthorized.should.be.false;
    });

    it('should not schedule a reconnect', () => {
        context.schedule.called.should.be.false;
    });

    it('should cancel any pending reconnect', () => {
        context.cancel.called.should.be.true;
    });

    it('should close the socket', () => {
        context.fakeWebSocket.close.called.should.be.true;
    });
}));
