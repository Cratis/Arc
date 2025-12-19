// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResult } from './ValidationResult';
import { PropertyValidator } from './PropertyValidator';
import { RuleBuilder } from './RuleBuilder';

/**
 * Represents a validator for a type that builds validation rules programmatically.
 * @template T The type being validated.
 */
export class Validator<T = object> {
    private readonly _propertyValidators = new Map<string, PropertyValidator<T>>();

    /**
     * Define validation rules for a property.
     * @template TProperty The type of the property.
     * @param propertyAccessor Function to access the property from the instance.
     * @returns A rule builder for the property.
     */
    ruleFor<TProperty>(propertyAccessor: (instance: T) => TProperty): RuleBuilder<T, TProperty> {
        const propertyName = this.getPropertyName(propertyAccessor);
        let propertyValidator = this._propertyValidators.get(propertyName);
        
        if (!propertyValidator) {
            propertyValidator = new PropertyValidator<T>(propertyName);
            this._propertyValidators.set(propertyName, propertyValidator);
        }

        return new RuleBuilder<T, TProperty>(propertyValidator, propertyAccessor);
    }

    /**
     * Validate an instance.
     * @param instance The instance to validate.
     * @returns An array of validation results, empty if valid.
     */
    validate(instance: T): ValidationResult[] {
        const results: ValidationResult[] = [];
        for (const propertyValidator of this._propertyValidators.values()) {
            const propertyResults = propertyValidator.validate(instance);
            results.push(...propertyResults);
        }
        return results;
    }

    /**
     * Gets whether or not the instance is valid.
     * @param instance The instance to validate.
     * @returns True if valid, false otherwise.
     */
    get isValid(): boolean {
        return true;
    }

    /**
     * Gets whether or not a specific instance is valid.
     * @param instance The instance to validate.
     * @returns True if valid, false otherwise.
     */
    isValidFor(instance: T): boolean {
        return this.validate(instance).length === 0;
    }

    private getPropertyName<TProperty>(propertyAccessor: (instance: T) => TProperty): string {
        // Create a proxy to capture property access
        const propertyNames: string[] = [];
        const proxy = new Proxy({} as T, {
            get(_target, prop) {
                if (typeof prop === 'string') {
                    propertyNames.push(prop);
                }
                return undefined;
            }
        });

        try {
            propertyAccessor(proxy);
        } catch {
            // Ignore errors - we're just capturing the property name
        }

        return propertyNames[0] || 'unknown';
    }
}
