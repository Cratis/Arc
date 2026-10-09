// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text;
using Cratis.Chronicle.Events;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Resolves stream ids from command properties using the same rules as the command pipeline.
/// </summary>
public static class EventStreamIdTemplate
{
    static readonly ConcurrentDictionary<string, IReadOnlyList<Part>> _templates = new();
    static readonly ConcurrentDictionary<(string Template, Type Type), Func<object, EventStreamId>> _resolvers = new();
    static readonly ConcurrentDictionary<Type, Func<object, EventStreamId?>> _commands = new();

    /// <summary>
    /// Determines whether a value contains property placeholders. Doubled braces are literals.
    /// </summary>
    /// <param name="value">The attribute value.</param>
    /// <returns>Whether the value contains a placeholder.</returns>
    public static bool IsTemplate(string value) => PartsOf(value).Any(part => part.IsProperty);

    /// <summary>
    /// Gets the distinct property names referenced by a template, in declaration order.
    /// </summary>
    /// <param name="template">The template.</param>
    /// <returns>The referenced property names.</returns>
    public static IReadOnlyList<string> PropertiesOf(string template) => Array.AsReadOnly(PartsOf(template).Where(part => part.IsProperty).Select(part => part.Value).Distinct().ToArray());

    /// <summary>
    /// Resolves a template from public instance properties of a command.
    /// </summary>
    /// <param name="template">The template; doubled braces escape literal braces.</param>
    /// <param name="command">The command carrying the property values.</param>
    /// <returns>The resolved stream id.</returns>
    /// <exception cref="UnknownEventStreamIdTemplateProperty">A placeholder is not a readable public instance property.</exception>
    /// <exception cref="EventStreamIdTemplatePartMissing">A value is blank or the resolved id is a sentinel.</exception>
    /// <exception cref="InvalidEventStreamIdTemplate">The template contains malformed braces.</exception>
    public static EventStreamId Resolve(string template, object command) => _resolvers.GetOrAdd((template, command.GetType()), key => Compile(key.Template, key.Type))(command);

    /// <summary>
    /// Resolves the stream id declared by a command's attribute or provider interface.
    /// </summary>
    /// <param name="command">The command.</param>
    /// <returns>The resolved id, or null when none is declared.</returns>
    /// <exception cref="AmbiguousEventStreamId">An attribute value and provider interface are both declared.</exception>
    public static EventStreamId? ResolveFor(object command) => _commands.GetOrAdd(command.GetType(), CreateCommandResolver)(command);

    static Func<object, EventStreamId?> CreateCommandResolver(Type type)
    {
        var attribute = type.GetCustomAttribute<EventStreamIdAttribute>(false);
        var provider = typeof(ICanProvideEventStreamId).IsAssignableFrom(type);
        if (attribute is not null && attribute.Value != EventStreamId.NotSet && provider)
        {
            throw new AmbiguousEventStreamId(type);
        }
        if (provider)
        {
            return command => ((ICanProvideEventStreamId)command).GetEventStreamId();
        }
        if (attribute is null || attribute.Value == EventStreamId.NotSet)
        {
            return _ => null;
        }
        if (!IsTemplate(attribute.Value.Value))
        {
            return _ => attribute.Value;
        }
        var resolve = _resolvers.GetOrAdd((attribute.Value.Value, type), key => Compile(key.Template, key.Type));

        return command => resolve(command);
    }

    static Func<object, EventStreamId> Compile(string template, Type type)
    {
        var parts = PartsOf(template).Select<Part, (Part Part, PropertyInfo? Property)>(part =>
        {
            if (!part.IsProperty)
            {
                return (Part: part, Property: null);
            }
            var property = type.GetProperty(part.Value, BindingFlags.Public | BindingFlags.Instance);
            if (property?.GetMethod?.IsPublic != true || property.GetIndexParameters().Length != 0)
            {
                throw new UnknownEventStreamIdTemplateProperty(type, part.Value);
            }

            return (Part: part, Property: property);
        }).ToArray();

        return command =>
        {
            var result = new StringBuilder();
            foreach (var (part, property) in parts)
            {
                var value = property is null ? part.Value : Format(property.GetValue(command));
                if (property is not null && string.IsNullOrWhiteSpace(value))
                {
                    throw new EventStreamIdTemplatePartMissing(template, part.Value);
                }
                result.Append(value);
            }
            var id = result.ToString();
            if (string.IsNullOrWhiteSpace(id) || id == EventStreamId.Default)
            {
                throw new EventStreamIdTemplatePartMissing(template, "resolved stream id");
            }

            return new(id);
        };
    }

    static string? Format(object? value)
    {
        if (value?.IsConcept() == true)
        {
            return Format(value.GetConceptValue());
        }

        return value switch
        {
            null => null,
            string text => text,
            DateOnly date => date.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture),
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset date => date.ToString("O", CultureInfo.InvariantCulture),
            Enum enumeration => enumeration.ToString(),
            IFormattable formattable => formattable.ToString(null, CultureInfo.InvariantCulture),
            _ => value.ToString()
        };
    }

    static IReadOnlyList<Part> PartsOf(string template) => _templates.GetOrAdd(template, Parse);

    static IReadOnlyList<Part> Parse(string template)
    {
        var parts = new List<Part>();
        var literal = new StringBuilder();
        for (var index = 0; index < template.Length; index++)
        {
            var character = template[index];
            if (character is not ('{' or '}'))
            {
                literal.Append(character);
                continue;
            }
            if (index + 1 < template.Length && template[index + 1] == character)
            {
                literal.Append(character);
                index++;
                continue;
            }
            if (character == '}')
            {
                throw new InvalidEventStreamIdTemplate(template);
            }
            var end = template.IndexOf('}', index + 1);
            if (end < 0 || end == index + 1 || template.AsSpan(index + 1, end - index - 1).Contains('{'))
            {
                throw new InvalidEventStreamIdTemplate(template);
            }
            parts.Add(new(literal.ToString(), false));
            literal.Clear();
            parts.Add(new(template[(index + 1)..end], true));
            index = end;
        }
        parts.Add(new(literal.ToString(), false));

        return parts.AsReadOnly();
    }

    sealed record Part(string Value, bool IsProperty);
}
