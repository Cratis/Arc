// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Concurrent;
using System.Globalization;
using System.Reflection;
using System.Text.Json;
using Cratis.Chronicle.Compliance.GDPR;
using Cratis.Concepts;
using Cratis.Strings;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Renders the values a command carries into the properties recorded on its causation, so an event says not only
/// which command produced it but what that command was asked to do.
/// </summary>
/// <remarks>
/// <para>
/// Every readable public instance property is recorded, keyed by its camel-cased name, except the ones that must not
/// be: anything Chronicle already treats as personal data, anything marked <see cref="NotAuditedAttribute"/>, and
/// anything whose name is already spoken for by the causation's own metadata. Exclusion is decided per property and
/// the answer is cached per command type, because the same command runs over and over.
/// </para>
/// <para>
/// The privacy decisions here fail closed. A property whose value cannot be read, or whose exclusion cannot be
/// decided, is left out rather than written: the cost of omitting a value from an audit trail is that someone asks a
/// question the chain cannot answer, and the cost of writing one that should have been withheld is a secret in the
/// event log forever.
/// </para>
/// </remarks>
static class CommandCausationValues
{
    /// <summary>
    /// The greatest number of characters a single recorded value may occupy.
    /// </summary>
    /// <remarks>
    /// The causation travels on every event the command appends, so an unbounded value is written once per event. A
    /// value long enough to be truncated has stopped being an audit note and become a payload.
    /// </remarks>
    internal const int MaximumValueLength = 1024;

    /// <summary>
    /// Appended to a value that was cut short, so a truncated value is never mistaken for the whole one.
    /// </summary>
    internal const string TruncationMarker = "…";

    static readonly PIIMetadataProvider _compliance = new();
    static readonly ConcurrentDictionary<Type, PropertyInfo[]> _recordableProperties = new();
    static readonly JsonSerializerOptions _serializerOptions = new();

    /// <summary>
    /// Adds the values a command carries to the properties recorded on its causation.
    /// </summary>
    /// <param name="properties">The causation properties to add to, already carrying the causation's own metadata.</param>
    /// <param name="commandType">The <see cref="Type"/> of the command.</param>
    /// <param name="command">The command instance to read the values from.</param>
    internal static void AddTo(IDictionary<string, string> properties, Type commandType, object? command)
    {
        if (command is null || !commandType.IsInstanceOfType(command))
        {
            return;
        }

        foreach (var property in RecordablePropertiesOf(commandType))
        {
            var key = property.Name.ToCamelCase();

            // The causation's own metadata names the command and the sequence, and a command free to overwrite
            // those could make an event misreport what produced it.
            if (CommandCausation.ReservedProperties.Contains(key))
            {
                continue;
            }

            if (Render(ValueOf(property, command)) is { } value)
            {
                properties[key] = value;
            }
        }
    }

    static PropertyInfo[] RecordablePropertiesOf(Type commandType) =>
        _recordableProperties.GetOrAdd(commandType, static type =>
            IsExcluded(type)
                ? []
                : [.. type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                    .Where(property =>
                        property.CanRead &&
                        property.GetIndexParameters().Length == 0 &&
                        !IsExcluded(property))]);

    static bool IsExcluded(Type commandType) =>
        Attribute.IsDefined(commandType, typeof(NotAuditedAttribute));

    static bool IsExcluded(PropertyInfo property)
    {
        try
        {
            return Attribute.IsDefined(property, typeof(NotAuditedAttribute)) ||
                   Attribute.IsDefined(property.PropertyType, typeof(NotAuditedAttribute)) ||
                   HasNotAuditedOnConstructorParameter(property) ||

                   // Chronicle already decides what counts as personal data, and it looks at the property, its
                   // declaring type, its property type - so a concept marked [PII] carries the marking wherever it is
                   // used - and the positional record parameter the property came from.
                   _compliance.CanProvide(property);
        }
        catch
        {
            // A model this provider refuses to describe is one whose sensitivity is unknown, and an unknown is not a
            // license to record the value.
            return true;
        }
    }

    /// <summary>
    /// Determines whether the positional record parameter a property came from is marked
    /// <see cref="NotAuditedAttribute"/>.
    /// </summary>
    /// <param name="property">The <see cref="PropertyInfo"/> to check the originating parameter of.</param>
    /// <returns>True when the parameter is marked, false otherwise.</returns>
    /// <remarks>
    /// An attribute written on a positional parameter lands on the parameter, not on the property it generates,
    /// unless it is spelled <c>[property: NotAudited]</c>. Both readings are what someone means.
    /// </remarks>
    static bool HasNotAuditedOnConstructorParameter(PropertyInfo property)
    {
        if (property.DeclaringType is null)
        {
            return false;
        }

        var constructor = property.DeclaringType.GetConstructors().MaxBy(_ => _.GetParameters().Length);
        var parameter = constructor?.GetParameters().FirstOrDefault(_ =>
            string.Equals(_.Name, property.Name, StringComparison.OrdinalIgnoreCase));

        return parameter is not null && Attribute.IsDefined(parameter, typeof(NotAuditedAttribute));
    }

    static object? ValueOf(PropertyInfo property, object command)
    {
        try
        {
            return property.GetValue(command);
        }
        catch
        {
            return null;
        }
    }

    static string? Render(object? value)
    {
        if (value is null)
        {
            return null;
        }

        // A concept is a wrapper around the value someone actually wrote; recording the wrapper's shape instead of
        // that value would make the chain say less than the command did.
        if (value.IsConcept())
        {
            return Render(value.GetConceptValue());
        }

        return value switch
        {
            string text => Truncate(text),
            DateTime date => date.ToString("O", CultureInfo.InvariantCulture),
            DateTimeOffset offset => offset.ToString("O", CultureInfo.InvariantCulture),
            IFormattable formattable => Truncate(formattable.ToString(null, CultureInfo.InvariantCulture)),
            _ => Truncate(Serialize(value))
        };
    }

    static string Serialize(object value)
    {
        try
        {
            return JsonSerializer.Serialize(value, _serializerOptions);
        }
        catch
        {
            return value.ToString() ?? string.Empty;
        }
    }

    static string Truncate(string value) =>
        value.Length <= MaximumValueLength
            ? value
            : string.Concat(value.AsSpan(0, MaximumValueLength), TruncationMarker);
}
