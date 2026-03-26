// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Callback invoked by {@link IReconnectPolicy} when it is time to attempt a reconnect.
 */
export type ReconnectCallback = () => void;

/**
 * Defines the contract for a reconnect policy that controls back-off and retry behaviour
 * for observable query connections.
 */
export interface IReconnectPolicy {
    /**
     * The current number of reconnect attempts made since the last {@link reset}.
     */
    readonly attempt: number;

    /**
     * Schedule the next reconnect attempt.
     * @param {ReconnectCallback} onReconnect Callback to invoke when the delay elapses.
     * @param {string} label Human-readable label used in log output (e.g. the connection URL).
     * @returns {boolean} {@code true} if the attempt was scheduled; {@code false} if the maximum
     *                    number of attempts has been reached and the policy has given up.
     */
    schedule(onReconnect: ReconnectCallback, label: string): boolean;

    /**
     * Reset the attempt counter and cancel any pending timer.
     * Call this when a connection is successfully established.
     */
    reset(): void;

    /**
     * Cancel any pending reconnect timer without resetting the attempt counter.
     * Call this when the connection is permanently disposed.
     */
    cancel(): void;
}
