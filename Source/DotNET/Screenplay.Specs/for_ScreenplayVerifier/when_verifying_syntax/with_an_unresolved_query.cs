// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayVerifier.when_verifying_syntax;

public class with_an_unresolved_query : Specification
{
    const string Source = """
        module Library
          feature Authors
            slice StateView Listing
              query AuthorById => Author optional
                by id String
        """;

    ScreenplayVerification _result;

    void Because() => _result = new ScreenplayVerifier().VerifySyntax(Source);

    [Fact] void should_accept_the_syntax() => _result.Compiles.ShouldBeTrue();
    [Fact] void should_not_bind_the_incomplete_document() => _result.BindingDiagnostics.ShouldBeEmpty();
    [Fact] void should_have_a_binding_defect_if_bound_in_isolation() => new ScreenplayVerifier().Verify(Source).UnexpectedBindingErrors().ShouldNotBeEmpty();
}
