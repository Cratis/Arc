// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class and_the_document_it_printed_binds : given.a_printed_document
{
    void Because() => Generate("""
        domain Library
        module Library
          feature Authors
            slice StateChange Registration
              command RegisterAuthor
                name String
                returns name
        """);

    [Fact] void should_compile() => Verified.Compiles.ShouldBeTrue();
    [Fact] void should_bind_without_errors() => Verified.UnexpectedBindingErrors().ShouldBeEmpty();
    [Fact] void should_report_no_binding_defect() => Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotBind);
}
