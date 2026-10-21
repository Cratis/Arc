// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Screenplay.Emission;
using Cratis.Arc.Screenplay.for_ScreenplayGenerator.given;
using Cratis.Arc.Screenplay.Model;

namespace Cratis.Arc.Screenplay.for_ScreenplayGenerator.when_generating;

public class from_a_nongenerated_uuid_validator : a_generated_document
{
    [Theory]
    [InlineData(false, "RuleFor(value => value.Value).NotEmpty();")]
    [InlineData(true, "RuleFor(value => value.Value).NotEmpty();")]
    [InlineData(false, "RuleFor(value => value.Value).Must(value => value != Guid.Empty);")]
    [InlineData(true, "RuleFor(value => value.Value).Must(value => value != Guid.Empty);")]
    public void should_preserve_the_pre_generation_validation_output(bool authoring, string rule)
    {
        var source = "using FluentValidation;\n" + IdentifierSources.With($$"""
            public class AuthorIdValidator : Cratis.Arc.Validation.ConceptValidator<AuthorId>
            {
                public AuthorIdValidator() { {{rule}} }
            }
            [Command] public record RegisterAuthor(AuthorId Id, string Name)
            {
                public AuthorRegistered Handle() => new(Name);
            }
            """);
        var options = new ScreenplayOptions { AuthoringOnlyConstructs = authoring };
        Generate(options, (Analyzed.SlicePath, source));
        var legacy = Result.Model with
        {
            Concepts = Result.Model.Concepts.Select(concept => new ConceptModel(concept.Name, concept.Primitive, concept.IsPii, concept.EnumValues, concept.Validations)).ToList()
        };
        Result.Source.ShouldEqual(new ScreenplayEmitter().Emit(legacy, options).Source);
        Result.Source.ShouldNotContain("rule AuthorIdValidator");
        Result.Model.Concepts.Single().ValidatorSources.ShouldBeEmpty();
        Result.Model.Concepts.Single().ValidationRecoveryComplete.ShouldBeTrue();
        RoundTrip.IsStable.ShouldBeTrue();
    }
}
