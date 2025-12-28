// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.ComponentModel.DataAnnotations;
using System.Reflection;
using Cratis.Arc.ProxyGenerator.Templates;

namespace Cratis.Arc.ProxyGenerator.for_ValidationRulesExtractor;

public class when_extracting_data_annotations_from_parameter : Specification
{
    static class TestClass
    {
        public static void TestMethod(
            [Required]
            [EmailAddress]
            string email,
            [Required]
            [StringLength(50, MinimumLength = 3)]
            string name,
            [Range(0, 150)]
            int age,
            [Url]
            string website)
        {
        }
    }

    IReadOnlyList<ValidationRuleDescriptor> _emailRules;
    IReadOnlyList<ValidationRuleDescriptor> _nameRules;
    IReadOnlyList<ValidationRuleDescriptor> _ageRules;
    IReadOnlyList<ValidationRuleDescriptor> _websiteRules;

    void Establish()
    {
        var method = typeof(TestClass).GetMethod(nameof(TestClass.TestMethod), BindingFlags.Public | BindingFlags.Static)!;
        var parameters = method.GetParameters();

        _emailRules = ValidationRulesExtractor.ExtractDataAnnotationsFromParameter(parameters[0]);
        _nameRules = ValidationRulesExtractor.ExtractDataAnnotationsFromParameter(parameters[1]);
        _ageRules = ValidationRulesExtractor.ExtractDataAnnotationsFromParameter(parameters[2]);
        _websiteRules = ValidationRulesExtractor.ExtractDataAnnotationsFromParameter(parameters[3]);
    }

    [Fact] void should_extract_required_for_email() => _emailRules.ShouldContain(_ => _.RuleName == "notEmpty");
    [Fact] void should_extract_email_address_for_email() => _emailRules.ShouldContain(_ => _.RuleName == "emailAddress");
    [Fact] void should_have_two_rules_for_email() => _emailRules.Count.ShouldEqual(2);

    [Fact] void should_extract_required_for_name() => _nameRules.ShouldContain(_ => _.RuleName == "notEmpty");
    [Fact] void should_extract_length_for_name() => _nameRules.ShouldContain(_ => _.RuleName == "length");
    [Fact] void should_have_two_rules_for_name() => _nameRules.Count.ShouldEqual(2);

    [Fact] void should_extract_greater_than_or_equal_for_age() => _ageRules.ShouldContain(_ => _.RuleName == "greaterThanOrEqual");
    [Fact] void should_extract_less_than_or_equal_for_age() => _ageRules.ShouldContain(_ => _.RuleName == "lessThanOrEqual");
    [Fact] void should_have_two_rules_for_age() => _ageRules.Count.ShouldEqual(2);

    [Fact] void should_extract_url_for_website() => _websiteRules.ShouldContain(_ => _.RuleName == "url");
    [Fact] void should_have_one_rule_for_website() => _websiteRules.Count.ShouldEqual(1);
}
