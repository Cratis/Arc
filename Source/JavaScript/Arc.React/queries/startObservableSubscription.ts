// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type {
    IObservableQueryFor,
    QueryInstanceCache,
} from '@cratis/arc/queries';
import type { IQueryScope } from './IQueryScope.js';
import {
    toObservableQueryResult,
    type ObservableQueryMetadata,
} from './toObservableQueryResult.js';

/**
 * Subscribes to an observable query and wires the results and teardown into the shared query cache.
 * @param {IObservableQueryFor} queryInstance The query instance to subscribe to.
 * @param {string} key Cache key of the query.
 * @param {QueryInstanceCache} queryCache The shared query cache.
 * @param {IQueryScope} queryScope The current query scope, notified about performing start/completion.
 * @param {object} args Arguments for the query, if any.
 */
export function startObservableSubscription<TDataType>(
    queryInstance: IObservableQueryFor<TDataType>,
    key: string,
    queryCache: QueryInstanceCache,
    queryScope: IQueryScope,
    args: object | undefined,
): void {
    let receivedFirstResult = false;
    const completeFirstResult = () => {
        if (!receivedFirstResult) {
            receivedFirstResult = true;
            queryScope.notifyPerformingCompleted();
        }
    };

    queryScope.notifyPerformingStarted();

    const subscription = queryInstance.subscribe((response) => {
        // SAFETY: Observable query implementations expose this runtime metadata even though the interface omits it.
        const queryMetadata = queryInstance as unknown as ObservableQueryMetadata;
        const withState = toObservableQueryResult<TDataType>(
            key,
            queryMetadata,
            response,
            queryCache.getLastResult<TDataType>(key),
        );

        completeFirstResult();
        queryCache.setLastResult(key, withState);
    }, args as object);

    queryCache.setTeardown(key, () => {
        subscription.unsubscribe();
        completeFirstResult();
    });
}
