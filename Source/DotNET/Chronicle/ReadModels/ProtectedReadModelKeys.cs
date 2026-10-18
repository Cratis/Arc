// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Runtime.CompilerServices;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Concepts;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Checks raw protected keys before conversion or acquisition, following Screenplay decision 0091.
/// </summary>
internal static class ProtectedReadModelKeys
{
    /// <summary>
    /// Rejects invalid keys, including invalid parts of composite keys, before consulting a reader.
    /// </summary>
    /// <param name="model">The read model type.</param>
    /// <param name="value">The key value to validate.</param>
    /// <param name="keyText">The original diagnostic text when validating a converted key.</param>
    /// <exception cref="ProtectedReadModelDoesNotExist">The key has a refused shape.</exception>
    internal static void Validate(Type model, object? value, string? keyText = null)
    {
        if (IsRefused(value, new(ReferenceEqualityComparer.Instance)))
        {
            throw new ProtectedReadModelDoesNotExist(model, keyText ?? Display(value));
        }
    }

    /// <summary>
    /// Formats diagnostic key text as the reference evaluator does: scalar text, or empty for a composite.
    /// </summary>
    /// <param name="value">The original key value.</param>
    /// <returns>The unmodified scalar key text, or an empty string for a composite.</returns>
    internal static string Display(object? value)
    {
        while (value?.IsConcept() == true)
        {
            value = value.GetConceptValue();
        }

        return value is null ? string.Empty : GeneratedIdentifierCanonicalizer.Canonicalize(value) ?? string.Empty;
    }

    /// <summary>
    /// Recovers the raw source key for validation and diagnostics when it still identifies the resolved command target.
    /// </summary>
    /// <param name="context">The current command.</param>
    /// <returns>The original key, respecting provider and response overrides.</returns>
    internal static object OriginalSourceKey(CommandContext context)
    {
        var source = context.GetEventSourceIdOrDefault();
        if (!source.IsSpecified) return source;
        if (context.Response is { } response && EventSourceExtensions.IsEventSourceIdValue(response)) return response;
        var value = context.Command is ITuple tuple
            ? Enumerable.Range(0, tuple.Length).Select(index => tuple[index]).FirstOrDefault(EventSourceExtensions.IsEventSourceIdValue)
            : context.Type.GetProperties().FirstOrDefault(property => property.IsEventSourceKeyProperty())?.GetValue(context.Command);
        return value is not null && EventSourceExtensions.ToEventSourceId(value).Value == source.Value ? value : source;
    }

    /// <summary>
    /// Preserves nullable absence while rejecting a missing required protected parameter.
    /// </summary>
    /// <param name="instance">The resolved instance.</param>
    /// <param name="parameter">The dependency parameter.</param>
    /// <param name="key">The original diagnostic key text.</param>
    /// <returns>The instance, including null for a nullable parameter.</returns>
    /// <exception cref="ProtectedReadModelDoesNotExist">A required instance does not exist.</exception>
    internal static object? RequireInstance(object? instance, ParameterInfo parameter, string key)
    {
        if (instance is null && new NullabilityInfoContext().Create(parameter).WriteState != NullabilityState.Nullable)
        {
            throw new ProtectedReadModelDoesNotExist(parameter.ParameterType, key);
        }

        return instance;
    }

    static bool IsRefused(object? value, HashSet<object> visited)
    {
        while (value?.IsConcept() == true)
        {
            value = value.GetConceptValue();
        }
        if (value is null) return true;
        if (value is string text)
        {
            return string.IsNullOrWhiteSpace(text) || text == "*" || text.Contains('#', StringComparison.Ordinal) ||
                char.IsWhiteSpace(text[0]) || char.IsWhiteSpace(text[^1]);
        }
        if (GeneratedIdentifierCanonicalizer.Canonicalize(value) is not null || !visited.Add(value)) return false;
        if (value is ITuple tuple)
        {
            return Enumerable.Range(0, tuple.Length).Any(index => IsRefused(tuple[index], visited));
        }

        return value.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Any(property => property.GetMethod is { IsPublic: true } && property.GetIndexParameters().Length == 0 && IsRefused(property.GetValue(value), visited));
    }
}
