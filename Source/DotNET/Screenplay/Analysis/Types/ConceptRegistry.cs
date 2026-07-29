// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Collects the concepts an application refers to, keeping one declaration per name.
/// </summary>
/// <remarks>
/// A concept is declared once at the top of the document and referenced by its simple name from there on, so which
/// concepts a document declares has nothing to do with which ones the application defines and everything to do with
/// which ones were reached while a type was being resolved. They are gathered as they are encountered rather than
/// found up front, which keeps the document to what the application actually uses.
/// <para>
/// What a concept says about itself arrives from more than one place and at different moments - the values of an
/// enumeration come from the type, the mark saying it carries personal data comes from the property referring to it,
/// and the rules it holds its own value to come from a validator read later still. Only the declaration is kept as
/// the concept; the rest is kept beside it and folded in when the concepts are read back, so that a mark or a rule
/// arriving after the concept was first seen still lands on it.
/// </para>
/// </remarks>
public class ConceptRegistry
{
    readonly Dictionary<string, ConceptModel> _concepts = new(StringComparer.Ordinal);
    readonly Dictionary<string, List<ValidationRuleModel>> _validations = new(StringComparer.Ordinal);
    readonly HashSet<string> _pii = new(StringComparer.Ordinal);
    readonly HashSet<string> _ambiguous = new(StringComparer.Ordinal);

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
    /// Registers a type as a concept when it is one.
    /// </summary>
    /// <param name="type">The type to register.</param>
    /// <returns>True when the type is a concept and was registered.</returns>
    /// <remarks>
    /// An enumeration and a type backed by <c>ConceptAs</c> are both one value with a name, which is what a concept
    /// is, and both are therefore declared. Anything else is a type referred to by name and never declared, which is
    /// what the false answer says.
    /// </remarks>
    public bool TryRegister(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum)
        {
            Register(type, new(type.Name, ScreenplayPrimitive.Enum, false, ValuesOf(type), []));

            return true;
        }

        if (type.FindBase(WellKnownTypeNames.ConceptAs) is { } concept)
        {
            Register(type, ToConcept(type, concept.TypeArguments[0]));

            return true;
        }

        return false;
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
    public void MarkAsPii(ITypeSymbol type) => _pii.Add(UnderlyingTypes.Of(type).Name);

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
    /// Gets the values of an enumeration, in declaration order.
    /// </summary>
    /// <param name="type">The enumeration to read.</param>
    /// <returns>The value names.</returns>
    static IEnumerable<string> ValuesOf(ITypeSymbol type) =>
        [.. type.GetMembers().OfType<IFieldSymbol>().Where(_ => _.HasConstantValue).Select(_ => _.Name)];

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
