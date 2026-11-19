// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { UnavailablePart } from './UnavailablePart.js';

/**
 * Something a client can ask about: the full name of a command or read model on the server, or a generated proxy
 * (class or instance) that carries the slice it belongs to.
 */
export type AvailabilityPart =
    | string
    | { readonly sliceLocation?: { readonly module: string; readonly feature: string; readonly slice: string } }
    | { readonly constructor: { readonly sliceLocation?: { readonly module: string; readonly feature: string; readonly slice: string } } };

/**
 * Represents what is available to the current context, from the last decision the server reported.
 *
 * Everything the server does not list is available. Until the first decision has arrived nothing is known, and
 * nothing is reported as available, so a client never shows a part it has not been told it may show.
 */
export class Availability {
    /** An availability that has not heard from the server yet. */
    static readonly unknown: Availability = new Availability([], false);

    /** An availability that treats every part as available, for hosts that do not use availability. */
    static readonly everything: Availability = new Availability([], true);

    /**
     * Initializes a new instance of {@link Availability}.
     * @param unavailable The parts the server reported as not available.
     * @param isKnown Whether a decision has arrived.
     */
    constructor(readonly unavailable: readonly UnavailablePart[], readonly isKnown: boolean = true) {
    }

    /**
     * Determines whether a part is available.
     * @param part The full name of a command or read model, or a generated proxy. A proxy is matched by the slice it
     * belongs to, so every part of a slice is hidden together, which is how navigation and screens are decided.
     * @returns True when the part is available; false when it is not, or when nothing is known yet.
     */
    isAvailable(part: AvailabilityPart): boolean {
        if (!this.isKnown) {
            return false;
        }

        if (typeof part === 'string') {
            return !this.unavailable.some(_ => _.name === part);
        }

        const location = sliceLocationOf(part);
        if (location === undefined || (location.module === '' && location.feature === '' && location.slice === '')) {
            return true;
        }

        return !this.unavailable.some(_ => _.module === location.module && _.feature === location.feature && _.slice === location.slice);
    }

    /**
     * Keeps the items whose part is available.
     * @param items The items, for example navigation entries.
     * @param partOf Gets the part an item belongs to; an item without one is always kept.
     * @returns The items that are available.
     */
    filter<TItem>(items: readonly TItem[], partOf: (item: TItem) => AvailabilityPart | undefined): TItem[] {
        return items.filter(item => {
            const part = partOf(item);
            return part === undefined || this.isAvailable(part);
        });
    }
}

function sliceLocationOf(part: Exclude<AvailabilityPart, string>): { module: string; feature: string; slice: string } | undefined {
    const own = (part as { sliceLocation?: { module: string; feature: string; slice: string } }).sliceLocation;
    if (own !== undefined) {
        return own;
    }

    return (part as { constructor?: { sliceLocation?: { module: string; feature: string; slice: string } } }).constructor?.sliceLocation;
}
