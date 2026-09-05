// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Operations;

namespace Cratis.Arc.CodeAnalysis;

/// <summary>
/// Analyzer that reports a query parameter declared as a raw string or Guid and converted to a concept inside the
/// method body, which skips the validator Arc would have run had the parameter been declared as the concept.
/// </summary>
/// <remarks>
/// The shape is almost always inherited rather than chosen: a query that began as a string keyed lookup keeps its
/// parameter through every later refactor while the conversion migrates into the body as a cast. Each refactor makes
/// the surrounding code more idiomatic and the stale parameter less conspicuous, which is why reading does not find
/// it — the query works, the value reaches the read model, and only the validator is missing.
/// </remarks>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class QueryParameterConceptTypeAnalyzer : DiagnosticAnalyzer
{
    const string ReadModelAttributeName = "Cratis.Arc.Queries.ModelBound.ReadModelAttribute";
    const string ConceptAsTypeName = "ConceptAs";
    const string ConceptsNamespace = "Cratis.Concepts";

    /// <inheritdoc/>
    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics => [DiagnosticDescriptors.ARC0015_QueryParameterShouldBeConcept];

    /// <inheritdoc/>
    public override void Initialize(AnalysisContext context)
    {
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.EnableConcurrentExecution();
        context.RegisterOperationBlockAction(AnalyzeOperationBlock);
    }

    static void AnalyzeOperationBlock(OperationBlockAnalysisContext context)
    {
        if (context.OwningSymbol is not IMethodSymbol method || !IsQueryMethod(method))
        {
            return;
        }

        var rawParameters = method.Parameters.Where(IsRawIdentifierType).ToArray();
        if (rawParameters.Length == 0)
        {
            return;
        }

        foreach (var parameter in rawParameters)
        {
            var conceptType = FindConceptConvertedTo(context.OperationBlocks, parameter);
            if (conceptType is null) continue;

            context.ReportDiagnostic(Diagnostic.Create(
                DiagnosticDescriptors.ARC0015_QueryParameterShouldBeConcept,
                parameter.Locations[0],
                parameter.Name,
                parameter.Type.ToDisplayString(),
                conceptType.Name));
        }
    }

    /// <summary>
    /// Whether the method is one query discovery would register.
    /// </summary>
    /// <param name="method">The method to check.</param>
    /// <returns>True if it is a query method.</returns>
    /// <remarks>
    /// Internal as well as public, because query discovery registers internal methods too — an internal query is
    /// just as routable, and just as unvalidated, as a public one.
    /// </remarks>
    static bool IsQueryMethod(IMethodSymbol method) =>
        method.MethodKind == MethodKind.Ordinary &&
        method.IsStatic &&
        method.DeclaredAccessibility is Accessibility.Public or Accessibility.Internal &&
        !method.ReturnsVoid &&
        method.ContainingType?.GetAttributes().Any(_ => _.AttributeClass?.ToDisplayString() == ReadModelAttributeName) == true;

    /// <summary>
    /// Whether the parameter is declared as one of the raw types a concept is usually backed by.
    /// </summary>
    /// <param name="parameter">The parameter to check.</param>
    /// <returns>True if it is a raw string or Guid.</returns>
    /// <remarks>
    /// A nullable is unwrapped first: <c>Guid?</c> is the same mistake as <c>Guid</c>, and declaring the parameter
    /// optional is orthogonal to declaring it raw.
    /// </remarks>
    static bool IsRawIdentifierType(IParameterSymbol parameter)
    {
        var type = parameter.Type;
        if (type is INamedTypeSymbol { IsGenericType: true } named &&
            named.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            type = named.TypeArguments[0];
        }

        return type.SpecialType == SpecialType.System_String ||
               (type.Name == nameof(Guid) && type.ContainingNamespace?.ToDisplayString() == nameof(System));
    }

    /// <summary>
    /// Finds the concept type, if any, that the parameter is converted to somewhere in the method.
    /// </summary>
    /// <param name="operationBlocks">The method's operation blocks.</param>
    /// <param name="parameter">The parameter to look for conversions of.</param>
    /// <returns>The concept type converted to, or null when there is none.</returns>
    /// <remarks>
    /// Every way of writing the conversion is one conversion operation over a parameter reference — an explicit
    /// cast, an implicit conversion at a call argument, and a comparison against a concept alike — so matching the
    /// operation catches all three rather than one syntax at a time.
    /// </remarks>
    static ITypeSymbol? FindConceptConvertedTo(ImmutableArray<IOperation> operationBlocks, IParameterSymbol parameter)
    {
        foreach (var block in operationBlocks)
        {
            foreach (var operation in block.Descendants())
            {
                if (operation is not IConversionOperation conversion) continue;
                if (!ReferencesParameter(conversion.Operand, parameter)) continue;
                if (conversion.Type is { } target && IsOrDerivesFromConceptAs(target))
                {
                    return target;
                }
            }
        }

        return null;
    }

    /// <summary>
    /// Whether the operation is a reference to the given parameter, through a nullable unwrap if there is one.
    /// </summary>
    /// <param name="operation">The converted operand.</param>
    /// <param name="parameter">The parameter to match.</param>
    /// <returns>True when the operand is that parameter's value.</returns>
    /// <remarks>
    /// A nullable parameter is never converted directly - there is no conversion from <c>Guid?</c> to a concept, so
    /// the author writes <c>(RequestId)id.Value</c> and the conversion sits over the property access rather than
    /// over the parameter. Matching only the bare reference would make the rule silent on exactly the shape a
    /// nullable parameter forces.
    /// </remarks>
    static bool ReferencesParameter(IOperation operation, IParameterSymbol parameter)
    {
        if (operation is IPropertyReferenceOperation { Instance: { } instance } property &&
            string.Equals(property.Property.Name, "Value", StringComparison.Ordinal) &&
            instance.Type is INamedTypeSymbol { IsGenericType: true } nullable &&
            nullable.ConstructedFrom.SpecialType == SpecialType.System_Nullable_T)
        {
            operation = instance;
        }

        return operation is IParameterReferenceOperation reference &&
               SymbolEqualityComparer.Default.Equals(reference.Parameter, parameter);
    }

    /// <summary>
    /// Whether the type is a concept.
    /// </summary>
    /// <param name="typeSymbol">The type to check.</param>
    /// <returns>True if it is or derives from ConceptAs.</returns>
    /// <remarks>
    /// The walk starts at the type itself rather than at its base, so <c>ConceptAs&lt;T&gt;</c> used directly is
    /// matched as well as a record deriving from it. Chronicle's <c>EventSourceId</c> needs no arm of its own: it
    /// is a <c>ConceptAs&lt;string&gt;</c> and is matched here, which keeps a Chronicle namespace out of a package
    /// that does not otherwise know Chronicle exists.
    /// </remarks>
    static bool IsOrDerivesFromConceptAs(ITypeSymbol typeSymbol)
    {
        var current = typeSymbol;
        while (current is not null)
        {
            if (current.Name == ConceptAsTypeName &&
                current.ContainingNamespace?.ToDisplayString() == ConceptsNamespace)
            {
                return true;
            }

            current = current.BaseType;
        }

        return false;
    }
}
