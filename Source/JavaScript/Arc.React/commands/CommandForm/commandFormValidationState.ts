// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { ICommandResult } from '@cratis/arc/commands';
import { ValidationResultReason } from '@cratis/arc/validation';

/** The place a validation ran: in the browser, or by asking the server. */
export type CommandFormValidationChannel = 'client' | 'server';

/**
 * How the verdict of a channel relates to the values the form holds now.
 * - `unvalidated`: the channel has produced no verdict, or could not run.
 * - `stale`: there is a verdict, but it describes values the form no longer holds.
 * - `current`: the verdict describes the values the form holds now.
 */
export type CommandFormValidationFreshness = 'unvalidated' | 'stale' | 'current';

/**
 * What a channel concluded.
 * - `unknown`: nothing was concluded.
 * - `valid` / `invalid`: the rules ran and accepted / rejected the values.
 * - `error`: the attempt itself failed, so nothing is known about the rules.
 */
export type CommandFormValidationVerdict = 'unknown' | 'valid' | 'invalid' | 'error';

/** The snapshot of one validation channel. */
export interface CommandFormValidationChannelState {
    /** Whether a validation is in flight or, for the server, scheduled behind the throttle. */
    pending: boolean;
    freshness: CommandFormValidationFreshness;
    verdict: CommandFormValidationVerdict;

    /** The revision of the form values the verdict describes. Absent until a verdict exists. */
    validatedVersion?: number;

    /** Why the channel did not run, when it is known. See {@link CommandFormValidationNotRunReason}. */
    notRunReason?: string;
}

/** The validation metadata of a command form, one snapshot per channel. */
export interface CommandFormValidationState {
    client: CommandFormValidationChannelState;
    server: CommandFormValidationChannelState;
}

/** The well-known values of {@link CommandFormValidationChannelState.notRunReason}. */
export const CommandFormValidationNotRunReason = {
    /** The server refused the validation before any rule ran. */
    Unauthorized: 'unauthorized',

    /** The form does not validate against the server - `autoServerValidate` is off. */
    ServerValidationDisabled: 'serverValidationDisabled',
} as const;

/** What a validation attempt concluded, before it is related to a revision. */
export interface CommandFormValidationOutcome {
    verdict: CommandFormValidationVerdict;
    notRunReason?: string;
}

const infrastructureReasons: string[] = [
    ValidationResultReason.ValidatorFailed,
    ValidationResultReason.DependencyUnavailable,
    ValidationResultReason.MalformedRequest,
    ValidationResultReason.FrameworkFailure,
];

const unvalidatedChannel = (): CommandFormValidationChannelState => ({
    pending: false,
    freshness: 'unvalidated',
    verdict: 'unknown',
});

/**
 * Creates the state of a form that has not validated anything yet.
 * @returns Two unvalidated, unknown, not pending channels.
 */
export const initialValidationState = (): CommandFormValidationState => ({
    client: unvalidatedChannel(),
    server: unvalidatedChannel(),
});

/**
 * Classifies a validation attempt from its result, never from message text.
 * Error outranks invalid when one response carries both infrastructure and rule failures; a reason
 * the client does not know counts as a rule, because the set of reasons is open.
 * @param result The result of the attempt, or undefined when the attempt threw.
 * @returns The verdict and, when no rule could run, why.
 */
export function classifyValidationOutcome(
    result: ICommandResult<unknown> | undefined,
): CommandFormValidationOutcome {
    if (!result) return { verdict: 'error' };
    if (!result.isAuthorized) {
        return { verdict: 'unknown', notRunReason: CommandFormValidationNotRunReason.Unauthorized };
    }

    const hasInfrastructureFailure = result.validationResults.some(
        (validationResult) => infrastructureReasons.includes(validationResult.reason),
    );
    if (result.hasExceptions || result.exceptionMessages.length > 0 || hasInfrastructureFailure) {
        return { verdict: 'error' };
    }

    return { verdict: result.isValid ? 'valid' : 'invalid' };
}

/**
 * Marks every verdict that is current as stale, because the form values it described have changed.
 * @param state The state to invalidate.
 * @returns The state, or the same instance when nothing was current.
 */
export function invalidateFreshness(state: CommandFormValidationState): CommandFormValidationState {
    if (state.client.freshness !== 'current' && state.server.freshness !== 'current') return state;
    const stale = (channel: CommandFormValidationChannelState): CommandFormValidationChannelState =>
        channel.freshness === 'current' ? { ...channel, freshness: 'stale' } : channel;
    return { client: stale(state.client), server: stale(state.server) };
}

const sameChannel = (a: CommandFormValidationChannelState, b: CommandFormValidationChannelState) =>
    a.pending === b.pending &&
    a.freshness === b.freshness &&
    a.verdict === b.verdict &&
    a.validatedVersion === b.validatedVersion &&
    a.notRunReason === b.notRunReason;

/**
 * Compares two states by value.
 * @param a The first state.
 * @param b The second state.
 * @returns True when both channels of both states are equal.
 */
export const sameValidationState = (a: CommandFormValidationState, b: CommandFormValidationState) =>
    sameChannel(a.client, b.client) && sameChannel(a.server, b.server);

/**
 * Explains why a server channel that has produced nothing never will, when the form does not ask the
 * server. Applied on read so it follows the prop instead of being stored.
 * @param state The state to describe.
 * @param serverValidationEnabled Whether the form validates against the server.
 * @returns The state, or the same instance when nothing needs explaining.
 */
export function describeDisabledServer(
    state: CommandFormValidationState,
    serverValidationEnabled: boolean,
): CommandFormValidationState {
    if (serverValidationEnabled || state.server.freshness !== 'unvalidated' || state.server.notRunReason) return state;
    return {
        ...state,
        server: { ...state.server, notRunReason: CommandFormValidationNotRunReason.ServerValidationDisabled },
    };
}
