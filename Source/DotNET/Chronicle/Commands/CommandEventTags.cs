// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Reflection;
using Cratis.Arc.Commands;
using Cratis.Chronicle;
using Cratis.Concepts;
using Cratis.Types;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Resolves command event tags when returned events are appended, after authorization and validation.
/// </summary>
public static class CommandEventTags
{
    const string ResolvedTagsKey = "resolvedCommandEventTags";

    /// <summary>
    /// Unions declared command tags, application-wide tags and tags returned from the handler.
    /// </summary>
    /// <param name="commandContext">The executing command context.</param>
    /// <returns>The named tags, distinct by name and value.</returns>
    /// <remarks>
    /// Declared tags and application providers are evaluated once per command, on its first returned-event append.
    /// Providers discovered through <see cref="ITypes"/> are resolved from the command's service provider, never a
    /// captured root provider. A manually constructed context without a service provider or type discovery resolves
    /// only attribute, command-interface and returned tags.
    /// </remarks>
    /// <exception cref="AmbiguousEventTagValue">A tag specifies both or neither a property and a constant.</exception>
    /// <exception cref="UnknownEventTagProperty">A tag property cannot be read.</exception>
    /// <exception cref="EventTagValueMissing">A tag property's value is null.</exception>
    public static IEnumerable<NamedTag> ResolveEventTags(this CommandContext commandContext)
    {
        if (!commandContext.Values.TryGetValue(ResolvedTagsKey, out var resolved))
        {
            var command = commandContext.Command;
            var tags = command.GetType().GetCustomAttributes<EventTagAttribute>()
                .Select(attribute => FromAttribute(command, attribute))
                .Concat(command is ICanProvideEventTags provider ? provider.GetEventTags() : [])
                .Concat(ApplicationTags(commandContext))
                .DistinctBy(_ => (_.Name.Value, _.Value))
                .ToArray();
            commandContext.Values[ResolvedTagsKey] = tags;
            resolved = tags;
        }

        return ((IEnumerable<NamedTag>)resolved).Concat(commandContext.GetEventTags())
            .DistinctBy(_ => (_.Name.Value, _.Value)).ToArray();
    }

    static IEnumerable<NamedTag> ApplicationTags(CommandContext context)
    {
        if (context.ServiceProvider is not { } services)
        {
            return [];
        }

        var types = services.GetService<ITypes>()?.FindMultiple<ICanProvideCommandEventTags>() ?? [];

        return types.Where(type => !type.ContainsGenericParameters)
            .Select(type => (ICanProvideCommandEventTags)ActivatorUtilities.GetServiceOrCreateInstance(services, type))
            .SelectMany(provider => provider.GetEventTags(context.Command));
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
