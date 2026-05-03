// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Cratis.Chronicle;

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

        var commandType = command.GetType();

        // Check properties first, then constructor parameters (record primary constructor shorthand).
        var subjectProperty =
            commandType.GetProperties().FirstOrDefault(p => Attribute.IsDefined(p, typeof(SubjectAttribute))) ??
            ResolvePropertyFromConstructorParameter(commandType);

        if (subjectProperty is not null)
        {
            var value = subjectProperty.GetValue(command);

            if (value is Subject subject)
            {
                return new CommandContextValues
                {
                    { WellKnownCommandContextKeys.Subject, subject }
                };
            }

            if (value is not null)
            {
                return new CommandContextValues
                {
                    { WellKnownCommandContextKeys.Subject, new Subject(value.ToString()!) }
                };
            }
        }

        return [];
    }

    static System.Reflection.PropertyInfo? ResolvePropertyFromConstructorParameter(Type type)
    {
        var constructor = type.GetConstructors().FirstOrDefault();
        if (constructor is null)
        {
            return null;
        }

        var parameter = constructor.GetParameters()
            .FirstOrDefault(p => Attribute.IsDefined(p, typeof(SubjectAttribute)));

        return parameter is not null
            ? type.GetProperty(parameter.Name!, System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance)
            : null;
    }
}
