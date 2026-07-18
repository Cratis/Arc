// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace Cratis.Arc.DependencyInjection;

/// <summary>
/// Defines a classifier that can recognize a non-nullable dependency which could not be resolved as invalid client
/// input rather than a server-side misconfiguration.
/// </summary>
/// <remarks>
/// When a non-nullable dependency resolves to null it is, by default, a misconfiguration and surfaces as a server error
/// (HTTP 500). Some absences are instead invalid client input — for example a Chronicle read model that does not exist
/// for the command's (valid) event source id. An implementation recognizes such a case and produces an exception
/// implementing <c>IValidationFailure</c>, so the pipeline surfaces it as a validation failure (HTTP 400). Implementations
/// are discovered through <see cref="Cratis.Types.IInstancesOf{T}"/>; a dependency that no classifier recognizes keeps
/// the default server-error behavior, so genuine misconfigurations are never masked as client input.
/// </remarks>
public interface IUnresolvableDependencyClassifier
{
    /// <summary>
    /// Tries to classify a non-nullable dependency that resolved to null as invalid client input.
    /// </summary>
    /// <param name="parameter">The <see cref="ParameterInfo"/> for the dependency that could not be resolved.</param>
    /// <param name="serviceProvider">The command-scoped <see cref="IServiceProvider"/> the dependency was resolved from.</param>
    /// <param name="failure">The exception describing the invalid client input, when classified; otherwise null.</param>
    /// <returns>True when the unresolved dependency represents invalid client input; otherwise false.</returns>
    bool TryClassifyAsClientInput(ParameterInfo parameter, IServiceProvider serviceProvider, [NotNullWhen(true)] out Exception? failure);
}
