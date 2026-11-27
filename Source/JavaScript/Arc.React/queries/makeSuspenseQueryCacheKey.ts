// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Paging, QueryInstanceCache, Sorting } from '@cratis/arc/queries';
import type { ArcConfiguration } from '../ArcContext.js';
import { ReconnectableQueryCache } from './ReconnectableQueryCache.js';

/** Builds the shared Suspense key scoped to the Arc provider and query version. */
export function makeSuspenseQueryCacheKey(
    queryName: string,
    connection: Pick<ArcConfiguration, 'microservice' | 'apiBasePath' | 'origin' | 'queryVersion'>,
    queryCache: QueryInstanceCache,
    parameters: { sorting: Sorting; paging: Paging; args: unknown },
): string {
    const { microservice } = connection;
    const apiBasePath = connection.apiBasePath ?? '';
    const origin = connection.origin ?? '';
    const { sorting, paging, args } = parameters;
    const scope = queryCache instanceof ReconnectableQueryCache ? `${queryCache.suspenseScope}${connection.queryVersion}:` : '';
    return `${scope}${queryName}:${microservice}:${apiBasePath}:${origin}:${sorting.field ?? ''}:${sorting.direction ?? 0}:${paging.page}:${paging.pageSize}:${JSON.stringify(args)}`;
}
