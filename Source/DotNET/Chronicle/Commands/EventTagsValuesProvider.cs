// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Concepts;
using Cratis.Types;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Unions event tags declared on a command with application-wide event tags.
/// </summary>
/// <param name="providers">The application-wide tag providers.</param>
public class EventTagsValuesProvider(IInstancesOf<ICanProvideCommandEventTags> providers) : ICommandContextValuesProvider
{
    /// <inheritdoc/>
    /// <exception cref="AmbiguousEventTagValue">A tag specifies both or neither a property and a constant.</exception>
    /// <exception cref="UnknownEventTagProperty">A tag property cannot be read.</exception>
    /// <exception cref="EventTagValueMissing">A tag property's value is null.</exception>
    public CommandContextValues Provide(object command)
    {
        var tags = command.GetType().GetCustomAttributes<EventTagAttribute>()
            .Select(attribute => FromAttribute(command, attribute))
            .Concat(command is ICanProvideEventTags provider ? provider.GetEventTags() : [])
            .Concat(providers.SelectMany(_ => _.GetEventTags(command)))
            .DistinctBy(_ => (_.Name.Value, _.Value))
            .ToArray();

        return tags.Length > 0 ? new() { { WellKnownCommandContextKeys.EventTags, tags } } : [];
    }

    static NamedTag FromAttribute(object command, EventTagAttribute attribute)
    {
        var commandType = command.GetType();
        if ((attribute.Property is null) == (attribute.Value is null))
        {
            throw new AmbiguousEventTagValue(commandType, attribute.Name);
        }

        if (attribute.Value is not null)
        {
            return new(attribute.Name, attribute.Value);
        }

        var property = commandType.GetProperty(attribute.Property!, BindingFlags.Public | BindingFlags.Instance);
        if (property?.GetMethod?.IsPublic != true || property.GetIndexParameters().Length > 0)
        {
            throw new UnknownEventTagProperty(commandType, attribute.Property!);
        }

        var value = property.GetValue(command) ?? throw new EventTagValueMissing(commandType, property.Name);
        if (value.IsConcept())
        {
            value = value.GetConceptValue();
        }

        var text = value is IFormattable formattable
            ? formattable.ToString(null, CultureInfo.InvariantCulture)
            : value.ToString();

        return new(attribute.Name, text ?? throw new EventTagValueMissing(commandType, property.Name));
    }
}
