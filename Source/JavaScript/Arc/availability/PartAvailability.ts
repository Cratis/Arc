// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { NotAvailable, Suspended } from './AvailabilityReason.js';

/**
 * The answer to whether one part is available, optionally for the data it is about.
 */
export class PartAvailability {
    /** The answer for a part that is available. */
    static readonly available: PartAvailability = new PartAvailability(true);

    /** The answer when the server has not told yet, or could not decide. Nothing is reported as available. */
    static readonly unknown: PartAvailability = new PartAvailability(false, undefined, false);

    /**
     * Initializes a new instance of {@link PartAvailability}.
     * @param isAvailable Whether the part is available.
     * @param reason Why it is not: `NotAvailable` or `Suspended`; undefined when it is available or not known.
     * @param isKnown Whether the server has decided.
     */
    constructor(readonly isAvailable: boolean, readonly reason?: string, readonly isKnown: boolean = true) {
    }

    /** Gets whether the part is not available for a reason other than a suspension. */
    get isNotAvailable(): boolean {
        return this.isKnown && !this.isAvailable && this.reason !== Suspended;
    }

    /** Gets whether an operator has suspended the part. It comes back when the suspension is lifted. */
    get isSuspended(): boolean {
        return this.isKnown && !this.isAvailable && this.reason === Suspended;
    }

    /**
     * Creates the answer for a reason the server gave.
     * @param reason The reason; a missing reason is `NotAvailable`.
     * @returns The answer.
     */
    static refused(reason?: string): PartAvailability {
        return new PartAvailability(false, reason ?? NotAvailable);
    }
}
