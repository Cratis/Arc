// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { PropertyValidator } from './PropertyValidator.js';
import { IValidationRule } from './IValidationRule.js';
import { PropertyRule } from './PropertyRule.js';
import { ValidationResultSeverity } from './ValidationResultSeverity.js';

/**
 * Represents a builder for creating validation rules for a property.
 * @template T The type being validated.
 * @template TProperty The type of the property being validated.
 */
export class RuleBuilder<T, TProperty> {
    private lastRule?: IValidationRule<T>;

    /**
     * Initializes a new instance of the {@link RuleBuilder} class.
     * @param propertyValidator The property validator to add rules to.
     * @param _propertyAccessor Function to access the property value.
     */
    constructor(
        private readonly propertyValidator: PropertyValidator<T>,
        private readonly _propertyAccessor: (instance: T) => TProperty
    ) {
    }

    /**
     * Add a custom validation rule.
     * @param rule The validation rule to add.
     * @returns The rule builder for chaining.
     */
    addRule(rule: IValidationRule<T>): RuleBuilder<T, TProperty> {
        this.propertyValidator.addRule(rule);
        this.lastRule = rule;
        return this;
    }

    /**
     * Set a custom error message for the last added rule.
     * @param message The custom error message.
     * @returns The rule builder for chaining.
     */
    withMessage(message: string): RuleBuilder<T, TProperty> {
        if (this.lastRule && this.lastRule instanceof PropertyRule) {
            this.lastRule.withMessage(message);
        }
        return this;
    }

    /**
     * Set the severity of the last property rule.
     * @param severity The rule's severity, or null when it is determined only on the server.
     * @returns The rule builder for chaining.
     */
    withSeverity(severity: ValidationResultSeverity | null): RuleBuilder<T, TProperty> {
        if (this.lastRule && this.lastRule instanceof PropertyRule) {
            this.lastRule.withSeverity(severity);
        }
        return this;
    }
}
