// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Analysis;
using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.Emission.Naming;
using Cratis.Arc.Screenplay.Verification;
using Cratis.Screenplay.Printing;
using Cratis.Screenplay.Syntax;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;

public class a_printed_document : Specification
{
    protected ScreenplayGenerationResult Result;
    protected ScreenplayVerification Verified;

    protected void Generate(string source, bool authoringOnlyConstructs = false)
    {
        var printer = Substitute.For<IScreenplayPrinter>();
        printer.Print(Arg.Any<ApplicationSyntax>()).Returns(source);
        var generator = new ScreenplayGenerator(new ApplicationModelAnalyzer(), new ScreenplayEmitter(printer, new ScreenplayNaming()));
        var compilation = Analyzed.Compile((Analyzed.SlicePath, "namespace Library.Authors.Registration;"));
        Result = generator.Generate(compilation, new ScreenplayOptions { AuthoringOnlyConstructs = authoringOnlyConstructs });
        Verified = new ScreenplayVerifier().Verify(source);
    }
}
