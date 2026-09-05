// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { QueryHttpMethod } from './QueryHttpMethod';

/**
 * The information a {@link QueryHttpMethodResolver} uses to decide the HTTP method for a query.
 */
export interface QueryHttpMethodResolverContext {
    /** The URL the query would use as a GET request (arguments in the query string). */
    readonly url: URL;
    /** The route template for the query. */
    readonly route: string;
    /** The arguments for the query. */
    readonly args: object;
}

/**
 * A policy that decides, per query, which {@link QueryHttpMethod} to use.
 * Consulted only when the query has no explicit method set via {@code setHttpMethod}.
 */
export type QueryHttpMethodResolver = (context: QueryHttpMethodResolverContext) => QueryHttpMethod;

/**
 * The default URL length, in characters, above which {@link lengthBasedQueryHttpMethod} prefers QUERY.
 * 2000 is the widely-safe URL length across browsers, servers and proxies.
 */
export const defaultQueryHttpMethodUrlThreshold = 2000;

/**
 * Options for {@link lengthBasedQueryHttpMethod}.
 */
export interface LengthBasedQueryHttpMethodOptions {
    /** URL length (in characters) above which QUERY is preferred. Defaults to {@link defaultQueryHttpMethodUrlThreshold}. */
    threshold?: number;
    /** The method to use when the URL exceeds the threshold. Defaults to {@link QueryHttpMethod.Auto} so it still falls back to GET. */
    whenLong?: QueryHttpMethod;
}

/**
 * Creates a {@link QueryHttpMethodResolver} that keeps short queries on GET and prefers QUERY only when
 * the GET URL would exceed a threshold — targeting the arguments-too-large-for-a-URL case while leaving
 * small, cacheable queries on GET.
 * @param options Optional {@link LengthBasedQueryHttpMethodOptions}.
 * @returns A {@link QueryHttpMethodResolver}.
 */
export function lengthBasedQueryHttpMethod(options?: LengthBasedQueryHttpMethodOptions): QueryHttpMethodResolver {
    const threshold = options?.threshold ?? defaultQueryHttpMethodUrlThreshold;
    const whenLong = options?.whenLong ?? QueryHttpMethod.Auto;
    return context => (context.url.href.length > threshold ? whenLong : QueryHttpMethod.Get);
}
