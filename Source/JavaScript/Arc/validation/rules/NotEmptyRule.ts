// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyRule } from '../PropertyRule';

/**
 * Validation rule that checks if a value is not null, undefined, or empty.
 * @template T The type being validated.
 */
export class NotEmptyRule<T> extends PropertyRule<T, unknown> {
    /**
     * Initializes a new instance of the {@link NotEmptyRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     */
    constructor(propertyAccessor: (instance: T) => unknown) {
        super(propertyAccessor, "'{PropertyName}' must not be empty.");
    }

    /** @inheritdoc */
    protected isValid(value: unknown): boolean {
        if (value === null || value === undefined) {
            return false;
        }
        if (typeof value === 'string' && value.trim() === '') {
            return false;
        }
        if (Array.isArray(value) && value.length === 0) {
            return false;
        }
        return true;
    }
}

/**
 * Validation rule that checks if a value is not null or undefined.
 * @template T The type being validated.
 */
export class NotNullRule<T> extends PropertyRule<T, unknown> {
    /**
     * Initializes a new instance of the {@link NotNullRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     */
    constructor(propertyAccessor: (instance: T) => unknown) {
        super(propertyAccessor, "'{PropertyName}' must not be empty.");
    }

    /** @inheritdoc */
    protected isValid(value: unknown): boolean {
        return value !== null && value !== undefined;
    }
}
