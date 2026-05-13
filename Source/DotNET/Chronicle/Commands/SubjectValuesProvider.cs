// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Represents an implementation of <see cref="ICommandContextValuesProvider"/> that provides the subject for event appends.
/// </summary>
public class SubjectValuesProvider : ICommandContextValuesProvider
{
    /// <inheritdoc/>
    public CommandContextValues Provide(object command)
    {
        if (command is ICanProvideSubject provider)
        {
            return new CommandContextValues
            {
                { WellKnownCommandContextKeys.Subject, provider.GetSubject() }
            };
        }

        if (command.HasSubject())
        {
            var subject = command.GetSubject();
            if (subject is not null)
            {
                return new CommandContextValues
                {
                    { WellKnownCommandContextKeys.Subject, subject }
                };
            }
        }

        return [];
    }
}
