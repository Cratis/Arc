// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyRule } from '../PropertyRule';

/**
 * Validation rule that checks if a string has a minimum length.
 * @template T The type being validated.
 */
export class MinLengthRule<T> extends PropertyRule<T, string> {
    /**
     * Initializes a new instance of the {@link MinLengthRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     * @param minLength The minimum length required.
     */
    constructor(propertyAccessor: (instance: T) => string, private readonly minLength: number) {
        super(propertyAccessor, `'{PropertyName}' must be at least ${minLength} characters.`);
    }

    /** @inheritdoc */
    protected isValid(value: string): boolean {
        if (value === null || value === undefined) {
            return true; // Use NotNull/NotEmpty for null checks
        }
        return value.length >= this.minLength;
    }
}

/**
 * Validation rule that checks if a string has a maximum length.
 * @template T The type being validated.
 */
export class MaxLengthRule<T> extends PropertyRule<T, string> {
    /**
     * Initializes a new instance of the {@link MaxLengthRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     * @param maxLength The maximum length allowed.
     */
    constructor(propertyAccessor: (instance: T) => string, private readonly maxLength: number) {
        super(propertyAccessor, `'{PropertyName}' must be at most ${maxLength} characters.`);
    }

    /** @inheritdoc */
    protected isValid(value: string): boolean {
        if (value === null || value === undefined) {
            return true; // Use NotNull/NotEmpty for null checks
        }
        return value.length <= this.maxLength;
    }
}

/**
 * Validation rule that checks if a string has an exact length.
 * @template T The type being validated.
 */
export class LengthRule<T> extends PropertyRule<T, string> {
    /**
     * Initializes a new instance of the {@link LengthRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     * @param min The minimum length required.
     * @param max The maximum length allowed.
     */
    constructor(propertyAccessor: (instance: T) => string, private readonly min: number, private readonly max: number) {
        super(propertyAccessor, `'{PropertyName}' must be between ${min} and ${max} characters.`);
    }

    /** @inheritdoc */
    protected isValid(value: string): boolean {
        if (value === null || value === undefined) {
            return true; // Use NotNull/NotEmpty for null checks
        }
        return value.length >= this.min && value.length <= this.max;
    }
}
