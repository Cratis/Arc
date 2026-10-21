// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Commands;

/// <summary>
/// Recovers definition names without guessing aliases for stored classifications.
/// </summary>
internal static class EventSourceNames
{
    const string Suffix = "EventSource";

    /// <summary>
    /// Reads the source model name, retaining legacy identity names.
    /// </summary>
    /// <param name="definition">The source definition.</param>
    /// <param name="models">The semantic models recognizing const-backed definitions.</param>
    /// <returns>The recovered name, or the stored name when it cannot be recovered.</returns>
    internal static string Source(INamedTypeSymbol definition, SemanticModels? models)
    {
        var stored = StoredSource(definition);
        var namedStreams = HasNamedStreams(definition, models);
        if (ScreenplayIdentifier.IsBareIdentifier(stored) && !namedStreams)
        {
            return stored;
        }
        var name = RecoveredSourceName(definition, stored);

        return name ?? (namedStreams ? string.Empty : stored);
    }

    /// <summary>
    /// Reads the exact stored source name.
    /// </summary>
    /// <param name="definition">The source definition.</param>
    /// <returns>The stored name.</returns>
    internal static string StoredSource(INamedTypeSymbol definition) =>
        definition.GetAttribute(WellKnownTypeNames.EventSourceDefinitionAttribute)?.GetArgument(0) is string stored ? stored :
            DefaultSourceName(definition);

    /// <summary>
    /// Reads a stream directly referenced by an attribute, retaining legacy literal identity names.
    /// </summary>
    /// <param name="attribute">The attribute naming the stream.</param>
    /// <param name="definition">The source owning its const field.</param>
    /// <param name="models">The semantic models resolving attribute expressions.</param>
    /// <returns>The recovered name, or the stored value when no name is recoverable.</returns>
    internal static string? Stream(AttributeData attribute, INamedTypeSymbol definition, SemanticModels? models)
    {
        var stored = attribute.GetArgument(0) as string;
        if (attribute.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax { ArgumentList.Arguments: [var argument, ..] } syntax)
        {
            return stored;
        }
        if (argument.Expression is LiteralExpressionSyntax)
        {
            return stored;
        }
        var symbol = models?.For(syntax.SyntaxTree)?.GetSymbolInfo(argument.Expression).Symbol;

        return DirectStreamName(symbol, definition, stored) ?? LiteralIdentityStream(definition, stored);
    }

    /// <summary>
    /// Checks that a name retains its declaration spelling.
    /// </summary>
    /// <param name="name">The proposed model name.</param>
    /// <returns>Whether it is an identity name.</returns>
    internal static bool IdentityName(string name) => ScreenplayIdentifier.IsBareIdentifier(name) && new ScreenplayNaming().ToDeclarationName(name) == name;

    /// <summary>
    /// Checks that a stored name prints and parses unchanged.
    /// </summary>
    /// <param name="name">The stored name.</param>
    /// <returns>Whether the text is portable.</returns>
    internal static bool Portable(string name) => name.Length > 0 && name.IsNormalized() &&
        new ScreenplayNaming().ToStringLiteral(name) == name && !name.Contains('\\', StringComparison.Ordinal);

    static string DefaultSourceName(INamedTypeSymbol definition) => definition.Name.EndsWith(Suffix, StringComparison.Ordinal) &&
        definition.Name.Length > Suffix.Length ? definition.Name[..^Suffix.Length] : definition.Name;

    static string? RecoveredSourceName(INamedTypeSymbol definition, string stored)
    {
        if (!definition.Name.EndsWith(Suffix, StringComparison.Ordinal))
        {
            return null;
        }
        var name = definition.Name[..^Suffix.Length];

        return IdentityName(name) && Portable(stored) ? name : null;
    }

    static string? DirectStreamName(ISymbol? symbol, INamedTypeSymbol definition, string? stored)
    {
        if (symbol is not IFieldSymbol { IsConst: true, ConstantValue: string value } field)
        {
            return null;
        }
        if (!SymbolEqualityComparer.Default.Equals(field.ContainingType, definition) || value != stored)
        {
            return null;
        }

        return IdentityName(field.Name) && Portable(value) ? field.Name : null;
    }

    static string? LiteralIdentityStream(INamedTypeSymbol definition, string? stored)
    {
        if (stored is null || !IdentityName(stored))
        {
            return null;
        }

        return definition.GetAttributes(WellKnownTypeNames.EventStreamDefinitionAttribute)
            .Any(attribute => (attribute.GetArgument(0) as string) == stored && IsLiteral(attribute)) ? stored : null;
    }

    static bool IsLiteral(AttributeData attribute) => attribute.ApplicationSyntaxReference?.GetSyntax() is not AttributeSyntax syntax ||
        syntax.ArgumentList?.Arguments.FirstOrDefault()?.Expression is LiteralExpressionSyntax;

    static bool HasNamedStreams(INamedTypeSymbol definition, SemanticModels? models) => definition.GetAttributes(WellKnownTypeNames.EventStreamDefinitionAttribute)
        .Any(attribute => !IsLiteral(attribute) && Stream(attribute, definition, models) is not null);
}
