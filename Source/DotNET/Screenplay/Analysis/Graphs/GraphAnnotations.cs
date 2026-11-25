// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Projections;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Graphs;

/// <summary>Limits recovery to the model-bound graph contract Chronicle actually executes.</summary>
public static class GraphAnnotations
{
    static readonly string[] _mappings =
    [
        ModelBoundNames.SetFrom, ModelBoundNames.SetFromContext, ModelBoundNames.SetValue,
        ModelBoundNames.Increment, ModelBoundNames.Decrement, ModelBoundNames.Count,
        ModelBoundNames.AddFrom, ModelBoundNames.SubtractFrom
    ];

    /// <summary>Determines whether an annotation declares an element handler.</summary>
    /// <param name="attribute">The annotation.</param>
    /// <returns>Whether it is a supported handler.</returns>
    public static bool IsHandler(AttributeData attribute) =>
        attribute.AttributeClass.Is(ModelBoundNames.FromEvent) || attribute.AttributeClass.Is(ModelBoundNames.RemovedWith) ||
        attribute.AttributeClass.Is(GraphNames.EdgeFrom) || attribute.AttributeClass.Is(GraphNames.EdgeRemoved);

    /// <summary>Determines whether an annotation declares an event-specific property mapping.</summary>
    /// <param name="attribute">The annotation.</param>
    /// <returns>Whether it is a supported mapping.</returns>
    public static bool IsMapping(AttributeData attribute) => _mappings.Contains(attribute.AttributeClass?.FullMetadataName(), StringComparer.Ordinal);

    /// <summary>Checks for custom code and unsupported model-bound graph behavior.</summary>
    /// <param name="type">The graph element.</param>
    /// <returns>Whether all of its graph behavior can be recovered.</returns>
    public static bool Supported(INamedTypeSymbol type) =>
        !type.GetMembers().OfType<IMethodSymbol>().Any(method => !method.IsImplicitlyDeclared && method.MethodKind == MethodKind.Ordinary) &&
        type.GetAttributes().All(attribute => !ProjectionAnnotation(attribute) || IsHandler(attribute)) &&
        type.DeclaredProperties().SelectMany(MemberAttributes.Of).All(attribute => !ProjectionAnnotation(attribute) || IsMapping(attribute)) &&
        type.GetAttributes().Where(IsHandler).All(ValidHandler);

    static bool ProjectionAnnotation(AttributeData attribute) => attribute.AttributeClass?.ContainingNamespace.ToDisplayString() == ModelBoundNames.Namespace;

    static bool ValidHandler(AttributeData attribute) =>
        attribute.AttributeClass?.TypeArguments.FirstOrDefault() is INamedTypeSymbol { TypeKind: not TypeKind.Error } &&
        (attribute.GetArgument(1) is null || attribute.AttributeClass.Is(GraphNames.EdgeFrom) || attribute.AttributeClass.Is(GraphNames.EdgeRemoved));
}
