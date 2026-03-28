// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor } from '@cratis/fundamentals';

/**
 * Determines whether the given constructor is a user-defined class type suitable for deserialization,
 * as opposed to a built-in primitive wrapper (Object, String, Number, Boolean).
 * @param type The constructor to check.
 * @returns True if the type is a user-defined class; false for built-in primitives.
 */
export function isUserDefinedClass(type: Constructor): boolean {
    return type !== Object && type !== String && type !== Number && type !== Boolean;
}
