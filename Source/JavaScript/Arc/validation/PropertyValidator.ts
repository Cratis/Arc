// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { IValidationRule } from './IValidationRule';
import { ValidationResult } from './ValidationResult';

/**
 * Represents a validator for a specific property.
 * @template T The type being validated.
 */
export class PropertyValidator<T> {
    private readonly _rules: IValidationRule<T>[] = [];

    /**
     * Initializes a new instance of the {@link PropertyValidator} class.
     * @param propertyName The name of the property being validated.
     */
    constructor(readonly propertyName: string) {
    }

    /**
     * Add a validation rule to this property validator.
     * @param rule The rule to add.
     */
    addRule(rule: IValidationRule<T>): void {
        this._rules.push(rule);
    }

    /**
     * Validate the property value.
     * @param instance The instance being validated.
     * @returns An array of validation results, empty if valid.
     */
    validate(instance: T): ValidationResult[] {
        const results: ValidationResult[] = [];
        for (const rule of this._rules) {
            const ruleResults = rule.validate(instance, this.propertyName);
            results.push(...ruleResults);
        }
        return results;
    }

    /**
     * Gets whether or not the property is valid.
     * @param instance The instance being validated.
     * @returns True if valid, false otherwise.
     */
    isValid(instance: T): boolean {
        return this.validate(instance).length === 0;
    }
}
