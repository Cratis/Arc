// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Commands;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ControllerBased;

public class when_executing_controller_data_annotations_validated_command_with_validation_errors_produced_by_the_client : given.a_scenario_web_application
{
    CommandResult<object>? _result;

    void Establish()
    {
        LoadControllerCommandProxy<ControllerCommandsController>(nameof(ControllerCommandsController.ExecuteDataAnnotationsValidated), "/tmp/data-annotations-command.ts");
        ControllerCommandsController.DataAnnotationsValidatedCallCount = 0;
    }

    async Task Because()
    {
        var executionResult = await Bridge.ExecuteCommandViaProxyAsync<object>(new ControllerDataAnnotationsValidatedCommand
            {
                Name = "ab", // Too short (min 3)
                Age = 15, // Too young (min 18)
                Email = "invalid-email", // Invalid email format
                Phone = "abc", // Invalid phone
                Website = "not-a-url" // Invalid URL
            },
            "ExecuteDataAnnotationsValidated");
        _result = executionResult.Result;
    }

    [Fact] void should_not_be_successful() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_not_be_valid() => _result.IsValid.ShouldBeFalse();
    [Fact] void should_have_validation_results() => _result.ValidationResults.ShouldNotBeEmpty();
    [Fact] void should_have_name_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("name"));
    [Fact] void should_have_name_validation_message() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("name") && v.Message == ControllerDataAnnotationsValidatedCommand.NameLengthMessage);
    [Fact] void should_have_age_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("age"));
    [Fact] void should_have_age_validation_message() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("age") && v.Message == ControllerDataAnnotationsValidatedCommand.AgeRangeMessage);
    [Fact] void should_have_email_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("email"));
    [Fact] void should_have_email_validation_message() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("email") && v.Message == ControllerDataAnnotationsValidatedCommand.EmailFormatMessage);
    [Fact] void should_have_phone_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("phone"));
    [Fact] void should_have_phone_validation_message() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("phone") && v.Message == ControllerDataAnnotationsValidatedCommand.PhoneFormatMessage);
    [Fact] void should_have_website_validation_error() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("website"));
    [Fact] void should_have_website_validation_message() => _result.ValidationResults.ShouldContain(v => v.Members.Contains("website") && v.Message == ControllerDataAnnotationsValidatedCommand.UrlFormatMessage);
    [Fact] void should_not_roundtrip_to_server() => ControllerCommandsController.DataAnnotationsValidatedCallCount.ShouldEqual(0);
}
