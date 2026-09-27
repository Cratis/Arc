// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Validator } from '../validation/Validator.js';
import { ValidationResult } from '../validation/ValidationResult.js';

/**
 * Represents the query validator
 */
export abstract class QueryValidator<T = object> extends Validator<T> {
    /**
     * Validate the query parameters.
     * @param query The query to validate.
     * @returns An array of validation results, empty if valid.
     */
    validate(query: T): ValidationResult[] {
        return super.validate(query);
    }
}
