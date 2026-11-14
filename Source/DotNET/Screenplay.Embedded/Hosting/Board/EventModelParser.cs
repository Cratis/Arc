// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Screenplay;
using Cratis.Screenplay.Diagnostics;

namespace Cratis.Arc.Screenplay.Embedded.Hosting.Board;

/// <summary>
/// Turns embedded Screenplay source into the document the board draws, by running it through the Screenplay
/// compiler and having the compiler hand the compiled application to <see cref="EventModelSyntaxVisitor"/>.
/// </summary>
/// <param name="compiler">The Screenplay compiler to compile source with.</param>
/// <remarks>
/// Authored documents use the compiler's visitor hook. Generated documents may also draw compiled syntax
/// with incomplete projection coverage; nothing outside the process is read to produce either document.
/// </remarks>
public class EventModelParser(IScreenplayCompiler compiler) : IEventModelParser
{
    const string ToleratedCoverageCode = "PLAY0284";

    /// <summary>
    /// Initializes a new instance of the <see cref="EventModelParser"/> class.
    /// </summary>
    public EventModelParser()
        : this(new ScreenplayCompiler())
    {
    }

    /// <inheritdoc/>
    public EventModelView Parse(string documentId, string name, string source) =>
        Parse(documentId, name, source, EventModelOrigin.Embedded);

    /// <inheritdoc/>
    public EventModelView Parse(string documentId, string name, string source, EventModelOrigin origin) =>
        origin == EventModelOrigin.Generated
            ? ParseGenerated(documentId, name, source)
            : ParseEmbedded(documentId, name, source);

    static bool IsToleratedCoverage(Diagnostic diagnostic) =>
        diagnostic.Severity == DiagnosticSeverity.Error && string.Equals(diagnostic.Code, ToleratedCoverageCode, StringComparison.Ordinal);

    EventModelView ParseGenerated(string documentId, string name, string source)
    {
        var compilation = compiler.Compile(source);
        var compiled = compilation.Diagnostics ?? [];
        var errors = compiled.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        if (compilation.Value is null || errors.Exists(diagnostic => !IsToleratedCoverage(diagnostic)))
        {
            return new EventModelView(
                null,
                [.. errors.Select(EventModelDiagnostic.From)],
                [.. compiled.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning).Select(EventModelDiagnostic.From)],
                false);
        }

        var visitor = new EventModelSyntaxVisitor(documentId, name);
        var model = visitor.Visit(compilation.Value);
        var warnings = compiled
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning || IsToleratedCoverage(diagnostic))
            .Concat(visitor.Warnings.All);

        return new EventModelView(model, [], [.. warnings.Select(EventModelDiagnostic.From)], true);
    }

    EventModelView ParseEmbedded(string documentId, string name, string source)
    {
        var visitor = new EventModelSyntaxVisitor(documentId, name);
        var compilation = compiler.Compile(source, visitor);

        // Warnings the compiler reported and warnings the mapping found are the same thing to a reader: the
        // document was produced, and here is what it does not say. They are served on one channel.
        var compiled = compilation.Diagnostics ?? [];
        var errors = compiled.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        var warnings = compiled
            .Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Warning)
            .Concat(compilation.Success ? visitor.Warnings.All : [])
            .ToList();

        return new EventModelView(
            compilation.Success ? compilation.Value : null,
            [.. errors.Select(EventModelDiagnostic.From)],
            [.. warnings.Select(EventModelDiagnostic.From)],
            compilation.Success && compilation.Value is not null);
    }
}
