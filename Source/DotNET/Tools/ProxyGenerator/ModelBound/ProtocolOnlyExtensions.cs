// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.ProxyGenerator.ModelBound;

/// <summary>
/// Extension methods for detecting protocol-only commands.
/// </summary>
public static class ProtocolOnlyExtensions
{
    /// <summary>
    /// Checks whether the type is directly marked for protocol-only command execution.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type has the protocol-only marker.</returns>
    public static bool IsProtocolOnly(this Type type) => type.GetCustomAttributesData()
        .Any(attribute => attribute.AttributeType.Name == WellKnownTypes.ProtocolOnlyAttribute);
}
