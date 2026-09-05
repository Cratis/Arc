// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { ValidationResultReason } from './ValidationResultReason';
import { ValidationResultSeverity } from './ValidationResultSeverity';

/* eslint-disable @typescript-eslint/no-explicit-any */

/**
 * Represents a validation error with a message for one or more members.
 */
export class ValidationResult {
    /**
     * Initializes a new instance of the {@link ValidationResult} class.
     * @param severity The severity of the result.
     * @param message The message. A developer diagnostic when {@link reason} is anything but `rule`.
     * @param members The members the result is attributed to.
     * @param state State associated with the result - the rule author's, carried straight through.
     * @param reason What composed the result. Defaults to `rule`, meaning an authored rule rejected the input.
     * @param reasonDetail Which specific thing within {@link reason} produced the result - the name of the violated
     * constraint for `constraintViolation`, for instance. Undefined when the reason carries no finer identity.
     * Branch on this rather than matching {@link message}, which is prose and free to change.
     */
    constructor(
        readonly severity: ValidationResultSeverity,
        readonly message: string,
        readonly members: string[],
        readonly state: any,
        readonly reason: ValidationResultReason = ValidationResultReason.Rule,
        readonly reasonDetail?: string) {
    }
}
