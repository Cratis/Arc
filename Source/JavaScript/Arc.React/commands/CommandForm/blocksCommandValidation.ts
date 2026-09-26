// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResultSeverity, type ValidationResult } from '@cratis/arc/validation';

/** Whether a validation result blocks the command under its declared policy. */
export const blocksCommandValidation = (result: ValidationResult, policy?: ValidationResultSeverity): boolean =>
    (policy !== undefined && result.severity === ValidationResultSeverity.Unknown) ||
    result.severity >= (policy ?? ValidationResultSeverity.Error);
