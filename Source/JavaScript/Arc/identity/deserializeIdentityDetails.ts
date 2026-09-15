// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor, Fields, JsonSerializer } from '@cratis/fundamentals';

/*
 * ⚠️ deserializeIdentityDetails is exported deliberately, unlike deserializeQueryModel in
 * ../queries/deserializeQueryModel.ts.
 *
 * JsonSerializer's converter registry is module state, and a consumer package can easily resolve a
 * different physical copy of @cratis/fundamentals than this one - which is why deserializeQueryModel
 * stays package-private. Identity details are different: @cratis/arc.react's useIdentity() needs to
 * apply the exact same deserialization as this package's own IdentityProvider, for a payload that may
 * already be flowing through this package's IdentityProvider/IIdentity. Exporting the function itself
 * - rather than having @cratis/arc.react re-implement the same logic against its own copy of
 * @cratis/fundamentals - guarantees every identity details value is deserialized through this one
 * physical JsonSerializer/Fields, regardless of which package's copy of @cratis/fundamentals the
 * caller resolved.
 */

/**
 * Determines whether a details type is a JavaScript primitive wrapper rather than a real,
 * `@field`-decorated model.
 * @param {Constructor} type The details type to check.
 * @returns {boolean} True when the type is a primitive wrapper.
 */
function isPrimitiveDetailsType(type: Constructor): boolean {
    return type === String || type === Number || type === Boolean;
}

/**
 * Deserializes identity details into their strongly-typed shape, guarding every way that doing so
 * would otherwise crash or silently destroy the payload.
 * @param {Constructor | undefined} type The details type to deserialize into, or `undefined` to leave
 * the payload untouched.
 * @param {unknown} details The raw details payload - parsed JSON from the identity cookie or the
 * `/.cratis/me` endpoint.
 * @returns {unknown} The deserialized instance, or `details` unchanged when deserializing it would be
 * unsafe, pointless, or has already been done.
 * @remarks
 * {@link JsonSerializer.deserializeFromInstance} assumes a well-formed object and a target type
 * carrying `@field` metadata - neither is guaranteed here:
 * - No type means there is nothing to deserialize into.
 * - A `null`/`undefined`/non-object payload would make `deserializeFromInstance`'s internal
 *   `instance[field.name]` lookup throw.
 * - A payload that is already an instance of the target type has already been deserialized -
 *   deserializing it again is destructive, not merely wasteful: it can throw on a nested temporal
 *   value and double-wraps a concept (`{ value: { value: '...' } }`).
 * - `Object` and the primitive wrapper types (`String`, `Number`, `Boolean`) are not real,
 *   `@field`-decorated models; deserializing into them would discard the payload.
 * - A type with no `@field`-decorated members deserializes to an empty instance, silently discarding
 *   the payload. That is worse than leaving the payload alone, so this warns and passes it through
 *   instead - preserving data beats a hard crash for anyone upgrading with an undecorated type.
 */
export function deserializeIdentityDetails(type: Constructor | undefined, details: unknown): unknown {
    if (!type || type === Object || isPrimitiveDetailsType(type)) {
        return details;
    }

    if (details === null || details === undefined || typeof details !== 'object') {
        return details;
    }

    if (details instanceof type) {
        return details;
    }

    const fields = Fields.getFieldsForType(type);
    if (fields.length === 0) {
        console.warn(
            `Identity details type '${type.name}' has no @field-decorated members. Deserializing into ` +
            'it would discard the payload and return an empty instance, so the raw payload is being ' +
            'passed through instead. Add @field decorators for every property that should be ' +
            'populated, or omit the details type to receive the raw payload as-is.');
        return details;
    }

    return JsonSerializer.deserializeFromInstance(type as Constructor<object>, details);
}
