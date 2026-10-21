// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Commands;

/// <summary>
/// Provider support required by protected commands, including supplied-provider pipeline calls.
/// </summary>
public interface ICommandProtectedDecisionSupport
{
    /// <summary>
    /// Refuses a provider that cannot supply command-aware protected reads.
    /// </summary>
    /// <param name="services">The command's service provider.</param>
    void EnsureSupported(IServiceProvider services);

    /// <summary>
    /// Refuses a directly injected command decision token or reader not issued by this invocation.
    /// </summary>
    /// <param name="dependencyType">The declared parameter type.</param>
    /// <param name="dependency">The resolved value, including null.</param>
    void ValidateCommandDependency(Type dependencyType, object? dependency);

    /// <summary>
    /// Opens a provider-specific allowance for an annotated method body, after its parameters have been resolved.
    /// </summary>
    /// <param name="method">The method being invoked.</param>
    /// <returns>The allowance to dispose after awaiting the method, or null.</returns>
    IDisposable? BeginMethodBody(MethodInfo method) => null;
}
