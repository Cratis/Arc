// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Emission.Validation;
using Cratis.Arc.Screenplay.Model;
using Cratis.Screenplay.Diagnostics;
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
                concept.IsPii ? [new ConceptAttributeSyntax(PersonallyIdentifiableInformation, SourceLocation.Start)] : [],
                values,
                SourceLocation.Start,
                [.. validations.Build(concept.Validations, concept.Name, impliedSubject: true)]);
        }

        return [.. declared.Values.OrderBy(_ => _.Name, StringComparer.Ordinal)];
    }

    /// <summary>
    /// Gets the values of an enumeration in the lower camel case form the grammar requires.
    /// </summary>
    /// <param name="concept">The concept to read the values of.</param>
    /// <returns>The values, empty when the concept is not an enumeration.</returns>
    /// <remarks>
    /// A value is written on a line of its own, so a value named after a word the concept body reads as a directive
    /// is swallowed by that directive rather than declared, and is left out instead.
    /// </remarks>
    List<string> ToValues(ConceptModel concept) =>
        concept.Primitive == ScreenplayPrimitive.Enum
            ?
            [
                .. concept.EnumValues
                    .Where(_ => names.Allows(_, ReservedWords.InConcept, concept.Name, concept.Name))
                    .Select(naming.ToPropertyName)
                    .Distinct(StringComparer.Ordinal)
            ]
            : [];
}
