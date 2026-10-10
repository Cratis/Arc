// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_required_mapping_of_an_unadmitted_generated_value : a_generated_document
{
    void Because() => Generate((Analyzed.SlicePath, IdentifierSources.With("""
        public class AuthorIdValidator : Cratis.Arc.Validation.ConceptValidator<AuthorId> { }
        [Command] public record RegisterAuthor(string Name)
        {
            public (AuthorId, AuthorRegistered) Handle()
            {
                AuthorId authorId = new(Guid.NewGuid());
                return (authorId, new(authorId, Name));
            }
        }
        """).Replace("public record AuthorRegistered(string Name);", "public record AuthorRegistered(AuthorId Copy, string Name);", StringComparison.Ordinal)));

    [Fact] void should_withhold_the_incomplete_production() => Result.Source.ShouldNotContain("produces AuthorRegistered");
    [Fact] void should_keep_the_handler_pointer() => Result.Source.ShouldContain("handler");
    [Fact] void should_report_the_required_mapping_loss() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Message.ShouldContain("required event property 'Copy'");
    [Fact] void should_classify_the_loss_as_information() => Result.Diagnostics.Single(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.UnmappableCommandProduction).Severity.ShouldEqual(ScreenplayDiagnosticSeverity.Information);
    [Fact] void should_round_trip() => RoundTrip.IsStable.ShouldBeTrue();
    [Fact] void should_not_fail_binding_verification() => Result.Diagnostics.Where(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.DocumentDidNotBind).ShouldBeEmpty();
    [Fact] void should_have_no_unexpected_binding_errors() => new ScreenplayVerifier().Verify(Result.Source).UnexpectedBindingErrors().ShouldBeEmpty();
}
