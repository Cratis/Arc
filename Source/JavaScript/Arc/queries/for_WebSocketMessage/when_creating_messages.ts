// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { WebSocketMessage, WebSocketMessageType } from '../WebSocketMessage';

describe('when creating a ping message', () => {
    let timestamp: number;
    let message: WebSocketMessage;

    beforeEach(() => {
        timestamp = Date.now();
        message = {
            type: WebSocketMessageType.Ping,
            timestamp: timestamp
        };
    });

    it('should have ping type', () => {
        message.type.should.equal(WebSocketMessageType.Ping);
    });

    it('should have timestamp', () => {
        message.timestamp!.should.equal(timestamp);
    });
});

describe('when creating a pong message', () => {
    let timestamp: number;
    let message: WebSocketMessage;

    beforeEach(() => {
        timestamp = Date.now();
        message = {
            type: WebSocketMessageType.Pong,
            timestamp: timestamp
        };
    });

    it('should have pong type', () => {
        message.type.should.equal(WebSocketMessageType.Pong);
    });

    it('should have timestamp', () => {
        message.timestamp!.should.equal(timestamp);
    });
});

describe('when creating a data message', () => {
    let data: object;
    let message: WebSocketMessage;

    beforeEach(() => {
        data = { test: 'value' };
        message = {
            type: WebSocketMessageType.Data,
            data: data
        };
    });

    it('should have data type', () => {
        message.type.should.equal(WebSocketMessageType.Data);
    });

    it('should have data', () => {
        message.data.should.equal(data);
    });
});
