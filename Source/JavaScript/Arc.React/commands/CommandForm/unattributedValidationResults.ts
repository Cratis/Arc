// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ValidationResult } from '@cratis/arc/validation';
import { memberMatchesField } from './memberMatchesField.js';

/** Keeps failures only when none of their members matches a mounted field. */
export function unattributedValidationResults(results: readonly ValidationResult[], fieldNames: readonly string[]): ValidationResult[] {
    return results.filter(result =>
        !fieldNames.some(fieldName => memberMatchesField(result.members, fieldName)),
    );
}
