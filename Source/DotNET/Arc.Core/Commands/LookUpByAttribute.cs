// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Names the command property used to look up a read-model parameter.
/// </summary>
/// <param name="property">The declared property.</param>
[AttributeUsage(AttributeTargets.Parameter, AllowMultiple = false, Inherited = true)]
public sealed class LookUpByAttribute(string property) : Attribute
{
    /// <summary>
    /// Gets the declared property.
    /// </summary>
    public string Property { get; } = property;
}
