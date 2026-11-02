// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using System.Security.Claims;
using Cratis.Arc.Authorization;
using Microsoft.Extensions.Logging;

namespace Cratis.Arc.Commands;

/// <summary>The fail-closed entry point for caller-derived key providers.</summary>
internal static class ReadModelKeyProviders
{
    /// <summary>Validates lookup forms before activating a provider.</summary>
    /// <param name="parameter">The parameter declaration.</param>
    /// <param name="commandType">The owning command type.</param>
    /// <returns>The provider declaration, or null.</returns>
    /// <exception cref="InvalidLookUpBy">Multiple lookup forms are declared.</exception>
    /// <exception cref="ReadModelKeyResolutionDenied">Provider activation failed.</exception>
    internal static ILookUpByKeyProvider? Validate(ParameterInfo parameter, Type commandType)
    {
        var declarations = CustomAttributeData.GetCustomAttributes(parameter);
        var providers = declarations.Where(attribute => typeof(ILookUpByKeyProvider).IsAssignableFrom(attribute.AttributeType)).ToArray();
        if (providers.Length == 0) return null;
        var providerType = providers[0].AttributeType;
        if (providers.Length > 1 || declarations.Any(attribute => attribute.AttributeType == typeof(LookUpByAttribute)))
        {
            throw new InvalidLookUpBy(commandType, providerType, "only one lookup form may be declared");
        }

        try
        {
            return parameter.GetCustomAttributes().OfType<ILookUpByKeyProvider>().Single();
        }
        catch (Exception)
        {
            throw new ReadModelKeyResolutionDenied(ReadModelKeyRefusal.ProviderFailed);
        }
    }

    /// <summary>Resolves a key only after checking the captured caller.</summary>
    /// <param name="attribute">The provider declaration.</param>
    /// <param name="readModelType">The read-model type.</param>
    /// <param name="commandType">The command type, used only for safe logging.</param>
    /// <param name="currentPrincipal">The observable current principal.</param>
    /// <param name="logger">The framework logger, never passed to providers.</param>
    /// <returns>The provider's key.</returns>
    /// <exception cref="ReadModelKeyResolutionDenied">The caller or provider could not supply one key.</exception>
    internal static object Resolve(ILookUpByKeyProvider attribute, Type readModelType, Type commandType, ClaimsPrincipal? currentPrincipal, ILogger? logger = null)
    {
        if (!AuthorizationCommandIdentity.TryGetCaller(currentPrincipal, out var caller, out var refusal))
        {
            throw Denied(commandType, readModelType, attribute.ProviderType, refusal, logger);
        }

        ReadModelKeyLookup lookup;
        try
        {
            lookup = attribute.Provider.Resolve(new ReadModelKeyContext(caller!, readModelType));
        }
        catch (Exception)
        {
            throw Denied(commandType, readModelType, attribute.ProviderType, ReadModelKeyRefusal.ProviderFailed, logger);
        }

        if (lookup.Outcome == ReadModelKeyLookupOutcome.Found && lookup.Key is not null and not "") return lookup.Key;
        throw Denied(commandType, readModelType, attribute.ProviderType, lookup.Outcome == ReadModelKeyLookupOutcome.Ambiguous ? ReadModelKeyRefusal.Ambiguous : ReadModelKeyRefusal.Missing, logger);
    }

    static ReadModelKeyResolutionDenied Denied(Type commandType, Type readModelType, Type providerType, ReadModelKeyRefusal reason, ILogger? logger)
    {
        logger?.KeyResolutionRefused(commandType, readModelType, providerType, reason);
        return new(reason);
    }
}
