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
}
