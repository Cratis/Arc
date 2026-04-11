// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Manages keep-alive behavior for hub connections (both WebSocket and Server-Sent Events).
 *
 * Records connection activity (any message received or sent). An interval fires every
 * {@link intervalMs} milliseconds; if no activity has been recorded since the last
 * {@link idleThresholdMs} milliseconds the provided {@link onIdle} callback is invoked —
 * the caller decides whether to send a ping, trigger a reconnect, or take some other action.
 *
 * The idle threshold defaults to the check interval but can be set higher to tolerate
 * network latency between the server's keep-alive ping and the client's idle check.
 * For SSE connections, where the idle callback triggers a reconnect, a tolerance of
 * 1.5× the server's keep-alive interval prevents spurious reconnects caused by the
 * client's timer firing just before the server's ping arrives.
 *
 * Both {@link WebSocketHubConnection} and {@link ServerSentEventHubConnection} own one instance
 * of this class so the keep-alive logic is written once and behaves identically for both
 * transports.
 */
export class HubConnectionKeepAlive {
    private _lastActivityTime = Date.now();
    private _timer?: ReturnType<typeof setInterval>;
    private readonly _idleThresholdMs: number;

    /**
     * Initializes a new instance of {@link HubConnectionKeepAlive}.
     * @param {number} intervalMs How often (in milliseconds) to check for idle connections.
     * @param {() => void} onIdle Callback invoked when the interval fires and no activity has
     *   been recorded within the idle threshold.
     * @param {number} idleThresholdMs Optional. How long (in milliseconds) without activity
     *   before the connection is considered idle. Defaults to {@link intervalMs}. Set this
     *   higher than {@link intervalMs} when the peer sends keep-alive messages on a similar
     *   cadence to account for network latency and timer jitter.
     */
    constructor(
        private readonly _intervalMs: number,
        private readonly _onIdle: () => void,
        idleThresholdMs?: number,
    ) {
        this._idleThresholdMs = idleThresholdMs ?? _intervalMs;
    }

    /**
     * Start the keep-alive timer. Safe to call multiple times — a running timer is stopped first.
     */
    start(): void {
        this.stop();
        this._lastActivityTime = Date.now();
        this._timer = setInterval(() => {
            if (Date.now() - this._lastActivityTime >= this._idleThresholdMs) {
                this._onIdle();
            }
        }, this._intervalMs);
    }

    /**
     * Stop the keep-alive timer.
     */
    stop(): void {
        if (this._timer !== undefined) {
            clearInterval(this._timer);
            this._timer = undefined;
        }
    }

    /**
     * Record that the connection has been active (message sent or received).
     * Resets the idle timer so that a keep-alive is not sent while data is already flowing.
     */
    recordActivity(): void {
        this._lastActivityTime = Date.now();
    }
}
