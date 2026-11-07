// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>A test-only proxy base for the internal readable-response-header seam.</summary>
public class ResponseHeadersForSpecs : ICanReadResponseHeaders
{
    /// <inheritdoc/>
    public string? GetResponseHeader(string name) => ReadHeader(name);

    /// <summary>Supplies the header value configured by a spec.</summary>
    /// <param name="name">The header name.</param>
    /// <returns>The header value, if any.</returns>
    public virtual string? ReadHeader(string name) => null;
}
