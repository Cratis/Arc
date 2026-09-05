// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

/// <summary>
/// Stands in for the printer, writing text that was prepared rather than rendered, so that a document the Screenplay
/// compiler rejects can be put in front of the generator.
/// </summary>
/// <remarks>
/// Every way of emitting a rejected document that anyone has found has since been fixed, which is exactly why the
/// check exists - it has to hold for the one nobody has found yet. Replacing the printed text is the smallest seam
/// that produces one: the analysis, the emitter, the syntax tree the emitter builds and the compiler reading the
/// text back are all the real ones, and nothing on the path a host takes is substituted. The text is the shape the
/// defect that shipped took - a name written where the grammar holds a quoted description - moved to a body that
/// reads <c>description</c> as nothing but the directive. The command body it was written in no longer rejects it:
/// the language now reads a directive-shaped property line as the property, so the exact text that shipped is
/// something it holds rather than something it turns away.
/// </remarks>
public class a_document_the_language_rejects : Specification
{
    /// <summary>
    /// The text the stand-in printer writes in place of the document.
    /// </summary>
    protected const string Rejected = """
        domain Library
        module Library
          feature Authors
            slice StateView Registration
              query AuthorById => Author
                description RequestDescription
        """;

    /// <summary>
    /// The line of the text the Screenplay compiler rejects.
    /// </summary>
    protected const int RejectedLine = 6;

    protected IScreenplayEmitter _emitter;

    void Establish()
    {
        var printer = Substitute.For<IScreenplayPrinter>();
        printer.Print(Arg.Any<ApplicationSyntax>()).Returns(Rejected);
        _emitter = new ScreenplayEmitter(printer, new ScreenplayNaming());
    }
}
