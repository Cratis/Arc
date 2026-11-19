// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Represents a command or read model that is not available to the current context.
 */
export interface UnavailablePart {
    /** The name of the part: the full name of its command or read model type on the server. */
    readonly name: string;

    /** The module of the slice the part belongs to; empty when it has none. */
    readonly module: string;

    /** The feature of the slice the part belongs to; empty when it has none. */
    readonly feature: string;

    /** The slice the part belongs to; empty when it has none. */
    readonly slice: string;
}
