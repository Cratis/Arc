// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Http;

/// <summary>
/// Provides access to the current <see cref="IHttpRequestContext"/>.
/// </summary>
public interface IHttpRequestContextAccessor
{
    /// <summary>
    /// Gets or sets the current HTTP request context.
    /// </summary>
    IHttpRequestContext? Current { get; set; }
}
