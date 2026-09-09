// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Collections.Immutable;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.Diagnostics;

namespace Cratis.Arc.Chronicle.CodeAnalysis.Specs.Testing;

public static class CodeFixVerifier<TAnalyzer, TCodeFix>
    where TAnalyzer : DiagnosticAnalyzer, new()
    where TCodeFix : CodeFixProvider, new()
{
    /// <summary>
    /// Verify the code fix output for the provided source.
    /// </summary>
    /// <param name="source">The C# source to analyze.</param>
    /// <param name="fixedSource">The expected fixed source.</param>
    /// <param name="expected">The expected diagnostic.</param>
    /// <returns>A task representing the verification.</returns>
    /// <exception cref="SnippetDoesNotCompile">Thrown when the changed project fails to compile.</exception>
    public static async Task VerifyCodeFixAsync(string source, string fixedSource, params ExpectedDiagnostic[] expected)
    {
        await AnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);

        var markedSource = SourceMarker.Parse(source);
        var project = TestProject.CreateProject(markedSource.Source);
        var document = project.Documents.First();
        var compilation = await project.GetCompilationAsync().ConfigureAwait(false);

        var analyzers = ImmutableArray.Create<DiagnosticAnalyzer>(new TAnalyzer());
        var compilationWithAnalyzers = compilation.WithAnalyzers(analyzers);
        var diagnostics = await compilationWithAnalyzers.GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
        var orderedDiagnostics = diagnostics.OrderBy(d => d.Location.SourceSpan.Start).ToArray();

        orderedDiagnostics.Length.ShouldEqual(expected.Length);

        var codeFixProvider = new TCodeFix();
        var diagnostic = orderedDiagnostics.FirstOrDefault(diagnostic => codeFixProvider.FixableDiagnosticIds.Contains(diagnostic.Id));
        diagnostic.ShouldNotBeNull();

        var actions = new List<CodeAction>();
        var context = new CodeFixContext(document, diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
        await codeFixProvider.RegisterCodeFixesAsync(context).ConfigureAwait(false);

        actions.ShouldNotBeEmpty();

        var operations = await actions[0].GetOperationsAsync(CancellationToken.None).ConfigureAwait(false);
        var applyChanges = operations.OfType<ApplyChangesOperation>().FirstOrDefault();
        applyChanges.ShouldNotBeNull();

        var newSolution = applyChanges.ChangedSolution;
        var newDocument = newSolution.GetDocument(document.Id);
        newDocument.ShouldNotBeNull();

        var newText = await newDocument.GetTextAsync().ConfigureAwait(false);
        NormalizeWhitespace(newText.ToString()).ShouldEqual(NormalizeWhitespace(fixedSource));

        var newCompilation = await newDocument.Project.GetCompilationAsync().ConfigureAwait(false);
        var errors = newCompilation!.GetDiagnostics().Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToArray();
        if (errors.Length > 0)
        {
            throw new SnippetDoesNotCompile(errors);
        }

        var remainingDiagnostics = await newCompilation.WithAnalyzers(analyzers).GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);

        // Other commands and other diagnostic IDs may legitimately remain after this single fix.
        var originalMatches = diagnostics.Count(candidate => candidate.Id == diagnostic.Id && candidate.GetMessage() == diagnostic.GetMessage());
        remainingDiagnostics.Count(candidate => candidate.Id == diagnostic.Id && candidate.GetMessage() == diagnostic.GetMessage())
            .ShouldBeLessThan(originalMatches);
    }

    /// <summary>
    /// Verifies the expected diagnostics and that no unsafe code action is offered.
    /// </summary>
    /// <param name="source">The source to analyze.</param>
    /// <param name="expected">The expected diagnostics.</param>
    /// <returns>A task representing the verification.</returns>
    public static async Task VerifyNoCodeFixAsync(string source, params ExpectedDiagnostic[] expected)
    {
        await AnalyzerVerifier<TAnalyzer>.VerifyAnalyzerAsync(source, expected).ConfigureAwait(false);
        var project = TestProject.CreateProject(SourceMarker.Parse(source).Source);
        var compilation = await project.GetCompilationAsync().ConfigureAwait(false);
        var diagnostics = await compilation!.WithAnalyzers([new TAnalyzer()])
            .GetAnalyzerDiagnosticsAsync().ConfigureAwait(false);
        var provider = new TCodeFix();
        var actions = new List<CodeAction>();
        foreach (var diagnostic in diagnostics.Where(diagnostic => provider.FixableDiagnosticIds.Contains(diagnostic.Id)))
        {
            var context = new CodeFixContext(project.Documents.First(), diagnostic, (action, _) => actions.Add(action), CancellationToken.None);
            await provider.RegisterCodeFixesAsync(context).ConfigureAwait(false);
        }

        actions.ShouldBeEmpty();
    }

    /// <summary>
    /// Normalize line whitespace for comparisons.
    /// </summary>
    /// <param name="source">The source string to normalize.</param>
    /// <returns>The normalized string.</returns>
    static string NormalizeWhitespace(string source)
    {
        return string.Join('\n', source.Replace("\r\n", "\n", StringComparison.Ordinal).Split('\n').Select(line => line.Trim()));
    }
}
