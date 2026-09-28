// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Queries.ModelBound;

namespace Cratis.Arc.Commands;

/// <summary>
/// Resolves explicitly declared model-bound command paths.
/// </summary>
internal static class CommandRoute
{
    /// <summary>
    /// Gets a command's custom path, if present.
    /// </summary>
    /// <param name="handler">The command handler.</param>
    /// <returns>The custom path, or null for conventional routing.</returns>
    internal static string? CustomRoute(ICommandHandler handler) =>
        handler.CommandType.GetCustomAttribute<PathAttribute>(inherit: false)?.Path is string { Length: > 0 } route ? route : null;
}
