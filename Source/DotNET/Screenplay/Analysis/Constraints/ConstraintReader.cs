// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Validation;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Constraints;

/// <summary>
/// Reads the constraints a slice declares, in both shapes an application can write them.
/// </summary>
/// <param name="models">The <see cref="SemanticModels"/> every defining method is read through.</param>
/// <param name="paths">The <see cref="SourcePaths"/> rewriting the path of the file each constraint lives in.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// Screenplay knows exactly two rules - a property of an event has to be unique, and an event may occur once. A
/// rule declared in code that says anything else is pointed at rather than described as something it is not.
/// <para>
/// A rule whose defining method belongs to a project that was not handed over is a rule nothing can be read from, so
/// the file holding it is pointed at exactly as one saying something unmappable is.
/// </para>
/// </remarks>
public class ConstraintReader(SemanticModels models, SourcePaths paths, ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The call declaring a uniqueness rule.
    /// </summary>
    public const string Unique = "Unique";

    /// <summary>
    /// The call naming the property a uniqueness rule applies to.
    /// </summary>
    public const string On = "On";

    /// <summary>
    /// The method a constraint declares its rules in.
    /// </summary>
    public const string DefineMethod = "Define";

    /// <summary>
    /// Determines whether a type declares constraints in code.
    /// </summary>
    /// <param name="type">The type to check.</param>
    /// <returns>True when the type is a constraint.</returns>
    public static bool IsConstraint(ITypeSymbol type) =>
        type is { IsAbstract: false, TypeKind: TypeKind.Class } && type.FindInterface(WellKnownTypeNames.Constraint) is not null;

    /// <summary>
    /// Reads the constraints declared on the properties of an event.
    /// </summary>
    /// <param name="type">The type declaring the event.</param>
    /// <returns>The constraints, ordered by name.</returns>
    public static IEnumerable<ConstraintModel> FromEvent(INamedTypeSymbol type) =>
    [
        .. type.DeclaredProperties()
            .Select(property => (property, attribute: property.GetAttribute(WellKnownTypeNames.UniqueAttribute)))
            .Where(_ => _.attribute is not null)
            .Select(_ => new UniquePropertyConstraintModel(
                _.attribute!.GetArgument(0) as string ?? $"Unique{type.Name}{_.property.Name}",
                _.property.Name,
                type.Name))
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Reads a constraint declared in code.
    /// </summary>
    /// <param name="type">The type declaring the constraint.</param>
    /// <param name="location">Where the constraint lives, for use in diagnostics.</param>
    /// <returns>The constraints it declares, never empty.</returns>
    public IEnumerable<ConstraintModel> Read(INamedTypeSymbol type, string location)
    {
        var declared = new List<ConstraintModel>();

        foreach (var method in type.GetMembers(DefineMethod).OfType<IMethodSymbol>())
        {
            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                ReadDeclaration(type, reference.GetSyntax(), location, declared);
            }
        }

        return declared.Count > 0 ? declared : [Custom(type)];
    }

    /// <summary>
    /// Builds the declaration pointing at the file holding a rule that could not be described.
    /// </summary>
    /// <param name="type">The type declaring the constraint.</param>
    /// <returns>The <see cref="CustomConstraintModel"/>.</returns>
    CustomConstraintModel Custom(INamedTypeSymbol type) => new(type.Name, paths.Relative(type.SourceFilePath()));

    /// <summary>
    /// Reads every uniqueness rule within one declaration of the defining method.
    /// </summary>
    /// <param name="type">The type declaring the constraint.</param>
    /// <param name="declaration">The declaration to read.</param>
    /// <param name="location">Where the constraint lives, for use in diagnostics.</param>
    /// <param name="declared">The constraints collected so far.</param>
    void ReadDeclaration(INamedTypeSymbol type, SyntaxNode declaration, string location, List<ConstraintModel> declared)
    {
        if (models.For(declaration.SyntaxTree) is not { } semanticModel)
        {
            return;
        }

        foreach (var invocation in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!string.Equals(InvocationChain.NameOf(invocation), Unique, StringComparison.Ordinal))
            {
                continue;
            }

            var read = ReadUnique(type, invocation, semanticModel);
            if (read is null)
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableConstraint,
                    $"The rule '{type.Name}' declares is neither unique on a property nor unique on an event type, so the file holding it is pointed at instead",
                    location);

                declared.Add(Custom(type));
                continue;
            }

            declared.Add(read);
        }
    }

    /// <summary>
    /// Reads one uniqueness rule.
    /// </summary>
    /// <param name="type">The type declaring the constraint.</param>
    /// <param name="invocation">The call declaring the rule.</param>
    /// <param name="semanticModel">The semantic model of the tree the call lives in.</param>
    /// <returns>The constraint, or <see langword="null"/> when the rule cannot be described.</returns>
    /// <remarks>
    /// The generic form names the event type directly and constrains the whole event, while the builder form names
    /// the property it constrains inside a nested call.
    /// </remarks>
    ConstraintModel? ReadUnique(INamedTypeSymbol type, InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (InvocationChain.TypeArgumentOf(invocation) is { } declaredEvent)
        {
            var resolved = semanticModel.GetTypeInfo(declaredEvent).Type;

            return resolved is null
                ? null
                : new UniqueEventConstraintModel(ConstraintNames.Resolve(invocation, semanticModel, type, resolved), resolved.Name);
        }

        var body = invocation.DescendantNodes().OfType<InvocationExpressionSyntax>()
            .FirstOrDefault(_ => string.Equals(InvocationChain.NameOf(_), On, StringComparison.Ordinal));

        if (body is null || InvocationChain.TypeArgumentOf(body) is not { } eventType)
        {
            return null;
        }

        var property = LambdaPaths.Read(InvocationChain.ArgumentOf(body));
        var declaring = semanticModel.GetTypeInfo(eventType).Type;

        return property is null || declaring is null
            ? null
            : new UniquePropertyConstraintModel(
                ConstraintNames.Resolve(invocation, semanticModel, type, declaring, property),
                property,
                declaring.Name);
    }
}
