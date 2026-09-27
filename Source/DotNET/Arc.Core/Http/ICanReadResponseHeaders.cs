// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Provides access to response headers for hosts that support reading them.
/// </summary>
internal interface ICanReadResponseHeaders
{
    /// <summary>
    /// Gets the current value of a response header.
    /// </summary>
    /// <param name="name">Header name.</param>
    /// <returns>The header value, with multiple values combined as a comma-separated list, or <see langword="null"/> if the header is not set.</returns>
    string? GetResponseHeader(string name);
}
