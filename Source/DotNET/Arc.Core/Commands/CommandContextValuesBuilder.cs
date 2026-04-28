// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Types;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandContextValuesBuilder"/>.
/// </summary>
/// <param name="providers">The instances of <see cref="ICommandContextValuesProvider"/> to use when building the values.</param>
public class CommandContextValuesBuilder(IInstancesOf<ICommandContextValuesProvider> providers) : ICommandContextValuesBuilder
{
    /// <inheritdoc/>
    public CommandContextValues Build(object command)
    {
        var values = new CommandContextValues();
        foreach (var provider in providers)
        {
            values.Merge(provider.Provide(command));
        }

        return values;
    }
}
