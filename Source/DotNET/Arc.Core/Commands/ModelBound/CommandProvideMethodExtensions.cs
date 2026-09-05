// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;

namespace Cratis.Arc.Commands.ModelBound;

/// <summary>
/// Extension methods for working with the optional <c>Provide</c> method on a command.
/// </summary>
/// <remarks>
/// The <c>Provide</c> method is an optional instance method on a command that runs before <c>Handle</c>.
/// It fetches or computes the values that <c>Handle</c> needs and returns them so they can be passed in
/// as arguments, keeping <c>Handle</c> a pure function of its arguments.
/// </remarks>
public static class CommandProvideMethodExtensions
{
    /// <summary>
    /// The name of the optional provide method on a command.
    /// </summary>
    public const string ProvideMethodName = "Provide";

    /// <summary>
    /// Check if a type has a <c>Provide</c> method.
    /// </summary>
    /// <param name="type">Type to check.</param>
    /// <returns>True if the type has a <c>Provide</c> method; otherwise, false.</returns>
    public static bool HasProvideMethod(this Type type) =>
        type.GetProvideMethod() is not null;

    /// <summary>
    /// Gets the <c>Provide</c> method from a type, if any.
    /// </summary>
    /// <param name="type">Type to get the <c>Provide</c> method from.</param>
    /// <returns>The <c>Provide</c> method, or <see langword="null"/> if the type does not declare one.</returns>
    public static MethodInfo? GetProvideMethod(this Type type) =>
        type.GetMethod(ProvideMethodName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
}
