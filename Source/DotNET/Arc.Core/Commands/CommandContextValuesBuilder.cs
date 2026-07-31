// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandContextValuesBuilder"/>.
/// </summary>
/// <param name="providers">The instances of <see cref="ICommandContextValuesProvider"/> to use when building the values.</param>
/// <param name="commandKeys">The <see cref="ICommandKeys"/> to read the command's key from when no provider resolved one.</param>
public class CommandContextValuesBuilder(
    IInstancesOf<ICommandContextValuesProvider> providers,
    ICommandKeys commandKeys) : ICommandContextValuesBuilder
{
    /// <inheritdoc/>
    public CommandContextValues Build(object command)
    {
        var values = new CommandContextValues();
        foreach (var provider in providers)
        {
            values.Merge(provider.Provide(command));
        }

        AddResolvedKeyIfNoProviderResolvedOne(command, values);

        return values;
    }

    /// <summary>
    /// Reads the command's key from the command itself when no provider resolved one.
    /// </summary>
    /// <param name="command">The command being executed.</param>
    /// <param name="values">The values built so far.</param>
    /// <remarks>
    /// An integration that owns key resolution resolves the key here, while the values are being built — the Chronicle
    /// one always does, writing an empty key when the command carried nothing usable. What it wrote stands, empty
    /// included: it is that integration's verdict on the command, and overturning it by reading the command again would
    /// resolve a read model the integration said there was no key for.
    /// <para>
    /// Only when nothing wrote a key — an application whose read models are backed by Entity Framework Core or MongoDB
    /// and which has no Chronicle — is it read from the command. Doing it here rather than where the key is read means
    /// it happens once per command rather than once per read model resolved, and that everything reading the key sees
    /// the same answer.
    /// </para>
    /// </remarks>
    void AddResolvedKeyIfNoProviderResolvedOne(object command, CommandContextValues values)
    {
        if (values.ContainsKey(CommandContextKeys.ResolvedKey))
        {
            return;
        }

        if (commandKeys.GetKeyFor(command) is { Length: > 0 } key)
        {
            values[CommandContextKeys.ResolvedKey] = key;
        }
    }
}
