// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Analyzer that reports a command marking its key with the data annotations <c>[Key]</c> in an application that
/// resolves keys through Chronicle.
/// </summary>
/// <remarks>
/// Two attributes are spelled <c>[Key]</c>. Chronicle resolves a command's event source id from
/// <c>Cratis.Chronicle.Keys.KeyAttribute</c>; Arc reads <c>System.ComponentModel.DataAnnotations.KeyAttribute</c>, but
/// only in an application that has no Chronicle. Marking the data annotations one here compiles, reads correctly, and
/// silently does nothing: Chronicle finds no key property, invents a fresh event source id, and every read model keyed
/// by the command resolves to nothing — surfacing as "the entity does not exist" rather than as the wiring mistake it is.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandDataAnnotationsKeyAnalyzer : DiagnosticAnalyzer
{
    const string CommandAttributeName = "Cratis.Arc.Commands.ModelBound.CommandAttribute";
    const string DataAnnotationsNamespace = "System.ComponentModel.DataAnnotations";
    const string KeysNamespace = "Cratis.Chronicle.Keys";
    const string KeyAttributeName = "KeyAttribute";
    const string EventsNamespace = "Cratis.Chronicle.Events";
    const string EventSourceIdTypeName = "EventSourceId";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARCCHR0008_CommandKeyMarkedWithDataAnnotationsKey];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterCompilationStartAction(OnCompilationStart);
    }

    static void OnCompilationStart(CompilationStartAnalysisContext context)
    {
        // Only an application that resolves keys through Chronicle has the ambiguity. Without Chronicle the data
        // annotations attribute is the right one, and reporting it would be reporting correct code.
        if (context.Compilation.GetTypeByMetadataName($"{EventsNamespace}.{EventSourceIdTypeName}") is null)
        {
            return;
        }

        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var command = (INamedTypeSymbol)context.Symbol;

        if (command.TypeKind != TypeKind.Class || !HasAttribute(command, CommandAttributeName))
        {
            return;
        }

        foreach (var property in command.GetMembers().OfType<IPropertySymbol>().Where(IsMarkedWithDataAnnotationsKeyOnly))
        {
            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARCCHR0008_CommandKeyMarkedWithDataAnnotationsKey,
                property.Locations[0],
                command.Name,
                property.Name));
        }
    }

    static bool IsMarkedWithDataAnnotationsKeyOnly(IPropertySymbol property) =>
        !property.IsStatic &&
        HasAttribute(property, DataAnnotationsNamespace, KeyAttributeName) &&
        !HasAttribute(property, KeysNamespace, KeyAttributeName);

    static bool HasAttribute(ISymbol symbol, string fullyQualifiedName) =>
        symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.ToDisplayString() == fullyQualifiedName);

    static bool HasAttribute(ISymbol symbol, string @namespace, string typeName) =>
        symbol.GetAttributes().Any(attribute =>
            attribute.AttributeClass?.Name == typeName &&
            attribute.AttributeClass?.ContainingNamespace?.ToDisplayString() == @namespace);
}
