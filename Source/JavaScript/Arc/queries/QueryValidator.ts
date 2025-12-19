// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Validator } from '../validation/Validator';
import { ValidationResult } from '../validation/ValidationResult';

export type QueryParameterValidators = { [key: string]: Validator; };

/**
 * Represents the query validator
 */
export abstract class QueryValidator {
    abstract readonly parameters: QueryParameterValidators;

    /**
     * Validate the query parameters.
     * @param query The query to validate.
     * @returns An array of validation results, empty if valid.
     */
    validate(query: object): ValidationResult[] {
        const results: ValidationResult[] = [];
        for (const parameterName in this.parameters) {
            const validator = this.parameters[parameterName];
            const parameterResults = validator.validate(query);
            results.push(...parameterResults);
        }
        return results;
    }

    /**
     * Gets whether or not the query is valid.
     * @param query The query to validate.
     * @returns True if valid, false otherwise.
     */
    isValid(query: object): boolean {
        return this.validate(query).length === 0;
    }
}
