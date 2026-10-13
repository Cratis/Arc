// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Validation;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace Cratis.Arc.Screenplay.Analysis.Constraints;

/// <summary>
/// Reads every target of one shared uniqueness claim without keeping a readable subset.
/// </summary>
internal static class ConstraintTargets
{
    /// <summary>
    /// The call declaring a release event.
    /// </summary>
    internal const string RemovedWith = "RemovedWith";

    /// <summary>
    /// Reads all property targets inside a uniqueness builder.
    /// </summary>
    /// <param name="invocation">The uniqueness declaration.</param>
    /// <param name="model">The semantic model owning the declaration.</param>
    /// <returns>All targets, or null if any target is unreadable.</returns>
    internal static IReadOnlyList<UniquePropertyConstraintModel>? Properties(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (!HasReadableCallback(invocation, model))
        {
            return null;
        }

        var calls = CallsIn(invocation).Where(_ => InvocationChain.NameOf(_) == ConstraintReader.On).ToArray();
        var targets = calls.Select(call => Property(call, model)).ToArray();

        return targets.Length == 0 || targets.Any(_ => _ is null) ? null : targets.OfType<UniquePropertyConstraintModel>().ToArray();
    }

    /// <summary>
    /// Reads every event releasing a uniqueness declaration.
    /// </summary>
    /// <param name="invocation">The uniqueness declaration.</param>
    /// <param name="model">The semantic model owning the declaration.</param>
    /// <returns>Release event names, or null if any release is unreadable.</returns>
    internal static IReadOnlyList<string>? Releases(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (HasUnprovenEventReleases(invocation, model))
        {
            return null;
        }

        var releases = ReleaseCalls(invocation, model).Select(call => Release(call, model)).ToArray();

        return releases.Any(_ => _ is null) ? null : releases.OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
    }

    static bool HasUnprovenEventReleases(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (InvocationChain.TypeArgumentOf(invocation) is null)
        {
            return false;
        }

        var declaration = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        if (declaration is null)
        {
            return false;
        }

        var builder = Receiver(invocation, model);
        var releases = CallsIn(declaration).SkipWhile(_ => _ != invocation).Skip(1)
            .Where(call => IsReleaseOn(call, model, builder));

        return releases.Any() && !HasReadableDefinition(declaration, model);
    }

    static bool IsReleaseOn(InvocationExpressionSyntax invocation, SemanticModel model, ISymbol? builder) =>
        InvocationChain.NameOf(invocation) == RemovedWith && SymbolEqualityComparer.Default.Equals(Receiver(invocation, model), builder);

    /// <summary>
    /// Proves the most-recent-claim association using only unconditional direct builder statements.
    /// </summary>
    /// <param name="declaration">The constraint definition.</param>
    /// <param name="model">The semantic model owning the definition.</param>
    /// <returns>Whether every call is unconditionally applied to its stated builder.</returns>
    /// <remarks>
    /// Conditional calls, deferred callbacks, reassigned builders and helpers can change which claim is released.
    /// </remarks>
    static bool HasReadableDefinition(MethodDeclarationSyntax declaration, SemanticModel model)
    {
        if (declaration.Body is { } body && body.Statements.Any(_ => _ is not ExpressionStatementSyntax { Expression: InvocationExpressionSyntax }))
        {
            return false;
        }

        if (declaration.DescendantNodes().Any(_ => _ is LambdaExpressionSyntax or AssignmentExpressionSyntax))
        {
            return false;
        }

        return CallsIn(declaration).All(call => IsKnownCall(call, model));
    }

    static bool HasReadableCallback(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (InvocationChain.ArgumentOf(invocation) is not LambdaExpressionSyntax callback)
        {
            return false;
        }

        if (callback.Body is BlockSyntax block && block.Statements.Any(_ => _ is not ExpressionStatementSyntax))
        {
            return false;
        }

        return CallsIn(invocation).All(call => IsKnownCall(call, model));
    }

    static bool IsKnownCall(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (model.GetConstantValue(invocation).HasValue)
        {
            return true;
        }

        return model.GetSymbolInfo(invocation).Symbol is IMethodSymbol method &&
            method.ContainingNamespace.ToDisplayString() == "Cratis.Chronicle.Events.Constraints";
    }

    static IEnumerable<InvocationExpressionSyntax> CallsIn(InvocationExpressionSyntax invocation) =>
        invocation.DescendantNodes().OfType<InvocationExpressionSyntax>().OrderBy(_ => _.Span.End);

    static IEnumerable<InvocationExpressionSyntax> ReleaseCalls(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        if (InvocationChain.TypeArgumentOf(invocation) is null)
        {
            return CallsIn(invocation).Where(_ => InvocationChain.NameOf(_) == RemovedWith);
        }

        var declaration = invocation.Ancestors().OfType<MethodDeclarationSyntax>().FirstOrDefault();
        var calls = declaration is null ? [invocation] : CallsIn(declaration);
        var builder = Receiver(invocation, model);

        return calls.Where(call => SymbolEqualityComparer.Default.Equals(Receiver(call, model), builder))
            .SkipWhile(_ => _ != invocation).Skip(1)
            .TakeWhile(_ => !IsUniqueEvent(_))
            .Where(_ => InvocationChain.NameOf(_) == RemovedWith);
    }

    static IEnumerable<InvocationExpressionSyntax> CallsIn(MethodDeclarationSyntax declaration) =>
        declaration.DescendantNodes().OfType<InvocationExpressionSyntax>().OrderBy(_ => _.Span.End);

    static bool IsUniqueEvent(InvocationExpressionSyntax invocation) =>
        InvocationChain.NameOf(invocation) == ConstraintReader.Unique && InvocationChain.TypeArgumentOf(invocation) is not null;

    static ISymbol? Receiver(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var root = InvocationChain.Sequence(invocation)[0];

        return root.Expression is MemberAccessExpressionSyntax member ? model.GetSymbolInfo(member.Expression).Symbol : null;
    }

    static ITypeSymbol? EventType(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var type = InvocationChain.TypeArgumentOf(invocation);
        var resolved = type is null ? null : model.GetTypeInfo(type).Type;

        return resolved?.TypeKind == TypeKind.Error ? null : resolved;
    }

    static string? Release(InvocationExpressionSyntax invocation, SemanticModel model) =>
        invocation.ArgumentList.Arguments.Count == 0 ? EventType(invocation, model)?.Name : null;

    static UniquePropertyConstraintModel? Property(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var eventType = EventType(invocation, model);
        var properties = invocation.ArgumentList.Arguments.Select(_ => LambdaPaths.Read(_.Expression)).ToArray();
        if (eventType is null || properties.Length == 0)
        {
            return null;
        }

        if (properties.Any(_ => _?.Contains('.', StringComparison.Ordinal) != false))
        {
            return null;
        }

        return new(string.Empty, properties[0]!, eventType.Name) { AdditionalProperties = properties.Skip(1).OfType<string>().ToArray() };
    }
}
