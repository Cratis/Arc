// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Reflection;

namespace Cratis.Arc.Commands.ModelBound;

/// <summary>
/// Extension methods for the <see cref="CommandAttribute"/>.
/// </summary>
public static class CommandAttributeExtensions
{
    /// <summary>
    /// Check if a type is a command.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if the type is a command; otherwise, false.</returns>
    public static bool IsCommand(this Type type) => type.HasAttribute<CommandAttribute>() && type.HasHandleMethod();

    /// <summary>
    /// Checks whether the type is marked for protocol-only command execution.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True if the type is directly marked with <see cref="ProtocolOnlyAttribute"/>.</returns>
    public static bool IsProtocolOnly(this Type type) => type.IsDefined(typeof(ProtocolOnlyAttribute), inherit: false);

    /// <summary>
    /// Check if a type has a Handle method that takes a single argument of type object.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if the type has a Handle method; otherwise, false.</returns>
    public static bool HasHandleMethod(this Type type) =>
        type.GetMethod("Handle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance) is not null;

    /// <summary>
    /// Gets the Handle method from a type.
    /// </summary>
    /// <param name="type">Type to get the Handle method from.</param>
    /// <returns>The Handle method.</returns>
    public static MethodInfo GetHandleMethod(this Type type) =>
        type.GetMethod("Handle", BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)!;
}
