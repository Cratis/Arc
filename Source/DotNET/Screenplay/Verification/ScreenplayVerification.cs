// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay.Diagnostics;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.Verification;

/// <summary>
/// Represents everything reading a printed Screenplay document back produced.
/// </summary>
/// <param name="Source">The printed <c>.play</c> text that was compiled.</param>
/// <param name="Application">The document the text compiled to, null when it did not compile at all.</param>
/// <param name="Diagnostics">Everything the Screenplay compiler reported about the text.</param>
public record ScreenplayVerification(
    string Source,
    ApplicationSyntax? Application,
    IReadOnlyList<Diagnostic> Diagnostics)
{
    /// <summary>
    /// Gets the diagnostics from compiling and binding the generated document to an executable semantic model.
    /// </summary>
    public IReadOnlyList<Diagnostic> BindingDiagnostics { get; init; } = [];

    /// <summary>
    /// Gets everything the Screenplay compiler reported as an error.
    /// </summary>
    public IReadOnlyList<Diagnostic> Errors => [.. Diagnostics.Where(_ => _.Severity == DiagnosticSeverity.Error)];

    /// <summary>
    /// Gets a value indicating whether the printed text compiles.
    /// </summary>
    public bool Compiles => Errors.Count == 0;

    /// <summary>
    /// Gets unexpected semantic binding errors in either generation mode.
    /// </summary>
    /// <param name="authoringOnlyConstructs">Whether additional authoring-only constructs were requested.</param>
    /// <returns>Every binding error except the known admission and legacy read-consistency messages.</returns>
    public IReadOnlyList<Diagnostic> UnexpectedBindingErrors(bool authoringOnlyConstructs = false) =>
        [.. BindingDiagnostics.Where(diagnostic =>
            diagnostic.Severity == DiagnosticSeverity.Error &&
            (!ExpectedBindingDiagnostics.IsExpected(diagnostic, authoringOnlyConstructs) ||
             (!authoringOnlyConstructs && diagnostic.Code == "PLAY0271" && NamesRoutedCommand(diagnostic))))];

    bool NamesRoutedCommand(Diagnostic diagnostic) => Application?.Modules.SelectMany(module => module.Features).SelectMany(Commands)
        .Any(command => command.Stream is not null && diagnostic.Message == $"Command '{command.Name}' concurrency metadata keeps its legacy meaning and cannot bind to ESM v1.") == true;

    static IEnumerable<CommandSyntax> Commands(FeatureSyntax feature) => feature.Slices.SelectMany(slice => slice.Commands)
        .Concat(feature.Features.SelectMany(Commands));
}
