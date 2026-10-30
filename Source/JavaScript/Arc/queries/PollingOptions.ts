// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/** Timing policy for HTTP observable-query polling. */
export interface PollingOptions {
    /** Normal interval after a successful poll, in milliseconds. Defaults to 5000. */
    intervalMs: number;
    /** Minimum delay, including server hints and jitter, in milliseconds. Defaults to 1000. */
    minIntervalMs: number;
    /** Maximum delay, including server hints and jitter, in milliseconds. Defaults to 60000. */
    maxIntervalMs: number;
    /** Exponential multiplier after transient failures. Defaults to 2. */
    backoffMultiplier: number;
    /** Symmetric random proportion applied to backoff delays. Defaults to 0.2 (20%). */
    jitterRatio: number;
}
