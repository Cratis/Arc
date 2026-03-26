// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Defines the transport method used for observable query connections.
 */
export enum QueryTransportMethod {
    /**
     * Use WebSocket for real-time data streaming.
     */
    WebSocket = 'webSocket',

    /**
     * Use Server-Sent Events (SSE) for real-time data streaming.
     */
    ServerSentEvents = 'serverSentEvents',
}
