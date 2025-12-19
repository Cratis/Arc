// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Validator } from '../validation/Validator';
import { ValidationResult } from '../validation/ValidationResult';

export type CommandPropertyValidators = { [key: string]: Validator; };

/**
 * Represents the command validator
 */
export abstract class CommandValidator {
    abstract readonly properties: CommandPropertyValidators;

    /**
     * Validate the command.
     * @param command The command to validate.
     * @returns An array of validation results, empty if valid.
     */
    validate(command: object): ValidationResult[] {
        const results: ValidationResult[] = [];
        for (const propertyName in this.properties) {
            const validator = this.properties[propertyName];
            const propertyResults = validator.validate(command);
            results.push(...propertyResults);
        }
        return results;
    }

    /**
     * Gets whether or not the command is valid.
     * @param command The command to validate.
     * @returns True if valid, false otherwise.
     */
    isValid(command: object): boolean {
        return this.validate(command).length === 0;
    }
}
