// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Paging, Sorting } from '@cratis/arc/queries';
import type { ArcConfiguration } from '../ArcContext.js';
import { getQueryGeneration } from './ReconnectableQueryCache.js';

type HttpHeadersCallback = NonNullable<ArcConfiguration['httpHeadersCallback']>;
const headerScopes = new WeakMap<HttpHeadersCallback, number>();
let nextHeaderScope = 1;

function getHeaderScope(callback?: HttpHeadersCallback): number {
    if (!callback) return 0;
    // Arc rebuilds its context value on render; use the callback identity without reading any headers.
    let scope = headerScopes.get(callback);
    if (scope === undefined) {
        scope = nextHeaderScope++;
        headerScopes.set(callback, scope);
    }
    return scope;
}

/** Tags the shared Suspense key with the generation that produces its result. */
export function makeSuspenseQueryCacheKey(
    queryName: string,
    connection: Pick<ArcConfiguration, 'microservice' | 'apiBasePath' | 'origin'>,
    parameters: { sorting: Sorting; paging: Paging; args: unknown },
    httpHeadersCallback?: HttpHeadersCallback,
): string {
    const { microservice } = connection;
    const apiBasePath = connection.apiBasePath ?? '';
    const origin = connection.origin ?? '';
    const { sorting, paging, args } = parameters;
    return `${getQueryGeneration()}:${getHeaderScope(httpHeadersCallback)}:${queryName}:${microservice}:${apiBasePath}:${origin}:${sorting.field ?? ''}:${sorting.direction ?? 0}:${paging.page}:${paging.pageSize}:${JSON.stringify(args)}`;
}
