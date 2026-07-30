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
    public static string? GetResolvedKey(this CommandContext commandContext) =>
        commandContext.GetResolvedKey(commandContext.ServiceProvider);

    /// <summary>
    /// Gets the provider-neutral resolved key for the command, reading it from the command itself when nothing wrote one.
    /// </summary>
    /// <param name="commandContext">The <see cref="CommandContext"/> to get the resolved key from.</param>
    /// <param name="serviceProvider">The <see cref="IServiceProvider"/> to resolve <see cref="ICommandKeys"/> from.</param>
    /// <returns>The resolved key, or null when the command carried no usable key.</returns>
    /// <remarks>
    /// An integration that owns key resolution writes the key while the command context is being built — the Chronicle
    /// one always does, writing an empty key when the command carried nothing usable. A written key therefore stands as
    /// it is, empty included: it is that integration's verdict, and an application with Chronicle resolves keys exactly
    /// as it always has.
    /// <para>
    /// Only when no key was written at all — an application whose read models are backed by Entity Framework Core or
    /// MongoDB and which has no Chronicle — is the key read from the command itself through <see cref="ICommandKeys"/>.
    /// </para>
    /// </remarks>
    public static string? GetResolvedKey(this CommandContext commandContext, IServiceProvider? serviceProvider)
    {
        if (commandContext.Values.TryGetValue(CommandContextKeys.ResolvedKey, out var value) && value is string resolvedKey)
        {
            return resolvedKey;
        }

        return serviceProvider?.GetService<ICommandKeys>()?.GetKeyFor(commandContext.Command);
    }
}
