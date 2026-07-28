// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Resolves the type of a property, a parameter or a return value, collecting the concepts encountered along the way.
/// </summary>
/// <remarks>
/// A concept is declared once at the top of the document and referenced by name from there on, so every concept an
/// artifact refers to has to be registered while its type is resolved. Only concepts that are actually referenced
/// are declared, which keeps the document to what the application uses.
/// </remarks>
public class TypeRegistry
{
    readonly Dictionary<string, ConceptModel> _concepts = new(StringComparer.Ordinal);
    readonly Dictionary<string, List<ValidationRuleModel>> _validations = new(StringComparer.Ordinal);
    readonly HashSet<string> _pii = new(StringComparer.Ordinal);
    readonly HashSet<string> _unmappable = new(StringComparer.Ordinal);
    readonly HashSet<string> _ambiguous = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the full name of every type that had to be referred to by a name that does not say what it is.
    /// </summary>
    public IEnumerable<string> Unmappable => _unmappable.Order(StringComparer.Ordinal);

    /// <summary>
    /// Gets the full name of every type whose simple name a concept was already declared under.
    /// </summary>
    public IEnumerable<string> Ambiguous => _ambiguous.Order(StringComparer.Ordinal);

    /// <summary>
    /// Gets every concept referenced by the application, ordered by name.
    /// </summary>
    public IEnumerable<ConceptModel> Concepts =>
    [
        .. _concepts.Values
            .Select(_ => _ with
            {
                IsPii = _.IsPii || _pii.Contains(_.Name),
                Validations = _validations.TryGetValue(_.Name, out var rules) ? rules : []
            })
            .OrderBy(_ => _.Name, StringComparer.Ordinal)
    ];

    /// <summary>
    /// Resolves the Screenplay type reference a symbol corresponds to.
    /// </summary>
    /// <param name="type">The type to resolve.</param>
    /// <returns>The <see cref="TypeReferenceModel"/>.</returns>
    public TypeReferenceModel Resolve(ITypeSymbol type)
    {
        var optional = false;
        var collection = false;

        return new(NameOf(Underlying(type, ref optional, ref collection)), collection, optional);
    }

    /// <summary>
    /// Records that a value of a concept carries personally identifiable information.
    /// </summary>
    /// <param name="type">The type of the value.</param>
    /// <remarks>
    /// The concept a value is marked under has to be the one it is referenced under, or the mark lands on a name no
    /// concept is declared with and the document says a value is not sensitive while the runtime encrypts it. Both
    /// therefore strip the same wrappers - a collection of an optional concept says one thing about the value and
    /// three things about how many there are and whether it may be absent.
    /// </remarks>
    public void MarkAsPii(ITypeSymbol type)
    {
        var optional = false;
        var collection = false;

        _pii.Add(Underlying(type, ref optional, ref collection).Name);
    }

    /// <summary>
    /// Records the validation rules a concept declares for itself.
    /// </summary>
    /// <param name="conceptName">The name of the concept.</param>
    /// <param name="rules">The rules to record.</param>
    public void AddValidations(string conceptName, IEnumerable<ValidationRuleModel> rules)
    {
        if (!_validations.TryGetValue(conceptName, out var declared))
        {
            declared = [];
            _validations[conceptName] = declared;
        }

        declared.AddRange(rules);
    }

    /// <summary>
    /// Strips everything a value is wrapped in that says how many there are or whether it may be absent.
    /// </summary>
    /// <param name="type">The type to strip.</param>
    /// <param name="optional">Set when a wrapper said the value may be absent.</param>
    /// <param name="collection">Set when the value is a collection of what is left.</param>
    /// <returns>The type of the value itself.</returns>
    static ITypeSymbol Underlying(ITypeSymbol type, ref bool optional, ref bool collection)
    {
        var current = Unwrap(type, ref optional);
        var element = CollectionElements.ElementOf(current);
        if (element is null)
        {
            return current;
        }

        collection = true;

        return Unwrap(element, ref optional);
    }

    /// <summary>
    /// Strips the wrappers that only say whether a value may be absent.
    /// </summary>
    /// <param name="type">The type to strip.</param>
    /// <param name="optional">Set when a wrapper said the value may be absent.</param>
    /// <returns>The wrapped type.</returns>
    static ITypeSymbol Unwrap(ITypeSymbol type, ref bool optional)
    {
        if (type is INamedTypeSymbol { OriginalDefinition.SpecialType: SpecialType.System_Nullable_T } nullable)
        {
            optional = true;

            return nullable.TypeArguments[0];
        }

        if (type.NullableAnnotation == NullableAnnotation.Annotated && type.IsReferenceType)
        {
            optional = true;
        }

        return type;
    }

    /// <summary>
    /// Gets the values of an enumeration, in declaration order.
    /// </summary>
    /// <param name="type">The enumeration to read.</param>
    /// <returns>The value names.</returns>
    static IEnumerable<string> ValuesOf(ITypeSymbol type) =>
        [.. type.GetMembers().OfType<IFieldSymbol>().Where(_ => _.HasConstantValue).Select(_ => _.Name)];

    /// <summary>
    /// Resolves the name a type is referenced by, registering it as a concept when it is one.
    /// </summary>
    /// <param name="type">The type to name.</param>
    /// <returns>The Screenplay type name.</returns>
    string NameOf(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol named && ScreenplayPrimitiveTypes.TryResolve(named.FullMetadataName(), out var primitive))
        {
            return ScreenplayPrimitiveTypes.GetName(primitive);
        }

        if (type.TypeKind == TypeKind.Enum)
        {
            Register(type, new(type.Name, ScreenplayPrimitive.Enum, false, ValuesOf(type), []));

            return type.Name;
        }

        if (type.FindBase(WellKnownTypeNames.ConceptAs) is { } concept)
        {
            Register(type, ToConcept(type, concept.TypeArguments[0]));

            return type.Name;
        }

        ReportWhatTheNameLoses(type);

        return type.Name;
    }

    /// <summary>
    /// Records a type whose simple name says less than the type does.
    /// </summary>
    /// <param name="type">The type being named.</param>
    /// <remarks>
    /// A read model or a nested object referred to by its own name is exactly right. A constructed generic is not -
    /// writing <c>IDictionary&lt;string, string&gt;</c> as the single identifier the grammar allows leaves the word
    /// <c>KeyValuePair</c> behind, which says nothing and which the document never declares. Same for a type
    /// parameter, whose name is a placeholder rather than a type.
    /// </remarks>
    void ReportWhatTheNameLoses(ITypeSymbol type)
    {
        if (type is INamedTypeSymbol { TypeArguments.Length: > 0 } or { TypeKind: TypeKind.TypeParameter })
        {
            _unmappable.Add(type.ToDisplayString());
        }
    }

    /// <summary>
    /// Builds the concept a type backed by <c>ConceptAs</c> declares.
    /// </summary>
    /// <param name="type">The concept type.</param>
    /// <param name="backing">The type the concept is backed by.</param>
    /// <returns>The <see cref="ConceptModel"/>.</returns>
    ConceptModel ToConcept(ITypeSymbol type, ITypeSymbol backing)
    {
        var pii = type.HasAttribute(WellKnownTypeNames.PiiAttribute);

        if (backing.TypeKind == TypeKind.Enum)
        {
            return new(type.Name, ScreenplayPrimitive.Enum, pii, ValuesOf(backing), []);
        }

        var resolved = backing is INamedTypeSymbol named && ScreenplayPrimitiveTypes.TryResolve(named.FullMetadataName(), out var primitive)
            ? primitive
            : ScreenplayPrimitive.String;

        return new(type.Name, resolved, pii, [], []);
    }

    /// <summary>
    /// Registers a concept, keeping the first declaration of a given name.
    /// </summary>
    /// <param name="type">The type the concept was read from.</param>
    /// <param name="concept">The concept to register.</param>
    /// <remarks>
    /// A concept is declared once at the top of the document and referenced by its simple name, so two types sharing
    /// that name cannot both be described. Keeping the first is the only choice left, and saying so is what stops the
    /// document from quietly claiming the second one is something it is not.
    /// </remarks>
    void Register(ITypeSymbol type, ConceptModel concept)
    {
        if (!_concepts.TryGetValue(concept.Name, out var existing))
        {
            _concepts[concept.Name] = concept;

            return;
        }

        if (existing.Primitive != concept.Primitive || !existing.EnumValues.SequenceEqual(concept.EnumValues, StringComparer.Ordinal))
        {
            _ambiguous.Add(type.ToDisplayString());
        }
    }
}
