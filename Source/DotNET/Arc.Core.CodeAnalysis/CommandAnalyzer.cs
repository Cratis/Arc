// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer for Command types.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class CommandAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [
            DiagnosticDescriptors.ARC0002_MissingCommandAttribute
        ];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeNamedType, SymbolKind.NamedType);
    }

    static void AnalyzeNamedType(SymbolAnalysisContext context)
    {
        var namedTypeSymbol = (INamedTypeSymbol)context.Symbol;

        if (namedTypeSymbol.TypeKind != TypeKind.Class)
        {
            return;
        }

        // Skip extension classes, utility classes, and types that are clearly not commands
        if (namedTypeSymbol.Name.EndsWith("Extensions", StringComparison.Ordinal) ||
            namedTypeSymbol.Name.EndsWith("Helper", StringComparison.Ordinal) ||
            namedTypeSymbol.Name.EndsWith("Helpers", StringComparison.Ordinal) ||
            namedTypeSymbol.IsStatic)
        {
            return;
        }

        var hasCommandAttribute = HasAttribute(namedTypeSymbol, "Cratis.Arc.Commands.ModelBound.CommandAttribute");
        var handleMethod = FindHandleMethod(namedTypeSymbol);

        // Only warn about missing [Command] attribute if type looks like a command
        if (!hasCommandAttribute && handleMethod != null && LooksLikeCommand(namedTypeSymbol, handleMethod))
        {
            var diagnostic = Diagnostic.Create(
                DiagnosticDescriptors.ARC0002_MissingCommandAttribute,
                namedTypeSymbol.Locations[0],
                namedTypeSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    static bool HasAttribute(INamedTypeSymbol typeSymbol, string attributeFullName)
    {
        return typeSymbol.GetAttributes().Any(attr =>
            attr.AttributeClass?.ToDisplayString() == attributeFullName);
    }

    static IMethodSymbol? FindHandleMethod(INamedTypeSymbol typeSymbol)
    {
        return typeSymbol.GetMembers("Handle")
            .OfType<IMethodSymbol>()
            .FirstOrDefault(m => m.MethodKind == MethodKind.Ordinary);
    }

    static bool LooksLikeCommand(INamedTypeSymbol typeSymbol, IMethodSymbol handleMethod)
    {
        // Skip types that implement specific interfaces that aren't commands
        if (typeSymbol.AllInterfaces.Any(i =>
            i.Name == "ICommandHandler" ||
            i.Name == "ICommandResponseValueHandler"))
        {
            return false;
        }

        // A command should be a class with properties and a Handle method
        // Skip if it's a static method (commands use instance handle methods)
        if (handleMethod.IsStatic)
        {
            return false;
        }

        // Check if the type has properties (typical of commands)
        var hasProperties = typeSymbol.GetMembers()
            .OfType<IPropertySymbol>()
            .Any(p => p.DeclaredAccessibility == Accessibility.Public);

        return hasProperties;
    }
}
