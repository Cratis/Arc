// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Projections;

/// <summary>
/// Reads the mappings the properties of a model-bound read model declare, grouped by the event each one observes.
/// </summary>
/// <remarks>
/// A model-bound projection is declared upside down compared to a fluent one - the read model's properties say
/// which event they come from, rather than the event saying which properties it fills in. Regrouping them by event
/// is what turns the declaration back into the blocks a document is written in.
/// </remarks>
public class ModelBoundPropertyMappings
{
    readonly Dictionary<string, Dictionary<string, string>> _byEvent = new(StringComparer.Ordinal);
    readonly Dictionary<string, string> _every = new(StringComparer.Ordinal);
    readonly List<ProjectionJoinModel> _joins = [];

    /// <summary>
    /// Gets the mappings applying to every observed event.
    /// </summary>
    public IReadOnlyDictionary<string, string> Every => _every;

    /// <summary>
    /// Gets the joins the read model declares.
    /// </summary>
    public IEnumerable<ProjectionJoinModel> Joins => _joins;

    /// <summary>
    /// Reads the mappings of a read model.
    /// </summary>
    /// <param name="readModel">The read model to read.</param>
    /// <returns>The <see cref="ModelBoundPropertyMappings"/>.</returns>
    public static ModelBoundPropertyMappings From(ITypeSymbol readModel)
    {
        var mappings = new ModelBoundPropertyMappings();

        foreach (var property in readModel.DeclaredProperties())
        {
            mappings.ReadProperty(property);
        }

        return mappings;
    }

    /// <summary>
    /// Gets the mappings declared for an event type.
    /// </summary>
    /// <param name="eventTypeName">The name of the event type.</param>
    /// <returns>The mappings, ordered by property.</returns>
    public IReadOnlyDictionary<string, string> For(string eventTypeName) =>
        _byEvent.TryGetValue(eventTypeName, out var mappings) ? mappings : new Dictionary<string, string>(StringComparer.Ordinal);

    /// <summary>
    /// Gets the event types any property names.
    /// </summary>
    /// <returns>The names, ordered.</returns>
    public IEnumerable<string> EventTypes() => _byEvent.Keys.Order(StringComparer.Ordinal);

    /// <summary>
    /// Builds the expression an attribute maps a property with.
    /// </summary>
    /// <param name="name">The fully qualified metadata name of the attribute.</param>
    /// <param name="attribute">The attribute to read.</param>
    /// <param name="property">The property being mapped onto.</param>
    /// <returns>The expression, or <see langword="null"/> when the attribute is not a mapping.</returns>
    static string? ExpressionOf(string name, AttributeData attribute, IPropertySymbol property) => name switch
    {
        ModelBoundNames.SetFrom => Source(attribute, "EventPropertyName", property),
        ModelBoundNames.SetValue => ProjectionExpressions.Value(attribute.GetArgument(0)),
        ModelBoundNames.SetFromContext => ProjectionExpressions.EventContext(Source(attribute, "ContextPropertyName", property)),
        ModelBoundNames.Count => ProjectionExpressions.Count,
        ModelBoundNames.Increment => ProjectionExpressions.Increment,
        ModelBoundNames.Decrement => ProjectionExpressions.Decrement,
        ModelBoundNames.AddFrom => ProjectionExpressions.Add(Source(attribute, "EventPropertyName", property)),
        ModelBoundNames.SubtractFrom => ProjectionExpressions.Subtract(Source(attribute, "EventPropertyName", property)),
        ModelBoundNames.FromEvery or ModelBoundNames.FromAll => Ambient(attribute, property),
        _ => null
    };

    /// <summary>
    /// Builds the expression an attribute applying to every observed event maps a property with.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <param name="property">The property being mapped onto.</param>
    /// <returns>The expression.</returns>
    static string Ambient(AttributeData attribute, IPropertySymbol property)
    {
        var context = ModelBoundAttributes.Path(attribute, "ContextProperty", 1);

        return context is null
            ? ModelBoundAttributes.Path(attribute, "Property", 0) ?? Named(property)
            : ProjectionExpressions.EventContext(context);
    }

    /// <summary>
    /// Gets the source an attribute names, falling back to the property it is applied to.
    /// </summary>
    /// <param name="attribute">The attribute to read.</param>
    /// <param name="argument">The name of the argument carrying the source.</param>
    /// <param name="property">The property being mapped onto.</param>
    /// <returns>The source, in the casing a projection body references it by.</returns>
    static string Source(AttributeData attribute, string argument, IPropertySymbol property) =>
        ModelBoundAttributes.Path(attribute, argument, 0) ?? Named(property);

    /// <summary>
    /// Gets the name of a property in the casing a projection body references it by.
    /// </summary>
    /// <param name="property">The property to name.</param>
    /// <returns>The name.</returns>
    static string Named(IPropertySymbol property) => ProjectionPaths.Convert(property.Name) ?? property.Name;

    /// <summary>
    /// Reads the mappings one property declares.
    /// </summary>
    /// <param name="property">The property to read.</param>
    void ReadProperty(IPropertySymbol property)
    {
        foreach (var attribute in ModelBoundMembers.AttributesOf(property))
        {
            var name = attribute.AttributeClass is null ? string.Empty : attribute.AttributeClass.FullMetadataName();
            var expression = ExpressionOf(name, attribute, property);

            if (expression is not null && ModelBoundAttributes.EventTypeOf(attribute) is { } eventType)
            {
                Add(eventType, property.Name, expression);
            }
            else if (expression is not null)
            {
                _every[property.Name] = expression;
            }
            else if (string.Equals(name, ModelBoundNames.Join, StringComparison.Ordinal))
            {
                ReadJoin(attribute, property);
            }
        }
    }

    /// <summary>
    /// Reads the join a property declares.
    /// </summary>
    /// <param name="attribute">The attribute declaring the join.</param>
    /// <param name="property">The property holding the joined data.</param>
    void ReadJoin(AttributeData attribute, IPropertySymbol property)
    {
        var on = ModelBoundAttributes.Argument(attribute, "On", 0) ?? string.Empty;
        var source = ModelBoundAttributes.Path(attribute, "EventPropertyName", 1) ?? Named(property);

        _joins.Add(new(
            property.Name,
            ModelBoundAttributes.EventTypeOf(attribute) ?? string.Empty,
            on,
            new Dictionary<string, string>(StringComparer.Ordinal) { [property.Name] = source }));
    }

    /// <summary>
    /// Adds a mapping for an event type.
    /// </summary>
    /// <param name="eventType">The name of the event type.</param>
    /// <param name="property">The property being mapped onto.</param>
    /// <param name="expression">The expression mapping it.</param>
    void Add(string eventType, string property, string expression)
    {
        if (!_byEvent.TryGetValue(eventType, out var mappings))
        {
            mappings = new(StringComparer.Ordinal);
            _byEvent[eventType] = mappings;
        }

        mappings[property] = expression;
    }
}
