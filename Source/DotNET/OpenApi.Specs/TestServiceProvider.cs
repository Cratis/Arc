// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.OpenApi;

/// <summary>
/// A test implementation of <see cref="IServiceProvider"/> that returns null for all service requests.
/// </summary>
public class TestServiceProvider : IServiceProvider
{
    /// <inheritdoc/>
    public object? GetService(Type serviceType) => null;
}
