// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Represents which part of the application a generated command or query proxy is: the full name the server knows it
 * by and the slice it belongs to.
 * @remarks
 * Generated proxies carry it as the static `part` member. It is identity only: the availability gate itself stays on the
 * server, so a client asks the server whether the part is available and never decides on its own.
 */
export class PartIdentity {
    /**
     * Initializes a new instance of the {@link PartIdentity} class.
     * @param name The full name of the command type, or of the type that declares the query, on the server.
     * @param module The module the slice belongs to; empty when it has none.
     * @param feature The feature the slice belongs to; empty when it has none.
     * @param slice The slice itself; empty when it has none.
     */
    constructor(readonly name: string, readonly module: string, readonly feature: string, readonly slice: string) {
    }
}
