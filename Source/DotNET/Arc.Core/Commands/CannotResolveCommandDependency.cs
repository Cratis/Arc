// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Commands;

/// <summary>
/// Exception thrown when a non-nullable command method dependency cannot be resolved or resolves to null.
/// </summary>
/// <param name="parameter">The parameter that could not be resolved.</param>
public class CannotResolveCommandDependency(ParameterInfo parameter)
    : Exception($"Cannot invoke '{parameter.Member.DeclaringType?.FullName}.{parameter.Member.Name}' because its required dependency '{parameter.ParameterType.FullName}' for parameter '{parameter.Name}' could not be resolved or resolved to null. If this is a Chronicle read model that may not exist, declare the parameter as nullable ('{parameter.ParameterType.Name}?') to receive null, or inject IReadModels and check existence explicitly.");
