// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryResultWithState } from '@cratis/arc/queries';
import { useState } from 'react';
import type { SettleValue } from './queryRequestSettlement.js';

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
    const [state, setState] = useState(() => ({ key: cacheKey, result: cachedResult ?? QueryResultWithState.initial(defaultValue) }));
    const result = state.key === cacheKey ? state.result : initialResult();
    if (state.key !== cacheKey) {
        setState({ key: cacheKey, result });
    }

    function initialResult() {
        return cachedResult ? adaptCached(cachedResult) : QueryResultWithState.initial(defaultValue);
    }

    function setResult(next: SettleValue<TDataType>) {
        setState(current => {
            const previous = current.key === cacheKey ? current.result : initialResult();
            return { key: cacheKey, result: typeof next === 'function' ? next(previous) : next };
        });
    }

    return [result, setResult] as const;
}
