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
        => EventTypes(returnType, compilation, nullableOnly: true);

    /// <summary>
    /// Finds event leaves through supported response wrappers, preserving optional containers.
    /// </summary>
    /// <param name="type">The response type.</param>
    /// <param name="compilation">The compilation containing framework identities.</param>
    /// <param name="nullableOnly">Whether only optional event leaves are wanted.</param>
    /// <returns>The event types without nullable annotations.</returns>
    internal static IEnumerable<ITypeSymbol> EventTypes(ITypeSymbol type, Compilation compilation, bool nullableOnly = false) =>
        EventTypes(type, compilation, (nullableOnly, false), new ResponseTypeTraversal());

    static ITypeSymbol[] EventTypes(ITypeSymbol type, Compilation compilation, (bool OnlyNullable, bool NullableContainer) filter, ResponseTypeTraversal traversal) =>
        traversal.Visit(type, () => EventBranches(type, compilation, filter, traversal).ToArray(), [], inheritedContext: filter.NullableContainer);

    static IEnumerable<ITypeSymbol> EventBranches(ITypeSymbol type, Compilation compilation, (bool OnlyNullable, bool NullableContainer) filter, ResponseTypeTraversal traversal)
    {
        type = RawGuidResponseAnalysis.UnwrapAwaitable(type, compilation);
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            return EventTypes(nullable.TypeArguments[0], compilation, (filter.OnlyNullable, true), traversal);
        }
        if (type is INamedTypeSymbol { IsTupleType: true } tuple)
        {
            return tuple.TupleElements.SelectMany(element => EventTypes(element.Type, compilation, filter, traversal));
        }
        var alternatives = RawGuidResponseAnalysis.UnionAlternatives(type, compilation);
        if (!alternatives.IsDefault)
        {
            return alternatives.SelectMany(alternative => EventTypes(alternative, compilation, filter, traversal));
        }
        return IsEvent(type, compilation) && MatchesFilter(type, filter)
            ? [type.WithNullableAnnotation(NullableAnnotation.NotAnnotated)]
            : [];
    }

    static bool MatchesFilter(ITypeSymbol type, (bool OnlyNullable, bool NullableContainer) filter) =>
        !filter.OnlyNullable || filter.NullableContainer || type.NullableAnnotation == NullableAnnotation.Annotated;

    static bool IsEvent(ITypeSymbol type, Compilation compilation) =>
        RawGuidResponseAnalysis.HasEventTypeAttribute(type, compilation) &&
        type.ToDisplayString() != "Cratis.Arc.Commands.ICommandOperation" &&
        !type.AllInterfaces.Any(contract => contract.ToDisplayString() == "Cratis.Arc.Commands.ICommandOperation");

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
            var events = NullableEvents(method.ReturnType, context.Compilation)
                .Concat(NativeUnionReturnOperations.EmptyEvents(method, context.Compilation, context.CancellationToken))
                .Select(type => type.Name).Distinct().ToArray();
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
