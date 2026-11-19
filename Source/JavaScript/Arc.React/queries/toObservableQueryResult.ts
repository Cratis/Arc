// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState, type QueryResult } from '@cratis/arc/queries';
import type { Constructor } from '@cratis/fundamentals';
import { applyChangeSet, deserializeChangeSet } from './applyChangeSet.js';

/**
 * Runtime metadata that observable query implementations expose even though the interface omits it.
 */
export type ObservableQueryMetadata = {
    modelType?: Constructor;
    enumerable: boolean;
    queryName?: string;
};

function isPresent(value: unknown): boolean {
    return value !== null && value !== undefined;
}

function isNonArrayEnumerableData(
    queryMetadata: ObservableQueryMetadata,
    responseData: unknown,
): boolean {
    return queryMetadata.enumerable && !Array.isArray(responseData) && isPresent(responseData);
}

function warnOnNonArrayData(
    key: string,
    queryMetadata: ObservableQueryMetadata,
    responseData: unknown,
): void {
    if (!isNonArrayEnumerableData(queryMetadata, responseData)) {
        return;
    }

    const responseDataConstructor =
        typeof responseData === 'object'
            ? (responseData as { constructor?: { name?: string } }).constructor?.name
            : undefined;
    console.error(
        `[useObservableQuery] NON-ARRAY data received for key="${key}" queryName="${queryMetadata.queryName}" data type=${typeof responseData} constructor=${responseDataConstructor}`,
        responseData,
    );
}

function isDeltaPush<TDataType>(response: QueryResult<TDataType>): boolean {
    return !!response.changeSet && Array.isArray(response.data) && response.data.length === 0;
}

function reconstructFromChangeSet<TDataType>(
    response: QueryResult<TDataType>,
    previousData: unknown[],
    modelType: Constructor | null,
): QueryResultWithState<TDataType> {
    const deserializedChangeSet = deserializeChangeSet(
        response.changeSet!,
        modelType ?? Object,
    );
    const reconstructed = applyChangeSet(previousData, deserializedChangeSet) as TDataType;
    return QueryResultWithState.fromQueryResult(
        {
            ...response,
            data: reconstructed,
            changeSet: deserializedChangeSet,
        } as QueryResult<TDataType>,
        false,
    );
}

/**
 * Converts a response pushed by an observable query into a {@link QueryResultWithState}.
 *
 * In delta mode the server omits `data` on subsequent pushes (serialized as null → []), so the full
 * collection is reconstructed by applying the ChangeSet to the previous result.
 * ObservableQueryFor has already deserialized the initial and full response data.
 * @param {string} key Cache key of the query, used for diagnostics.
 * @param {ObservableQueryMetadata} queryMetadata Runtime metadata of the query instance.
 * @param {QueryResult} response The pushed response.
 * @param {QueryResultWithState | undefined} previousResult The previously cached result, if any.
 * @returns {QueryResultWithState} The result to publish.
 */
export function toObservableQueryResult<TDataType>(
    key: string,
    queryMetadata: ObservableQueryMetadata,
    response: QueryResult<TDataType>,
    previousResult: QueryResultWithState<TDataType> | undefined,
): QueryResultWithState<TDataType> {
    warnOnNonArrayData(key, queryMetadata, response.data);

    const previousData = previousResult?.data;
    if (isDeltaPush(response) && Array.isArray(previousData)) {
        return reconstructFromChangeSet(
            response,
            previousData as unknown[],
            queryMetadata.modelType ?? null,
        );
    }

    return QueryResultWithState.fromQueryResult(response, false);
}
