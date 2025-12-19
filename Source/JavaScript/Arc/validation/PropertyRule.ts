// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IValidationRule } from './IValidationRule';
import { ValidationResult } from './ValidationResult';
import { ValidationResultSeverity } from './ValidationResultSeverity';

/**
 * Base class for property-based validation rules.
 * @template T The type being validated.
 * @template TProperty The type of the property being validated.
 */
export abstract class PropertyRule<T, TProperty> implements IValidationRule<T> {
    protected errorMessage: string;

    /**
     * Initializes a new instance of the {@link PropertyRule} class.
     * @param propertyAccessor Function to access the property value from the instance.
     * @param defaultErrorMessage The default error message if validation fails.
     */
    constructor(
        protected readonly propertyAccessor: (instance: T) => TProperty,
        defaultErrorMessage: string
    ) {
        this.errorMessage = defaultErrorMessage;
    }

    /**
     * Set a custom error message for this rule.
     * @param message The custom error message.
     * @returns This rule instance for chaining.
     */
    withMessage(message: string): this {
        this.errorMessage = message;
        return this;
    }

    /** @inheritdoc */
    validate(instance: T, propertyName: string): ValidationResult[] {
        const value = this.propertyAccessor(instance);
        if (!this.isValid(value, instance)) {
            return [new ValidationResult(
                ValidationResultSeverity.Error,
                this.errorMessage.replace('{PropertyName}', propertyName),
                [propertyName],
                null
            )];
        }
        return [];
    }

    /**
     * Determine if the property value is valid.
     * @param value The property value.
     * @param instance The instance being validated.
     * @returns True if valid, false otherwise.
     */
    protected abstract isValid(value: TProperty, instance: T): boolean;
}
