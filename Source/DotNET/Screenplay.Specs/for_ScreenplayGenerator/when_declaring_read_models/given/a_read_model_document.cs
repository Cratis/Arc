// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_declaring_read_models.given;

/// <summary>
/// Generates a document from one slice and holds it to the compiler, the printer and the executable semantic binder.
/// </summary>
/// <remarks>
/// <c>PLAY0273</c> is what the binder says when a projection or a query names a read model the document never
/// declares, which every projection and every keyed query said before read models were declared at all. It is asked
/// of every shape here, whether or not the first ESM vertical admits the rest of the shape.
/// </remarks>
public class a_read_model_document : a_generated_document
{
    /// <summary>
    /// The code the binder reports a reference it cannot resolve under.
    /// </summary>
    protected const string Unresolved = "PLAY0273";

    /// <summary>
    /// Gets the binding errors reporting an unresolved reference.
    /// </summary>
    protected IEnumerable<string> UnresolvedReferences =>
        Bound.Diagnostics.Where(_ => _.Code == Unresolved).Select(_ => _.Message);

    /// <summary>
    /// Gets the warnings and errors compiling the generated document reported.
    /// </summary>
    protected IEnumerable<string> CompilationFindings =>
        RoundTrip.Diagnostics.Where(_ => _.Severity is DiagnosticSeverity.Warning or DiagnosticSeverity.Error).Select(_ => $"{_.Code}: {_.Message}");

    /// <summary>
    /// Gets the message of every read model the generator reported leaving out.
    /// </summary>
    protected IEnumerable<string> LeftOut =>
        Result.Diagnostics.Where(_ => _.Code == ScreenplayDiagnosticCodes.UndeclarableReadModel).Select(_ => _.Message);

    /// <summary>
    /// Gets every warning and error the generator reported.
    /// </summary>
    protected IEnumerable<string> GenerationWarnings =>
        Result.Diagnostics.Where(_ => _.Severity != ScreenplayDiagnosticSeverity.Information).Select(_ => $"{_.Code}: {_.Message}");

    /// <summary>
    /// Gets how many lines of the document are a line, indentation ignored.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <returns>The count.</returns>
    protected int Count(string line) =>
        Result.Source.Split('\n').Count(_ => _.Trim() == line);

    /// <summary>
    /// Gets whether the document carries a line, indentation ignored.
    /// </summary>
    /// <param name="line">The line.</param>
    /// <returns>True when it does.</returns>
    protected bool Says(string line) =>
        Result.Source.Split('\n').Any(_ => _.Trim() == line);

    /// <summary>
    /// Generates the document of a single slice.
    /// </summary>
    /// <param name="source">The source of the slice.</param>
    protected void GenerateSlice(string source) => Generate((Analyzed.SlicePath, source));
}
