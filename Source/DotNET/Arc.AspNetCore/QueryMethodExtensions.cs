// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Reflection;
using Microsoft.AspNetCore.Mvc;

namespace Cratis.Arc.Queries;

/// <summary>
/// Extension methods for methods representing commands.
/// </summary>
public static class QueryMethodExtensions
{
    /// <summary>
    /// Check if a method is a command.
    /// </summary>
    /// <param name="methodInfo">The <see cref="MethodInfo"/> to check.</param>
    /// <returns>True if it is a command, false if not.</returns>
    public static bool IsQuery(this MethodInfo methodInfo) =>
        methodInfo.HasAttribute<HttpGetAttribute>() &&
        methodInfo.ReturnType != typeof(void) &&
        methodInfo.ReturnType != typeof(Task) &&
        methodInfo.ReturnType != typeof(ValueTask) &&
        !methodInfo.HasAttribute<AspNetResultAttribute>() &&
        (!methodInfo.DeclaringType?.HasAttribute<AspNetResultAttribute>() ?? false);
}