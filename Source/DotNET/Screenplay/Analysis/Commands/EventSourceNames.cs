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
        var name = definition.Name.EndsWith(Suffix, StringComparison.Ordinal) ? definition.Name[..^Suffix.Length] : null;

        return name is not null && IdentityName(name) && Portable(stored) ? name : namedStreams ? string.Empty : stored;
    }

    /// <summary>
    /// Reads the exact stored source name.
    /// </summary>
    /// <param name="definition">The source definition.</param>
    /// <returns>The stored name.</returns>
    internal static string StoredSource(INamedTypeSymbol definition) =>
        definition.GetAttribute(WellKnownTypeNames.EventSourceDefinitionAttribute)?.GetArgument(0) is string stored ? stored :
            definition.Name.EndsWith(Suffix, StringComparison.Ordinal) && definition.Name.Length > Suffix.Length ? definition.Name[..^Suffix.Length] : definition.Name;

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

        return symbol is IFieldSymbol { IsConst: true, ConstantValue: string value } field &&
            SymbolEqualityComparer.Default.Equals(field.ContainingType, definition) && value == stored &&
            IdentityName(field.Name) && Portable(value) ? field.Name : null;
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

    static bool HasNamedStreams(INamedTypeSymbol definition, SemanticModels? models) => definition.GetAttributes(WellKnownTypeNames.EventStreamDefinitionAttribute)
        .Any(attribute => attribute.ApplicationSyntaxReference?.GetSyntax() is AttributeSyntax { ArgumentList.Arguments: [var argument, ..] } &&
            argument.Expression is not LiteralExpressionSyntax && Stream(attribute, definition, models) is not null);
}
