// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Arc.ProxyGenerator.ModelBound;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.Scenarios.for_Commands.ModelBound;

public class when_generating_proxy_for_fluent_validated_command : Specification
{
    string? _generatedCode;
    CommandDescriptor? _descriptor;

    void Because()
    {
        var commandType = typeof(FluentValidatedCommand).GetTypeInfo();
        _descriptor = commandType.ToCommandDescriptor(
            targetPath: string.Empty,
            segmentsToSkip: 0,
            skipCommandNameInRoute: false,
            apiPrefix: "api");

        _generatedCode = Infrastructure.InMemoryProxyGenerator.GenerateCommand(_descriptor);
    }

    [Fact] void should_generate_code() => _generatedCode.ShouldNotBeNull();
    [Fact] void should_have_validation_rules() => _descriptor!.ValidationRules.ShouldNotBeEmpty();
    [Fact] void should_contain_validator_class() => _generatedCode.ShouldContain("class FluentValidatedCommandValidator");
    [Fact] void should_contain_name_validation_rule() => _generatedCode.ShouldContain("ruleFor(c => c.name)");
    [Fact] void should_contain_age_validation_rule() => _generatedCode.ShouldContain("ruleFor(c => c.age)");
    [Fact] void should_contain_email_validation_rule() => _generatedCode.ShouldContain("ruleFor(c => c.email)");
    [Fact] void should_contain_not_empty_rule() => _generatedCode.ShouldContain(".notEmpty()");
    [Fact] void should_contain_greater_than_or_equal_rule() => _generatedCode.ShouldContain(".greaterThanOrEqual(");
    [Fact] void should_contain_email_address_rule() => _generatedCode.ShouldContain(".emailAddress()");
}
