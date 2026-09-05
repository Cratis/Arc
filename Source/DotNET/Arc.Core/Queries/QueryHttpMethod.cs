// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Queries;

/// <summary>
/// Defines the HTTP method a generated client proxy uses to perform a query over HTTP.
/// </summary>
/// <remarks>
/// The member names match the TypeScript <c>QueryHttpMethod</c> enum so they can be carried through
/// proxy generation.
/// </remarks>
public enum QueryHttpMethod
{
    /// <summary>
    /// Use the GET method, carrying arguments in the URL query string.
    /// </summary>
    Get = 0,

    /// <summary>
    /// Use the QUERY method (RFC 10008), carrying arguments in a JSON request body.
    /// </summary>
    Query = 1,

    /// <summary>
    /// Prefer the QUERY method and fall back to GET when the server or network path does not support it.
    /// </summary>
    Auto = 2
}
