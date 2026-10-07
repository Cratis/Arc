// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class and_the_document_has_authoring_binding_errors : given.a_printed_document
{
    void Because() => Generate("""
        domain Library
        module Library
          feature Authors
            slice StateChange Registration
              command RegisterAuthor
                name String
                handler
                  file Authors/Registration/RegisterAuthor.cs
        """,
        authoringOnlyConstructs: true);

    [Fact] void should_compile() => Assert.True(Verified.Compiles, string.Join(Environment.NewLine, Verified.Errors.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_have_only_authoring_admission_errors() => Verified.UnexpectedBindingErrors(false).Select(diagnostic => diagnostic.Code).Distinct().ShouldEqual(["PLAY0268"]);
    [Fact] void should_accept_authoring_admission_errors_when_enabled() => Verified.UnexpectedBindingErrors(true).ShouldBeEmpty();
    [Fact] void should_report_no_binding_defect() => Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotBind);

    [Fact]
    void should_report_authoring_admission_errors_in_default_mode()
    {
        Generate(Verified.Source);
        Result.Diagnostics.Count(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldEqual(1);
    }
}
