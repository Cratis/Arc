// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Emission.Types;
using Cratis.Arc.Screenplay.Emission.Validation;
using Cratis.Arc.Screenplay.Model;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Semantics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Emission.Concepts;

/// <summary>
/// Shares the version, recovery and language-owned rule decision across generation consumers.
/// </summary>
internal static class GenerationAdmission
{
    /// <summary>
    /// Gets why a concept's generation cannot be emitted without weakening its validator.
    /// </summary>
    /// <param name="concept">The complete recovered concept.</param>
    /// <param name="cap">The requested executable version cap.</param>
    /// <returns>The refusal, including a language witness when refuted, or null.</returns>
    internal static string? Failure(ConceptModel concept, SemanticVersion? cap)
    {
        if (concept.Primitive == ScreenplayPrimitive.Uuid && concept.Validations.Any())
        {
            return "UUID concept validation has no admissible non-opaque rule in Screenplay 4.164; the original validator is retained as a source attachment";
        }
        if (!concept.ValidationRecoveryComplete || concept.Validations.Any(rule => rule.Kind == Model.ValidationRuleKind.Rule))
        {
            return "its default concept validation is unreadable, conditional or opaque; the complete executable validation contract could not be recovered";
        }
        if (concept.Generation is not null && !IsImplicit(concept) && (concept.IsPii || concept.IsSensitive))
        {
            return "generation with compliance markers is not executable yet (Cratis/Screenplay#616 phase 4)";
        }
        var required = concept.Validations.Any() ? SemanticVersion.V20 : SemanticVersion.V15;
        if ((concept.Validations.Any() || !IsImplicit(concept)) && cap is { } version && !version.IsAtLeast(required))
        {
            return $"generation requires ESM v{required}, but ScreenplayOptions.MaximumExecutableModelVersion is capped at ESM v{cap}; validation rules are retained";
        }
        if (!concept.Validations.Any())
        {
            return null;
        }

        return CheckRules(concept);
    }

    /// <summary>
    /// Checks whether the strategy is the unmodified UUID default.
    /// </summary>
    /// <param name="concept">The concept owning the strategy.</param>
    /// <returns>Whether no explicit strategy needs serialization.</returns>
    internal static bool IsImplicit(ConceptModel concept) => concept.Primitive == ScreenplayPrimitive.Uuid && (concept.Generation is null || concept.Generation.Kind == "uuid");

    /// <summary>
    /// Builds strategy syntax without making an admission decision.
    /// </summary>
    /// <param name="generation">The proven built-in strategy.</param>
    /// <returns>The syntax passed to the language compiler.</returns>
    internal static GenerationSyntax Syntax(GenerationModel generation) => new(generation.Kind, SourceLocation.Start)
    {
        Prefix = generation.Prefix,
        Case = generation.Case,
        Size = generation.Size,
        Alphabet = generation.Alphabet,
        Count = generation.Count,
        Encoding = generation.Encoding
    };

    /// <summary>
    /// Builds an isolated generation contract for the language's binder and reference runner.
    /// </summary>
    /// <param name="concept">The emitted concept, including all rules.</param>
    /// <returns>The source of a command returning one generated value.</returns>
    internal static string ProbeSource(ConceptSyntax concept) =>
        new ScreenplayPrinter().Print(new ApplicationSyntax([], [concept], [], [], SourceLocation.Start)) +
        $"\nmodule GenerationProbe\n  feature Generation\n    slice StateChange Allocate\n      command Allocate\n        value {concept.Name} generated\n        returns value\n";

    static string? CheckRules(ConceptModel concept)
    {
        var naming = new ScreenplayNaming();
        var diagnostics = new ScreenplayDiagnostics();
        var name = naming.ToDeclarationName(concept.Name);
        var validations = new ValidationSyntaxBuilder(naming, diagnostics).Build(concept.Validations, name, impliedSubject: true).ToList();
        if (diagnostics.All.Count > 0)
        {
            return "its concept validation rules cannot all be emitted faithfully";
        }
        var declaration = new ConceptSyntax(name, ScreenplayPrimitiveTypes.GetName(concept.Primitive), [], [], SourceLocation.Start, validations)
        {
            Generation = concept.Generation is { } generation && !IsImplicit(concept) ? Syntax(generation) : null
        };
        var source = ProbeSource(declaration);
        var verification = new ScreenplayVerifier().Verify(source);
        var errors = verification.Errors.Concat(verification.BindingDiagnostics.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error)).ToList();

        return errors.Count > 0 ? string.Join("; ", errors.Select(error => $"{error.Code}: {error.Message}")) : null;
    }
}
