// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Resolves the command property a read model parameter is looked up by, the way Arc does at run time.
/// </summary>
/// <remarks>
/// An explicit <c>[LookUpBy]</c> names the property in every read-protection mode. Under protection On, a read
/// without one uses the only command property of the read model's key type and falls back to the command's event
/// source key, and several matching properties are ambiguous. Under any other mode the event source key is used.
/// </remarks>
public static class CommandReadKeys
{
    /// <summary>The read-protection value declaring protection on.</summary>
    internal const int ProtectionOn = 2;

    /// <summary>
    /// Resolves the key property of a read.
    /// </summary>
    /// <param name="command">The command declaring the parameter.</param>
    /// <param name="parameter">The read model parameter.</param>
    /// <param name="readModel">The read model type.</param>
    /// <param name="readProtection">The effective read-protection mode of the command.</param>
    /// <param name="identifier">The command's proven event source key property, if any.</param>
    /// <returns>The key property, or the reason none could be proven.</returns>
    public static (string? Property, string? Failure) Resolve(INamedTypeSymbol command, IParameterSymbol parameter, INamedTypeSymbol readModel, int readProtection, string? identifier)
    {
        var lookup = CommandAttributeSymbols.ResolveLookup(command, parameter, true);
        if (lookup.Conflict is not null)
        {
            return (null, $"its LookUpBy declaration names no public command property ({lookup.Conflict})");
        }

        if (lookup.Property is not null)
        {
            return (lookup.Property, null);
        }

        if (readProtection != ProtectionOn)
        {
            return identifier is null ? (null, "the command has no proven event source key to read by") : (identifier, null);
        }

        var keyType = CommandAttributeSymbols.PublicProperties(readModel).FirstOrDefault(property => property.Name == "Id")?.Type;
        var candidates = keyType is null
            ? []
            : CommandAttributeSymbols.PublicProperties(command).Where(property => SymbolEqualityComparer.Default.Equals(property.Type, keyType))
                .Select(property => property.Name).Distinct(StringComparer.Ordinal).ToArray();

        return candidates switch
        {
            [var single] => (single, null),
            { Length: > 1 } => (null, $"several command properties ({string.Join(", ", candidates)}) have the key type of '{readModel.Name}'; name one with LookUpBy"),
            _ => identifier is null ? (null, "the command has no proven event source key to read by") : (identifier, null)
        };
    }
}
