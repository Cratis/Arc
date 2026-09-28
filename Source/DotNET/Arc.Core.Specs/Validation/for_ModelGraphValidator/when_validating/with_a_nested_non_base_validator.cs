// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;

namespace Cratis.Arc.Validation.for_ModelGraphValidator.when_validating;

public class with_a_nested_non_base_validator : given.a_model_graph_validator
{
    record Child(string Name);
    record Root(Child Child);

    class ChildValidator : AbstractValidator<Child>
    {
        public ChildValidator() => RuleFor(child => child.Name).NotEmpty().WithMessage("Name is required");
    }

    IEnumerable<ValidationResult> _results;

    void Establish() => WithValidatorFor(typeof(Child), new ChildValidator());

    async Task Because() => _results = await _validator.Validate(new ModelGraphValidationRequest(new Root(new Child(string.Empty))));

    [Fact] void should_run_the_nested_validator() => _results.Single().Message.ShouldEqual("Name is required");
    [Fact] void should_attribute_the_failure_to_the_nested_member() => _results.Single().Members.ShouldContain("child.name");
}
