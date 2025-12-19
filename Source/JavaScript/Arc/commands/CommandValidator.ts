// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Validator } from '../validation/Validator';
import { ValidationResult } from '../validation/ValidationResult';
import { RuleBuilder } from '../validation/RuleBuilder';

/**
 * Represents the command validator
 */
export abstract class CommandValidator<T = object> extends Validator<T> {
    /**
     * Validate the command.
     * @param command The command to validate.
     * @returns An array of validation results, empty if valid.
     */
    validate(command: T): ValidationResult[] {
        return super.validate(command);
    }

    /**
     * Define validation rules for a property.
     * @template TProperty The type of the property.
     * @param propertyAccessor Function to access the property from the instance.
     * @returns A rule builder for the property.
     */
    ruleFor<TProperty>(propertyAccessor: (instance: T) => TProperty): RuleBuilder<T, TProperty> {
        return super.ruleFor(propertyAccessor);
    }
}
