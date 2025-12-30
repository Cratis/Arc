// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyRule } from '../PropertyRule';

/**
 * Validation rule that checks if a string matches a phone number pattern.
 * @template T The type being validated.
 */
export class PhoneRule<T> extends PropertyRule<T, string> {
    private static readonly phoneRegex = /^[\d\s()+-]+$/;

    /**
     * Initializes a new instance of the {@link PhoneRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     */
    constructor(propertyAccessor: (instance: T) => string) {
        super(propertyAccessor, "'{PropertyName}' is not a valid phone number.");
    }

    /** @inheritdoc */
    protected isValid(value: string): boolean {
        if (value === null || value === undefined || value === '') {
            return true; // Use NotNull/NotEmpty for null checks
        }
        return PhoneRule.phoneRegex.test(value);
    }
}
