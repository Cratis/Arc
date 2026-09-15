// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer that warns when the [EventType] attribute passes an id that is redundant — either the empty string,
/// or the type's own name, which is what the id defaults to anyway.
/// </summary>
/// <remarks>
/// An id that *differs* from the type name is left alone: it is the documented way to rename an event record
/// while stored events keep resolving under the old identifier, and reporting it would orphan them the moment
/// someone "cleaned up" the supposed redundancy.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class EventTypeIdArgumentAnalyzer : DiagnosticAnalyzer
{
    const string EventTypeAttributeName = "Cratis.Chronicle.Events.EventTypeAttribute";
    const string IdParameterName = "id";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0004_EventTypeIdRepeatsTypeName];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSyntaxNodeAction(AnalyzeAttribute, SyntaxKind.Attribute);
    }

    static void AnalyzeAttribute(SyntaxNodeAnalysisContext context)
    {
        var attribute = (AttributeSyntax)context.Node;

        if (attribute.ArgumentList is null || attribute.ArgumentList.Arguments.Count == 0)
        {
            return;
        }

        if (context.SemanticModel.GetSymbolInfo(attribute, context.CancellationToken).Symbol is not IMethodSymbol constructor ||
            constructor.ContainingType?.ToDisplayString() != EventTypeAttributeName)
        {
            return;
        }

        if (attribute.Parent?.Parent is not BaseTypeDeclarationSyntax typeDeclaration ||
            context.SemanticModel.GetDeclaredSymbol(typeDeclaration, context.CancellationToken) is not INamedTypeSymbol typeSymbol)
        {
            return;
        }

        var idArgument = GetIdArgument(attribute.ArgumentList.Arguments, constructor);
        if (idArgument is null)
        {
            return;
        }

        var constantValue = context.SemanticModel.GetConstantValue(idArgument.Expression, context.CancellationToken);
        if (!constantValue.HasValue || constantValue.Value is not string id)
        {
            // A non-constant id cannot be compared against the type name here; leave it alone rather than guess.
            return;
        }

        if (id.Length != 0 && !string.Equals(id, typeSymbol.MetadataName, StringComparison.Ordinal))
        {
            return;
        }

        context.ReportDiagnostic(Diagnostic.Create(
            DiagnosticDescriptors.ARCCHR0004_EventTypeIdRepeatsTypeName,
            attribute.GetLocation(),
            typeSymbol.Name));
    }

    /// <summary>
    /// Finds the argument bound to the <c>id</c> constructor parameter, walking positional arguments through
    /// the resolved constructor so a reordered parameter list, or an explicit generation before the id, still
    /// binds correctly.
    /// </summary>
    /// <param name="arguments">The attribute's argument list.</param>
    /// <param name="constructor">The resolved <see cref="IMethodSymbol"/> for the attribute constructor.</param>
    /// <returns>The <see cref="AttributeArgumentSyntax"/> bound to <c>id</c>, or <see langword="null"/> when none is present.</returns>
    static AttributeArgumentSyntax? GetIdArgument(SeparatedSyntaxList<AttributeArgumentSyntax> arguments, IMethodSymbol constructor)
    {
        var position = 0;

        foreach (var argument in arguments)
        {
            if (argument.NameEquals is not null)
            {
                // A property setter (e.g. [EventType(Generation = 2)]) is never the id.
                continue;
            }

            if (argument.NameColon is not null)
            {
                if (argument.NameColon.Name.Identifier.Text == IdParameterName)
                {
                    return argument;
                }

                continue;
            }

            if (position < constructor.Parameters.Length && constructor.Parameters[position].Name == IdParameterName)
            {
                return argument;
            }

            position++;
        }

        return null;
    }
}
