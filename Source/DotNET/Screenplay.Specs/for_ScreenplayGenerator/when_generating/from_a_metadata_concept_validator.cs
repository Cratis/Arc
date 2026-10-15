// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Verification;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_metadata_concept_validator : a_generation_strategy
{
    [Fact]
    void should_not_treat_an_unreadable_inherited_validator_as_empty()
    {
        const string PackageSource = """
            using FluentValidation;
            namespace External;
            public class BaseValidator<T> : AbstractValidator<T>
            {
                public BaseValidator() { RuleFor(value => value).Must(value => false); }
            }
            """;
        var package = Analyzed.Package("ValidationLibrary", PackageSource);
        var source = Source("GenerateValue.NanoId()") + "public class AuthorIdValidator : External.BaseValidator<AuthorId> { }";
        var compilation = Analyzed.Compile([package], [(Analyzed.SlicePath, source)]);
        var result = new ScreenplayGenerator().Generate(compilation, new());
        result.Source.ShouldNotContain("generated");
        result.Source.ShouldNotContain("generate nanoid");
        result.Diagnostics.Any(diagnostic => diagnostic.Code == ScreenplayDiagnosticCodes.GenerationNotRecoverable).ShouldBeTrue();
        new ScreenplayVerifier().Verify(result.Source).UnexpectedBindingErrors().ShouldBeEmpty();
    }
}
