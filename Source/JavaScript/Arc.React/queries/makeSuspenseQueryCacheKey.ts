// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import type { Paging, Sorting } from '@cratis/arc/queries';
import type { ArcConfiguration } from '../ArcContext.js';
import { getQueryGeneration } from './ReconnectableQueryCache.js';

// cyrb53 mixes both halves before reducing the result to a safe 53-bit integer.
function cyrb53(text: string): number {
    let low = 0xdeadbeef;
    let high = 0x41c6ce57;
    for (let index = 0; index < text.length; index++) {
        const code = text.charCodeAt(index);
        low = Math.imul(low ^ code, 2654435761);
        high = Math.imul(high ^ code, 1597334677);
    }
    low = Math.imul(low ^ (low >>> 16), 2246822507) ^ Math.imul(high ^ (high >>> 13), 3266489909);
    high = Math.imul(high ^ (high >>> 16), 2246822507) ^ Math.imul(low ^ (low >>> 13), 3266489909);
    return 4294967296 * (high & 2097151) + (low >>> 0);
}

function fingerprintHeaders(headers?: Headers): string {
    // Headers iterates lowercased names in sorted order, combining repeated values as fetch does.
    const pairs = Array.from(headers ?? []);
    // Reserve zero for requests with no custom headers.
    return pairs.length === 0 ? '0' : (cyrb53(JSON.stringify(pairs)) + 1).toString(36);
}

/** Tags the shared Suspense key with the generation that produces its result. */
export function makeSuspenseQueryCacheKey(
    queryName: string,
    connection: Pick<ArcConfiguration, 'microservice' | 'apiBasePath' | 'origin'>,
    parameters: { sorting: Sorting; paging: Paging; args: unknown },
    headers?: Headers,
): string {
    const { microservice } = connection;
    const apiBasePath = connection.apiBasePath ?? '';
    const origin = connection.origin ?? '';
    const { sorting, paging, args } = parameters;
    return `${getQueryGeneration()}:${fingerprintHeaders(headers)}:${queryName}:${microservice}:${apiBasePath}:${origin}:${sorting.field ?? ''}:${sorting.direction ?? 0}:${paging.page}:${paging.pageSize}:${JSON.stringify(args)}`;
}
