// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyRule } from '../PropertyRule';

/**
 * Validation rule that checks if a number is greater than a specified value.
 * @template T The type being validated.
 */
export class GreaterThanRule<T> extends PropertyRule<T, number> {
    /**
     * Initializes a new instance of the {@link GreaterThanRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     * @param threshold The value that the property must be greater than.
     */
    constructor(propertyAccessor: (instance: T) => number, private readonly threshold: number) {
        super(propertyAccessor, `'{PropertyName}' must be greater than ${threshold}.`);
    }

    /** @inheritdoc */
    protected isValid(value: number): boolean {
        if (value === null || value === undefined) {
            return true;
        }
        return value > this.threshold;
    }
}

/**
 * Validation rule that checks if a number is greater than or equal to a specified value.
 * @template T The type being validated.
 */
export class GreaterThanOrEqualRule<T> extends PropertyRule<T, number> {
    /**
     * Initializes a new instance of the {@link GreaterThanOrEqualRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     * @param threshold The value that the property must be greater than or equal to.
     */
    constructor(propertyAccessor: (instance: T) => number, private readonly threshold: number) {
        super(propertyAccessor, `'{PropertyName}' must be greater than or equal to ${threshold}.`);
    }

    /** @inheritdoc */
    protected isValid(value: number): boolean {
        if (value === null || value === undefined) {
            return true;
        }
        return value >= this.threshold;
    }
}

/**
 * Validation rule that checks if a number is less than a specified value.
 * @template T The type being validated.
 */
export class LessThanRule<T> extends PropertyRule<T, number> {
    /**
     * Initializes a new instance of the {@link LessThanRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     * @param threshold The value that the property must be less than.
     */
    constructor(propertyAccessor: (instance: T) => number, private readonly threshold: number) {
        super(propertyAccessor, `'{PropertyName}' must be less than ${threshold}.`);
    }

    /** @inheritdoc */
    protected isValid(value: number): boolean {
        if (value === null || value === undefined) {
            return true;
        }
        return value < this.threshold;
    }
}

/**
 * Validation rule that checks if a number is less than or equal to a specified value.
 * @template T The type being validated.
 */
export class LessThanOrEqualRule<T> extends PropertyRule<T, number> {
    /**
     * Initializes a new instance of the {@link LessThanOrEqualRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     * @param threshold The value that the property must be less than or equal to.
     */
    constructor(propertyAccessor: (instance: T) => number, private readonly threshold: number) {
        super(propertyAccessor, `'{PropertyName}' must be less than or equal to ${threshold}.`);
    }

    /** @inheritdoc */
    protected isValid(value: number): boolean {
        if (value === null || value === undefined) {
            return true;
        }
        return value <= this.threshold;
    }
}
