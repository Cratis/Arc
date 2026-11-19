// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { IQueryFor, QueryInstanceCache, QueryResult } from '@cratis/arc/queries';
import { QueryResultWithState } from '@cratis/arc/queries';
import type { MutableRefObject } from 'react';
import type { InFlightRequest } from './InFlightRequest.js';
import type { IQueryScope } from './IQueryScope.js';
import { QueryDeadline } from './QueryDeadline.js';
import { type QueryExecution, startQueryExecution } from './QueryExecution.js';
import { failedResult, isAbortError, latestRequests, settleAfterAbort, type SettleValue } from './queryRequestSettlement.js';
import { serializeArgsForDependency } from './serializeArgsForDependency.js';

/** Options for a single query execution. */
export interface QueryExecutionOptions {
    /** Read deadline in milliseconds. Omitted, nonpositive or nonfinite values disable the deadline. */
    timeoutMs?: number;
}

/** Performs the query on an instance, with the arguments to use. */
export type QueryPerformer<TQuery extends IQueryFor<TDataType>, TDataType, TArguments = object> = (performer: TQuery, args?: TArguments) => Promise<QueryResult<TDataType>>;

/** Everything an execution of a query hook needs from the hook that runs it. */
export interface QueryExecutionEnvironment<TQuery extends IQueryFor<TDataType>, TDataType, TArguments> {
    queryInstance: TQuery;
    performer: QueryPerformer<TQuery, TDataType, TArguments>;
    args?: TArguments;
    options?: QueryExecutionOptions;
    argumentsDependency: string;
    cacheKey: string;
    queryCache: QueryInstanceCache;
    queryScope: IQueryScope;
    requestVersionRef: MutableRefObject<number>;
    executionRef: MutableRefObject<QueryExecution | undefined>;
    deadlineRef: MutableRefObject<QueryDeadline | undefined>;
    setResult: (next: SettleValue<TDataType>) => void;
}

/**
 * Settles the hook on the outcome of an in-flight request, caching a successful result under the key it answers.
 */
async function settleOnOutcome<TQuery extends IQueryFor<TDataType>, TDataType, TArguments>(
    env: QueryExecutionEnvironment<TQuery, TDataType, TArguments>,
    inFlight: InFlightRequest,
    deadline: QueryDeadline,
    settle: (next: SettleValue<TDataType>) => void): Promise<void> {
    try {
        const queryResult = await inFlight.request as QueryResult<TDataType>;
        const withState = QueryResultWithState.fromQueryResult(queryResult, false);
        // Only a successful result is cached, and only under the key it answers - a request
        // performed with explicitly different arguments does not belong to this key. The cache
        // exists to hand the last known good payload to future subscribers, so caching a failure
        // would poison every mount for the whole retention window; leaving it out means a remount
        // shows stale-but-good data and re-fetches - stale while revalidate.
        if (withState.isSuccess && inFlight.argumentsKey === env.argumentsDependency) {
            env.queryCache.setLastResult(env.cacheKey, withState);
        }
        settle(withState);
    } catch (error) {
        if (isAbortError(error)) {
            settle(await settleAfterAbort<TDataType>(env.queryInstance, inFlight, deadline, env.queryInstance.defaultValue));
            return;
        }
        settle(failedResult(env.queryInstance.defaultValue, error));
    }
}

/**
 * Runs one execution of a query on behalf of a hook, retiring the execution before it.
 * @param env The environment of the hook running the execution.
 * @param requestArgs Arguments for this execution, overriding those of the hook.
 * @param executionOptions Options for this execution, overriding those of the hook.
 */
export async function runQueryExecution<TQuery extends IQueryFor<TDataType>, TDataType, TArguments>(
    env: QueryExecutionEnvironment<TQuery, TDataType, TArguments>,
    requestArgs?: TArguments,
    executionOptions?: QueryExecutionOptions): Promise<void> {
    const { queryInstance, executionRef, deadlineRef } = env;

    // A newer execution retires the one before it. The earlier one may be waiting on a request this hook
    // does not own - one a co-subscriber started - which can outlive it by any amount; left alone it would
    // keep this hook's query scope performing until that request settles. The request itself is not
    // aborted: it is shared, and the co-subscriber that started it still needs its outcome.
    executionRef.current?.retire();
    deadlineRef.current?.dispose();
    const execution = startQueryExecution(env.queryScope);
    executionRef.current = execution;

    // Only this hook's latest request may settle its state. An earlier one can still finish after a later
    // one has settled - for instance when the later one returned early on client validation without
    // aborting anything, or ran on another instance after the arguments changed - and it must not
    // overwrite that newer state. A retired execution - its hook unmounted - may not settle it at all.
    const version = ++env.requestVersionRef.current;
    const settle = (next: SettleValue<TDataType>) => {
        if (!execution.isRetired && version === env.requestVersionRef.current) {
            env.setResult(next);
        }
    };
    const argumentsKey = serializeArgsForDependency((requestArgs ?? env.args) as object | undefined);
    const request = env.performer(queryInstance, requestArgs);
    // Capture this request's controller: a later request may replace the shared instance's controller.
    const deadline = new QueryDeadline(executionOptions?.timeoutMs ?? env.options?.timeoutMs, (queryInstance as { abortController?: AbortController }).abortController);
    deadlineRef.current = deadline;
    const inFlight: InFlightRequest = { request: deadline.waitFor(request), argumentsKey };
    latestRequests.set(queryInstance, inFlight);
    try {
        await settleOnOutcome(env, inFlight, deadline, settle);
    } finally {
        deadline.dispose();
        if (deadlineRef.current === deadline) {
            deadlineRef.current = undefined;
        }
        execution.complete();
        if (executionRef.current === execution) {
            executionRef.current = undefined;
        }
    }
}
