// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyRule } from '../PropertyRule';

/**
 * Validation rule that checks if a value matches a regular expression.
 * @template T The type being validated.
 */
export class RegexRule<T> extends PropertyRule<T, string> {
    /**
     * Initializes a new instance of the {@link RegexRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     * @param pattern The regular expression pattern to match.
     * @param errorMessage Optional error message.
     */
    constructor(propertyAccessor: (instance: T) => string, private readonly pattern: RegExp, errorMessage?: string) {
        super(propertyAccessor, errorMessage || "'{PropertyName}' is not in the correct format.");
    }

    /** @inheritdoc */
    protected isValid(value: string): boolean {
        if (value === null || value === undefined || value === '') {
            return true; // Use NotNull/NotEmpty for null checks
        }
        return this.pattern.test(value);
    }
}
