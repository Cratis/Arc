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
/// The compiler's visitor hook is the only way in: the document is built from what the compiler accepted,
/// never from the text itself, and nothing outside the process is read to produce it.
/// </remarks>
public class EventModelParser(IScreenplayCompiler compiler) : IEventModelParser
{
    /// <summary>
    /// Initializes a new instance of the <see cref="EventModelParser"/> class.
    /// </summary>
    public EventModelParser()
        : this(new ScreenplayCompiler())
    {
    }

    /// <inheritdoc/>
    public EventModelView Parse(string documentId, string name, string source)
    {
        var visitor = new EventModelSyntaxVisitor(documentId, name);
        var compilation = compiler.Compile(source, visitor);

        // Warnings the compiler reported and warnings the mapping found are the same thing to a reader: the
        // document was produced, and here is what it does not say. They are served on one channel.
        var compiled = compilation.Diagnostics ?? [];
        var errors = compiled.Where(diagnostic => diagnostic.Severity == DiagnosticSeverity.Error).ToList();
        var warnings = compiled
            .Where(diagnostic => diagnostic.Severity != DiagnosticSeverity.Error)
            .Concat(compilation.Success ? visitor.Warnings.All : [])
            .ToList();

        return new EventModelView(
            compilation.Success ? compilation.Value : null,
            [.. errors.Select(EventModelDiagnostic.From)],
            [.. warnings.Select(EventModelDiagnostic.From)],
            compilation.Success && compilation.Value is not null);
    }
}
