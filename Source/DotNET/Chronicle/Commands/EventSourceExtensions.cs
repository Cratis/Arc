// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Keys;
using Cratis.Reflection;

namespace Cratis.Arc.Chronicle.Commands;

/// <summary>
/// Provides extension methods for working with event sources in command contexts.
/// </summary>
public static class EventSourceExtensions
{
    static readonly Type _genericEventSourceIdDefinition = typeof(EventSourceId<>);

    /// <summary>
    /// Determines whether the command has an event source ID associated with it.
    /// </summary>
    /// <param name="command">The command to check for an event source ID.</param>
    /// <returns>True if the command has an event source ID; otherwise, false.</returns>
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "command.GetType().GetProperties() on runtime command objects; their public properties are preserved. Source-generated dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    public static bool HasEventSourceId(this object command) =>
        command.GetType().GetProperties().Any(p =>
            p.PropertyType.IsAssignableTo(typeof(EventSourceId)) ||
            IsGenericEventSourceIdType(p.PropertyType) ||
            p.HasAttribute<KeyAttribute>()) ||
            ((command is ITuple tuple) && tuple.HasEventSourceId());

    /// <summary>
    /// Determines whether the tuple has an event source ID.
    /// </summary>
    /// <param name="tuple">The tuple to check.</param>
    /// <returns>True if the tuple has an event source ID; otherwise, false.</returns>
    public static bool HasEventSourceId(this ITuple tuple)
    {
        var values = new List<object?>();
        for (var i = 0; i < tuple.Length; i++)
        {
            values.Add(tuple[i]);
        }

        return values.Exists(IsEventSourceIdValue);
    }

    /// <summary>
    /// Gets the event source ID associated with the command.
    /// </summary>
    /// <param name="command">The command to get the event source ID from.</param>
    /// <returns>The event source ID.</returns>
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "command.GetType().GetProperties() on runtime command objects; command types are user-defined and their public properties are preserved. Source-generated dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    public static EventSourceId GetEventSourceId(this object command)
    {
        var eventSourceId = EventSourceId.Unspecified;

        if (command is ITuple tuple)
        {
            var values = new List<object?>();
            for (var i = 0; i < tuple.Length; i++)
            {
                values.Add(tuple[i]);
            }

            var id = values.Find(IsEventSourceIdValue);
            if (id is not null)
            {
                eventSourceId = ToEventSourceId(id);
            }
        }
        else
        {
            var property = command.GetType().GetProperties()
                .FirstOrDefault(p =>
                    p.PropertyType.IsAssignableTo(typeof(EventSourceId)) ||
                    IsGenericEventSourceIdType(p.PropertyType) ||
                    p.HasAttribute<KeyAttribute>());

            if (property is not null)
            {
                var value = property.GetValue(command);
                if (value is not null)
                {
                    eventSourceId = ToEventSourceId(value);
                }
            }
        }

        return eventSourceId;
    }

    /// <summary>
    /// Determines whether the given value is an <see cref="EventSourceId"/> or a generic <see cref="EventSourceId{T}"/> subtype.
    /// </summary>
    /// <param name="value">The value to check.</param>
    /// <returns>True if the value is an event source ID; otherwise, false.</returns>
    internal static bool IsEventSourceIdValue(object? value) =>
        value is EventSourceId || (value is not null && IsGenericEventSourceIdType(value.GetType()));

    /// <summary>
    /// Converts a value to an <see cref="EventSourceId"/>, handling direct instances, generic <see cref="EventSourceId{T}"/> subtypes via their implicit operator, and falling back to <see cref="object.ToString"/>.
    /// </summary>
    /// <param name="value">The value to convert.</param>
    /// <returns>The corresponding <see cref="EventSourceId"/>.</returns>
    [UnconditionalSuppressMessage("AOT", "IL2075", Justification = "value.GetType().GetMethods() on runtime EventSourceId subtypes; these types are user-defined and their public static methods are preserved. Source-generated dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    internal static EventSourceId ToEventSourceId(object value)
    {
        if (value is EventSourceId esId)
        {
            return esId;
        }

        if (IsGenericEventSourceIdType(value.GetType()))
        {
            var op = FindImplicitEventSourceIdOperator(value.GetType());
            if (op is not null)
            {
                return (EventSourceId)op.Invoke(null, [value])!;
            }
        }

        return value.ToString()!;
    }

    [UnconditionalSuppressMessage("AOT", "IL2070", Justification = "GetMethods on EventSourceId<T> subtypes discovered at runtime; these types are framework-defined and their public methods are preserved. Source-generated dispatch is the long-term fix (tracked in GitHub issue #2204).")]
    static MethodInfo? FindImplicitEventSourceIdOperator(Type type)
    {
        while (type is not null)
        {
            if (type.IsGenericType && type.GetGenericTypeDefinition() == _genericEventSourceIdDefinition)
            {
                var op = type.GetMethods(BindingFlags.Public | BindingFlags.Static)
                    .FirstOrDefault(m => m.Name == "op_Implicit" && m.ReturnType == typeof(EventSourceId));
                if (op is not null)
                {
                    return op;
                }
            }

            type = type.BaseType!;
        }

        return null;
    }

    static bool IsGenericEventSourceIdType(Type? type)
    {
        if (type is null) return false;
        if (type.IsGenericType && type.GetGenericTypeDefinition() == _genericEventSourceIdDefinition) return true;
        return IsGenericEventSourceIdType(type.BaseType);
    }
}
