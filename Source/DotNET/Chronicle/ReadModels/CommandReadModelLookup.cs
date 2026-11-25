// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Arc.Commands;
using Cratis.Arc.Queries;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.ReadModels;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

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
    /// <exception cref="ProtectedCommandRequiresCommandAwareReader">A protected key needs checking, but no key traversal is available.</exception>
    internal static ReadModelKey? Resolve(CommandContext context, ParameterInfo parameter, ReadProtectionMode mode)
        => Resolve(context, parameter, mode, null, out _);

    /// <summary>
    /// Resolves a key while preserving its original diagnostic text for protected reads.
    /// </summary>
    /// <param name="context">The current command.</param>
    /// <param name="parameter">The dependency parameter.</param>
    /// <param name="mode">The invocation's read protection mode.</param>
    /// <param name="traversal">The registered key traversal; required only when a protected key is checked.</param>
    /// <param name="keyText">The original key text, or empty for a composite.</param>
    /// <returns>The selected read model key.</returns>
    /// <exception cref="InvalidLookUpBy">The named command property does not exist.</exception>
    /// <exception cref="AmbiguousReadModelLookup">Several command properties match the model's key type.</exception>
    /// <exception cref="ProtectedReadModelDoesNotExist">The protected key has a refused shape.</exception>
    /// <exception cref="ProtectedCommandRequiresCommandAwareReader">A protected key needs checking, but no key traversal is available.</exception>
    /// <exception cref="ProtectedReadModelKeyAccessorMissing">Strict coverage has no accessors for a composite key part.</exception>
    internal static ReadModelKey? Resolve(CommandContext context, ParameterInfo parameter, ReadProtectionMode mode, ProtectedReadModelKeyTraversal? traversal, out string keyText)
    {
        var protect = mode == ReadProtectionMode.On && !IsAdvisory(parameter);
        var keyLookup = new KeyLookup(parameter.ParameterType, traversal, protect);
        var logger = context.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger(typeof(CommandReadModelLookup).FullName!);
        var provider = CommandAttributePolicy.ResolveKeyProvider(parameter, context.Type);
        if (provider is not null)
        {
            var value = ReadModelKeyProviders.Resolve(provider, new(parameter.ParameterType, context.Type, context.ServiceProvider?.GetService<ICurrentPrincipalAccessor>()?.Current, logger));
            keyText = string.Empty;
            return CallerReadModelKeyLookup.Resolve((value, parameter.ParameterType), traversal, protect, key => KeyFor(key, keyLookup with { Protect = false }, out _));
        }

        var lookup = Attribute.GetCustomAttributes(parameter, true).OfType<LookUpByAttribute>().FirstOrDefault();
        var configuration = context.ServiceProvider?.GetService<IConfiguration>();
        var properties = lookup is null && mode != ReadProtectionMode.On
            ? []
            : CommandReadModelKeys.PropertiesFor(context.Type, configuration, logger);
        if (lookup is not null)
        {
            var property = properties.FirstOrDefault(_ => _.Name == lookup.Property)
                ?? throw new InvalidLookUpBy(context.Type, lookup.Property);

            return ParameterKey(property.Read(context.Command), parameter, keyLookup, out keyText);
        }

        return mode == ReadProtectionMode.On
            ? ConventionKey(context, parameter, keyLookup, out keyText)
            : LegacyKey(context, parameter.ParameterType, mode, out keyText);
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
            var configuration = context.ServiceProvider?.GetService<IConfiguration>();
            var logger = context.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger(typeof(CommandReadModelLookup).FullName!);
            var keyType = CommandReadModelKeys.KeyTypeFor(model, configuration, logger);
            var candidates = CommandReadModelKeys.PropertiesFor(context.Type, configuration, logger)
                .Where(_ => _.PropertyType == keyType).ToArray();
            if (IsRedirect(candidates, context.Command, commandKey))
            {
                ReadProtectionTelemetry.Record(context.Type, "redirect", model);
            }
        }
        catch (CommandReadModelKeysMissing)
        {
            throw;
        }
        catch (Exception failure)
        {
            ReadProtectionTelemetry.Record(context.Type, "measurement_unavailable", model, failure.GetType().Name);
        }
    }

    static bool IsAdvisory(ParameterInfo parameter) =>
        Attribute.IsDefined(parameter, typeof(UnprotectedAttribute)) || Attribute.IsDefined(parameter.ParameterType, typeof(UnprotectedAttribute));

    static ReadModelKey LegacyKey(CommandContext context, Type model, ReadProtectionMode mode, out string keyText)
    {
        // Keep the legacy missing/unspecified-key behavior, including its exception type.
        var commandKey = RequiredKey(context.GetEventSourceId(), model);
        if (mode == ReadProtectionMode.Measure)
        {
            ObserveRedirect(context, model, commandKey);
        }

        keyText = commandKey.Value;
        return commandKey;
    }

    static ReadModelKey? ConventionKey(CommandContext context, ParameterInfo parameter, KeyLookup lookup, out string keyText)
    {
        var configuration = context.ServiceProvider?.GetService<IConfiguration>();
        var logger = context.ServiceProvider?.GetService<ILoggerFactory>()?.CreateLogger(typeof(CommandReadModelLookup).FullName!);
        var properties = CommandReadModelKeys.PropertiesFor(context.Type, configuration, logger);
        var keyType = CommandReadModelKeys.KeyTypeFor(parameter.ParameterType, configuration, logger);
        var candidates = properties.Where(_ => _.PropertyType == keyType).ToArray();
        if (candidates.Length > 1)
        {
            throw new AmbiguousReadModelLookup(context.Type, parameter.ParameterType, candidates.Select(_ => _.Name));
        }

        if (candidates is [{ } selected])
        {
            return ParameterKey(selected.Read(context.Command), parameter, lookup, out keyText);
        }

        var source = context.GetEventSourceId();
        keyText = source.Value;
        if (lookup.Protect)
        {
            // The raw key is checked only while it still denotes the resolved target; provider and response overrides win.
            var raw = context.GetRawEventSourceKey();
            keyText = ProtectedReadModelKeys.Display(raw);
            if (ProtectedReadModelKeys.IsAbsent(raw, parameter)) return null;
            ProtectedReadModelKeys.ValidateRawKey(lookup.Traversal, parameter.ParameterType, raw, keyText);
            ProtectedReadModelKeys.ValidateWireText(parameter.ParameterType, source.Value, keyText);
        }

        return RequiredKey(source, parameter.ParameterType);
    }

    static bool IsRedirect(CommandReadModelKeyProperty[] candidates, object command, ReadModelKey commandKey)
    {
        if (candidates is not [{ } candidate] || candidate.Read(command) is not { } value) return false;
        return GeneratedIdentifierCanonicalizer.Canonicalize(value) is { } key && key != commandKey.Value;
    }

    static ReadModelKey? ParameterKey(object? value, ParameterInfo parameter, KeyLookup lookup, out string keyText)
    {
        if (lookup.Protect && ProtectedReadModelKeys.IsAbsent(value, parameter))
        {
            keyText = ProtectedReadModelKeys.Display(value);
            return null;
        }

        return KeyFor(value, lookup, out keyText);
    }

    static ReadModelKey KeyFor(object? value, KeyLookup lookup, out string keyText)
    {
        var display = lookup.Protect ? ProtectedReadModelKeys.Display(value) : string.Empty;
        if (lookup.Protect)
        {
            ProtectedReadModelKeys.ValidateRawKey(lookup.Traversal, lookup.Model, value, display);
        }
        var key = value is null ? EventSourceId.Unspecified.Value : GeneratedIdentifierCanonicalizer.Canonicalize(value) ?? EventSourceExtensions.ToEventSourceId(value).Value;
        keyText = lookup.Protect ? display : key;
        if (lookup.Protect)
        {
            ProtectedReadModelKeys.ValidateWireText(lookup.Model, key, keyText);
        }

        return RequiredKey(key, lookup.Model);
    }

    static ReadModelKey RequiredKey(EventSourceId key, Type readModelType) =>
        key.IsSpecified ? (ReadModelKey)key : throw new UnableToResolveReadModelFromCommandContext(readModelType);

    sealed record KeyLookup(Type Model, ProtectedReadModelKeyTraversal? Traversal, bool Protect);
}
