// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Globals } from '../Globals';
import { IObservableQueryConnection } from './IObservableQueryConnection';
import { IReconnectPolicy } from './IReconnectPolicy';
import { ReconnectPolicy } from './ReconnectPolicy';
import { QueryResult } from './QueryResult';
import { WebSocketMessage, WebSocketMessageType } from './WebSocketMessage';

export type DataReceived<TDataType> = (data: QueryResult<TDataType>) => void;

/**
 * Represents the connection for an observable query.
 */
export class ObservableQueryConnection<TDataType> implements IObservableQueryConnection<TDataType> {

    private _socket!: WebSocket;
    private _disconnected = false;
    private _url: string;
    private _pingInterval?: ReturnType<typeof setInterval>;
    private _pingIntervalMs: number = 10000;
    private _lastPingSentTime?: number;
    private _lastPongLatency: number = 0;
    private _latencySamples: number[] = [];
    private _connectionStartTime?: number;

    /**
     * Initializes a new instance of the {@link ObservableQueryConnection<TDataType>} class.
     * @param {Url} url The fully qualified Url.
     * @param {string} _microservice The microservice name.
     * @param {number} pingIntervalMs The ping interval in milliseconds (default: 10000).
     * @param {IReconnectPolicy} reconnectPolicy The reconnect policy to use (default: {@link ReconnectPolicy}).
     */
    constructor(url: URL, private readonly _microservice: string, pingIntervalMs: number = 10000, private readonly _reconnectPolicy: IReconnectPolicy = new ReconnectPolicy()) {
        this._pingIntervalMs = pingIntervalMs;
        const secure = url.protocol?.indexOf('https') === 0 || false;

        this._url = `${secure ? 'wss' : 'ws'}://${url.host}${url.pathname}${url.search}`;
        if (this._microservice?.length > 0) {
            const microserviceParam = `${Globals.microserviceWSQueryArgument}=${this._microservice}`;
            if (this._url.indexOf('?') > 0) {
                this._url = `${this._url}&${microserviceParam}`;
            } else {
                this._url = `${this._url}?${microserviceParam}`;
            }
        }
    }

    /**
     * Disposes the connection.
     */
    dispose() {
        this.disconnect();
    }

    /**
     * Gets the latency of the last ping/pong sequence in milliseconds.
     */
    get lastPingLatency(): number {
        return this._lastPongLatency;
    }

    /**
     * Gets the average latency since the connection started in milliseconds.
     */
    get averageLatency(): number {
        if (this._latencySamples.length === 0) {
            return 0;
        }
        const sum = this._latencySamples.reduce((acc, val) => acc + val, 0);
        return sum / this._latencySamples.length;
    }

    /** @inheritdoc */
    connect(dataReceived: DataReceived<TDataType>, queryArguments?: object) {
        let url = this._url;
        if (queryArguments) {
            if (url.indexOf('?') < 0) {
                url = `${url}?`;
            } else {
                url = `${url}&`;
            }
            const query = Object.keys(queryArguments).map(key => `${key}=${queryArguments[key]}`).join('&');
            url = `${url}${query}`;
        }

        const connectSocket = () => {
            this._socket = new WebSocket(url);
            this._socket.onopen = () => {
                if (this._disconnected) return;
                console.log(`Connection for '${url}' established`);
                this._reconnectPolicy.reset();
                this._connectionStartTime = Date.now();
                this.startPinging();
            };
            this._socket.onclose = () => {
                if (this._disconnected) return;
                console.log(`Unexpected connection closed for route '${url}'`);
                this.stopPinging();
                this._reconnectPolicy.schedule(connectSocket, url);
            };
            this._socket.onerror = (error) => {
                if (this._disconnected) return;
                console.log(`Error with connection for '${url}' - ${error}`);
                this.stopPinging();
                this._reconnectPolicy.schedule(connectSocket, url);
            };
            this._socket.onmessage = (ev) => {
                if (this._disconnected) {
                    console.log('Received message after closing connection');
                    return;
                }
                this.handleMessage(ev.data, dataReceived);
            };
        };

        if (this._disconnected) return;
        connectSocket();
    }

    /** @inheritdoc */
    disconnect() {
        if (this._disconnected) {
            return;
        }
        console.log(`Disconnecting '${this._url}'`);
        this._disconnected = true;
        this._reconnectPolicy.cancel();
        this.stopPinging();
        this._socket?.close();
        console.log(`Connection for '${this._url}' closed`);
        this._socket = undefined!;
    }

    private startPinging() {
        this.stopPinging();
        this._pingInterval = setInterval(() => {
            this.sendPing();
        }, this._pingIntervalMs);
    }

    private stopPinging() {
        if (this._pingInterval) {
            clearInterval(this._pingInterval);
            this._pingInterval = undefined;
        }
    }

    private sendPing() {
        if (this._disconnected || !this._socket || this._socket.readyState !== WebSocket.OPEN) {
            return;
        }

        this._lastPingSentTime = Date.now();
        const pingMessage: WebSocketMessage = {
            type: WebSocketMessageType.Ping,
            timestamp: this._lastPingSentTime
        };

        this._socket.send(JSON.stringify(pingMessage));
    }

    private handleMessage(rawData: string, dataReceived: DataReceived<TDataType>) {
        try {
            const message = JSON.parse(rawData) as WebSocketMessage;

            // Handle messages based on type
            if (message.type === WebSocketMessageType.Pong) {
                this.handlePong(message);
            } else if (message.type === WebSocketMessageType.Data || !message.type) {
                // For new-style messages (type === 'Data') the query result is wrapped in message.data.
                // For legacy/backward-compatible messages (no type) the entire message IS the query result.
                const data = message.type === WebSocketMessageType.Data ? message.data : message;
                dataReceived(data as QueryResult<TDataType>);
            }
        } catch (error) {
            console.error('Error parsing WebSocket message:', error);
        }
    }

    private handlePong(message: WebSocketMessage) {
        if (message.timestamp && this._lastPingSentTime) {
            const latency = Date.now() - message.timestamp;
            this._lastPongLatency = latency;
            this._latencySamples.push(latency);

            // Keep only the last 100 samples for average calculation
            if (this._latencySamples.length > 100) {
                this._latencySamples.shift();
            }
        }
    }
}
