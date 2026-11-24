// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { UnavailablePart } from './UnavailablePart.js';
import { PartAvailability } from './PartAvailability.js';

interface Location {
    readonly module: string;
    readonly feature: string;
    readonly slice: string;
}

interface Identity extends Location {
    readonly name: string;
}

/**
 * Something a client can ask about: the full name of a command or read model on the server, or a generated proxy
 * (class or instance) that carries the part it is and the slice it belongs to.
 */
export type AvailabilityPart =
    | string
    | Identity
    | { readonly part?: Identity; readonly sliceLocation?: Location }
    | { readonly constructor: { readonly part?: Identity; readonly sliceLocation?: Location } };

/**
 * Gets the full name of a part, when it is known.
 * @param part The full name, or a generated proxy or part identity.
 * @returns The name the server knows the part by; undefined when the proxy does not carry one.
 */
export function partNameOf(part: AvailabilityPart): string | undefined {
    if (typeof part === 'string') {
        return part;
    }

    return identityOf(part)?.name;
}

/**
 * Represents what is available to the current context, from the last decision the server reported.
 *
 * Everything the server does not list is available. Until the first decision has arrived nothing is known, and
 * nothing is reported as available, so a client never shows a part it has not been told it may show.
 *
 * This answers for the context alone, such as the tenant and the user. A gate that depends on the data a command or
 * query is about, such as one vessel, is asked with `askAvailability` (or `useIsAvailable` in React), which sends the data.
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
     * @param part The full name of a command or read model, or a generated proxy. A proxy is matched by the part it is
     * and by the slice it belongs to, so every part of a slice is hidden together, which is how navigation and screens are decided.
     * @returns True when the part is available; false when it is not, or when nothing is known yet.
     */
    isAvailable(part: AvailabilityPart): boolean {
        return this.availabilityOf(part).isAvailable;
    }

    /**
     * Gets whether, and if not why, a part is available.
     * @param part The full name of a command or read model, or a generated proxy.
     * @returns The {@link PartAvailability}; `NotAvailable` is told apart from `Suspended` by its reason.
     */
    availabilityOf(part: AvailabilityPart): PartAvailability {
        if (!this.isKnown) {
            return PartAvailability.unknown;
        }

        const listed = this.find(part);
        return listed === undefined ? PartAvailability.available : PartAvailability.refused(listed.reason);
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

    private find(part: AvailabilityPart): UnavailablePart | undefined {
        if (typeof part === 'string') {
            return this.unavailable.find(_ => _.name === part);
        }

        const identity = identityOf(part);
        if (identity !== undefined) {
            const byName = this.unavailable.find(_ => _.name === identity.name);
            if (byName !== undefined) {
                return byName;
            }
        }

        const location = locationOf(part, identity);
        if (location === undefined || (location.module === '' && location.feature === '' && location.slice === '')) {
            return undefined;
        }

        return this.unavailable.find(_ => _.module === location.module && _.feature === location.feature && _.slice === location.slice);
    }
}

function identityOf(part: Exclude<AvailabilityPart, string>): Identity | undefined {
    // A proxy class declares its identity as the static `part`. An instance is read through its class only, because a
    // command or query may itself have a property named `part`.
    if (typeof part === 'function') {
        return asIdentity((part as { part?: unknown }).part);
    }

    return asIdentity(part) ?? asIdentity((part as { constructor?: { part?: unknown } }).constructor?.part);
}

function asIdentity(value: unknown): Identity | undefined {
    if (typeof value !== 'object' || value === null) {
        return undefined;
    }

    const candidate = value as Record<string, unknown>;
    return typeof candidate.name === 'string' && typeof candidate.module === 'string' && typeof candidate.feature === 'string' && typeof candidate.slice === 'string'
        ? value as Identity
        : undefined;
}

function locationOf(part: Exclude<AvailabilityPart, string>, identity: Identity | undefined): Location | undefined {
    if (identity !== undefined) {
        return identity;
    }

    const own = (part as { sliceLocation?: Location }).sliceLocation;
    if (own !== undefined) {
        return own;
    }

    return (part as { constructor?: { sliceLocation?: Location } }).constructor?.sliceLocation;
}
