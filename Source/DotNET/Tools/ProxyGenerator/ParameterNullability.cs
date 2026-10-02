// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.ProxyGenerator;

/// <summary>
/// Determines parameter optionality consistently for controller and model-bound proxies.
/// </summary>
static class ParameterNullability
{
    /// <summary>
    /// Check defaults, nullable value types, and nullable-reference metadata without loading attributes.
    /// </summary>
    /// <param name="parameter">Parameter to check.</param>
    /// <returns>True if the parameter is optional, false otherwise.</returns>
    public static bool IsOptional(ParameterInfo parameter)
    {
        if (parameter.HasDefaultValue)
        {
            return true;
        }

        if (parameter.ParameterType.IsValueType)
        {
            return parameter.ParameterType.IsNullable();
        }

        var context = new NullabilityInfoContext();
        return context.Create(parameter).WriteState == NullabilityState.Nullable;
    }
}
