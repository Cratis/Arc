// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Paging } from './Paging';
import { Sorting } from './Sorting';
import { SortDirection } from './SortDirection';
import { QueryHttpMethod } from './QueryHttpMethod';
import { joinPaths } from '../joinPaths';
import { UrlHelpers } from '../UrlHelpers';

/**
 * Options for building an HTTP request for a query.
 */
export interface BuildQueryHttpRequestOptions {
    /** The route template for the query, possibly containing route parameters. */
    route: string;
    /** The base path for the API. */
    apiBasePath: string;
    /** The origin for the API. */
    origin: string;
    /** The arguments used for route-parameter substitution and as query arguments. */
    args: object;
    /** Descriptor-collected parameter values that also form query arguments. */
    parameterValues: object;
    /** The paging for the query. */
    paging: Paging;
    /** The sorting for the query. */
    sorting: Sorting;
    /** The HTTP headers to include. */
    headers: HeadersInit;
    /** Optional abort signal for the request. */
    signal?: AbortSignal;
}

interface QueryRequestPayload {
    arguments: object;
    paging?: { page: number; pageSize: number };
    sorting?: { field: string; direction: string };
}

function directionToString(sorting: Sorting): string {
    return sorting.direction === SortDirection.descending ? 'desc' : 'asc';
}

/**
 * Builds the URL and {@link RequestInit} for performing a query with the given HTTP method.
 *
 * For {@link QueryHttpMethod.Get}, arguments, paging and sorting are placed in the URL query string.
 * For {@link QueryHttpMethod.Query}, route parameters remain in the path while the arguments, paging
 * and sorting are carried in a JSON body envelope.
 * @param method The {@link QueryHttpMethod} to use.
 * @param options The {@link BuildQueryHttpRequestOptions} describing the request.
 * @returns The URL and {@link RequestInit} to pass to {@link fetch}.
 */
export function buildQueryHttpRequest(method: QueryHttpMethod, options: BuildQueryHttpRequestOptions): { url: URL; init: RequestInit } {
    const { route, apiBasePath, origin, args, parameterValues, paging, sorting, headers, signal } = options;

    const { route: replacedRoute, unusedParameters } = UrlHelpers.replaceRouteParameters(route, args);
    const argumentValues = { ...unusedParameters, ...parameterValues };
    let actualRoute = joinPaths(apiBasePath, replacedRoute);

    if (method === QueryHttpMethod.Query) {
        const url = UrlHelpers.createUrlFrom(origin, apiBasePath, actualRoute);
        const payload: QueryRequestPayload = { arguments: argumentValues };
        if (paging.hasPaging) {
            payload.paging = { page: paging.page, pageSize: paging.pageSize };
        }
        if (sorting.hasSorting) {
            payload.sorting = { field: sorting.field, direction: directionToString(sorting) };
        }

        const requestHeaders = new Headers(headers);
        if (!requestHeaders.has('Content-Type')) {
            requestHeaders.set('Content-Type', 'application/json');
        }

        const init: RequestInit = {
            method: QueryHttpMethod.Query,
            headers: requestHeaders,
            body: JSON.stringify(payload),
            signal
        };
        return { url, init };
    }

    const additionalParams: Record<string, string | number> = {};
    if (paging.hasPaging) {
        additionalParams.page = paging.page;
        additionalParams.pageSize = paging.pageSize;
    }
    if (sorting.hasSorting) {
        additionalParams.sortBy = sorting.field;
        additionalParams.sortDirection = directionToString(sorting);
    }

    const queryParams = UrlHelpers.buildQueryParams(argumentValues, additionalParams);
    const queryString = queryParams.toString();
    if (queryString) {
        actualRoute += (actualRoute.includes('?') ? '&' : '?') + queryString;
    }

    const url = UrlHelpers.createUrlFrom(origin, apiBasePath, actualRoute);
    const init: RequestInit = {
        method: QueryHttpMethod.Get,
        headers,
        signal
    };
    return { url, init };
}

/**
 * The transport learned for {@link QueryHttpMethod.Auto} — once QUERY is found to be unsupported it is
 * pinned to GET for the rest of the session; while QUERY works it stays undefined so each attempt keeps
 * verifying cheaply. App-wide, since a client typically talks to a single backend.
 */
let autoResolvedMethod: QueryHttpMethod | undefined;

/**
 * Resets the transport learned for {@link QueryHttpMethod.Auto}, so the next Auto query re-probes for
 * QUERY support. Useful after a network change, or between tests.
 */
export function resetQueryHttpMethodResolution(): void {
    autoResolvedMethod = undefined;
}

function isMethodUnsupported(status: number): boolean {
    // 405 Method Not Allowed / 501 Not Implemented — the server received the request but will not
    // handle the verb (e.g. QUERY disabled). A missing intermediary surfaces as a thrown TypeError.
    return status === 405 || status === 501;
}

function isAbortError(error: unknown): boolean {
    return (error as { name?: string })?.name === 'AbortError';
}

/**
 * Performs the query HTTP request for the given method, resolving {@link QueryHttpMethod.Auto} by
 * preferring QUERY and falling back to GET when the server or network path does not support it.
 *
 * Explicit {@link QueryHttpMethod.Get} and {@link QueryHttpMethod.Query} are honored exactly, with no
 * fallback. For Auto, a transport-level failure — a `405`/`501` response, or a network/CORS error from
 * {@link fetch} — falls back to GET and pins the session to GET. Application-level errors (any other
 * status, returned as a normal {@link Response}) are never treated as a fallback signal.
 * @param method The configured {@link QueryHttpMethod}.
 * @param options The {@link BuildQueryHttpRequestOptions} describing the request.
 * @returns The {@link Response} from the request that was ultimately sent.
 */
export async function executeQueryHttpRequest(method: QueryHttpMethod, options: BuildQueryHttpRequestOptions): Promise<Response> {
    const send = (httpMethod: QueryHttpMethod): Promise<Response> => {
        const { url, init } = buildQueryHttpRequest(httpMethod, options);
        return fetch(url, init);
    };

    if (method !== QueryHttpMethod.Auto) {
        return send(method);
    }

    if (autoResolvedMethod === QueryHttpMethod.Get) {
        return send(QueryHttpMethod.Get);
    }

    try {
        const response = await send(QueryHttpMethod.Query);
        if (isMethodUnsupported(response.status)) {
            autoResolvedMethod = QueryHttpMethod.Get;
            return send(QueryHttpMethod.Get);
        }
        autoResolvedMethod = QueryHttpMethod.Query;
        return response;
    } catch (error) {
        if (isAbortError(error)) {
            throw error;
        }
        autoResolvedMethod = QueryHttpMethod.Get;
        return send(QueryHttpMethod.Get);
    }
}
