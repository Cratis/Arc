// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Holds the well-known values for what composed a validation result - the machine-readable counterpart to its
 * message.
 * @remarks
 * A rejection's message is a developer diagnostic, not copy to show a user. Branch on this instead of matching the
 * message, and render your own wording for anything the framework composed.
 *
 * This is deliberately not an enum. Rejections are composed in Arc, in Chronicle and in application code, so the set
 * is open: treat an unrecognized value the way you would treat {@link ValidationResultReason.Rule} rather than as an
 * error.
 */
export const ValidationResultReason = {

    /**
     * A rule authored by the application rejected the input. The message is the author's, and is meant to be shown.
     */
    Rule: 'rule',

    /**
     * The append was rejected because the event source moved on since it was read. Retryable - re-read and resubmit.
     */
    ConcurrencyViolation: 'concurrencyViolation',

    /**
     * A constraint on the event store rejected the append.
     */
    ConstraintViolation: 'constraintViolation',

    /**
     * A validator threw, and the framework substituted this result for the one the validator would have produced.
     * Nothing the author wrote survives, so the message is not theirs to show.
     */
    ValidatorFailed: 'validatorFailed',

    /**
     * Something the command needed in order to be evaluated was not available, most often the read model a
     * validator depends on. Raised before any rule ran, so nothing about the command's own rules was decided.
     */
    DependencyUnavailable: 'dependencyUnavailable',

    /**
     * The request itself could not be read - a malformed body, or a value that could not be bound. No rule was
     * reached.
     */
    MalformedRequest: 'malformedRequest'
} as const;

/**
 * The type of a validation result reason. Widened to string because the set is open.
 */
export type ValidationResultReason = string;
