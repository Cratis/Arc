// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Globalization;
using System.Numerics;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Emission.Validation;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Concepts;

/// <summary>
/// Builds the document level <c>concept</c> declarations.
/// </summary>
/// <param name="naming">The <see cref="IScreenplayNaming"/> used for name conversion.</param>
/// <param name="validations">The <see cref="ValidationSyntaxBuilder"/> used for the validate blocks.</param>
/// <param name="diagnostics">The <see cref="ScreenplayDiagnostics"/> anything unmappable is reported to.</param>
/// <param name="names">The <see cref="NameAvailability"/> deciding which value names the body can carry.</param>
public class ConceptSyntaxBuilder(
    IScreenplayNaming naming,
    ValidationSyntaxBuilder validations,
    ScreenplayDiagnostics diagnostics,
    NameAvailability names)
{
    /// <summary>
    /// The Screenplay attribute marking a concept as personally identifiable information.
    /// </summary>
    public const string PersonallyIdentifiableInformation = ConceptAttributeSyntax.Pii;

    /// <summary>
    /// Gets whether syntax-only compliance details may be emitted.
    /// </summary>
    public bool AuthoringOnlyConstructs { get; init; }

    /// <summary>
    /// Gets the maximum executable model version, or null for the latest admitted version.
    /// </summary>
    public SemanticVersion? MaximumExecutableModelVersion { get; init; }

    /// <summary>
    /// Builds every concept the document declares.
    /// </summary>
    /// <param name="concepts">The concepts to build.</param>
    /// <returns>The concept declarations, ordered by name.</returns>
    public IEnumerable<ConceptSyntax> Build(IEnumerable<ConceptModel> concepts)
    {
        var declared = new Dictionary<string, ConceptSyntax>(StringComparer.Ordinal);

        foreach (var concept in concepts)
        {
            var name = naming.ToDeclarationName(concept.Name);
            if (name.Length <= 1 || declared.ContainsKey(name))
            {
                continue;
            }

            var values = ToValues(concept);
            if (concept.Primitive == ScreenplayPrimitive.Enum && values.Count == 0)
            {
                // An enumeration with no values has an empty body, which does not compile.
                diagnostics.Warning(
                    ScreenplayDiagnosticCodes.EnumWithoutValues,
                    $"The concept '{concept.Name}' is an enumeration with no values and was left out",
                    concept.Name);
                continue;
            }

            declared[name] = new(
                name,
                ScreenplayPrimitiveTypes.GetName(concept.Primitive),
                [.. Attributes(concept)],
                values.ConvertAll(value => value.Name),
                SourceLocation.Start,
                [.. validations.Build(ValidationContract(concept), concept.Name, impliedSubject: true)])
            {
                ValueNumbers = NumbersOf(concept, values),
                Generation = ConceptGenerationSyntax.Build(concept, MaximumExecutableModelVersion, diagnostics)
            };
        }

        return [.. declared.Values.OrderBy(_ => _.Name, StringComparer.Ordinal)];
    }

    IEnumerable<ValidationRuleModel> ValidationContract(ConceptModel concept)
    {
        var contract = ConceptValidationContract.For(concept, MaximumExecutableModelVersion);
        if (contract.VersionFailure is { } reason)
        {
            diagnostics.Information(ScreenplayDiagnosticCodes.GenerationNotRecoverable, $"Concept '{concept.Name}': {reason}", concept.Name);
        }

        return contract.Rules;
    }

    /// <summary>
    /// Gets the compliance annotations the concept's value carries.
    /// </summary>
    /// <param name="concept">The concept to annotate.</param>
    /// <returns>The annotations, in their printed order.</returns>
    IEnumerable<ConceptAttributeSyntax> Attributes(ConceptModel concept)
    {
        if (concept.IsPii)
        {
            ReportPiiPromotion(concept);
            yield return new(PersonallyIdentifiableInformation, SourceLocation.Start);
        }

        if (concept.IsSensitive)
        {
            var scope = concept.IsPii ? null : concept.EncryptionScope;
            if (scope is not null && !AuthoringOnlyConstructs)
            {
                diagnostics.Information(
                    ScreenplayDiagnosticCodes.EncryptionScopeNotRepresentable,
                    $"Concept '{concept.Name}' has encryption scope '{scope}', which is not stated; bare secret implies Subject scope. Enable ScreenplayOptions.AuthoringOnlyConstructs to state the scope",
                    concept.Name);
            }

            yield return new(ConceptAttributeSyntax.Sensitive, SourceLocation.Start)
            {
                Scope = AuthoringOnlyConstructs ? scope : null
            };
        }
    }

    /// <summary>
    /// Reports widened classification only for a concept that retains its pii annotation.
    /// </summary>
    /// <param name="concept">The emitted concept.</param>
    void ReportPiiPromotion(ConceptModel concept)
    {
        if (concept.UnmarkedPiiUses.Count == 0)
        {
            return;
        }

        diagnostics.Information(
            ScreenplayDiagnosticCodes.ConceptPiiPromotion,
            $"Concept '{concept.Name}' is promoted to pii by member [PII] markings; the document also classifies these unmarked uses as pii: {string.Join(", ", concept.UnmarkedPiiUses)}",
            concept.Name);
    }

    /// <summary>
    /// Gets the values of an enumeration in the lower camel case form the grammar requires.
    /// </summary>
    /// <param name="concept">The concept to read the values of.</param>
    /// <returns>The values, empty when the concept is not an enumeration.</returns>
    /// <exception cref="EnumNumbersDoNotMatchValues">The concept states member numbers that are not one per member.</exception>
    /// <remarks>
    /// A value is written on a line of its own, so a value named after a word the concept body reads as a directive
    /// is swallowed by that directive rather than declared, and is left out instead.
    /// </remarks>
    List<(string Name, BigInteger? Number)> ToValues(ConceptModel concept)
    {
        if (concept.Primitive != ScreenplayPrimitive.Enum)
        {
            return [];
        }

        var numbers = concept.EnumNumbers.ToList();
        var memberCount = concept.EnumValues.Count();
        if (numbers.Count != 0 && numbers.Count != memberCount)
        {
            throw new EnumNumbersDoNotMatchValues(concept.Name, memberCount, numbers.Count);
        }

        var members = concept.EnumValues.Select((name, index) => (Name: name, Number: numbers.Count == 0 ? (BigInteger?)null : numbers[index]))
            .Where(member => names.Allows(member.Name, ReservedWords.InConcept, concept.Name, concept.Name))
            .Select(member => (Name: naming.ToPropertyName(member.Name), member.Number)).ToList();
        foreach (var collision in members.GroupBy(member => member.Name, StringComparer.Ordinal).Where(group => group.Count() > 1))
        {
            diagnostics.Warning(
                ScreenplayDiagnosticCodes.EnumMemberNumbersNotRepresentable,
                $"Enum '{concept.Name}' members share the emitted name '{collision.Key}'; only the first member is declared, and the other member identities are not preserved",
                concept.Name);
        }

        return members.DistinctBy(member => member.Name, StringComparer.Ordinal).ToList();
    }

    /// <summary>
    /// Pins every recovered number when the target can preserve it, otherwise reports positional fallback.
    /// </summary>
    /// <param name="concept">The enumeration being emitted.</param>
    /// <param name="values">The retained name and number pairs.</param>
    /// <returns>All member numbers, or null for positional fallback.</returns>
    List<int>? NumbersOf(ConceptModel concept, IReadOnlyList<(string Name, BigInteger? Number)> values)
    {
        if (values.All(value => value.Number is null))
        {
            return null;
        }

        var outsideRange = values.Any(value => value.Number < int.MinValue || value.Number > int.MaxValue);
        var positional = values.Select((value, index) => value.Number == index).All(equal => equal);
        var belowVersion = !positional && MaximumExecutableModelVersion is { } cap && !cap.IsAtLeast(SemanticVersion.V12);
        if (outsideRange || belowVersion)
        {
            ReportPositionalFallback(concept, values, outsideRange, belowVersion);
            return null;
        }

        return values.Select(value => (int)value.Number!.Value).ToList();
    }

    /// <summary>
    /// Reports that an enumeration's numbers fall back to positions in the document.
    /// </summary>
    /// <param name="concept">The enumeration being emitted.</param>
    /// <param name="values">The retained name and number pairs.</param>
    /// <param name="outsideRange">Whether a number is outside the int32 range.</param>
    /// <param name="belowVersion">Whether the version cap is below ESM v12.</param>
    void ReportPositionalFallback(ConceptModel concept, IReadOnlyList<(string Name, BigInteger? Number)> values, bool outsideRange, bool belowVersion)
    {
        var reason = string.Join(" / ", new[]
        {
            belowVersion ? $"needs ESM v12, cap v{MaximumExecutableModelVersion}" : null,
            outsideRange ? "a number is outside int32" : null
        }.OfType<string>());
        var members = string.Join(", ", values.Select(value => $"{value.Name}={value.Number!.Value.ToString(CultureInfo.InvariantCulture)}"));
        diagnostics.Warning(
            ScreenplayDiagnosticCodes.EnumMemberNumbersNotRepresentable,
            $"Enum '{concept.Name}' members {members} cannot be numbered ({reason}); the document numbers them by position and stored identity is not preserved",
            concept.Name);
    }
}
