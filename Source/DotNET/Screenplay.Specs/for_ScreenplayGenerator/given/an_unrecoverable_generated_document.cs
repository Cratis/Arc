// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Semantics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class an_unrecoverable_generated_document : a_generated_document
{
    protected List<(ScreenplayOptions Options, ScreenplayGenerationResult Result)> Documents = [];

    protected static string WithUnavailable(string command) => GeneratedValueSources.With(command).Replace(
        "public static AuthorId New() => new(Guid.NewGuid());",
        "public static AuthorId New() => new(Guid.Empty);",
        StringComparison.Ordinal);

    protected void GenerateAcrossOptions(string source, params (string Path, string Text)[] additional)
    {
        foreach (var authoring in new[] { false, true })
        {
            foreach (var cap in new SemanticVersion?[] { null, SemanticVersion.V6, SemanticVersion.V15 })
            {
                var options = new ScreenplayOptions { AuthoringOnlyConstructs = authoring, MaximumExecutableModelVersion = cap };
                Generate(options, [(Analyzed.SlicePath, source), .. additional]);
                RoundTrip.IsStable.ShouldBeTrue();
                Documents.Add((options, Result));
            }
        }
    }

    protected void AssertFaithfulDocuments()
    {
        Documents.Count.ShouldEqual(6);
        foreach (var (options, result) in Documents)
        {
            result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind || diagnostic.Code == ScreenplayDiagnosticCodes.SourceDidNotCompile).ShouldBeEmpty();
            var verification = new ScreenplayVerifier().Verify(result.Source);
            verification.Compiles.ShouldBeTrue();
            verification.BindingDiagnostics.Where(diagnostic => diagnostic.Severity == Cratis.Screenplay.Diagnostics.DiagnosticSeverity.Error &&
                !ExpectedBindingDiagnostics.IsExpected(diagnostic, options.AuthoringOnlyConstructs)).Select(diagnostic => $"{diagnostic.Code}: {diagnostic.Message}").ShouldBeEmpty();
            result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldEqual(1);
            result.Source.ShouldNotContain("authorId AuthorId generated");
        }
    }
}
