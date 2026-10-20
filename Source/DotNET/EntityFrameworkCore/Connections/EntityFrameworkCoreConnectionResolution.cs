// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.DependencyInjection;

namespace Cratis.Arc.EntityFrameworkCore.Connections;

/// <summary>
/// Represents the connection resolution Arc applies to the database contexts it registers.
/// </summary>
/// <param name="Interceptor">The <see cref="ConnectionResolutionInterceptor"/>, or <see langword="null"/> when no connection resolution is configured.</param>
[IgnoreConvention]
internal sealed record EntityFrameworkCoreConnectionResolution(ConnectionResolutionInterceptor? Interceptor);
