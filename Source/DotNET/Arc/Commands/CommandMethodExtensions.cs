// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Commands;

/// <summary>
/// Extension methods for methods representing commands.
/// </summary>
public static class CommandMethodExtensions
{
    /// <summary>
    /// Check if a method is a command.
    /// </summary>
    /// <param name="methodInfo">The <see cref="MethodInfo"/> to check.</param>
    /// <returns>True if it is a command, false if not.</returns>
    public static bool IsCommand(this MethodInfo methodInfo) =>
        methodInfo.HasAttribute<HttpPostAttribute>() &&
        !methodInfo.HasAttribute<AspNetResultAttribute>() &&
        (!methodInfo.DeclaringType?.HasAttribute<AspNetResultAttribute>() ?? false);
}