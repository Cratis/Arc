// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.ControllerBased;

/// <summary>
/// Extension methods for working with commands.
/// </summary>
public static class CommandExtensions
{
    /// <summary>
    /// Convert a <see cref="MethodInfo"/> to a <see cref="CommandDescriptor"/>.
    /// </summary>
    /// <param name="method">Method to convert.</param>
    /// <param name="targetPath">The target path the proxies are generated to.</param>
    /// <param name="segmentsToSkip">Number of segments to skip from the namespace when generating the output path.</param>
    /// <returns>Converted <see cref="CommandDescriptor"/>.</returns>
    public static CommandDescriptor ToCommandDescriptor(this MethodInfo method, string targetPath, int segmentsToSkip)
    {
        var properties = method.GetPropertyDescriptors();
        var arguments = method.GetParameterDescriptors();
        var route = method.GetRoute(arguments);

        return method.ToCommandDescriptor(method.Name, properties, arguments, route, targetPath, segmentsToSkip, appliesResponseValueHandlers: false);
    }
}
