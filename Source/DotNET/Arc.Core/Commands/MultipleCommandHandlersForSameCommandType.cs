// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Commands;

/// <summary>
/// Exception that gets thrown when multiple command handlers are found for the same command type.
/// </summary>
/// <param name="commandType">The command type there is a duplicate of.</param>
public class MultipleCommandHandlersForSameCommandType(Type commandType) : Exception($"Multiple command handlers found for command type '{commandType.FullName}'")
{
    /// <summary>
    /// Throw if there are multiple handlers handling the same command.
    /// </summary>
    /// <param name="handlers">The collection of <see cref="ICommandHandler"/> to check against.</param>
    /// <exception cref="MultipleCommandHandlersForSameCommandType">The exception that gets thrown if there are multiple.</exception>
    public static void ThrowIfDuplicates(IEnumerable<ICommandHandler> handlers)
    {
        var duplicateHandler = handlers.GroupBy(h => h.CommandType).FirstOrDefault(g => g.Count() > 1)?.FirstOrDefault();
        if (duplicateHandler is not null)
        {
            throw new MultipleCommandHandlersForSameCommandType(duplicateHandler.CommandType);
        }
    }
}

