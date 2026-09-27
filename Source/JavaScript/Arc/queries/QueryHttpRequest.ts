// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

import { Paging } from './Paging.js';
import { Sorting } from './Sorting.js';
import { SortDirection } from './SortDirection.js';
import { QueryHttpMethod } from './QueryHttpMethod.js';
import { joinPaths } from '../joinPaths.js';
import { UrlHelpers } from '../UrlHelpers.js';
import { Globals } from '../Globals.js';
import { isAbortError } from './isAbortError.js';

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
 * The transport learned for {@link QueryHttpMethod.Auto}, keyed by backend (origin + API base path).
 * Once QUERY is found to be unsupported for a backend it is pinned to GET for the rest of the session;
 * while QUERY works the backend stays absent so each attempt keeps verifying cheaply. Keying by backend
 * means one backend rejecting QUERY does not downgrade queries to other backends.
 */
const autoResolvedMethods = new Map<string, QueryHttpMethod>();

// A NUL separator cannot occur in an origin or an API base path, so no pair of backends can
// produce the same composite key. Written as an escape rather than a literal control byte,
// which would make git classify this file as binary and hide every diff of it from review.
function backendKey(options: BuildQueryHttpRequestOptions): string {
    return `${options.origin}\0${options.apiBasePath}`;
}

/**
 * Resets the transport learned for {@link QueryHttpMethod.Auto} for every backend, so the next Auto
 * query re-probes for QUERY support. Useful after a network change, or between tests.
 */
export function resetQueryHttpMethodResolution(): void {
    autoResolvedMethods.clear();
}

function isMethodUnsupported(status: number): boolean {
    // 405 Method Not Allowed / 501 Not Implemented — the server received the request but will not
    // handle the verb (e.g. QUERY disabled). A missing intermediary surfaces as a thrown TypeError.
    return status === 405 || status === 501;
}

/**
 * Performs the query HTTP request for the given method, resolving {@link QueryHttpMethod.Auto} by
 * preferring QUERY and falling back to GET when the server or network path does not support it.
 *
 * Explicit {@link QueryHttpMethod.Get} and {@link QueryHttpMethod.Query} are honored exactly, with no
 * fallback. For Auto, a transport-level failure — a `405`/`501` response, or a network/CORS error from
 * {@link fetch} — falls back to GET and pins the session to GET. Application-level errors (any other
 * status, returned as a normal {@link Response}) are never treated as a fallback signal.
 *
 * The method is resolved in order of precedence: an explicit per-query {@code override}, then
 * {@link Globals.queryHttpMethodResolver} (given the built GET URL), then {@link Globals.queryHttpMethod}.
 * @param override The explicit per-query {@link QueryHttpMethod}, or `undefined` to resolve from globals.
 * @param options The {@link BuildQueryHttpRequestOptions} describing the request.
 * @returns The {@link Response} from the request that was ultimately sent.
 */
export async function executeQueryHttpRequest(override: QueryHttpMethod | undefined, options: BuildQueryHttpRequestOptions): Promise<Response> {
    // The GET request is built up front when a resolver needs the URL, and reused if GET is chosen.
    let getRequest: { url: URL; init: RequestInit } | undefined;
    let method: QueryHttpMethod;
    if (override !== undefined) {
        method = override;
    } else if (Globals.queryHttpMethodResolver) {
        getRequest = buildQueryHttpRequest(QueryHttpMethod.Get, options);
        method = Globals.queryHttpMethodResolver({ url: getRequest.url, route: options.route, args: options.args });
    } else {
        method = Globals.queryHttpMethod;
    }

    const send = (httpMethod: QueryHttpMethod): Promise<Response> => {
        if (httpMethod === QueryHttpMethod.Get && getRequest) {
            return fetch(getRequest.url, getRequest.init);
        }
        const { url, init } = buildQueryHttpRequest(httpMethod, options);
        return fetch(url, init);
    };

    if (method !== QueryHttpMethod.Auto) {
        return send(method);
    }

    const key = backendKey(options);
    if (autoResolvedMethods.get(key) === QueryHttpMethod.Get) {
        return send(QueryHttpMethod.Get);
    }

    try {
        const response = await send(QueryHttpMethod.Query);
        if (isMethodUnsupported(response.status)) {
            autoResolvedMethods.set(key, QueryHttpMethod.Get);
            return send(QueryHttpMethod.Get);
        }
        autoResolvedMethods.set(key, QueryHttpMethod.Query);
        return response;
    } catch (error) {
        if (isAbortError(error)) {
            throw error;
        }
        autoResolvedMethods.set(key, QueryHttpMethod.Get);
        return send(QueryHttpMethod.Get);
    }
}
