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
    readonly HashSet<string> _partialSecrets = new(StringComparer.Ordinal);
    readonly HashSet<string> _ambiguous = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the types with encryption or audit suppression alone.
    /// </summary>
    public IEnumerable<string> PartialSecrets => _partialSecrets
        .Where(name => !_concepts.TryGetValue(name, out var concept) || !concept.IsSensitive)
        .Order(StringComparer.Ordinal);

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
    /// Gets the name every concept is declared under.
    /// </summary>
    /// <remarks>
    /// A concept and a shape are declared side by side at the top of the document and referred to the same way, so
    /// the two cannot share a name - which is a question only something holding both can answer.
    /// </remarks>
    public IReadOnlySet<string> Names => _concepts.Keys.ToHashSet(StringComparer.Ordinal);

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
        if (ModelOf(type) is not { } concept)
        {
            return false;
        }

        Register(type, concept);

        return true;
    }

    /// <summary>
    /// Determines whether registering a type would declare it as the concept it is.
    /// </summary>
    /// <param name="type">The type to ask about.</param>
    /// <returns>True when it is a concept whose simple name is free or already declared with the same meaning.</returns>
    /// <remarks>
    /// Two concepts sharing a simple name are declared once, so the later one is only faithfully named when the
    /// declaration already written says the same about it - the same primitive and the same values.
    /// </remarks>
    public bool WouldResolveTo(ITypeSymbol type) =>
        ModelOf(type) is { } concept &&
        (!_concepts.TryGetValue(concept.Name, out var existing) || IsSameAs(existing, concept));

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
    /// Records member markings that cannot establish a concept-wide secret contract.
    /// </summary>
    /// <param name="type">The type of the value.</param>
    /// <param name="encrypted">Whether the value is encrypted.</param>
    /// <param name="notAudited">Whether the value is withheld from auditing.</param>
    public void MarkSecret(ITypeSymbol type, bool encrypted, bool notAudited)
    {
        var carried = UnderlyingTypes.Of(type);
        if ((encrypted || notAudited) &&
            (carried.TypeKind == TypeKind.Enum || carried.FindBase(WellKnownTypeNames.ConceptAs) is not null))
        {
            _partialSecrets.Add(carried.Name);
        }
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
    /// Determines whether two concepts sharing a name are declared the same way.
    /// </summary>
    /// <param name="existing">The concept already declared.</param>
    /// <param name="concept">The concept arriving under the same name.</param>
    /// <returns>True when one declaration says what both are.</returns>
    static bool IsSameAs(ConceptModel existing, ConceptModel concept) =>
        existing.Primitive == concept.Primitive && existing.EnumValues.SequenceEqual(concept.EnumValues, StringComparer.Ordinal);

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
        var encrypted = type.HasAttribute(WellKnownTypeNames.EncryptedAttribute);
        var notAudited = type.HasAttribute(WellKnownTypeNames.NotAuditedAttribute);
        MarkSecret(type, encrypted, notAudited);

        if (backing.TypeKind == TypeKind.Enum)
        {
            return new(type.Name, ScreenplayPrimitive.Enum, pii, ValuesOf(backing), []) { IsSensitive = encrypted && notAudited };
        }

        var resolved = backing is INamedTypeSymbol named && ScreenplayPrimitiveTypes.TryResolve(named.FullMetadataName(), out var primitive)
            ? primitive
            : ScreenplayPrimitive.String;

        return new(type.Name, resolved, pii, [], []) { IsSensitive = encrypted && notAudited };
    }

    /// <summary>
    /// Gets the concept a type is declared as.
    /// </summary>
    /// <param name="type">The type.</param>
    /// <returns>The <see cref="ConceptModel"/>, or <see langword="null"/> when the type is not a concept.</returns>
    ConceptModel? ModelOf(ITypeSymbol type)
    {
        if (type.TypeKind == TypeKind.Enum)
        {
            return new(type.Name, ScreenplayPrimitive.Enum, false, ValuesOf(type), []);
        }

        return type.FindBase(WellKnownTypeNames.ConceptAs) is { } concept ? ToConcept(type, concept.TypeArguments[0]) : null;
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

        if (!IsSameAs(existing, concept))
        {
            _ambiguous.Add(type.ToDisplayString());
        }
    }
}
