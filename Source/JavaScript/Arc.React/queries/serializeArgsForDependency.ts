// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Derives a stable, constant-shape dependency value from query arguments for use in a React
 * dependency array.
 *
 * React compares dependencies with `Object.is`, so an argument whose runtime type is an object -
 * a `Guid`, a `DateOnly`, any generated concept - is compared by identity. A value re-derived in
 * render position (`Guid.parse(useParams().id)`) is a new object every render, so an effect keyed
 * on the raw value re-runs on every render. Every query argument type Arc generates implements
 * `toJSON()`, so serializing collapses them to their canonical string and the comparison becomes
 * one by value.
 *
 * Spreading `Object.values(args)` positionally makes the dependency array's length track the
 * argument object's key count. React's `areHookInputsEqual` only compares the overlapping prefix
 * when a dependency array's length changes between renders, so a render where `args` goes from
 * `undefined` to `{ key: value }` (or vice versa) can have its memo/effect report "no change" even
 * though the arguments did change. Collapsing `args` into one sorted, serialized string keeps the
 * dependency array a constant length regardless of `args`' shape.
 * @param {object} [args] The query arguments to serialize.
 * @returns {string} A stable string dependency, empty when there are no arguments.
 */
export function serializeArgsForDependency(args?: object): string {
    if (!args || Object.keys(args).length === 0) {
        return '';
    }

    const sorted = Object.keys(args)
        .sort()
        .reduce<Record<string, unknown>>((accumulator, key) => {
            accumulator[key] = (args as Record<string, unknown>)[key];
            return accumulator;
        }, {});

    return JSON.stringify(sorted);
}
