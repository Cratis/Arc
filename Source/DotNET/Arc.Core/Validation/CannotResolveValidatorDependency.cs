// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Validation;

/// <summary>
/// Exception thrown when a discoverable validator cannot be constructed because a non-nullable constructor dependency cannot be resolved or resolves to null.
/// </summary>
/// <param name="validatorType">The validator type that could not be constructed.</param>
/// <param name="parameter">The dependency parameter that could not be resolved.</param>
public class CannotResolveValidatorDependency(Type validatorType, ParameterInfo parameter)
    : Exception($"Cannot construct validator '{validatorType.FullName}' because its required dependency '{parameter.ParameterType.FullName}' for parameter '{parameter.Name}' could not be resolved or resolved to null. If this is a Chronicle read model that may not exist, declare the parameter as nullable ('{parameter.ParameterType.Name}?') to receive null, or inject IReadModels and check existence explicitly.")
{
}
