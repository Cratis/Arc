// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { GetHttpHeaders } from '../GetHttpHeaders.js';
import { Globals } from '../Globals.js';
import { UrlHelpers } from '../UrlHelpers.js';
import { deepEqual } from '../deepEqual.js';
import { DataReceived } from './ObservableQueryConnection.js';
import { IObservableQueryConnection } from './IObservableQueryConnection.js';
import { PollingOptions } from './PollingOptions.js';
import { QueryResult } from './QueryResult.js';
import { isAbortError } from './isAbortError.js';

/** Response header specifying the next poll delay in seconds. */
export const QUERY_POLL_AFTER_HEADER = 'Cratis-Query-Poll-After';

/** Observes a query using sequential HTTP GET snapshots rather than a persistent connection. */
export class PollingQueryConnection<TDataType> implements IObservableQueryConnection<TDataType> {
    private _started = false;
    private _disconnected = false;
    private _unauthorized = false;
    private _timer?: ReturnType<typeof setTimeout>;
    private _controller?: AbortController;
    private _failures = 0;
    private _previousComparable?: object;
    private _lastPingLatency = 0;
    private _totalLatency = 0;
    private _requestCount = 0;

    /**
     * Initializes a polling connection.
     * @param _url The per-query HTTP endpoint.
     * @param _httpHeadersCallback Custom headers evaluated for each request.
     * @param _microservice The microservice identifier sent in the configured HTTP header.
     * @param _options The resolved timing policy for this connection.
     */
    constructor(
        private readonly _url: URL,
        private readonly _httpHeadersCallback: GetHttpHeaders,
        private readonly _microservice: string,
        private readonly _options: PollingOptions
    ) {}

    /** @inheritdoc */
    get lastPingLatency(): number { return this._lastPingLatency; }

    /** @inheritdoc */
    get averageLatency(): number {
        return this._requestCount ? this._totalLatency / this._requestCount : 0;
    }

    /** @inheritdoc */
    connect(dataReceived: DataReceived<TDataType>, queryArguments?: object): void {
        if (this._started || this._disconnected || this._unauthorized) return;
        this._started = true;
        const url = new URL(this._url);
        const parameters = UrlHelpers.buildQueryParams(queryArguments ?? {}, { waitForFirstResult: 'true' });
        parameters.forEach((value, key) => url.searchParams.append(key, value));
        void this.poll(url, dataReceived);
    }

    /** @inheritdoc */
    disconnect(): void {
        this._disconnected = true;
        clearTimeout(this._timer);
        this._timer = undefined;
        this._controller?.abort();
    }

    private async poll(url: URL, dataReceived: DataReceived<TDataType>): Promise<void> {
        this._controller = new AbortController();
        const started = performance.now();
        let delay: number;
        try {
            const headers = new Headers({
                ...Object.fromEntries(new Headers(Globals.httpHeadersCallback?.())),
                ...Object.fromEntries(new Headers(this._httpHeadersCallback()))
            });
            headers.set('Accept', 'application/json');
            headers.set('Content-Type', 'application/json');
            if (this._microservice) headers.set(Globals.microserviceHttpHeader, this._microservice);
            const response = await fetch(url.toString(), { method: 'GET', headers, signal: this._controller.signal });
            if (this._disconnected) return;
            const body = await response.text();
            if (this._disconnected) return;
            this._lastPingLatency = performance.now() - started;
            this._totalLatency += this._lastPingLatency;
            this._requestCount++;
            delay = this.handleResponse(response, body, dataReceived);
        } catch (error) {
            if (this._disconnected || this._unauthorized || isAbortError(error)) return;
            delay = this.backoff();
        }
        if (!this._disconnected && !this._unauthorized) {
            this._timer = setTimeout(() => { void this.poll(url, dataReceived); }, delay);
        }
    }

    private handleResponse(response: Response, body: string, dataReceived: DataReceived<TDataType>): number {
        let result = this.parseResult(body);
        if (response.status === 401 || response.status === 403 || result?.isAuthorized === false) {
            console.warn(`Polling: query for '${this._url}' was ended by the server - no longer authorized`);
            this._unauthorized = true;
            result ??= { ...QueryResult.unauthorized<TDataType>() } as QueryResult<TDataType>;
            result = { ...result, isAuthorized: false } as QueryResult<TDataType>;
            this.emitChanged(result, JSON.stringify(result), dataReceived);
            return 0;
        }
        if (result?.isValid === false) {
            this.disconnect();
            this.emitChanged(result, body, dataReceived);
            return 0;
        }
        const success = response.status === 200 && result !== undefined;
        if (result && (success || response.status >= 500)) this.emitChanged(result, body, dataReceived);
        if (success) this._failures = 0;
        const delay = success ? this.clamp(this._options.intervalMs) : this.backoff();
        const hint = response.headers.get(QUERY_POLL_AFTER_HEADER);
        const seconds = hint?.trim() ? Number(hint) : NaN;
        return Number.isFinite(seconds) && seconds >= 0 ? this.clamp(seconds * 1000) : delay;
    }

    private parseResult(body: string): QueryResult<TDataType> | undefined {
        try {
            const result = JSON.parse(body) as QueryResult<TDataType>;
            if (result && typeof result === 'object' && typeof result.isAuthorized === 'boolean') return result;
        } catch {
            // Non-JSON responses are transient failures, except for terminal authentication failures.
        }
        return undefined;
    }

    private emitChanged(result: QueryResult<TDataType>, body: string, dataReceived: DataReceived<TDataType>): void {
        const comparable = JSON.parse(body) as { correlationId?: string };
        delete comparable.correlationId;
        if (deepEqual(this._previousComparable, comparable)) return;
        this._previousComparable = comparable;
        dataReceived(result);
    }

    private backoff(): number {
        const jitter = 1 + this._options.jitterRatio * (2 * Math.random() - 1);
        return this.clamp(this._options.intervalMs * this._options.backoffMultiplier ** ++this._failures * jitter);
    }

    private clamp(delay: number): number {
        return Math.min(this._options.maxIntervalMs, Math.max(this._options.minIntervalMs, delay));
    }
}
