// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

/**
 * Defines the HTTP method used to perform a query over HTTP.
 */
export enum QueryHttpMethod {
    /**
     * Use the GET method, carrying arguments in the URL query string (default).
     */
    Get = 'GET',

    /**
     * Use the QUERY method (RFC 10008), carrying arguments in a JSON request body.
     */
    Query = 'QUERY',

    /**
     * Prefer the QUERY method and automatically fall back to GET when the server or network path
     * does not support it. The outcome is remembered for the rest of the session so subsequent
     * queries go straight to the working transport.
     */
    Auto = 'AUTO',
}
