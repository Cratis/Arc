// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Authorization;

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
        var declarations = LookUpByKeyProviderDeclarations.For(parameter).ToArray();
        var providers = declarations.Where(LookUpByKeyProviderDeclarations.IsProvider).ToArray();
        if (providers.Length == 0) return null;
        var providerType = providers[0].AttributeType;
        if (providers.Length > 1 || declarations.Any(attribute => attribute.AttributeType == typeof(LookUpByAttribute)))
        {
            throw new InvalidLookUpBy(commandType, providerType, "only one lookup form may be declared");
        }

        try
        {
            return (ILookUpByKeyProvider)providers[0].Constructor.Invoke([]);
        }
        catch (Exception)
        {
            throw new ReadModelKeyResolutionDenied(ReadModelKeyRefusal.ProviderFailed);
        }
    }

    /// <summary>Resolves a key only after checking the captured caller.</summary>
    /// <param name="attribute">The provider declaration.</param>
    /// <param name="request">The framework-only resolution context.</param>
    /// <returns>The provider's key.</returns>
    /// <exception cref="ReadModelKeyResolutionDenied">The caller or provider could not supply one key.</exception>
    internal static object Resolve(ILookUpByKeyProvider attribute, ReadModelKeyProviderRequest request)
    {
        if (!AuthorizationCommandIdentity.TryGetCaller(request.CurrentPrincipal, out var caller, out var refusal))
        {
            throw Denied(request, attribute.ProviderType, refusal);
        }

        ReadModelKeyLookup lookup;
        try
        {
            lookup = attribute.Provider.Resolve(new ReadModelKeyContext(caller!, request.ReadModelType));
        }
        catch (Exception)
        {
            throw Denied(request, attribute.ProviderType, ReadModelKeyRefusal.ProviderFailed);
        }

        if (lookup.Outcome == ReadModelKeyLookupOutcome.Found && lookup.Key is not null and not "") return lookup.Key;
        throw Denied(request, attribute.ProviderType, lookup.Outcome == ReadModelKeyLookupOutcome.Ambiguous ? ReadModelKeyRefusal.Ambiguous : ReadModelKeyRefusal.Missing);
    }

    static ReadModelKeyResolutionDenied Denied(ReadModelKeyProviderRequest request, Type providerType, ReadModelKeyRefusal reason)
    {
        request.Logger?.KeyResolutionRefused(request.CommandType, request.ReadModelType, providerType, reason);
        return new(reason);
    }
}
