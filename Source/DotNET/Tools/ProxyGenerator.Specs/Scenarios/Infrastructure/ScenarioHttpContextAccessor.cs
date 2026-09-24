// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.AspNetCore.Http;

namespace Cratis.Arc.ProxyGenerator.Scenarios.Infrastructure;

/// <summary>A pre-registered application HTTP accessor whose ordinary behavior Arc must preserve.</summary>
public class ScenarioHttpContextAccessor : IHttpContextAccessor
{
    readonly HttpContextAccessor _inner = new();

    /// <inheritdoc/>
    public HttpContext? HttpContext
    {
        get => _inner.HttpContext;
        set => _inner.HttpContext = value;
    }
}
