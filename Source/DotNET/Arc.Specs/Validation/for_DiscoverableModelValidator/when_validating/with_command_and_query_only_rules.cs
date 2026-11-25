// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using FluentValidation;
using Microsoft.AspNetCore.Mvc.ModelBinding.Validation;

namespace Cratis.Arc.Validation.for_DiscoverableModelValidator.when_validating;

public class with_command_and_query_only_rules : given.a_discoverable_model_validator
{
    Dictionary<string, ModelValidationResult[]> _results;

    void Establish() => _modelValidator = new DiscoverableModelValidator(new ConditionalValidator());

    void Because()
    {
        _results = [];
        foreach (var method in new[] { "POST", "PUT", "PATCH", "DELETE", "post", "put", "patch", "delete", "GET", "HEAD", "OPTIONS" })
        {
            _actionContext.HttpContext.Request.Method = method;
            _results[method] = _modelValidator.Validate(_validationContext).ToArray();
        }
    }

    [Fact] void should_apply_only_command_rules_for_post() => _results["POST"].Single().Message.ShouldEqual("Command rule");
    [Fact] void should_apply_only_command_rules_for_put() => _results["PUT"].Single().Message.ShouldEqual("Command rule");
    [Fact] void should_apply_only_command_rules_for_patch() => _results["PATCH"].Single().Message.ShouldEqual("Command rule");
    [Fact] void should_apply_only_command_rules_for_delete() => _results["DELETE"].Single().Message.ShouldEqual("Command rule");
    [Fact] void should_apply_only_command_rules_for_lowercase_post() => _results["post"].Single().Message.ShouldEqual("Command rule");
    [Fact] void should_apply_only_command_rules_for_lowercase_put() => _results["put"].Single().Message.ShouldEqual("Command rule");
    [Fact] void should_apply_only_command_rules_for_lowercase_patch() => _results["patch"].Single().Message.ShouldEqual("Command rule");
    [Fact] void should_apply_only_command_rules_for_lowercase_delete() => _results["delete"].Single().Message.ShouldEqual("Command rule");
    [Fact] void should_apply_only_query_rules_for_get() => _results["GET"].Single().Message.ShouldEqual("Query rule");
    [Fact] void should_apply_only_query_rules_for_head() => _results["HEAD"].Single().Message.ShouldEqual("Query rule");
    [Fact] void should_apply_only_query_rules_for_options() => _results["OPTIONS"].Single().Message.ShouldEqual("Query rule");

    class ConditionalValidator : BaseValidator<TestModel>
    {
        public ConditionalValidator()
        {
            WhenCommand(() => RuleFor(model => model.Value).NotEmpty().WithMessage("Command rule"));
            WhenQuery(() => RuleFor(model => model.Value).NotEmpty().WithMessage("Query rule"));
        }
    }
}
