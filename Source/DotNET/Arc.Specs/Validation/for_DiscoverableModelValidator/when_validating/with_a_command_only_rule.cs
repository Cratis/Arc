// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.when_validating;

public class with_a_command_only_rule : given.a_discoverable_model_validator
{
    IEnumerable<ModelValidationResult> _commandResult;
    IEnumerable<ModelValidationResult> _queryResult;

    void Establish() => _modelValidator = new DiscoverableModelValidator(new CommandOnlyValidator());

    void Because()
    {
        _actionContext.HttpContext.Request.Method = "POST";
        _commandResult = _modelValidator.Validate(_validationContext).ToArray();
        _actionContext.HttpContext.Request.Method = "GET";
        _queryResult = _modelValidator.Validate(_validationContext).ToArray();
    }

    [Fact] void should_apply_the_rule_to_a_command() => _commandResult.Single().MemberName.ShouldEqual(nameof(TestModel.Value));
    [Fact] void should_not_apply_the_rule_to_a_query() => _queryResult.ShouldBeEmpty();

    class CommandOnlyValidator : BaseValidator<TestModel>
    {
        public CommandOnlyValidator() => WhenCommand(() => RuleFor(model => model.Value).NotEmpty());
    }
}
