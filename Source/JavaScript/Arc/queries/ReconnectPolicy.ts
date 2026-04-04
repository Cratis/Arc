// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IReconnectPolicy, ReconnectCallback } from './IReconnectPolicy';

/**
 * Default exponential back-off reconnect policy shared by all observable query connections.
 *
 * Delay formula: `Math.min(initialDelayMs + delayStepMs * attempt, maxDelayMs)`
 *
 * With the default parameters:
 * - Attempt 1 → 1 000 ms
 * - Attempt 2 → 1 500 ms
 * - …
 * - Attempt 19+ → 10 000 ms (capped)
 * - After 100 attempts → abandoned (returns `false`)
 */
export class ReconnectPolicy implements IReconnectPolicy {
    private _attempt = 0;
    private _timer?: ReturnType<typeof setTimeout>;

    /**
     * Initializes a new instance of {@link ReconnectPolicy}.
     * @param {number} maxAttempts Maximum number of reconnect attempts before giving up (default: 100).
     * @param {number} initialDelayMs Base delay in milliseconds added to every attempt (default: 500).
     * @param {number} delayStepMs Additional delay per attempt in milliseconds (default: 500).
     * @param {number} maxDelayMs Upper bound on the computed delay in milliseconds (default: 10 000).
     */
    constructor(
        private readonly _maxAttempts: number = 100,
        private readonly _initialDelayMs: number = 500,
        private readonly _delayStepMs: number = 500,
        private readonly _maxDelayMs: number = 10_000,
    ) {}

    /** @inheritdoc */
    get attempt(): number {
        return this._attempt;
    }

    /** @inheritdoc */
    schedule(onReconnect: ReconnectCallback, label: string): boolean {
        if (this._attempt >= this._maxAttempts) {
            console.log(`Reconnect: abandoned after ${this._maxAttempts} attempts for '${label}'`);
            return false;
        }

        // Cancel any pending reconnect timer so we don't fire multiple
        // concurrent reconnect attempts when schedule() is called rapidly.
        this.cancel();

        this._attempt++;
        const delay = Math.min(this._initialDelayMs + this._delayStepMs * this._attempt, this._maxDelayMs);
        console.log(`Reconnect: attempt ${this._attempt} in ${delay}ms for '${label}'`);
        this._timer = setTimeout(onReconnect, delay);
        return true;
    }

    /** @inheritdoc */
    reset(): void {
        this._attempt = 0;
        this.cancel();
    }

    /** @inheritdoc */
    cancel(): void {
        if (this._timer) {
            clearTimeout(this._timer);
            this._timer = undefined;
        }
    }
}
