// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Paging, Sorting } from '@cratis/arc/queries';

/**
 * Builds the key a hook shares its query instance and cached result under. The query type and the serialized
 * arguments identify the request; paging and sorting are part of it too, since they live on the instance and are
 * sent with every request it performs - two hooks on different pages must not share a request or subscription.
 * Without paging or sorting the key is exactly the cache's own.
 * @param queryCache The cache the key is for.
 * @param typeName The stable type name of the query.
 * @param args The arguments of the query, if any.
 * @param paging The paging of the query.
 * @param sorting The sorting of the query.
 * @returns The key.
 */
export function buildQueryKey(queryCache: { buildKey(queryTypeName: string, args?: object): string }, typeName: string, args: object | undefined, paging: Paging, sorting: Sorting): string {
    const pagingPart = paging.page > 0 || paging.pageSize > 0 ? `::page=${paging.page},${paging.pageSize}` : '';
    const sortingPart = sorting.field ? `::sort=${sorting.field},${sorting.direction}` : '';
    return `${queryCache.buildKey(typeName, args)}${pagingPart}${sortingPart}`;
}
