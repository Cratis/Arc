// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState } from '@cratis/arc/queries';
import { useState } from 'react';

/**
 * Holds the result a hook exposes and resets it during render when the cache key changes, so a result belonging to
 * the previous key is never shown - not even for one frame. The reset shows the cached result for the new key,
 * adapted by the given function, or the initial result when there is none.
 * @param cacheKey The key the hook currently works on.
 * @param cachedResult The cached result for the key, if any.
 * @param defaultValue The default value of the query, used for the initial result.
 * @param adaptCached Turns a cached result into the one to expose.
 * @returns The result and its setter.
 */
export function useResultForKey<TDataType>(
    cacheKey: string,
    cachedResult: QueryResultWithState<TDataType> | undefined,
    defaultValue: TDataType,
    adaptCached: (cached: QueryResultWithState<TDataType>) => QueryResultWithState<TDataType>
) {
    const [result, setResult] = useState<QueryResultWithState<TDataType>>(
        cachedResult ?? QueryResultWithState.initial(defaultValue)
    );
    const [resultKey, setResultKey] = useState(cacheKey);
    if (resultKey !== cacheKey) {
        setResultKey(cacheKey);
        setResult(cachedResult ? adaptCached(cachedResult) : QueryResultWithState.initial(defaultValue));
    }
    return [result, setResult] as const;
}
