// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.Http;

/// <summary>
/// Default implementation of <see cref="IHttpRequestContextAccessor"/>.
/// </summary>
[Singleton]
public class HttpRequestContextAccessor : IHttpRequestContextAccessor
{
    static readonly AsyncLocal<IHttpRequestContext?> _current = new();

    /// <inheritdoc/>
    public IHttpRequestContext? Current
    {
        get => _current.Value;
        set => _current.Value = value;
    }
}
