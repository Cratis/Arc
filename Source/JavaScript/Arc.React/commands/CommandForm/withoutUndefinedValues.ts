// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Copies the values that are actually supplied, dropping every key holding `undefined`.
 *
 * Object spread copies own enumerable keys regardless of what they hold, so `{ name: undefined }`
 * spread over a resolved value replaces it with nothing rather than leaving it alone. A seed layer
 * has no way to express "clear this" - it describes what to start from - so a key holding
 * `undefined` supplies nothing and the layer beneath it keeps whatever it resolved.
 * @param values The values to copy from.
 * @returns A copy holding only the keys whose value is defined.
 */
export function withoutUndefinedValues<TValues extends object>(values: Partial<TValues> | undefined): Partial<TValues> {
    if (!values) {
        return {};
    }

    const supplied: Record<string, unknown> = {};
    for (const [key, value] of Object.entries(values)) {
        if (value !== undefined) {
            supplied[key] = value;
        }
    }

    return supplied as Partial<TValues>;
}
