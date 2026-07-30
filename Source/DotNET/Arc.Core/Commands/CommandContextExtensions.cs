// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Commands;

/// <summary>
/// Provides provider-neutral extension methods for the <see cref="CommandContext"/>.
/// </summary>
public static class CommandContextExtensions
{
    /// <summary>
    /// Gets the provider-neutral resolved key for the command.
    /// </summary>
    /// <param name="commandContext">The <see cref="CommandContext"/> to get the resolved key from.</param>
    /// <returns>The resolved key, or null when the command carried no usable key.</returns>
    /// <remarks>
    /// The key is resolved once, while the command context values are being built: by whichever integration owns key
    /// resolution — the Chronicle one resolves an event source id — and otherwise from the command itself through
    /// <see cref="ICommandKeys"/>. Reading it is therefore only ever a lookup, and everything reading it sees the same
    /// answer for a command.
    /// </remarks>
    public static string? GetResolvedKey(this CommandContext commandContext) =>
        commandContext.Values.TryGetValue(CommandContextKeys.ResolvedKey, out var value) && value is string resolvedKey
            ? resolvedKey
            : null;

    /// <summary>
    /// Gets the provider-neutral resolved key for the command, reading it from the command itself when nothing wrote one.
    /// </summary>
    /// <param name="commandContext">The <see cref="CommandContext"/> to get the resolved key from.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve <see cref="ICommandKeys"/> from.</param>
    /// <returns>The resolved key, or null when the command carried no usable key.</returns>
    [Obsolete("The key is resolved while the command context values are being built, so GetResolvedKey() reads it without a service provider. This overload only matters for a CommandContext assembled without ICommandContextValuesBuilder, and will be removed.")]
    public static string? GetResolvedKey(this CommandContext commandContext, IServiceProvider? serviceProvider) =>
        commandContext.GetResolvedKey() ?? serviceProvider?.GetService<ICommandKeys>()?.GetKeyFor(commandContext.Command);
}
