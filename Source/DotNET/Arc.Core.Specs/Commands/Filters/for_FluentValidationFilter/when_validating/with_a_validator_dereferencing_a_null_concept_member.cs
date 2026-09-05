// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Net;
using Cratis.Arc.Http;
using Cratis.Arc.Validation;
using Cratis.Concepts;
using FluentValidation;

namespace Cratis.Arc.Commands.Filters.for_FluentValidationFilter.when_validating;

public class with_a_validator_dereferencing_a_null_concept_member : given.a_fluent_validation_filter
{
    CommandResult _result;

    void Establish()
    {
        var command = new CommandWithRequiredConcept(null!);
        _context = new CommandContext(_correlationId, typeof(CommandWithRequiredConcept), command, [], new());

        var validator = new CommandWithRequiredConceptValidator();
        _discoverableValidators.TryGet(typeof(CommandWithRequiredConcept), out Arg.Any<IValidator>())
            .Returns(x =>
            {
                x[1] = validator;
                return true;
            });
    }

    async Task Because() => _result = await _filter.OnExecution(_context);

    [Fact] void should_not_succeed() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_not_carry_any_exception_detail() => _result.HasExceptions.ShouldBeFalse();
    [Fact] void should_surface_a_single_validation_error() => _result.ValidationResults.Count().ShouldEqual(1);
    [Fact] void should_surface_the_generic_validation_message() => _result.ValidationResults.Single().Message.ShouldEqual(ValidatorInvoker.CouldNotValidateMessage);
    [Fact] void should_reach_the_client_marked_as_a_validator_failure() => _result.ValidationResults.Single().Reason.ShouldEqual(ValidationResultReason.ValidatorFailed);
    [Fact] void should_map_to_bad_request() => EndpointRouteHelper.GetStatusCode(_result.IsSuccess, _result.IsAuthorized, _result.IsValid).ShouldEqual(HttpStatusCode.BadRequest);

    record RequiredConcept(string Value) : ConceptAs<string>(Value);
    record CommandWithRequiredConcept(RequiredConcept Concept);

    class CommandWithRequiredConceptValidator : AbstractValidator<CommandWithRequiredConcept>
    {
        public CommandWithRequiredConceptValidator() => RuleFor(c => c.Concept.Value).NotEmpty();
    }
}
