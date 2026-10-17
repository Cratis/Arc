// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * One execution of a hook's query - the lifecycle of a single perform, from the moment it tells the query scope
 * it started performing until it settles the hook or is retired.
 */
export type QueryExecution = {
    /** Whether the execution may no longer settle the hook - its hook unmounted or started a newer execution. */
    readonly isRetired: boolean;

    /** Tells the query scope the execution stopped performing. Only the first call has an effect. */
    complete(): void;

    /** Stops the execution from settling the hook and completes it. */
    retire(): void;
};

/**
 * Starts an execution, telling the query scope it is performing. Every execution completes on its scope exactly
 * once, whether it settles, fails or is retired - an execution waiting on a request it does not own must not keep
 * the scope performing after its hook is gone, nor complete it a second time when that request finally settles.
 * @param queryScope The query scope the execution performs in.
 * @returns The started {@link QueryExecution}.
 */
export function startQueryExecution(queryScope: { notifyPerformingStarted(): void; notifyPerformingCompleted(): void }): QueryExecution {
    let isRetired = false;
    let isCompleted = false;
    queryScope.notifyPerformingStarted();
    const execution: QueryExecution = {
        get isRetired() {
            return isRetired;
        },
        complete() {
            if (!isCompleted) {
                isCompleted = true;
                queryScope.notifyPerformingCompleted();
            }
        },
        retire() {
            isRetired = true;
            execution.complete();
        }
    };
    return execution;
}
