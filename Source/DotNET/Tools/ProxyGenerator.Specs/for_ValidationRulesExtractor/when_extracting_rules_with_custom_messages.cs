// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.ProxyGenerator.Templates;
using Cratis.Arc.Validation;
using FluentValidation;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class TypeWithCustomMessages
{
    public string Email { get; set; } = string.Empty;
    public int Age { get; set; }
}

public class TypeWithCustomMessagesValidator : BaseValidator<TypeWithCustomMessages>
{
    public TypeWithCustomMessagesValidator()
    {
        RuleFor(x => x.Email).NotEmpty().WithMessage("Email is required");
        RuleFor(x => x.Age).GreaterThanOrEqualTo(18).WithMessage("Must be at least 18 years old");
    }
}

public class when_extracting_rules_with_custom_messages : Specification
{
    IEnumerable<PropertyValidationDescriptor> _rules = [];

    void Establish() => _rules = ValidationRulesExtractor.ExtractValidationRules(typeof(TypeWithCustomMessagesValidator).Assembly, typeof(TypeWithCustomMessages));

    [Fact] void should_extract_two_property_rules() => _rules.Count().ShouldEqual(2);
    [Fact] void should_have_rule_for_email() => _rules.ShouldContain(r => r.PropertyName == "email");
    [Fact] void should_have_rule_for_age() => _rules.ShouldContain(r => r.PropertyName == "age");
    [Fact] void should_have_custom_message_for_email() => _rules.First(r => r.PropertyName == "email").Rules.First().ErrorMessage.ShouldEqual("Email is required");
    [Fact] void should_have_custom_message_for_age() => _rules.First(r => r.PropertyName == "age").Rules.First().ErrorMessage.ShouldEqual("Must be at least 18 years old");
}
