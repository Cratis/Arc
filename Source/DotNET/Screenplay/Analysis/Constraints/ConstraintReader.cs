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
/// uniqueness rule whose targets or releases cannot all be recovered is withheld rather than partially described.
/// <para>
/// A rule whose defining method belongs to a project that was not handed over cannot be read, so its source file
/// is referenced instead of inventing a declarative rule.
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
    /// <returns>The readable constraints, or a source reference when no uniqueness declaration was found.</returns>
    public IEnumerable<ConstraintModel> Read(INamedTypeSymbol type, string location)
    {
        var declared = new List<ConstraintModel>();
        var foundUnique = false;

        foreach (var method in type.GetMembers(DefineMethod).OfType<IMethodSymbol>())
        {
            foreach (var reference in method.DeclaringSyntaxReferences)
            {
                foundUnique |= ReadDeclaration(type, reference.GetSyntax(), location, declared);
            }
        }

        return foundUnique ? declared : [Custom(type)];
    }

    /// <summary>
    /// Reads a complete uniqueness declaration for emission and reaction batch admission.
    /// </summary>
    /// <param name="type">The type declaring the constraint.</param>
    /// <param name="invocation">The call declaring the rule.</param>
    /// <param name="semanticModel">The semantic model owning the call.</param>
    /// <returns>The faithfully recovered rule, or null.</returns>
    internal static ConstraintModel? ReadUnique(INamedTypeSymbol type, InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        var releases = ConstraintTargets.Releases(invocation, semanticModel);
        if (releases is null)
        {
            return null;
        }

        var rule = ReadRule(type, invocation, semanticModel);

        return rule is null ? null : rule with { ReleasedBy = releases };
    }

    /// <summary>
    /// Reads every target sharing a rule, retaining the existing single-target naming behavior.
    /// </summary>
    /// <param name="type">The type declaring the constraint.</param>
    /// <param name="invocation">The uniqueness declaration.</param>
    /// <param name="semanticModel">The semantic model owning the declaration.</param>
    /// <returns>The complete rule, or null if any target cannot be stated.</returns>
    static ConstraintModel? ReadRule(INamedTypeSymbol type, InvocationExpressionSyntax invocation, SemanticModel semanticModel)
    {
        if (InvocationChain.TypeArgumentOf(invocation) is { } declaredEvent)
        {
            var resolved = semanticModel.GetTypeInfo(declaredEvent).Type;

            return resolved is null
                ? null
                : new UniqueEventConstraintModel(ConstraintNames.Resolve(invocation, semanticModel, type, resolved), resolved.Name);
        }

        var targets = ConstraintTargets.Properties(invocation, semanticModel);
        if (targets is null)
        {
            return null;
        }

        var calls = ConstraintBuilderCalls.Callback(invocation, semanticModel)!;
        var firstCall = calls.First(_ => InvocationChain.NameOf(_) == On);
        var declaring = semanticModel.GetTypeInfo(InvocationChain.TypeArgumentOf(firstCall)!).Type!;
        var first = targets[0];

        return new UniquePropertyConstraintModel(
            ConstraintNames.Resolve(invocation, semanticModel, type, declaring, first.Property),
            first.Property,
            first.EventName)
        {
            AdditionalProperties = first.AdditionalProperties,
            AdditionalRules = targets.Skip(1).ToArray(),
            IgnoreCasing = calls.Any(_ => InvocationChain.NameOf(_) == "IgnoreCasing")
        };
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
    /// <returns>Whether a uniqueness declaration was found, including withheld declarations.</returns>
    bool ReadDeclaration(INamedTypeSymbol type, SyntaxNode declaration, string location, List<ConstraintModel> declared)
    {
        if (models.For(declaration.SyntaxTree) is not { } semanticModel)
        {
            return false;
        }

        var foundUnique = false;
        foreach (var invocation in declaration.DescendantNodes().OfType<InvocationExpressionSyntax>())
        {
            if (!string.Equals(InvocationChain.NameOf(invocation), Unique, StringComparison.Ordinal))
            {
                continue;
            }

            foundUnique = true;
            var read = ReadUnique(type, invocation, semanticModel);
            if (read is null)
            {
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.UnmappableConstraint,
                    $"The uniqueness rule '{type.Name}' declares has targets, a lifetime or configuration that cannot be proven and stated faithfully, so the whole constraint was withheld",
                    location);

                continue;
            }

            declared.Add(read);
        }

        return foundUnique;
    }
}
