// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Constructor, JsonSerializer } from '@cratis/fundamentals';

/*
 * ⚠️ deserializeQueryModel / deserializeQueryModels are for use WITHIN this package only.
 *
 * JsonSerializer's converter registry is module state, and a consumer package can easily resolve a
 * different physical copy of @cratis/fundamentals than this one. Calling these from another package
 * therefore looks up Guid/Date/concept converters in the wrong registry and silently degrades those
 * values to plain deserialization. Other packages should use isPrimitiveModelType - which compares
 * against the JavaScript globals and is safe across copies - and deserialize through their own
 * JsonSerializer.
 */

/**
 * Determines whether a model type is a JavaScript primitive wrapper rather than a real model.
 *
 * A query whose backend returns a primitive collection - `IEnumerable<string>`, `IEnumerable<int>` -
 * generates a proxy with `String`, `Number` or `Boolean` as its model type.
 * @param {Constructor} modelType The model type the query describes itself with.
 * @returns {boolean} True when the type is a primitive wrapper.
 */
export function isPrimitiveModelType(modelType: Constructor | null | undefined): boolean {
    return modelType === String || modelType === Number || modelType === Boolean;
}

/**
 * Deserializes a single query payload into its model type, passing primitives through untouched.
 *
 * {@link JsonSerializer.deserializeFromInstance} is destructive for primitive wrapper types: it
 * constructs `new String()` and copies declared fields onto it, which for a primitive means the
 * value is discarded and an empty wrapper object is produced. Primitives therefore need to bypass
 * deserialization entirely - they already arrive in their final shape.
 * @param {Constructor} modelType The model type to deserialize into.
 * @param {unknown} data The payload to deserialize.
 * @returns {TDataType} The deserialized instance, or the payload unchanged for primitive types.
 */
export function deserializeQueryModel<TDataType>(modelType: Constructor | null | undefined, data: unknown): TDataType {
    if (!modelType || modelType === Object || isPrimitiveModelType(modelType)) {
        return data as TDataType;
    }

    return JsonSerializer.deserializeFromInstance(modelType, data) as TDataType;
}

/**
 * Deserializes a query payload collection into its model type, passing primitives through untouched.
 *
 * See {@link deserializeQueryModel} for why primitive model types must bypass deserialization.
 * @param {Constructor} modelType The instance type of the items to deserialize into.
 * @param {unknown} data The payload to deserialize. A non-array payload yields an empty collection.
 * @returns {TDataType[]} The deserialized items, or the items unchanged for primitive types.
 */
export function deserializeQueryModels<TDataType>(modelType: Constructor | null | undefined, data: unknown): TDataType[] {
    if (!Array.isArray(data)) {
        return [];
    }

    if (!modelType || modelType === Object || isPrimitiveModelType(modelType)) {
        return Array.from(data) as TDataType[];
    }

    return JsonSerializer.deserializeArrayFromInstance(modelType, data) as TDataType[];
}
