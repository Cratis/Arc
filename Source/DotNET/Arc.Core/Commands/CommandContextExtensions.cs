// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Provides provider-neutral extension methods for the <see cref="CommandContext"/>.
/// </summary>
public static class CommandContextExtensions
{
    /// <summary>
    /// Gets the provider-neutral resolved key from the command context values, if present.
    /// </summary>
    /// <param name="commandContext">The <see cref="CommandContext"/> to get the resolved key from.</param>
    /// <returns>The resolved key, or null when the command carried no usable key.</returns>
    public static string? GetResolvedKey(this CommandContext commandContext) =>
        commandContext.Values.TryGetValue(CommandContextKeys.ResolvedKey, out var value) && value is string resolvedKey
            ? resolvedKey
            : null;
}
