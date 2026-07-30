// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using Cratis.DependencyInjection;
using Cratis.Reflection;

namespace Cratis.Arc.Commands;

/// <summary>
/// Represents the <see cref="ICanResolveKeyForCommand"/> Arc ships: a command composes its key itself, or marks the
/// property holding it.
/// </summary>
/// <remarks>
/// Both rules are things the application says out loud. Nothing is inferred from the shape of a command — a command
/// carrying two identifiers has no answer that is not a guess, and one that grows a second identifier later would
/// silently stop resolving the read model injected somewhere else entirely.
/// </remarks>
[Singleton]
public class DefaultKeyForCommandResolver : ICanResolveKeyForCommand
{
    /// <inheritdoc/>
    public string? Resolve(object command) =>
        command is ICanProvideKeyForCommand provider
            ? NullIfEmpty(provider.GetKey())
            : FromKeyProperty(command);

    /// <summary>
    /// Reads the key from the property the command marks with <see cref="KeyAttribute"/>.
    /// </summary>
    /// <param name="command">The command to read.</param>
    /// <returns>The key, or null when no property is marked or the marked one holds nothing.</returns>
    static string? FromKeyProperty(object command)
    {
        var property = Array.Find(command.GetType().GetProperties(), _ => _.HasAttribute<KeyAttribute>());

        return property is null ? null : NullIfEmpty(KeyFrom(property.GetValue(command)));
    }

    /// <summary>
    /// Renders a key value as the string every store loads by.
    /// </summary>
    /// <param name="value">The value held by the key property.</param>
    /// <returns>The value as a string, or null when there is none.</returns>
    /// <remarks>
    /// A concept is rendered as the value it wraps rather than as itself — a record's own <c>ToString</c> writes out
    /// <c>CustomerId { Value = … }</c>, which no store holds a row or document under.
    /// </remarks>
    static string? KeyFrom(object? value) => value switch
    {
        null => null,
        _ when value.GetType().IsConcept() => value.GetConceptValue()?.ToString(),
        _ => value.ToString()
    };

    static string? NullIfEmpty(string? key) => string.IsNullOrEmpty(key) ? null : key;
}
