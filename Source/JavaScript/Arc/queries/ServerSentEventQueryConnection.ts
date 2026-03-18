// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IObservableQueryConnection } from './IObservableQueryConnection';
import { DataReceived } from './ObservableQueryConnection';
import { QueryResult } from './QueryResult';

/**
 * The SSE hub route used when connecting through the composite observable query hub.
 */
export const SSE_HUB_ROUTE = '/.cratis/queries/sse';

/**
 * Represents a Server-Sent Events (SSE) connection for an observable query.
 *
 * The connection targets the composite hub endpoint (`/.cratis/queries/sse?query=<queryName>&…`)
 * rather than per-query WebSocket endpoints, enabling SSE-based real-time updates with lower
 * client-side overhead.
 */
export class ServerSentEventQueryConnection<TDataType> implements IObservableQueryConnection<TDataType> {
    private _eventSource?: EventSource;
    private _disconnected = false;

    /** @inheritdoc */
    readonly lastPingLatency: number = 0;

    /** @inheritdoc */
    readonly averageLatency: number = 0;

    /**
     * Initialises a new instance of {@link ServerSentEventQueryConnection}.
     * @param {URL} url The fully qualified URL of the SSE endpoint (including query parameters).
     */
    constructor(private readonly _url: URL) {}

    /** @inheritdoc */
    connect(dataReceived: DataReceived<TDataType>, queryArguments?: object): void {
        if (this._disconnected) return;

        let url = this._url.toString();
        if (queryArguments) {
            const separator = url.includes('?') ? '&' : '?';
            const query = Object.entries(queryArguments)
                .filter(([, value]) => value !== undefined && value !== null)
                .map(([key, value]) => `${encodeURIComponent(key)}=${encodeURIComponent(String(value))}`)
                .join('&');
            if (query) {
                url = `${url}${separator}${query}`;
            }
        }

        this._eventSource = new EventSource(url);

        this._eventSource.onmessage = (event: MessageEvent) => {
            if (this._disconnected) return;
            try {
                const parsed = JSON.parse(event.data as string);

                // The hub wraps data in an ObservableQueryHubMessage envelope.
                // Extract the inner result when the payload is present.
                const result: QueryResult<TDataType> = parsed?.payload ?? parsed;
                dataReceived(result as QueryResult<TDataType>);
            } catch (error) {
                console.error('SSE: error parsing message', error);
            }
        };

        this._eventSource.onerror = () => {
            if (this._disconnected) return;
            console.warn(`SSE: connection error for '${url}', EventSource will retry automatically.`);
        };
    }

    /** @inheritdoc */
    disconnect(): void {
        if (this._disconnected) return;
        this._disconnected = true;
        this._eventSource?.close();
        this._eventSource = undefined;
    }
}
