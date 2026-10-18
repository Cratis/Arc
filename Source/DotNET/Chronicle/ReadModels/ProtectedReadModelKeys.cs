// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.Chronicle.Commands;
using Cratis.Concepts;
using Microsoft.Extensions.DependencyInjection;

namespace Cratis.Arc.Chronicle.ReadModels;

/// <summary>
/// Applies the protected-read missing-model policy on top of <see cref="ProtectedReadModelKeyTraversal"/>, following Screenplay decision 0091.
/// </summary>
/// <remarks>
/// Key parts are inspected only through the traversal's generated, registered or shared compatibility accessors; this type adds no reflection.
/// </remarks>
internal static class ProtectedReadModelKeys
{
    /// <summary>
    /// Gets the key traversal that <see cref="ReadModelServiceCollectionExtensions.AddCommandAwareDecisionReads"/> registers.
    /// </summary>
    /// <param name="services">The invocation's services.</param>
    /// <returns>The registered traversal, or null when command-aware decision reads are not configured.</returns>
    internal static ProtectedReadModelKeyTraversal? Traversal(IServiceProvider services) => services.GetService<ProtectedReadModelKeyTraversal>();

    /// <summary>
    /// Rejects a raw key, including any invalid composite part, before it is converted or acquired.
    /// </summary>
    /// <param name="traversal">The registered key traversal.</param>
    /// <param name="model">The read model type.</param>
    /// <param name="rawKey">The key before conversion.</param>
    /// <param name="keyText">The diagnostic key text.</param>
    /// <exception cref="ProtectedCommandRequiresCommandAwareReader">Command-aware decision reads are not configured.</exception>
    /// <exception cref="ProtectedReadModelDoesNotExist">The raw key has a refused shape.</exception>
    /// <exception cref="ProtectedReadModelKeyAccessorMissing">Strict coverage has no accessors for a composite part.</exception>
    internal static void ValidateRawKey(ProtectedReadModelKeyTraversal? traversal, Type model, object? rawKey, string keyText)
    {
        if (!(traversal ?? throw new ProtectedCommandRequiresCommandAwareReader()).IsValidRawKey(rawKey))
        {
            throw new ProtectedReadModelDoesNotExist(model, keyText);
        }
    }

    /// <summary>
    /// Rejects already-resolved wire text that has a refused shape.
    /// </summary>
    /// <param name="model">The read model type.</param>
    /// <param name="wireText">The converted target text.</param>
    /// <param name="keyText">The diagnostic key text.</param>
    /// <exception cref="ProtectedReadModelDoesNotExist">The wire text has a refused shape.</exception>
    internal static void ValidateWireText(Type model, string wireText, string keyText)
    {
        if (!ProtectedReadModelKeyTraversal.IsValidWireText(wireText))
        {
            throw new ProtectedReadModelDoesNotExist(model, keyText);
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
}
