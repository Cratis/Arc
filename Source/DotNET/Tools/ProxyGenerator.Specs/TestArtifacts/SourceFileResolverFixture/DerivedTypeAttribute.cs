// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Serialization;

/// <summary>
/// Marks a generated-proxy fixture type as a derived type without adding product dependencies to the fixture assembly.
/// </summary>
/// <param name="identifier">The derived type identifier.</param>
[AttributeUsage(AttributeTargets.Class)]
sealed class DerivedTypeAttribute(string identifier) : Attribute
{
    /// <summary>
    /// Gets the derived type identifier.
    /// </summary>
    public string Identifier { get; } = identifier;
}
