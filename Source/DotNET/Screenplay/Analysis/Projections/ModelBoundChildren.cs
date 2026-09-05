// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads the child collections and nested objects a model-bound read model declares with attributes.
/// </summary>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <remarks>
/// The property says where the instances live and the type of the instances says what fills them in, so a child is
/// only half declared where it is written - the other half is read from the type it holds.
/// </remarks>
public class ModelBoundChildren(ScreenplayDiagnostics diagnostics)
{
    /// <summary>
    /// The name of the property a read model is identified by unless it says otherwise.
    /// </summary>
    public const string ConventionalKey = "Id";

    /// <summary>
    /// Reads the child collections a read model declares.
    /// </summary>
    /// <param name="readModel">The read model to read.</param>
    /// <param name="location">Where the read model lives, for use in diagnostics.</param>
    /// <returns>The children, ordered by the property holding them.</returns>
    public IEnumerable<ModelBoundChild> In(ITypeSymbol readModel, string location)
    {
        var children = new List<ModelBoundChild>();

        foreach (var property in readModel.DeclaredProperties())
        {
            var declarations = Declarations(property, ModelBoundNames.ChildrenFrom);
            if (declarations.Count == 0)
            {
                continue;
            }

            if (CollectionElements.ElementOf(property.Type) is not INamedTypeSymbol child)
            {
                Report(property, "children", location);
                continue;
            }

            ReportRemovals(property, location);

            children.Add(new(
                property.Name,
                child,
                IdentifiedByOf(declarations[0], child),
                AutoMapOf(child),
                [.. declarations.Select(_ => FromOf(_, readModel)).OrderBy(_ => _.EventTypes.First(), StringComparer.Ordinal)]));
        }

        return [.. children.OrderBy(_ => _.Property, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Reads the nested objects a read model declares.
    /// </summary>
    /// <param name="readModel">The read model to read.</param>
    /// <param name="location">Where the read model lives, for use in diagnostics.</param>
    /// <returns>The nested objects, ordered by the property holding them.</returns>
    /// <remarks>
    /// A nested object carries no key of its own - there is only ever one of it - and the events filling it in are
    /// declared by its own type rather than by the property, so the attribute itself carries nothing to read.
    /// </remarks>
    public IEnumerable<ModelBoundChild> NestedIn(ITypeSymbol readModel, string location)
    {
        var nested = new List<ModelBoundChild>();

        foreach (var property in readModel.DeclaredProperties().Where(_ => MemberAttributes.Has(_, ModelBoundNames.Nested)))
        {
            if (UnderlyingOf(property.Type) is not { } type)
            {
                Report(property, "nested object", location);
                continue;
            }

            nested.Add(new(property.Name, type, string.Empty, AutoMapOf(type), []));
        }

        return [.. nested.OrderBy(_ => _.Property, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Gets the attributes of a given type declaring something about a member.
    /// </summary>
    /// <param name="property">The property to read.</param>
    /// <param name="fullMetadataName">The fully qualified metadata name of the attribute.</param>
    /// <returns>The attributes, in declaration order.</returns>
    static List<AttributeData> Declarations(IPropertySymbol property, string fullMetadataName) =>
        [.. MemberAttributes.Of(property).Where(_ => _.AttributeClass.Is(fullMetadataName))];

    /// <summary>
    /// Builds the block bringing an instance of a child into being.
    /// </summary>
    /// <param name="attribute">The attribute declaring the child.</param>
    /// <param name="readModel">The read model holding the child.</param>
    /// <returns>The <see cref="ProjectionFromModel"/>.</returns>
    static ProjectionFromModel FromOf(AttributeData attribute, ITypeSymbol readModel) =>
        new(
            [ModelBoundAttributes.EventTypeOf(attribute) ?? string.Empty],
            ModelBoundAttributes.Path(attribute, "Key", 0),
            ModelBoundParentKeys.Of(attribute, readModel),
            new Dictionary<string, string>(StringComparer.Ordinal));

    /// <summary>
    /// Gets the expression identifying each instance of a child collection.
    /// </summary>
    /// <param name="attribute">The attribute declaring the child.</param>
    /// <param name="child">The type each instance is of.</param>
    /// <returns>The expression.</returns>
    /// <remarks>
    /// A declaration that names nothing falls back to what the type itself says - the property marked as its key,
    /// then one named by convention - and finally to the event source, which is what a child with no identity of
    /// its own is distinguished by.
    /// </remarks>
    static string IdentifiedByOf(AttributeData attribute, ITypeSymbol child) =>
        ModelBoundAttributes.Path(attribute, "IdentifiedBy", 1) ??
        ProjectionPaths.Convert(KeyPropertyOf(child)) ??
        ProjectionExpressions.EventSourceId;

    /// <summary>
    /// Gets the property a type says identifies it.
    /// </summary>
    /// <param name="type">The type to read.</param>
    /// <returns>The name, or <see langword="null"/> when the type names none.</returns>
    static string? KeyPropertyOf(ITypeSymbol type) =>
        type.DeclaredProperties().FirstOrDefault(_ => MemberAttributes.Has(_, ModelBoundNames.Key))?.Name ??
        type.DeclaredProperties().FirstOrDefault(_ => string.Equals(_.Name, ConventionalKey, StringComparison.OrdinalIgnoreCase))?.Name;

    /// <summary>
    /// Gets how automatic property mapping applies within a scope.
    /// </summary>
    /// <param name="type">The type the scope builds.</param>
    /// <returns>The mode, inherited from the enclosing scope unless the type turns mapping off.</returns>
    static ProjectionAutoMapMode AutoMapOf(ITypeSymbol type) =>
        type.HasAttribute(ModelBoundNames.NoAutoMap) ? ProjectionAutoMapMode.Disabled : ProjectionAutoMapMode.Inherit;

    /// <summary>
    /// Gets the type a nested object is of, seeing through the nullability every one of them is declared with.
    /// </summary>
    /// <param name="type">The type of the property.</param>
    /// <returns>The type, or <see langword="null"/> when it is not one a scope can be read from.</returns>
    static INamedTypeSymbol? UnderlyingOf(ITypeSymbol type) =>
        type is not INamedTypeSymbol named
            ? null
            : named.OriginalDefinition.SpecialType == SpecialType.System_Nullable_T
                ? named.TypeArguments[0] as INamedTypeSymbol
                : named;

    /// <summary>
    /// Reports a declaration whose property holds nothing a scope can be read from.
    /// </summary>
    /// <param name="property">The property declaring it.</param>
    /// <param name="kind">What the property was declared to hold.</param>
    /// <param name="location">Where the read model lives.</param>
    void Report(IPropertySymbol property, string kind, string location) =>
        diagnostics.Warning(
            ScreenplayDiagnosticCodes.UnmappableProjectionScope,
            $"'{property.Name}' is declared to hold a {kind}, but its type is not one whose contents can be read, so it was left out",
            location);

    /// <summary>
    /// Reports a removal declared beside a child collection rather than by the type of the child.
    /// </summary>
    /// <param name="property">The property holding the children.</param>
    /// <param name="location">Where the read model lives.</param>
    /// <remarks>
    /// What removes an instance is read from the type of the instance, which is where the events filling it in are
    /// read from as well. The same removal written beside the collection instead is a form nothing reads yet, so it
    /// is reported rather than leaving a child collection nothing ever removes anything from.
    /// </remarks>
    void ReportRemovals(IPropertySymbol property, string location)
    {
        foreach (var attribute in MemberAttributes.Of(property)
            .Where(_ => _.AttributeClass.Is(ModelBoundNames.RemovedWith) || _.AttributeClass.Is(ModelBoundNames.RemovedWithJoin)))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnmappableProjectionScope,
                $"'{property.Name}' declares that '{ModelBoundAttributes.EventTypeOf(attribute)}' removes one of its children, which is read back only when the type of the child declares it, so it was left out",
                location);
        }
    }
}
