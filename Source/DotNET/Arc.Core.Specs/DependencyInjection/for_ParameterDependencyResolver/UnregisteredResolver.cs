// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.DependencyInjection.for_ParameterDependencyResolver;

/// <summary>
/// A discovered resolver that the container deliberately does not register.
/// </summary>
internal sealed class UnregisteredResolver : ICanResolveParameterDependency
{
    /// <inheritdoc/>
    public bool TryResolve(IServiceProvider serviceProvider, ParameterInfo parameter, out object? dependency)
    {
        dependency = null;
        return false;
    }
}
