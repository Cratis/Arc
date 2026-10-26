// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Represents a class, abstract or concrete, that may declare a static `sliceLocation` member.
 */
export type SliceLocationDeclaringType = abstract new (...args: never[]) => unknown;

/**
 * Represents where a command or query lives: the module, feature and slice resolved from its backend namespace.
 * @remarks
 * Generated proxies carry it as the static `sliceLocation` member, resolved from the same namespace mapping as the
 * folder the proxy is generated to. When the namespace is too shallow for all three parts, the deepest parts are
 * filled and the rest are empty strings, never null.
 *
 * The location is static metadata on the proxy class only. Instances carry no `sliceLocation` member, because a
 * command or query may itself have a property named `SliceLocation`, which the generated accessor would clash with.
 * Read it with {@link SliceLocation.of} for an instance or {@link SliceLocation.ofType} for a class.
 */
export class SliceLocation {
    /**
     * Gets a slice location where every part is empty.
     */
    static readonly empty: SliceLocation = new SliceLocation('', '', '');

    /**
     * Initializes a new instance of the {@link SliceLocation} class.
     * @param module The module the slice belongs to.
     * @param feature The feature the slice belongs to.
     * @param slice The slice itself.
     */
    constructor(readonly module: string, readonly feature: string, readonly slice: string) {
    }

    /**
     * Gets the slice location declared on the class of an instance, such as a generated command or query proxy.
     * @param instance The instance to get for.
     * @returns The declared {@link SliceLocation}, or {@link SliceLocation.empty} when the class declares none.
     */
    static of(instance: object): SliceLocation {
        return SliceLocation.ofType(instance.constructor as SliceLocationDeclaringType);
    }

    /**
     * Gets the slice location declared as the static `sliceLocation` member of a type, such as a generated command or query proxy class.
     * @param type The type to get for.
     * @returns The declared {@link SliceLocation}, or {@link SliceLocation.empty} when the type declares none.
     */
    static ofType(type: SliceLocationDeclaringType): SliceLocation {
        const declared = (type as { sliceLocation?: unknown }).sliceLocation;
        return declared instanceof SliceLocation ? declared : SliceLocation.empty;
    }

    /**
     * Gets the parts that are set, joined with a forward slash.
     * @returns The slice location as a path, for example `Billing/Invoices/Pay`.
     */
    toString(): string {
        return [this.module, this.feature, this.slice].filter(part => part.length > 0).join('/');
    }
}
