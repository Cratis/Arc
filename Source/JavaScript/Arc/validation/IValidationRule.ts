// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResult } from './ValidationResult';

/**
 * Defines a validation rule that can be applied to a property.
 * @template T The type being validated.
 */
export interface IValidationRule<T> {
    /**
     * Validate the given value.
     * @param instance The instance being validated.
     * @param propertyName The name of the property being validated.
     * @returns An array of validation results, empty if valid.
     */
    validate(instance: T, propertyName: string): ValidationResult[];
}
