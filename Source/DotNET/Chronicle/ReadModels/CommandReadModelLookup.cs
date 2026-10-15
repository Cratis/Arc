// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Selects a key without acquiring a model or invoking a provider.
/// </summary>
internal static class CommandReadModelLookup
{
    /// <summary>
    /// Resolves the explicit, conventional, or legacy command key for a parameter.
    /// </summary>
    /// <param name="context">The current command.</param>
    /// <param name="parameter">The dependency parameter.</param>
    /// <param name="mode">The invocation's read protection mode.</param>
    /// <returns>The selected read model key.</returns>
    /// <exception cref="InvalidLookUpBy">The named command property does not exist.</exception>
    /// <exception cref="AmbiguousReadModelLookup">Several command properties match the model's key type.</exception>
    internal static ReadModelKey Resolve(CommandContext context, ParameterInfo parameter, ReadProtectionMode mode)
    {
        var lookup = Attribute.GetCustomAttributes(parameter, true).OfType<LookUpByAttribute>().FirstOrDefault();
        var properties = context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
            .Where(_ => _.GetMethod is { IsPublic: true } && _.GetIndexParameters().Length == 0)
            .ToArray();
        if (lookup is not null)
        {
            var property = properties.FirstOrDefault(_ => _.Name == lookup.Property)
                ?? throw new InvalidLookUpBy(context.Type, lookup.Property);

            return KeyFor(property.GetValue(context.Command), parameter.ParameterType);
        }

        if (mode != ReadProtectionMode.On)
        {
            // Keep the legacy missing/unspecified-key behavior, including its exception type.
            var commandKey = RequiredKey(context.GetEventSourceId(), parameter.ParameterType);
            if (mode == ReadProtectionMode.Measure)
            {
                ObserveRedirect(context, parameter.ParameterType, commandKey);
            }

            return commandKey;
        }

        var keyType = parameter.ParameterType.GetProperty("Id")?.PropertyType;
        var candidates = properties.Where(_ => _.PropertyType == keyType).ToArray();
        if (candidates.Length > 1)
        {
            throw new AmbiguousReadModelLookup(context.Type, parameter.ParameterType, candidates.Select(_ => _.Name));
        }

        return candidates is [{ } selected]
            ? KeyFor(selected.GetValue(context.Command), parameter.ParameterType)
            : RequiredKey(context.GetEventSourceId(), parameter.ParameterType);
    }

    /// <summary>
    /// Observes a convention redirect from the key the legacy path actually resolved, without selecting another model.
    /// </summary>
    /// <param name="context">The measured invocation.</param>
    /// <param name="model">The model's shape.</param>
    /// <param name="commandKey">The actual legacy key.</param>
    internal static void ObserveRedirect(CommandContext context, Type model, ReadModelKey commandKey)
    {
        try
        {
            var keyType = model.GetProperty("Id")?.PropertyType;
            var candidates = context.Type.GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(_ => _.PropertyType == keyType && _.GetMethod is { IsPublic: true } && _.GetIndexParameters().Length == 0)
                .ToArray();
            if (candidates is [{ } candidate] && candidate.GetValue(context.Command) is { } value &&
                GeneratedIdentifierCanonicalizer.Canonicalize(value) is { } key && key != commandKey.Value)
            {
                ReadProtectionTelemetry.Record(context.Type, "redirect", model);
            }
        }
        catch (Exception failure)
        {
            ReadProtectionTelemetry.Record(context.Type, "measurement_unavailable", model, failure.GetType().Name);
        }
    }

    static ReadModelKey KeyFor(object? value, Type readModelType) =>
        RequiredKey(value is null ? EventSourceId.Unspecified : GeneratedIdentifierCanonicalizer.Canonicalize(value) ?? EventSourceExtensions.ToEventSourceId(value).Value, readModelType);

    static ReadModelKey RequiredKey(EventSourceId key, Type readModelType) =>
        key.IsSpecified ? (ReadModelKey)key : throw new UnableToResolveReadModelFromCommandContext(readModelType);
}
