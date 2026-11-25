// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>Provides metadata-only ordering before any member arguments are acquired.</summary>
internal interface IOrderParameterDependencies
{
    /// <summary>Validates and orders the complete member before any arguments are acquired.</summary>
    /// <param name="services">The invocation services.</param>
    /// <param name="parameters">The member's parameters.</param>
    /// <returns>The dependency-first acquisition order.</returns>
    ParameterInfo[] Order(IServiceProvider services, ParameterInfo[] parameters);
}
