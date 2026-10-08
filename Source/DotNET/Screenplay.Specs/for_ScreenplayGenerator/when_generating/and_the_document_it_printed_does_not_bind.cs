// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class and_the_document_it_printed_does_not_bind : given.a_printed_document
{
    void Because() => Generate("""
        domain Library
        module Library
          feature Authors
            slice StateView Listing
              query AuthorById => Author optional
                by id String
              query OtherAuthorById => OtherAuthor optional
                by id String
        """);

    IEnumerable<ScreenplayDiagnostic> Reported => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind);

    [Fact] void should_compile() => Assert.True(Verified.Compiles, string.Join(Environment.NewLine, Verified.Errors.Select(diagnostic => diagnostic.Message)));
    [Fact] void should_have_binding_errors() => Verified.UnexpectedBindingErrors().Count.ShouldEqual(2);
    [Fact] void should_report_every_binding_error() => Reported.Count().ShouldEqual(2);
    [Fact] void should_report_warnings() => Reported.All(diagnostic => diagnostic.Severity == ScreenplayDiagnosticSeverity.Warning).ShouldBeTrue();
    [Fact] void should_preserve_the_binder_messages() => Reported.Zip(Verified.UnexpectedBindingErrors()).All(pair => pair.First.Message.Contains(pair.Second.Message, StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_preserve_the_binder_locations() => Reported.Zip(Verified.UnexpectedBindingErrors()).All(pair => pair.First.Message.Contains($"on line {pair.Second.Location.Line}, column {pair.Second.Location.Column}", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_name_the_generator_defect() => Reported.All(diagnostic => diagnostic.Message.Contains("the generator being wrong", StringComparison.Ordinal)).ShouldBeTrue();
    [Fact] void should_still_return_the_document() => Result.Source.ShouldEqual(Verified.Source);
    [Fact] void should_not_report_a_compilation_failure() => Result.Diagnostics.Select(diagnostic => diagnostic.Code).ShouldNotContain(ScreenplayDiagnosticCodes.DocumentDidNotCompile);

    [Fact]
    void should_not_hide_other_binding_errors_in_authoring_mode()
    {
        Generate(Verified.Source, authoringOnlyConstructs: true);
        Reported.Count().ShouldEqual(2);
    }
}
