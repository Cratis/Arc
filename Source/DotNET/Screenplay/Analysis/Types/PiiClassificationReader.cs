// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Model;
using Microsoft.CodeAnalysis;

namespace Cratis.Arc.Screenplay.Analysis.Types;

/// <summary>
/// Reads concept compliance attributes and records classifications Screenplay cannot fully state.
/// </summary>
internal class PiiClassificationReader
{
    static readonly Dictionary<string, string> _specialCategories = new(StringComparer.Ordinal)
    {
        ["RacialOrEthnicOrigin"] = "racialOrEthnicOrigin",
        ["PoliticalOpinions"] = "politicalOpinions",
        ["ReligiousOrPhilosophicalBeliefs"] = "religiousOrPhilosophicalBeliefs",
        ["TradeUnionMembership"] = "tradeUnionMembership",
        ["Genetic"] = "genetic",
        ["Biometric"] = "biometric",
        ["Health"] = "health",
        ["SexLifeOrSexualOrientation"] = "sexLifeOrSexualOrientation"
    };

    readonly HashSet<string> _partialSecrets = new(StringComparer.Ordinal);
    readonly HashSet<string> _legacyPiiEncryption = new(StringComparer.Ordinal);
    readonly HashSet<string> _unknownSpecialCategories = new(StringComparer.Ordinal);

    /// <summary>
    /// Gets the concept names used with encryption or audit suppression alone.
    /// </summary>
    public IEnumerable<string> PartialSecrets => _partialSecrets;

    /// <summary>
    /// Determines whether two concept declarations state the same compliance claims.
    /// </summary>
    /// <param name="existing">The concept already declared.</param>
    /// <param name="concept">The concept arriving under the same name.</param>
    /// <returns>Whether their personal-data and secret classifications agree.</returns>
    public static bool HaveSameClassification(ConceptModel existing, ConceptModel concept) =>
        existing.IsPii == concept.IsPii &&
        existing.IsSensitive == concept.IsSensitive &&
        existing.SpecialCategory == concept.SpecialCategory &&
        existing.IsCriminal == concept.IsCriminal &&
        StatedScopeOf(existing) == StatedScopeOf(concept);

    /// <summary>
    /// Applies the type's personal-data and secret classification to its concept declaration.
    /// </summary>
    /// <param name="type">The concept type.</param>
    /// <param name="concept">The declaration before classification.</param>
    /// <returns>The classified concept.</returns>
    public ConceptModel Read(ITypeSymbol type, ConceptModel concept)
    {
        var personal = type.GetAttributes().FirstOrDefault(attribute => attribute.AttributeClass?.FullMetadataName() == WellKnownTypeNames.PiiAttribute);
        var encrypted = type.HasAttribute(WellKnownTypeNames.EncryptedAttribute) || PersonalFlag(personal, "OperationalSecret");
        var notAudited = type.HasAttribute(WellKnownTypeNames.NotAuditedAttribute);
        MarkSecret(type, encrypted, notAudited);
        RecordLegacyEncryption(type, personal);

        return concept with
        {
            IsPii = personal is not null,
            IsSensitive = encrypted && notAudited,
            EncryptionScope = ScopeOf(type),
            SpecialCategory = SpecialCategoryOf(personal, type),
            IsCriminal = PersonalFlag(personal, "Criminal")
        };
    }

    /// <summary>
    /// Records member markings that cannot establish a concept-wide secret contract.
    /// </summary>
    /// <param name="type">The type of the value.</param>
    /// <param name="encrypted">Whether the value is encrypted.</param>
    /// <param name="notAudited">Whether the value is withheld from auditing.</param>
    public void MarkSecret(ITypeSymbol type, bool encrypted, bool notAudited)
    {
        if (!encrypted && !notAudited)
        {
            return;
        }

        var carried = UnderlyingTypes.Of(type);
        if (IsConcept(carried))
        {
            _partialSecrets.Add(carried.Name);
        }
    }

    /// <summary>
    /// Reports unsupported personal-data classification and legacy encryption attributes.
    /// </summary>
    /// <param name="diagnostics">Where the findings are reported.</param>
    public void Report(ScreenplayDiagnostics diagnostics)
    {
        foreach (var type in _legacyPiiEncryption.Order(StringComparer.Ordinal))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.LegacyPiiEncryption,
                $"Concept '{type}' combines [PII] with [Encrypted], which Chronicle rejects; use PIIAttribute.OperationalSecret instead",
                type);
        }

        foreach (var type in _unknownSpecialCategories.Order(StringComparer.Ordinal))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.UnknownSpecialCategory,
                $"Concept '{type}' has an unknown PIIAttribute.SpecialCategory value; the special category is not stated",
                type);
        }
    }

    static bool IsConcept(ITypeSymbol type) =>
        type.TypeKind == TypeKind.Enum || type.FindBase(WellKnownTypeNames.ConceptAs) is not null;

    static bool PersonalFlag(AttributeData? attribute, string name) =>
        attribute?.NamedArguments.Any(argument => argument.Key == name && argument.Value.Value is true) == true;

    static string? SpecialCategoryMemberOf(TypedConstant category) =>
        category.Type is INamedTypeSymbol enumeration && enumeration.FullMetadataName() == WellKnownTypeNames.SpecialCategory
            ? EnumConstants.MemberOf(enumeration, category.Value!) : null;

    /// <summary>
    /// Reads the non-default encryption scope from a concept's attribute.
    /// </summary>
    /// <param name="type">The concept type.</param>
    /// <returns>The scope, or null for Subject scope.</returns>
    static string? ScopeOf(ITypeSymbol type) => type.GetAttribute(WellKnownTypeNames.EncryptedAttribute)?.GetArgument(0) switch
    {
        1 => "namespace",
        2 => "global",
        _ => null
    };

    /// <summary>
    /// Gets the encryption scope a concept's declaration can state.
    /// </summary>
    /// <param name="concept">The concept.</param>
    /// <returns>The scope of a secret that is not personal data, otherwise <see langword="null"/>.</returns>
    /// <remarks>
    /// Only a secret states a scope, and a personal-data concept is always Subject scoped, so a scope recorded on any
    /// other concept says nothing the declaration could disagree about.
    /// </remarks>
    static string? StatedScopeOf(ConceptModel concept) => concept.IsSensitive && !concept.IsPii ? concept.EncryptionScope : null;

    string? SpecialCategoryOf(AttributeData? attribute, ITypeSymbol type)
    {
        var category = attribute?.NamedArguments.FirstOrDefault(argument => argument.Key == "SpecialCategory").Value ?? default;
        if (category.Value is null)
        {
            return null;
        }

        var member = SpecialCategoryMemberOf(category);
        var token = _specialCategories.GetValueOrDefault(member ?? string.Empty);
        if (member != "None" && token is null)
        {
            _unknownSpecialCategories.Add(type.ToDisplayString());
        }

        return token;
    }

    void RecordLegacyEncryption(ITypeSymbol type, AttributeData? personal)
    {
        if (personal is not null && type.HasAttribute(WellKnownTypeNames.EncryptedAttribute))
        {
            _legacyPiiEncryption.Add(type.ToDisplayString());
        }
    }
}
