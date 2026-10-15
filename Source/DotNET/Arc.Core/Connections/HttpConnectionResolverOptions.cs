// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Connections;

/// <summary>
/// Represents the options for <see cref="HttpConnectionResolver"/>.
/// </summary>
public class HttpConnectionResolverOptions
{
    /// <summary>
    /// Gets or sets the endpoint the request is posted to.
    /// </summary>
    public Uri? Endpoint { get; set; }

    /// <summary>
    /// Gets or sets headers sent with every request, for instance an API key.
    /// </summary>
    public IDictionary<string, string> Headers { get; set; } = new Dictionary<string, string>();

    /// <summary>
    /// Gets or sets how long to wait for the endpoint to answer.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);
}
