// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Globals } from '../Globals';
import { IObservableQueryConnection } from './IObservableQueryConnection';
import { DataReceived } from './ObservableQueryConnection';
import { QueryResult } from './QueryResult';

/**
 * The SSE demultiplexer route used when connecting through the multiplexed observable query endpoint.
 */
export const SSE_HUB_ROUTE = '/.cratis/queries/sse';

/**
 * Represents a direct Server-Sent Events (SSE) connection for a single observable query.
 *
 * In direct mode the URL points to the per-query endpoint (e.g. `/api/queries/latest`).
 * The backend detects the `Accept: text/event-stream` header and streams results directly.
 *
 * The caller (typically {@link createObservableQueryConnection}) decides which URL to use;
 * this class is transport-agnostic beyond being SSE.
 */
export class ServerSentEventQueryConnection<TDataType> implements IObservableQueryConnection<TDataType> {
    private _eventSource?: EventSource;
    private _disconnected = false;
    private _unauthorized = false;

    /** @inheritdoc */
    readonly lastPingLatency: number = 0;

    /** @inheritdoc */
    readonly averageLatency: number = 0;

    /**
     * Initializes a new instance of {@link ServerSentEventQueryConnection}.
     * @param {URL} url The fully qualified URL of the SSE endpoint (including query parameters).
     */
    constructor(private readonly _url: URL) {}

    /** @inheritdoc */
    connect(dataReceived: DataReceived<TDataType>, queryArguments?: object): void {
        if (this._disconnected || this._unauthorized) return;

        // Guard against environments where EventSource is not available (e.g. Node.js, SSR)
        // and no custom factory has been supplied to substitute it.
        if (!Globals.eventSourceFactory && typeof EventSource === 'undefined') {
            return;
        }

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

        this._eventSource = Globals.eventSourceFactory ? Globals.eventSourceFactory(url) : new EventSource(url);

        this._eventSource.onmessage = (event: MessageEvent) => {
            if (this._disconnected) return;
            try {
                const result = JSON.parse(event.data as string) as QueryResult<TDataType>;
                if ((result as unknown as { isAuthorized?: boolean })?.isAuthorized === false) {
                    this.handleUnauthorized(url, result, dataReceived);
                    return;
                }
                dataReceived(result);
            } catch (error) {
                console.error('SSE: error parsing message', error);
            }
        };

        this._eventSource.onerror = () => {
            if (this._disconnected || this._unauthorized) return;
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

    /**
     * Handles the terminal result the server sends when the caller is no longer authorized for the query it is
     * subscribed to.
     *
     * Unlike a WebSocket, an `EventSource` re-establishes a stream the server ended - the server closing its response
     * is precisely the signal the browser retries on, roughly every three seconds. Each retry re-runs the whole query
     * pipeline server side just to be denied again, so the stream is only actually terminal if this client closes it.
     * @param {string} url The stream url, for diagnostics.
     * @param {QueryResult<TDataType>} result The unauthorized result to deliver.
     * @param {DataReceived<TDataType>} dataReceived Callback that receives the result.
     */
    private handleUnauthorized(url: string, result: QueryResult<TDataType>, dataReceived: DataReceived<TDataType>): void {
        console.warn(`SSE: stream for '${url}' was ended by the server - no longer authorized`);
        this._unauthorized = true;
        this._eventSource?.close();
        this._eventSource = undefined;
        dataReceived(result);
    }
}
