// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState, QueryResult } from '@cratis/arc/queries';
import type { InFlightRequest } from './InFlightRequest.js';
import type { QueryDeadline } from './QueryDeadline.js';

/** A state to settle a hook on: either the next result, or a function deriving it from the current one. */
export type SettleValue<TDataType> =
    | QueryResultWithState<TDataType>
    | ((current: QueryResultWithState<TDataType>) => QueryResultWithState<TDataType>);

/**
 * Determines whether an error is the rejection produced by aborting a request.
 * Duplicated from the query transport rather than imported, since it is internal to `@cratis/arc`.
 * @param error The error to inspect.
 * @returns True if the error represents an aborted request, false otherwise.
 */
export function isAbortError(error: unknown): boolean {
    return (error as { name?: string })?.name === 'AbortError';
}

/**
 * Creates the terminal state for a query that failed before it produced a result, so the hook settles
 * as unsuccessful instead of staying on its initial - and permanently performing - state.
 * @template TDataType Type of model the query is for.
 * @param defaultValue The default value of the query, used as the data of the failed result.
 * @param error The error the query failed with.
 * @returns A {@link QueryResultWithState} describing the failure.
 */
export function failedResult<TDataType>(defaultValue: TDataType, error: unknown): QueryResultWithState<TDataType> {
    // Destructuring a nullish rejection value throws, which would make the `String(error)` fallback
    // written for exactly that case unreachable - so the rejection is coerced to an object first.
    const { message } = (error ?? {}) as { message?: string };
    return QueryResultWithState.fromQueryResult({
        ...QueryResult.noSuccess,
        data: defaultValue,
        isSuccess: false,
        isAuthorized: true,
        isValid: true,
        hasExceptions: true,
        exceptionMessages: [message ?? String(error)],
        // Left empty on purpose - see the matching comment in QueryFor. A stack trace here would be
        // the one that escapes the redaction every server-returned result goes through.
        exceptionStackTrace: ''
    } as QueryResult<TDataType>, false);
}

/**
 * The most recent request started on each query instance. Instances are shared between hooks through the
 * query instance cache, and starting a request aborts whatever that instance had in flight - so the latest
 * request is the one every hook on the instance is waiting for, whichever hook started it.
 */
export const latestRequests = new WeakMap<object, InFlightRequest>();

/**
 * Waits for the request that superseded an aborted one and returns its outcome. A superseding request can
 * itself be superseded, so this follows the chain until a request settles with something other than an abort.
 * Only a request for the same arguments is adopted - one performed with explicitly different arguments answers
 * a different question.
 * @template TDataType Type of model the query is for.
 * @param queryInstance The shared query instance.
 * @param abortedRequest The request that was aborted.
 * @param deadline The deadline shared with the original execution.
 * @returns The superseding result, the error it failed with, or `undefined` when there is nothing to adopt.
 */
async function settleFromSupersedingRequest<TDataType>(queryInstance: object, abortedRequest: InFlightRequest, deadline: QueryDeadline): Promise<{ result?: QueryResult<TDataType>; error?: unknown } | undefined> {
    let current = abortedRequest;
    for (;;) {
        const latest = latestRequests.get(queryInstance);
        if (latest === undefined || latest === current || latest.argumentsKey !== abortedRequest.argumentsKey) {
            return undefined;
        }
        current = latest;
        try {
            return { result: await deadline.waitFor(latest.request) as QueryResult<TDataType> };
        } catch (error) {
            if (!isAbortError(error)) {
                return { error };
            }
        }
    }
}

/**
 * Resolves what a hook settles on after its request was aborted, i.e. superseded.
 *
 * The superseding request is not necessarily this hook's own: the query instance is shared through the
 * cache, so a co-subscriber mounting with the same query and arguments aborts whatever that shared
 * instance had in flight, and only that co-subscriber's state receives the newer result.
 * The superseded request therefore adopts the outcome of the request that superseded it -
 * otherwise a co-subscriber that never issued the newer request settles on its initial,
 * never-ready state. When this hook superseded itself, that outcome is its own newer one.
 * @template TDataType Type of model the query is for.
 * @param queryInstance The shared query instance.
 * @param abortedRequest The request that was aborted.
 * @param deadline The deadline shared with the original execution.
 * @param defaultValue The default value of the query, used for a failed result.
 * @returns The state to settle on.
 */
export async function settleAfterAbort<TDataType>(queryInstance: object, abortedRequest: InFlightRequest, deadline: QueryDeadline, defaultValue: TDataType): Promise<SettleValue<TDataType>> {
    const superseding = await settleFromSupersedingRequest<TDataType>(queryInstance, abortedRequest, deadline);
    if (superseding === undefined) {
        return current => QueryResultWithState.fromQueryResult(current, false);
    }

    if (superseding.result !== undefined) {
        return QueryResultWithState.fromQueryResult(superseding.result, false);
    }

    return failedResult(defaultValue, superseding.error);
}
