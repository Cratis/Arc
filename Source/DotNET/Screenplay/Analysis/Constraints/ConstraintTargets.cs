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
    /// <returns>All targets, or null if any target or retention mode is unreadable.</returns>
    internal static IReadOnlyList<UniquePropertyConstraintModel>? Properties(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var calls = ConstraintBuilderCalls.Callback(invocation, model);
        if (calls is null || !HasDefaultRetention(calls, model))
        {
            return null;
        }

        var targets = calls.Where(_ => InvocationChain.NameOf(_) == ConstraintReader.On).Select(call => Property(call, model)).ToArray();

        return targets.Length == 0 || targets.Any(_ => _ is null) ? null : targets.OfType<UniquePropertyConstraintModel>().ToArray();
    }

    /// <summary>
    /// Reads every event releasing a uniqueness declaration, only after proving its builder's registration flow.
    /// </summary>
    /// <param name="invocation">The uniqueness declaration.</param>
    /// <param name="model">The semantic model owning the declaration.</param>
    /// <returns>Release event names, or null if builder identity or any release is unreadable.</returns>
    internal static IReadOnlyList<string>? Releases(InvocationExpressionSyntax invocation, SemanticModel model)
    {
        var definition = ConstraintBuilderCalls.Definition(invocation, model);
        if (definition is null)
        {
            return null;
        }

        var calls = InvocationChain.TypeArgumentOf(invocation) is null
            ? ConstraintBuilderCalls.Callback(invocation, model)!
            : definition.SkipWhile(_ => _ != invocation).Skip(1).TakeWhile(_ => !IsUniqueEvent(_));
        var releases = calls.Where(_ => InvocationChain.NameOf(_) == RemovedWith).Select(call => Release(call, model)).ToArray();

        return releases.Any(_ => _ is null) ? null : releases.OfType<string>().Distinct(StringComparer.Ordinal).ToArray();
    }

    static bool HasDefaultRetention(IEnumerable<InvocationExpressionSyntax> calls, SemanticModel model) =>
        calls.Where(_ => InvocationChain.NameOf(_) == "WithMode")
            .All(call => model.GetConstantValue(InvocationChain.ArgumentOf(call)!).Value is 0);

    static bool IsUniqueEvent(InvocationExpressionSyntax invocation) =>
        InvocationChain.NameOf(invocation) == ConstraintReader.Unique && InvocationChain.TypeArgumentOf(invocation) is not null;

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
