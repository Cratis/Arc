// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis;

/// <summary>
/// Reports model-bound command handlers that declare an optional event result.
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class NullableCommandEventReturnAnalyzer : DiagnosticAnalyzer
{
    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        [DiagnosticDescriptors.ARCCHR0015_NullableCommandEventReturn];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterSymbolAction(AnalyzeCommand, SymbolKind.NamedType);
    }

    /// <summary>
    /// Finds nullable event branches in supported handler return wrappers.
    /// </summary>
    /// <param name="returnType">The declared handler return type.</param>
    /// <param name="compilation">The compilation containing framework identities.</param>
    /// <returns>The nullable event types, without their nullable annotation or wrapper.</returns>
    internal static IEnumerable<ITypeSymbol> NullableEvents(ITypeSymbol returnType, Compilation compilation)
    {
        foreach (var branch in RawGuidResponseAnalysis.UnionBranches(RawGuidResponseAnalysis.UnwrapAwaitable(returnType, compilation), compilation))
        {
            var eventType = branch.NullableAnnotation == NullableAnnotation.Annotated && branch.IsReferenceType
                ? branch.WithNullableAnnotation(NullableAnnotation.NotAnnotated)
                : null;
            if (eventType is not null && RawGuidResponseAnalysis.HasEventTypeAttribute(eventType, compilation) &&
                eventType.ToDisplayString() != "Cratis.Arc.Commands.ICommandOperation" &&
                !eventType.AllInterfaces.Any(type => type.ToDisplayString() == "Cratis.Arc.Commands.ICommandOperation"))
            {
                yield return eventType;
            }
        }
    }

    static void AnalyzeCommand(SymbolAnalysisContext context)
    {
        var command = (INamedTypeSymbol)context.Symbol;
        if (command.TypeKind != TypeKind.Class ||
            !RawGuidResponseAnalysis.HasAttribute(command, "Cratis.Arc.Commands.ModelBound.CommandAttribute", context.Compilation))
        {
            return;
        }

        foreach (var method in RawGuidResponseAnalysis.HandleMethods(command))
        {
            var events = NullableEvents(method.ReturnType, context.Compilation).Select(type => type.Name).Distinct().ToArray();
            if (events.Length == 0)
            {
                continue;
            }

            // An inherited handler or shared alias must not report on source outside this command.
            var location = command.Locations[0];
            if (SymbolEqualityComparer.Default.Equals(method.ContainingType, command) &&
                method.DeclaringSyntaxReferences.FirstOrDefault()?.GetSyntax(context.CancellationToken) is MethodDeclarationSyntax declaration)
            {
                location = declaration.ReturnType.GetLocation();
            }

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARCCHR0015_NullableCommandEventReturn,
                location,
                command.Name,
                string.Join(", ", events)));
        }
    }
}
