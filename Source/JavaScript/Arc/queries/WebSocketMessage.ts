// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Represents the type of WebSocket message.
 */
export enum WebSocketMessageType {
    /**
     * A data message containing query results.
     */
    Data = 'Data',

    /**
     * A ping message sent from client to server.
     */
    Ping = 'Ping',

    /**
     * A pong message sent from server to client in response to a ping.
     */
    Pong = 'Pong'
}

/**
 * Represents a WebSocket message envelope.
 */
export type WebSocketMessage = {
    /**
     * The type of message.
     */
    type: WebSocketMessageType;

    /**
     * The timestamp when the message was sent (for ping/pong latency tracking).
     */
    timestamp?: number;

    /**
     * The payload data (for data messages).
     */
    /* eslint-disable @typescript-eslint/no-explicit-any */
    data?: any;
    /* eslint-enable @typescript-eslint/no-explicit-any */
};
